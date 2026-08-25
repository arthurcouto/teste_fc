#!/usr/bin/env bash
set -euo pipefail

awslocal sqs create-queue --queue-name cashflow-entries-exception \
  --attributes MessageRetentionPeriod=1209600

EXCEPTION_ARN=$(awslocal sqs get-queue-attributes \
  --queue-url http://localhost:4566/000000000000/cashflow-entries-exception \
  --attribute-names QueueArn --query 'Attributes.QueueArn' --output text)

awslocal sqs create-queue --queue-name cashflow-entries --attributes "$(cat <<JSON
{
  "ReceiveMessageWaitTimeSeconds": "20",
  "VisibilityTimeout": "60",
  "MessageRetentionPeriod": "1209600",
  "RedrivePolicy": "{\"deadLetterTargetArn\":\"$EXCEPTION_ARN\",\"maxReceiveCount\":\"10\"}"
}
JSON
)"
