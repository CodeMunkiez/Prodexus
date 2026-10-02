<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# Approval Gate Template

Display template for the deploy approval gate. Present after preflight validation, before execution.

## Display Format

```
## Deploy Approval

ðŸ¢ **Subscription:** {context.json.azure.subscriptionName} (`{context.json.azure.subscriptionId}`)
ðŸ“ **Resource Group:** {context.json.azure.resourceGroup}
ðŸŒ **Region:** {context.json.azure.region}

**Services:**
| Service | SKU | Region | Resource Name |
|---------|-----|--------|---------------|
{for each service in prepare-plan.json.services[]}

**ðŸ’° Estimated Monthly Cost:** ${costEstimate.monthlyUsd}/month
| Service | SKU | Monthly |
|---------|-----|---------|
{for each item in costEstimate.breakdown[]}
{costEstimate.disclaimer}

**ðŸ”’ Validation:** {scaffold-manifest.json.validationResult.status}
{if FLAGGED findings from scaffold self-review â†’ âš ï¸ list each}

**ðŸ“‹ What-if preview:** {N} resources to create, {N} to modify, {N} unchanged
{if any Delete operations â†’ âš ï¸ list each deleted resource with name + type}

**Generated Files:**
{for each file in scaffold-manifest.json.files[]}

**ðŸ“¦ Deployment Summary:**
{Display the "What's Being Deployed" table here, built from prepare-plan.json.services[] â€” showing which components map to which Azure services, SKUs, and estimated costs. (deployment-summary.md is not written until the deploy/handoff phase)}

---
**ðŸš€ Ready to deploy? (Yes / Run manually / Edit plan / Cancel)**
```

## Response Handlers

| Response | Action |
|----------|--------|
| **Yes** | Execute deployment (Step 6 of deploy workflow). Do NOT re-confirm. |
| **Run manually** | Show exact CLI commands based on `iacFormat`:<br>**Bicep (subscription-scope, default):** `az deployment sub create --subscription {subscriptionId} --location {location} --template-file infra/main.bicep --parameters @infra/main.parameters.json --query properties.provisioningState -o tsv`<br>**Bicep (resource-group scope, after 403 fallback):** `az deployment group create --resource-group {rg} --template-file infra/main.bicep --parameters @infra/main.parameters.json --query properties.provisioningState -o tsv`<br>**Terraform (alternative):** `cd infra && terraform init && terraform plan -out=tfplan && terraform apply tfplan`. Stop. |
| **Edit plan** | Ask what to change â†’ write to `context.json.overrides[]` â†’ re-run prepare (Step 4) â†’ re-scaffold â†’ return to approval gate |
| **Cancel** | Preserve all session artifacts. Say "ðŸ’¾ Session preserved â€” resume anytime." Stop. |

## Rules

- â›” Gate is the LAST content in the response â€” no continued execution until user replies
- â›” SKU column must show exact Azure SKU code + tier name (e.g., "B1 Linux (Basic)", "P1v3 (Premium)") â€” not generic labels like "cheapest tier". F1/D1/Free are never selected.
- Always show the monthly cost estimate â€” there is no $0 tier (B1 floor ~$13/mo)
- Surface ALL `FLAGGED` self-review findings â€” user must see risks before approving
- If validation failed, show failures and block Yes option until resolved

