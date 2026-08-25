variable "enable_persistence" {
  description = "Creates the databases of both services. Kept apart from the other toggles because it holds state and survives the removal of the runtime layer."
  type        = bool
  default     = false
}
