output "persistence" {
  description = "Access contract for the databases of both services."

  value = {
    enabled = var.enable_persistence

    databases = {
      for key, cluster in aws_dsql_cluster.database : key => {
        identifier    = cluster.identifier
        arn           = cluster.arn
        endpoint      = local.database_endpoints[key]
        database_name = "postgres"
        ssm_parameter = aws_ssm_parameter.database_endpoint[key].name
      }
    }
  }
}

output "environment" {
  description = "Identity of the provisioned environment."

  value = {
    name       = var.environment
    region     = var.aws_region
    account_id = data.aws_caller_identity.current.account_id
  }
}
