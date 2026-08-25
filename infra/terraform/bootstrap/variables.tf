variable "aws_region" {
  description = "Region where the remote state is kept."
  type        = string
  default     = "us-east-1"
}

variable "state_bucket_name" {
  description = "Name of the bucket that holds the Terraform remote state."
  type        = string

  validation {
    condition     = can(regex("^[a-z0-9][a-z0-9.-]{1,61}[a-z0-9]$", var.state_bucket_name))
    error_message = "Bucket name must be valid for S3: lowercase letters, digits, dot and hyphen."
  }
}

variable "expected_account_id" {
  description = "Account the state bucket must live in. Guards against applying with the wrong credentials."
  type        = string

  validation {
    condition     = can(regex("^[0-9]{12}$", var.expected_account_id))
    error_message = "Account id must be exactly twelve digits."
  }
}
