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
  }
}

resource "azurerm_container_app_custom_domain" "root" {
  name                                     = azurerm_dns_zone.sample-domain.name
  container_app_id                         = azurerm_container_app.ingress.id
  container_app_environment_certificate_id = azurerm_container_app_environment_certificate.container-app-certificate.id
  certificate_binding_type                 = "SniEnable"
}

resource "azurerm_container_app_custom_domain" "api" {
  name                                     = "api.${azurerm_dns_zone.sample-domain.name}"
  container_app_id                         = azurerm_container_app.ingress.id
  container_app_environment_certificate_id = azurerm_container_app_environment_certificate.container-app-certificate.id
  certificate_binding_type                 = "SniEnable"
}

resource "azurerm_container_app_custom_domain" "admin" {
  name                                     = "admin.${azurerm_dns_zone.sample-domain.name}"
  container_app_id                         = azurerm_container_app.ingress.id
  container_app_environment_certificate_id = azurerm_container_app_environment_certificate.container-app-certificate.id
  certificate_binding_type                 = "SniEnable"
}
