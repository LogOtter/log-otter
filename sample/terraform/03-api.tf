resource "azurerm_dns_a_record" "api" {
  name                = "api"
  zone_name           = azurerm_dns_zone.sample-domain.name
  resource_group_name = azurerm_resource_group.rg.name
  ttl                 = 3600
  records             = [azurerm_container_app_environment.container-app-environment.static_ip_address]
}

resource "azurerm_dns_txt_record" "api" {
  name                = "asuid.api"
  zone_name           = azurerm_dns_zone.sample-domain.name
  resource_group_name = azurerm_resource_group.rg.name
  ttl                 = 3600
  record {
    value = azurerm_container_app.ingress.custom_domain_verification_id
  }
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
    target_port = 8080
    traffic_weight {
      percentage      = 100
      latest_revision = true
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

  registry {
    server               = "ghcr.io"
    username             = "AButler"
    password_secret_name = "registry-secret"
  }

  secret {
    name  = "registry-secret"
    value = var.docker_registry_password
  }
}
