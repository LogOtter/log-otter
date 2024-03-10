terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.94.0"
    }
  }

  backend "azurerm" {
    resource_group_name  = "Terraform"
    storage_account_name = "logotterterraformstorage"
    container_name       = "tfstate"
    key                  = "terraform.tfstate"
  }

  required_version = ">= 1.1.0"
}

provider "azurerm" {
  tenant_id       = var.azurerm_tenant_id
  subscription_id = var.azurerm_subscription_id
  client_id       = var.azurerm_client_id
  client_secret   = var.azurerm_client_secret

  features {
  }
}

resource "azurerm_resource_group" "rg" {
  name     = "LogOtter"
  location = "uksouth"
}

resource "azurerm_log_analytics_workspace" "log-analytics-workspace" {
  name                = "log-otter-log-analytics-workspace"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

resource "azurerm_application_insights" "app-insights" {
  name                = "log-otter-app-insights"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  workspace_id        = azurerm_log_analytics_workspace.log-analytics-workspace.id
  application_type    = "web"
}

resource "azurerm_cosmosdb_account" "db" {
  name                = "log-otter-samples"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  offer_type          = "Standard"
  enable_free_tier    = true

  capacity {
    total_throughput_limit = 1000
  }

  geo_location {
    location          = azurerm_resource_group.rg.location
    failover_priority = 0
  }

  consistency_policy {
    consistency_level = "Session"
  }
}

resource "azurerm_container_app_environment" "container-app-environment" {
  name                       = "log-otter-sample-container-app-environment"
  location                   = azurerm_resource_group.rg.location
  resource_group_name        = azurerm_resource_group.rg.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.log-analytics-workspace.id
}

resource "azurerm_container_app" "customer-api" {
  name                         = "customer-api"
  container_app_environment_id = azurerm_container_app_environment.container-app-environment.id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode                = "Single"

  template {
    container {
      name   = "customer-api"
      image  = "ghcr.io/logotter/customer-api:${var.container_tag_name}"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "APPLICATIONINSIGHTS__CONNECTIONSTRING"
        value = azurerm_application_insights.app-insights.connection_string
      }

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Development"
      }

      env {
        name  = "COSMOSDB__CONNECTIONSTRING"
        value = azurerm_cosmosdb_account.db.primary_sql_connection_string
      }

      liveness_probe {
        port      = 8080
        transport = "HTTP"
        path      = "/health"
      }

      readiness_probe {
        port      = 8080
        transport = "HTTP"
        path      = "/health"
      }

      startup_probe {
        port      = 8080
        transport = "HTTP"
        path      = "/health"
      }
    }
  }

  ingress {
    target_port = 8080
    traffic_weight {
      percentage = 100
    }
  }
}

resource "azurerm_container_app" "customer-worker" {
  name                         = "customer-worker"
  container_app_environment_id = azurerm_container_app_environment.container-app-environment.id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode                = "Single"

  template {
    container {
      name   = "customer-worker"
      image  = "ghcr.io/logotter/customer-worker:${var.container_tag_name}"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "APPLICATIONINSIGHTS__CONNECTIONSTRING"
        value = azurerm_application_insights.app-insights.connection_string
      }

      liveness_probe {
        port      = 80
        transport = "HTTP"
        path      = "/health"
      }

      readiness_probe {
        port      = 80
        transport = "HTTP"
        path      = "/health"
      }

      startup_probe {
        port      = 80
        transport = "HTTP"
        path      = "/health"
      }
    }
  }
}

resource "azurerm_container_app" "hub" {
  name                         = "hub"
  container_app_environment_id = azurerm_container_app_environment.container-app-environment.id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode                = "Single"

  template {
    container {
      name   = "hub"
      image  = "ghcr.io/logotter/hub:${var.container_tag_name}"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "APPLICATIONINSIGHTS__CONNECTIONSTRING"
        value = azurerm_application_insights.app-insights.connection_string
      }

      env {
        name  = "Hub__Services__0__Name"
        value = "CustomerApi"
      }

      env {
        name  = "Hub__Services__0__Url"
        value = "http://customer-api/logotter/api"
      }

      liveness_probe {
        port      = 8080
        transport = "HTTP"
        path      = "/health"
      }

      readiness_probe {
        port      = 8080
        transport = "HTTP"
        path      = "/health"
      }

      startup_probe {
        port      = 8080
        transport = "HTTP"
        path      = "/health"
      }
    }
  }

  ingress {
    target_port = 8080
    traffic_weight {
      percentage = 100
    }
  }
}

resource "azurerm_container_app" "ingress" {
  name                         = "ingress"
  container_app_environment_id = azurerm_container_app_environment.container-app-environment.id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode                = "Single"

  template {
    container {
      name   = "hub"
      image  = "ghcr.io/logotter/ingress:${var.container_tag_name}"
      cpu    = 0.25
      memory = "0.5Gi"
    }
  }

  ingress {
    target_port      = 80
    external_enabled = true
    traffic_weight {
      percentage = 100
    }
  }
}
