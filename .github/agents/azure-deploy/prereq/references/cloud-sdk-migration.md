<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# Non-Azure Cloud Service Dependencies

> Detected during Step 2 manifest scan. The `ask_user` redirect gate is handled in instructions.md Step 2 â€” this file defines the classification rules.

Functional cloud SDK deps are ðŸ”¶ `CLOUD_SDK_MIGRATION`. Populate `prereq-output.json.cloudSdkFindings[]`.

| Found Dependency | Azure Equivalent |
|-----------------|-----------------|
| AWS DynamoDB (`AWSSDK.DynamoDBv2`, `@aws-sdk/client-dynamodb`), GCP Firestore | Cosmos DB (NoSQL API) |
| AWS Cognito (`AWSSDK.CognitoIdentityProvider`, `amazon-cognito-identity-js`), Firebase auth (`firebase-admin`) | Entra ID / Entra External ID |
| AWS S3, GCP Cloud Storage (`@google-cloud/storage`), MinIO | Azure Blob Storage |
| AWS Lambda, GCP Cloud Functions (handler signatures) | Azure Functions (rewrite handlers) |
| AWS SQS (`@aws-sdk/client-sqs`, `AWSSDK.SQS`), GCP Cloud Tasks (`google-cloud-tasks`) | Queue Storage / Service Bus |
| AWS SNS (`@aws-sdk/client-sns`, `AWSSDK.SimpleNotificationService`), GCP Pub/Sub (`google-cloud-pubsub`) | Service Bus / Event Grid |
| Firebase (`firebase`, `firebase-admin`) full stack | Entra ID + Cosmos DB + Functions |

> **Observability carve-out:** Observability deps are âš ï¸ WARN (app runs without them), NOT ðŸ”¶. Use this table to distinguish:
>
> | Package | Classification | Why |
> |---------|---------------|-----|
> | `@google-cloud/opentelemetry-*` | âš ï¸ WARN | Telemetry â€” app works without it |
> | `@google-cloud/logging` | âš ï¸ WARN | Logging â€” app works without it |
> | `@google-cloud/monitoring` | âš ï¸ WARN | Monitoring â€” app works without it |
> | `aws-xray-sdk`, `aws-rum-web` | âš ï¸ WARN | Tracing/RUM â€” app works without it |
> | `google-cloud-tasks` | ðŸ”¶ CLOUD_SDK_MIGRATION | Functional â€” app breaks without it |
> | `google-cloud-pubsub` | ðŸ”¶ CLOUD_SDK_MIGRATION | Functional â€” app breaks without it |
> | `google-cloud-storage` | ðŸ”¶ CLOUD_SDK_MIGRATION | Functional â€” app breaks without it |
> | `@aws-sdk/client-dynamodb`, `boto3` (DynamoDB) | ðŸ”¶ CLOUD_SDK_MIGRATION | Functional â€” app breaks without it |
> | `@aws-sdk/client-sqs`, `@aws-sdk/client-sns` | ðŸ”¶ CLOUD_SDK_MIGRATION | Functional â€” app breaks without it |

