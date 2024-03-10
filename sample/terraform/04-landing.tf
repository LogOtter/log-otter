resource "azurerm_dns_cname_record" "www-cname" {
  name                = "@"
  zone_name           = azurerm_dns_zone.sample-domain.name
  resource_group_name = azurerm_resource_group.rg.name
  ttl                 = 3600
  record              = azurerm_container_app.ingress.latest_revision_fqdn
}

resource "azurerm_dns_txt_record" "www-cname" {
  name                = "asuid.www"
  zone_name           = azurerm_dns_zone.sample-domain.name
  resource_group_name = azurerm_resource_group.rg.name
  ttl                 = 3600
  record {
    value = azurerm_container_app.ingress.custom_domain_verification_id
  }
}
