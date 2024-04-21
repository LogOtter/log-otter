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
  free_tier_enabled   = true

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
