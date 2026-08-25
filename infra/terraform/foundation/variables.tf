variable "aws_region" {
  description = "Region where the environment is provisioned."
  type        = string
  default     = "us-east-1"
}

variable "environment" {
  description = "Environment name. Composes the prefix of every resource."
  type        = string

  validation {
    condition     = contains(["development", "staging", "production"], var.environment)
    error_message = "Valid environments: development, staging, production."
  }
}

variable "expected_account_id" {
  description = "Account this environment must be provisioned in. Guards against applying with the wrong credentials."
  type        = string

  validation {
    condition     = can(regex("^[0-9]{12}$", var.expected_account_id))
    error_message = "Account id must be exactly twelve digits."
  }
}
