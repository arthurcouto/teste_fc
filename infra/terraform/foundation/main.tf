provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project     = "cashflow"
      Environment = var.environment
      ManagedBy   = "terraform"
    }
  }
}

data "aws_caller_identity" "current" {}

check "account_matches_expectation" {
  assert {
    condition     = data.aws_caller_identity.current.account_id == var.expected_account_id
    error_message = "Refusing to apply: credentials belong to account ${data.aws_caller_identity.current.account_id}, expected ${var.expected_account_id}."
  }
}

locals {
  name_prefix = "cashflow-${var.environment}"

  databases = {
    ledger        = "Ledger database"
    consolidation = "Consolidation database"
  }

  database_endpoints = {
    for key, cluster in aws_dsql_cluster.database :
    key => "${cluster.identifier}.dsql.${var.aws_region}.on.aws"
  }
}
