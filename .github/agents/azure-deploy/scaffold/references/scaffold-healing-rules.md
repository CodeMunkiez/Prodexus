<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# Scaffold Healing Rules

Self-healing loop for scaffold validation failures. Contains error classification (FIXABLE vs BLOCKING) and scaffold-specific escalation and plan-change rules.

## Error Classification

| Error Type | Class | Auto-Fix Strategy |
|------------|-------|-------------------|
| Invalid property name | FIXABLE | Fix from schema |
| Syntax error (HCL/Bicep) | FIXABLE | Regenerate from references |
| Missing required property | FIXABLE | Call `mcp_bicep_build_bicep` for structured error, fix from diagnostics output |
| Wrong API version | FIXABLE | Call `mcp_bicep_list_az_resource_types_for_provider` with `{ providerNamespace: "..." }` for latest GA. Fallback: reference file examples |
| Provider version conflict | FIXABLE | Update `required_providers` |
| Undeclared variable | FIXABLE | Add to `variables.tf` |
| Policy-blocked SKU | FIXABLE | Next-best from `rejectedAlternatives[]` |
| Circular dependency | FIXABLE | Break cycle â€” refactor module refs |
| Permission/RBAC insufficient | BLOCKING | Surface role + `az role assignment create` |
| State backend inaccessible | BLOCKING | Surface `az storage account create` |
| Region unsupported | BLOCKING | Suggest alternate regions (user decision) |
| Quota exhaustion (all tiers+regions) | PLAN_LEVEL_CHANGE | â›” See Â§ PLAN_LEVEL_CHANGE |
| Quota exhaustion (single region) | PLAN_LEVEL_CHANGE | â›” Region pivot â€” skip `quotaValidation.checkedRegions` failures |
| Policy blocks service entirely | PLAN_LEVEL_CHANGE | â›” Map to `rejectedAlternatives[]` |

## Healing Escalation Cadence

Classify Step 11 failures using the table above. **FIXABLE:** edit IaC â†’ re-validate (max 3 before asking). Each attempt must be a *different* fix. **BLOCKING:** surface to user, halt.

> â›” **After each FIXABLE auto-fix:** call `mcp_bicep_build_bicep` with `{ filePath: "infra/main.bicep" }` to quick-check before re-dispatching the validate sub-agent. If errors remain, fix again â€” saves a sub-agent dispatch. Fallback: `az bicep build`.

> â›” **After 3 attempts:** summarize error pattern, propose specific next fix (never "I'll try again" without naming the change), present options: **"Yes, try that"** (5 more) | **"I have a suggestion"** (apply user fix) | **"Stop"** (write `validationResult.status: "Failed"`). Ask again every 5 thereafter.

> â›” **Same error 3Ã—:** read [iac-resources.md](../../references/iac-resources.md) for docs. Present to user; stop auto-healing that pattern.

## PLAN_LEVEL_CHANGE

> â›” Service type or region changes require user re-approval â€” never silently rewrite IaC.

| Step | Action |
|------|--------|
| 1. STOP | Never silently change service/region |
| 2. Update plan | `prepare-plan.json`: `services[]`, `naming.resources[]`, `costEstimate`, `deployStrategy` per [`prepare-schemas.ts`](../../prepare/references/prepare-schemas.ts) |
| 3. Re-approve | "âš ï¸ Plan change: {old} â†’ {new} (~${new}/mo). Approve? (Yes / Edit / Cancel)" |
| 4. Regenerate | New module files with correct names, delete stale, re-run self-review + validation |
| 5. Log | [`ScaffoldHealingAttempt`](scaffold-schemas.ts) with `planLevelChange: true`. Counts toward healing cap |

> â›” **Artifact consistency:** After any service/region/SKU change, update ALL upstream artifacts (`prepare-plan.json`, `scaffold-manifest.json`, `context.json`, IaC files) â€” the plan MUST match deployed reality.

