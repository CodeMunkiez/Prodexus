<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# Subagent Template â€” Quota Validation (Step 5)

Validate SKU quota and offer restrictions across candidate regions before presenting region choices.

## Critical Rules

- â›” **Do NOT invoke any other agents or hand off** â€” no external agent calls of any kind. You are a quota-check subagent only.
- **Read [`sku-quota-validation.md`](sku-quota-validation.md) before executing ANY quota or offer restriction check.** It contains the per-provider API patterns, anti-patterns to avoid, offer restriction checks for database services, region selection logic, and output schema. All procedures live there â€” follow them exactly.

## Input (provided by caller)

| Field | Required |
|-------|---------|
| `subscriptionId` | YES |
| SKU list from Step 4 (service type + SKU per service) | YES |
| Restricted-offer services (PostgreSQL, MySQL) | If present in plan |
| **Detected DB version per DB service** (from `context.json.detectedServices[]`, e.g. MySQL `5.7`) | â›” REQUIRED if a DB is in the plan â€” the version-selection algorithm in [`sku-quota-validation.md`](sku-quota-validation.md) needs it. |

## Output

Return JSON (â‰¤500 tokens):
```jsonc
{
  "quotaResults": [
    {
      "region": "{checked region}",
      "services": [
        { "service": "{provider}", "sku": "{sku}", "limit": "{from API}", "used": "{from API}", "available": "{limit - used > 0}" },
        // if DB service: add "offerRestricted": "{from capabilities API}", "version": "{selected per sku-quota-validation.md â€” exact patch, never major-only '8.0'}"
      ],
      "allAvailable": "{true only if ALL services in this region have available=true}"
    }
    // one entry per checked region
  ],
  "recommendedRegion": "{first region where allAvailable=true}",
  "checkedRegions": ["{all regions checked}"],
  "offerRestrictions": [
    // one entry per DB service+region checked â€” derive from capabilities API response
    { "provider": "{namespace}", "region": "{region}", "restricted": "{true if blocked}", "reason": "{from API or null}" }
  ],
  "offerRestrictionsVerified": "{true only if every DB service from input has â‰¥1 entry in offerRestrictions[]}"
}
```

â›” **Caller:** copy each DB service's returned `version` into `prepare-plan.json.services[].version` at plan-write â€” scaffold needs the exact patch (ARM rejects major-only `'8.0'`).

## Workflow

1. Read [`sku-quota-validation.md`](sku-quota-validation.md)
2. Run the per-provider quota checks for every SKU across all candidate regions
3. If restricted-offer services are in the input, run the offer restriction check for each database service in each candidate region per `sku-quota-validation.md` Â§ Offer Restriction Check
4. Return results per the Output schema above (â‰¤500 tokens)

## Anti-Patterns (from sku-quota-validation.md â€” repeated here as guardrails)

- â›” `az vm list-usage` â€” wrong provider, misleading data
- â›” `az appservice list-locations` â€” lists locations, NOT quota
- â›” `az appservice list-usages` â€” wrong scope
- â›” `mcp_azure_mcp_quota` â€” unreliable for App Service
- â›” `az quota list` â€” extension loading fails on Windows
- â›” Use `az rest` for ALL quota checks

## Rules

- â›” Paid â‰  unlimited â€” B1, Flex Consumption, Serverless all have per-subscription, per-region quotas (F1/Free are never selected â€” floor is B1)
- â›” Check BOTH limit AND current usage â€” `limit=1, usage=1` means FULL
- â›” For PostgreSQL/MySQL: run offer restriction check per `sku-quota-validation.md` Â§ Offer Restriction Check

## Token Budget

â‰¤500 tokens for quota results report.

