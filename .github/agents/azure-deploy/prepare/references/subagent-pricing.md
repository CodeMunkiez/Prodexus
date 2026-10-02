<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# Subagent Template â€” Cost Estimation (Step 6)

Estimate monthly costs for planned Azure services using Azure Retail Prices API.

## References to Read Internally

Read BOTH before making any pricing calls:
- [pricing-guide.md](pricing-guide.md) â€” methodology, free-grant handling, API patterns, troubleshooting
- [pricing-guide-services.md](pricing-guide-services.md) â€” per-service filter strings, meter names, formulas

## Input (provided by caller)

| Field | Required |
|-------|----------|
| `services[]` with service type and selected SKU per service | YES |
| `region` â€” deployment region | YES |
| Budget tier (free / balanced / performance) | YES |

## Output

Return JSON (â‰¤500 tokens):
```json
{
  "costEstimate": {
    "monthlyTotal": 42.50,
    "currency": "USD",
    "breakdown": [
      { "service": "App Service", "sku": "B1", "monthlyUsd": 13.14, "formula": "retailPrice Ã— 730" },
      { "service": "PostgreSQL Flexible Server", "sku": "Standard_B1ms", "monthlyUsd": 24.82, "formula": "compute + storage" }
    ],
    "freeGrants": [
      { "service": "Container Apps", "grant": "180K vCPU-sec + 360K GiB-sec", "monthlySavings": 0 }
    ],
    "assumptions": [],
    "disclaimer": "Estimate based on Azure Retail Prices API. Verify at azure.com/pricing."
  }
}
```

## Rules

- â›” **Do NOT invoke any other agents or hand off** â€” no external agent calls of any kind. You are a pricing subagent only. Use direct HTTP to `https://prices.azure.com/api/retail/prices` for price queries (MCP pricing was already attempted inline by the caller).
- â›” **No $0 shortcut â€” compute always has cost** (App Service B1 / SWA Standard / Functions Flex Consumption floor). Price every compute app via live API; note free **grants** (Cosmos free-tier account, Container Apps/Functions grants) but still price paid meters. Do NOT return $0 for a compute app.
- â›” `armSkuName` is case-sensitive â€” use `B1` not `b1`
- â›” For services with empty `armSkuName` (Container Apps, Functions Flex Consumption, ACR, Storage, Cosmos): use `filter`/`meterName` matching â€” a `sku` filter returns `[]`
- â›” **Monthly multiplier = each meter's `unitOfMeasure`** (`1 Hour`â†’Ã—730, `1/Day`â†’Ã—30 [ACR/registry, SQL DTU], `1 GB/Month`â†’Ã—GB, `1 Second`â†’usage) â€” NEVER blanket Ã—730; applying Ã—730 to a `1/Day` meter overstates ~24Ã—
- â›” Use direct HTTP to `https://prices.azure.com/api/retail/prices` for all price lookups. Do NOT use `mcp_azure_mcp_pricing` â€” it was already tried inline and failed.
- â›” Never hardcode dollar amounts â€” always query live prices
- â›” If pricing API returns 400: verify `--sku` included in the query

## Token Budget

â‰¤500 tokens for cost estimate report.

