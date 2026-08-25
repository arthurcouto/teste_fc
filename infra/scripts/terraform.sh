#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TERRAFORM_DIR="$ROOT/terraform"

COMMAND="${1:-}"
ENVIRONMENT="${2:-}"

if [[ -z "$COMMAND" || -z "$ENVIRONMENT" ]]; then
  echo "usage: terraform.sh <plan|apply|destroy|output|bootstrap> <environment>" >&2
  exit 1
fi

VARIABLES="$TERRAFORM_DIR/environments/$ENVIRONMENT.tfvars"
if [[ ! -f "$VARIABLES" ]]; then
  echo "unknown environment: $ENVIRONMENT" >&2
  exit 1
fi

read_variable() {
  sed -n "s/^[[:space:]]*$1[[:space:]]*=[[:space:]]*\"\([^\"]*\)\".*/\1/p" "$VARIABLES" | head -1
}

REGION="$(read_variable aws_region)"
EXPECTED_ACCOUNT="$(read_variable expected_account_id)"

if [[ -z "$REGION" || -z "$EXPECTED_ACCOUNT" ]]; then
  echo "the environment file must declare aws_region and expected_account_id" >&2
  exit 1
fi

if [[ -n "${AWS_REGION:-}" && "$AWS_REGION" != "$REGION" ]]; then
  echo "AWS_REGION is $AWS_REGION but $ENVIRONMENT is declared in $REGION" >&2
  exit 1
fi

ACCOUNT="$(aws sts get-caller-identity --query Account --output text)"
if [[ "$ACCOUNT" != "$EXPECTED_ACCOUNT" ]]; then
  echo "credentials belong to account $ACCOUNT but $ENVIRONMENT expects $EXPECTED_ACCOUNT" >&2
  exit 1
fi

BUCKET="cashflow-iac-state-$ACCOUNT-$REGION"

init_bootstrap_backend() {
  terraform init -input=false -reconfigure \
    -backend-config="bucket=$BUCKET" \
    -backend-config="key=_bootstrap/terraform.tfstate" \
    -backend-config="region=$REGION" \
    -backend-config="use_lockfile=true" >/dev/null
}

create_state_bucket() {
  echo "==> creating the remote state bucket: $BUCKET"
  cd "$TERRAFORM_DIR/bootstrap"
  trap 'mv -f backend.tf.pending backend.tf 2>/dev/null || true' EXIT
  mv backend.tf backend.tf.pending
  terraform init -input=false >/dev/null
  terraform apply -input=false -auto-approve \
    -var="state_bucket_name=$BUCKET" \
    -var="aws_region=$REGION" \
    -var="expected_account_id=$EXPECTED_ACCOUNT"
  mv backend.tf.pending backend.tf
  trap - EXIT
  terraform init -input=false -force-copy -migrate-state \
    -backend-config="bucket=$BUCKET" \
    -backend-config="key=_bootstrap/terraform.tfstate" \
    -backend-config="region=$REGION" \
    -backend-config="use_lockfile=true" >/dev/null
  rm -f terraform.tfstate terraform.tfstate.backup
}

reconcile_state_bucket() {
  cd "$TERRAFORM_DIR/bootstrap"
  init_bootstrap_backend
  terraform apply -input=false -auto-approve \
    -var="state_bucket_name=$BUCKET" \
    -var="aws_region=$REGION" \
    -var="expected_account_id=$EXPECTED_ACCOUNT"
}

ensure_remote_state() {
  if aws s3api head-bucket --bucket "$BUCKET" 2>/dev/null; then
    return
  fi

  if aws s3api list-buckets --query "Buckets[?Name=='$BUCKET'].Name" --output text | grep -q "$BUCKET"; then
    echo "the state bucket exists but is not reachable with these credentials" >&2
    exit 1
  fi

  create_state_bucket
}

if [[ "$COMMAND" == "bootstrap" ]]; then
  ensure_remote_state
  reconcile_state_bucket
  echo "==> remote state reconciled"
  exit 0
fi

ensure_remote_state

cd "$TERRAFORM_DIR/foundation"
terraform init -input=false -reconfigure \
  -backend-config="bucket=$BUCKET" \
  -backend-config="key=$ENVIRONMENT/foundation.tfstate" \
  -backend-config="region=$REGION" \
  -backend-config="use_lockfile=true" >/dev/null

confirm() {
  [[ "${1:-}" == "$ENVIRONMENT" ]] && return 0
  read -r -p "the plan above will be applied to [$ENVIRONMENT]. type the environment name to proceed: " CONFIRMATION
  [[ "$CONFIRMATION" == "$ENVIRONMENT" ]] || { echo "cancelled" >&2; exit 1; }
}

case "$COMMAND" in
  plan)
    terraform plan -input=false -var-file="$VARIABLES"
    ;;
  apply)
    PLAN_FILE="$(mktemp -t cashflow-plan)"
    trap 'rm -f "$PLAN_FILE"' EXIT
    terraform plan -input=false -var-file="$VARIABLES" -out="$PLAN_FILE"
    confirm "${TF_APPLY:-}"
    terraform apply -input=false "$PLAN_FILE"
    ;;
  destroy)
    PLAN_FILE="$(mktemp -t cashflow-plan)"
    trap 'rm -f "$PLAN_FILE"' EXIT
    terraform plan -destroy -input=false -var-file="$VARIABLES" -out="$PLAN_FILE"
    confirm "${TF_DESTROY:-}"
    terraform apply -input=false "$PLAN_FILE"
    ;;
  output)
    terraform output -json
    ;;
  *)
    echo "unknown command: $COMMAND" >&2
    exit 1
    ;;
esac
