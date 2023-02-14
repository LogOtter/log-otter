variable "azurerm_tenant_id" {
  type        = string
  description = "The tenant ID for the Terraform service principal"
}

variable "azurerm_subscription_id" {
  type        = string
  description = "The subscription ID for the Terraform service principal"
}

variable "azurerm_client_id" {
  type        = string
  description = "The client ID for the Terraform service principal"
}

variable "azurerm_client_secret" {
  type        = string
  description = "The client secret for the Terraform service principal"
  sensitive   = true
}

variable "docker_registry_url" {
  type        = string
  description = "The URL for accessing the Docker container registry"
}

variable "docker_registry_username" {
  type        = string
  description = "The username for accessing the Docker container registry"
}

variable "docker_registry_password" {
  type        = string
  description = "The personal access token for the Docker container registry"
  sensitive   = true
}

variable "container_tag_name" {
  type        = string
  description = "The tag name of the container to use"
  default     = "main"
}
