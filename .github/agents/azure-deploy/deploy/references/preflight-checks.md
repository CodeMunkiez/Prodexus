<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# Preflight Checks

Pre-deployment validation steps. Run after user approval, before deployment execution.

> AppOnboard runs direct deployment (no `azd`).

## Check Sequence

Branch on `scaffold-manifest.json.iacFormat`:

### 0. Auth Token Verification

```bash
az account show
```

- Success â†’ proceed. Active subscription + tenant confirmed.
- Failure â†’ `ENVIRONMENT_BLOCKING`. Suggest `az login` (plain, no scope).
- â›” NEVER suggest `az login --scope https://graph.microsoft.com/.default` â€” Graph scope is irrelevant for ARM deployments.

### 0b. Resource Name Availability

Check globally-unique names before deploy: `az acr check-name`, `az storage account check-name`, `az webapp show`, `az keyvault show`. Name taken â†’ suggest alternate from `prepare-plan.json.naming.suffix`: "Name `{name}` taken. Use `{altName}`?"

### 0c. F1/Free Tier Warning

If plan includes F1/D1/free SKUs, surface at deploy gate (do NOT block):
> âš ï¸ Free tier: no custom domains, no SSL, no always-on, 60 min/day compute (F1). Dev/test only.

### 0d. RBAC Scope Pre-Check

```bash
az role assignment list --assignee {userId} --scope /subscriptions/{sub} --query "[].roleDefinitionName" -o tsv
```

Subscription-scope deploy requires `Contributor`/`Owner` on subscription. Missing â†’ `ENVIRONMENT_BLOCKING` with `az role assignment create` command.

### 1. Deployment Preview

â›” **MANDATORY â€” do NOT skip.** What-if validates + previews in one call. Use `what-if` exclusively â€” `az deployment sub/group validate` hits a known CLI bug (HTTP stream consumed error). If what-if fails, log + warn user â€” do not skip to execution.

#### Bicep (subscription scope)

```bash
az deployment sub what-if \
  --name "{deploymentName}" \
  --location {location} \
  --template-file infra/main.bicep \
  --parameters @infra/main.parameters.json \
  --subscription {subscriptionId} \
  --what-if-result-format FullResourcePayloads
```

#### Bicep (resource-group scope)

```bash
az deployment group create \
  --resource-group {rg} \
  --template-file infra/main.bicep \
  --parameters @infra/main.parameters.json \
  --subscription {subscriptionId} \
  --what-if \
  --what-if-result-format FullResourcePayloads
```

- Review changes: `Create`, `Modify`, `Delete`, `NoChange`. Surface `Delete` as warnings â€” user must acknowledge.
- Auth error â†’ `ENVIRONMENT_BLOCKING`.

#### Terraform

```bash
terraform plan -out=tfplan -detailed-exitcode
```

- Exit 0 â†’ no changes. Exit 2 â†’ changes (normal). Exit 1 â†’ error.
- Surface `destroy` as warnings â€” user must acknowledge. Auth error â†’ `ENVIRONMENT_BLOCKING`.

### 3. RBAC Permission Check

```bash
az role assignment list \
  --assignee {currentUserObjectId} \
  --scope /subscriptions/{sub}/resourceGroups/{rg} \
  --query "[].roleDefinitionName" -o tsv
```

Required: `Contributor` or `Owner` on the target resource group. If missing â†’ `ENVIRONMENT_BLOCKING` with remediation command.

### 4. SKU Quota Verification

â›” **`what-if` does NOT catch quota errors.** It returns `Succeeded` even when target SKU has limit=0.

If `prepare-plan.json.quotaValidation.verified == true` â†’ proceed.

Otherwise â†’ **read [sku-quota-validation.md](../../prepare/references/sku-quota-validation.md)** and run direct quota checks NOW (per-provider API patterns, offer restrictions). If limit=0 â†’ HALT, present region fallback. Skip regions in `quotaValidation.checkedRegions` with zero availability.

â›” `SubscriptionIsOverQuotaForSku` or `LocationIsOfferRestricted` in deploy output â†’ HALT. See [error-classification.md](error-classification.md).

### 5. Resource Group Existence

```bash
az group show --name {rg} --query "location" -o tsv 2>/dev/null
```

- Exists â†’ verify location matches `prepare-plan.json` region. Mismatch â†’ warn.
- Not exists â†’ will be created by deployment (if `main.bicep` has subscription scope).

## Error Handling

Each check runs independently. Collect all results, then present structured report.

| Check | Fail Behavior |
|-------|---------------|
| Deployment preview | Warn, don't block (can fail on unsupported types) |
| RBAC | Block. Surface `az role assignment create`. |
| RG check | Warn on location mismatch. Don't block. |

## Report Format

```
## Preflight Results
âœ… IaC syntax: valid (terraform validate / bicep build)
âš ï¸ Deployment preview: 3 creates, 0 destroys, 1 update
âœ… RBAC: Contributor role confirmed
âœ… Resource group: rg-myapp-dev (eastus2)
```

