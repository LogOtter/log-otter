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
