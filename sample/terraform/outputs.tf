output "customer_api_webhook" {
  description = "Webhook to trigger container update"
  value       = "https://${azurerm_linux_web_app.customer-api.site_credential[0].name}:${azurerm_linux_web_app.customer-api.site_credential[0].password}@${lower(azurerm_linux_web_app.customer-api.name)}.scm.azurewebsites.net/api/registry/webhook"
  sensitive   = true
}

output "customer_worker_webhook" {
  description = "Webhook to trigger container update"
  value       = "https://${azurerm_linux_web_app.customer-worker.site_credential[0].name}:${azurerm_linux_web_app.customer-worker.site_credential[0].password}@${lower(azurerm_linux_web_app.customer-worker.name)}.scm.azurewebsites.net/api/registry/webhook"
  sensitive   = true
}
