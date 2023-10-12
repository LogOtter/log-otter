terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.43.0"
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
  geo_location {
    location          = azurerm_resource_group.rg.location
    failover_priority = 0
  }
  consistency_policy {
    consistency_level = "Session"
  }
}

resource "azurerm_service_plan" "service-plan" {
  name                = "log-otter-samples-service-plan"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  os_type             = "Linux"
  sku_name            = "F1"
}

resource "azurerm_linux_web_app" "customer-api" {
  name                = "log-otter-customer-api"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_service_plan.service-plan.location
  service_plan_id     = azurerm_service_plan.service-plan.id
  https_only          = true

  app_settings = {
    APPLICATIONINSIGHTS__CONNECTIONSTRING = azurerm_application_insights.app-insights.connection_string
    ASPNETCORE_ENVIRONMENT                = "Development"
    COSMOSDB__CONNECTIONSTRING            = azurerm_cosmosdb_account.db.primary_sql_connection_string
    DOCKER_REGISTRY_SERVER_URL            = var.docker_registry_url
    DOCKER_REGISTRY_SERVER_USERNAME       = var.docker_registry_username
    DOCKER_REGISTRY_SERVER_PASSWORD       = var.docker_registry_password
    WEBSITES_ENABLE_APP_SERVICE_STORAGE   = "false"
    WEBSITE_WARMUP_PATH                   = "/health"
  }

  site_config {
    always_on     = false
    http2_enabled = true
    application_stack {
      docker_image     = "ghcr.io/logotter/customer-api"
      docker_image_tag = var.container_tag_name
    }
  }

  logs {
    application_logs {
      file_system_level = "Information"
    }
    http_logs {
      file_system {
        retention_in_days = 30
        retention_in_mb   = 35
      }
    }
  }
}

resource "azurerm_linux_web_app" "customer-worker" {
  name                = "log-otter-customer-worker"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_service_plan.service-plan.location
  service_plan_id     = azurerm_service_plan.service-plan.id
  https_only          = true

  app_settings = {
    APPLICATIONINSIGHTS__CONNECTIONSTRING = azurerm_application_insights.app-insights.connection_string
    DOCKER_REGISTRY_SERVER_URL            = var.docker_registry_url
    DOCKER_REGISTRY_SERVER_USERNAME       = var.docker_registry_username
    DOCKER_REGISTRY_SERVER_PASSWORD       = var.docker_registry_password
    WEBSITES_ENABLE_APP_SERVICE_STORAGE   = "false"
    WEBSITE_WARMUP_PATH                   = "/health"
  }

  site_config {
    always_on     = false
    http2_enabled = true
    application_stack {
      docker_image     = "ghcr.io/logotter/customer-worker"
      docker_image_tag = var.container_tag_name
    }
  }

  logs {
    application_logs {
      file_system_level = "Information"
    }
    http_logs {
      file_system {
        retention_in_days = 30
        retention_in_mb   = 35
      }
    }
  }
}

resource "azurerm_linux_web_app" "hub" {
  name                = "log-otter-hub"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_service_plan.service-plan.location
  service_plan_id     = azurerm_service_plan.service-plan.id
  https_only          = true

  app_settings = {
    ASPNETCORE_ENVIRONMENT              = "Development"
    DOCKER_REGISTRY_SERVER_URL          = var.docker_registry_url
    DOCKER_REGISTRY_SERVER_USERNAME     = var.docker_registry_username
    DOCKER_REGISTRY_SERVER_PASSWORD     = var.docker_registry_password
    Hub__Services__0__Name              = "CustomerApi"
    Hub__Services__0__Url               = "https://${azurerm_linux_web_app.customer-api.default_hostname}/logotter/api"
    WEBSITES_ENABLE_APP_SERVICE_STORAGE = "false"
    #WEBSITE_WARMUP_PATH                 = "/health"
  }

  site_config {
    always_on     = false
    http2_enabled = true
    application_stack {
      docker_image     = "ghcr.io/logotter/hub"
      docker_image_tag = var.container_tag_name
    }
  }

  logs {
    application_logs {
      file_system_level = "Information"
    }
    http_logs {
      file_system {
        retention_in_days = 30
        retention_in_mb   = 35
      }
    }
  }
}
