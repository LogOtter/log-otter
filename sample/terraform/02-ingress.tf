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

resource "azurerm_dns_a_record" "admin" {
  name                = "admin"
  zone_name           = azurerm_dns_zone.sample-domain.name
  resource_group_name = azurerm_resource_group.rg.name
  ttl                 = 3600
  records             = [azurerm_container_app_environment.container-app-environment.static_ip_address]
}

resource "azurerm_dns_txt_record" "admin" {
  name                = "asuid.admin"
  zone_name           = azurerm_dns_zone.sample-domain.name
  resource_group_name = azurerm_resource_group.rg.name
  ttl                 = 3600
  record {
    value = azurerm_container_app.ingress.custom_domain_verification_id
  }
}

resource "acme_certificate" "root" {
  account_key_pem          = acme_registration.reg.account_key_pem
  common_name              = trimsuffix(azurerm_dns_a_record.root.fqdn, ".")
  certificate_p12_password = random_password.certificate-password.result

  dns_challenge {
    provider = "azuredns"

    config = {
      AZURE_RESOURCE_GROUP = azurerm_resource_group.rg.name
      AZURE_ZONE_NAME      = azurerm_dns_zone.sample-domain.name
    }
  }
}

resource "acme_certificate" "api" {
  account_key_pem          = acme_registration.reg.account_key_pem
  common_name              = trimsuffix(azurerm_dns_a_record.api.fqdn, ".")
  certificate_p12_password = random_password.certificate-password.result

  dns_challenge {
    provider = "azuredns"

    config = {
      AZURE_RESOURCE_GROUP = azurerm_resource_group.rg.name
      AZURE_ZONE_NAME      = azurerm_dns_zone.sample-domain.name
    }
  }
}

resource "acme_certificate" "admin" {
  account_key_pem          = acme_registration.reg.account_key_pem
  common_name              = trimsuffix(azurerm_dns_a_record.admin.fqdn, ".")
  certificate_p12_password = random_password.certificate-password.result

  dns_challenge {
    provider = "azuredns"

    config = {
      AZURE_RESOURCE_GROUP = azurerm_resource_group.rg.name
      AZURE_ZONE_NAME      = azurerm_dns_zone.sample-domain.name
    }
  }
}

resource "azurerm_container_app_environment_certificate" "root" {
  name                         = "root-certificate"
  container_app_environment_id = azurerm_container_app_environment.container-app-environment.id
  certificate_blob_base64      = acme_certificate.root.certificate_p12
  certificate_password         = acme_certificate.root.certificate_p12_password
}

resource "azurerm_container_app_environment_certificate" "api" {
  name                         = "api-certificate"
  container_app_environment_id = azurerm_container_app_environment.container-app-environment.id
  certificate_blob_base64      = acme_certificate.api.certificate_p12
  certificate_password         = acme_certificate.api.certificate_p12_password
}

resource "azurerm_container_app_environment_certificate" "admin" {
  name                         = "admin-certificate"
  container_app_environment_id = azurerm_container_app_environment.container-app-environment.id
  certificate_blob_base64      = acme_certificate.admin.certificate_p12
  certificate_password         = acme_certificate.admin.certificate_p12_password
}

resource "azurerm_container_app_custom_domain" "root" {
  name                                     = trimsuffix(azurerm_dns_a_record.root.fqdn, ".")
  container_app_id                         = azurerm_container_app.ingress.id
  container_app_environment_certificate_id = azurerm_container_app_environment_certificate.root.id
  certificate_binding_type                 = "SniEnabled"
}

resource "azurerm_container_app_custom_domain" "api" {
  name                                     = trimsuffix(azurerm_dns_a_record.api.fqdn, ".")
  container_app_id                         = azurerm_container_app.ingress.id
  container_app_environment_certificate_id = azurerm_container_app_environment_certificate.api.id
  certificate_binding_type                 = "SniEnabled"
}

resource "azurerm_container_app_custom_domain" "admin" {
  name                                     = trimsuffix(azurerm_dns_a_record.admin.fqdn, ".")
  container_app_id                         = azurerm_container_app.ingress.id
  container_app_environment_certificate_id = azurerm_container_app_environment_certificate.admin.id
  certificate_binding_type                 = "SniEnabled"
}
