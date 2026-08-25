resource "aws_dsql_cluster" "database" {
  for_each = var.enable_persistence ? local.databases : {}

  deletion_protection_enabled = var.dsql_deletion_protection
  kms_encryption_key          = coalesce(var.dsql_kms_key_arn, "AWS_OWNED_KMS_KEY")

  tags = {
    Name    = "${local.name_prefix}-${each.key}"
    Service = each.key
  }

  lifecycle {
    prevent_destroy = true
  }
}

resource "aws_ssm_parameter" "database_endpoint" {
  for_each = aws_dsql_cluster.database

  name        = "/cashflow/${var.environment}/${each.key}/database-endpoint"
  description = "Endpoint of the ${local.databases[each.key]}. Not a secret: access is authenticated by workload identity."
  type        = "String"
  value       = local.database_endpoints[each.key]
}
