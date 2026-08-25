variable "dsql_deletion_protection" {
  description = "Prevents accidental removal of the databases. Turned off in disposable environments, where recreating is an expected operation."
  type        = bool
  default     = true
}

variable "dsql_kms_key_arn" {
  description = "Customer managed key for the databases. Null falls back to the AWS owned key, acceptable only in disposable environments."
  type        = string
  default     = null
}
