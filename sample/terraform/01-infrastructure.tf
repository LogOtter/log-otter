resource "azurerm_resource_group" "rg" {
  name     = "LogOtter"
  location = "uksouth"
}

resource "azurerm_dns_zone" "sample-domain" {
  name                = "sample.logotter.co.uk"
  resource_group_name = azurerm_resource_group.rg.name
}

resource "tls_private_key" "private_key" {
  algorithm = "RSA"
}

resource "random_password" "certificate-password" {
  length  = 32
  special = true
}

resource "acme_registration" "reg" {
  account_key_pem = tls_private_key.private_key.private_key_pem
  email_address   = "acme@logotter.co.uk"
}

resource "acme_certificate" "certificate" {
  account_key_pem           = acme_registration.reg.account_key_pem
  common_name               = azurerm_dns_zone.sample-domain.name
  subject_alternative_names = ["*.${azurerm_dns_zone.sample-domain.name}"]
  certificate_p12_password  = random_password.certificate-password.result

  dns_challenge {
    provider = "azuredns"
  }
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

resource "azurerm_container_app_environment_certificate" "container-app-certificate" {
  name                         = "*.${azurerm_dns_zone.sample-domain.name}"
  container_app_environment_id = azurerm_container_app_environment.container-app-environment.id
  certificate_blob_base64      = acme_certificate.certificate.certificate_p12
  certificate_password         = acme_certificate.certificate.certificate_p12_password
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

  registry {
    server               = "ghcr.io"
    username             = "AButler"
    password_secret_name = "registry-secret"
  }

  secret {
    name  = "registry-secret"
    value = var.docker_registry_password
  }

  ingress {
    target_port      = 80
    external_enabled = true
    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
    custom_domain {
      certificate_id           = azurerm_container_app_environment_certificate.container-app-certificate.id
      name                     = "sample.logotter.co.uk"
      certificate_binding_type = "SniEnabled"
    }
    custom_domain {
      certificate_id           = azurerm_container_app_environment_certificate.container-app-certificate.id
      name                     = "api.sample.logotter.co.uk"
      certificate_binding_type = "SniEnabled"
    }
    custom_domain {
      certificate_id           = azurerm_container_app_environment_certificate.container-app-certificate.id
      name                     = "admin.sample.logotter.co.uk"
      certificate_binding_type = "SniEnabled"
    }
  }
}
