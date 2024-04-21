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
