<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# App Onboard Prepare â€” Pricing Guide

> â›” **SELF-CHECK:** If `costEstimate.breakdown[]` has ANY `monthlyUsd > 0`, at least one `pricing_get` or `az rest` pricing API call MUST appear in this session. Estimating from memory or training data is NEVER acceptable â€” reference values in this file may be outdated. If API fails after 2 attempts, use the reference value WITH disclaimer: `"âš ï¸ Estimated from reference â€” live API unavailable."`

## No Free-Tier Shortcut â€” Compute Always Has Cost

> â›” **There is no $0 shortcut.** Compute has a floor (App Service **B1**, Static Web Apps **Standard**, Functions **Flex Consumption**) â€” F1/D1/Free/Consumption are never selected (see [sku-matrix.md](sku-matrix.md)). So any app with a compute component has a non-zero monthly cost and MUST get a live pricing call. Do NOT write a $0 estimate for a compute app.

Some services can still fall within free **grant** limits (they are metered, not a fixed free SKU) â€” note the grant in the estimate but still price the paid meters:

| Service | Free grant (still price paid usage above it) |
|---------|-----------------------------------------------|
| Functions (Flex Consumption) | First 250K executions + 100K GB-s/month |
| Cosmos DB | Free-tier account: 1000 RU/s + 25 GB (one per subscription) |
| Container Apps | First 180K vCPU-s + 360K GiB-s + 2M requests/month |
| Log Analytics | First 5 GB/month ingestion |

---

## App Service Quick Reference

| SKU | `skuName` | Monthly |
|-----|-----------|---------|
| B1 Linux | `B1` | ~$13.14 |

> â›” **Case-sensitive:** use `B1` not `b1`. Use `skuName` (not `armSkuName`) for Basic/Standard.

---

## How to Use

1. Pick SKU from [sku-matrix.md](sku-matrix.md) based on budget intent.
2. Find service section in [pricing-guide-services.md](pricing-guide-services.md) for filter strings and formulas.
3. Call the pricing router â€” tool `mcp_azure_mcp_pricing` (VS Code) / `azure-pricing` (CLI) â€” with `intent`, `command: "pricing_get"`, and a `parameters` object (NOT `--flags`). Parallel OK. â›” At least one filter inside `parameters`: `sku`, `service`, `region`, `service-family`, or `filter`. â›” **`sku` matches `armSkuName`, not `skuName`** â€” use it ONLY when `armSkuName` is populated (App Service, MySQL/PostgreSQL, Redis). Empty-`armSkuName` services (ACR, Storage, Cosmos) return `[]` or 400 for `sku`/`service` â€” use a raw `filter` on `serviceName` + `meterName` instead.
4. Apply the monthly multiplier from each meter's `unitOfMeasure` (see Â§ Monthly Multiplier) â€” NOT a per-service constant.
5. Cross-check returned `retailPrice` against planned SKU.

## Monthly Multiplier â€” by Meter Unit

â›” **Read `unitOfMeasure` per price record â€” NEVER blanket `Ã— 730`** (one service mixes units, e.g. ACR returns `1/Day` + `1 Second` + `1 GB/Month`):

| `unitOfMeasure` | Monthly formula |
|-----------------|-----------------|
| `1 Hour` / `1/Hour` | `retailPrice Ã— 730` |
| `1/Day` / `1 Day` | `retailPrice Ã— 30` |
| `1 GB/Month` | `retailPrice Ã— estimatedGB` (already monthly) |
| `1 Second` / `1 GiB Second` | `retailPrice Ã— activeUnitsPerMonth` (usage-based) |
| `10K` / `1M` operations | `retailPrice Ã— estimatedOps Ã· unitSize` |

## Usage-Based Services

When AI/OpenAI/LLM components detected: add `"ðŸ’¸ Usage-based â€” excluded from total"` to `costEstimate.breakdown[]` and `"âš ï¸ðŸ¤– AI inference costs excluded"` to `assumptions[]`. Surface at EVERY approval gate.

## Service Pricing Reference

See [pricing-guide-services.md](pricing-guide-services.md) for per-service filter strings, meter names, and monthly formulas.

**Key rules:**
- `armSkuName` is empty for most PaaS â€” filter by `meterName` or `productName`
- Monthly multiplier: read `unitOfMeasure` (Â§ Monthly Multiplier) â€” never assume Ã—730
- Functions Consumption / Static Web Apps: not in API
- Default `isPrimaryMeterRegion eq true` + `priceType eq 'Consumption'` â€” but check the service section in [pricing-guide-services.md](pricing-guide-services.md) first; some drop `isPrimaryMeterRegion` (Service Bus, Cosmos 100 RU/s, Redis Basic).
- Service names are case-sensitive

## Troubleshooting

When `pricing_get` returns empty: (1) Check `armSkuName` populated/case-sensitive? (2) HTTP fallback: `https://prices.azure.com/api/retail/prices?$filter={filter}`. (3) After 2 failures, use [pricing-guide-services.md](pricing-guide-services.md) with disclaimer. (4) Log to `costEstimate.apiFailures[]`.

> â›” **Never claim user has free credits remaining.** Use: "Check balance at portal."


