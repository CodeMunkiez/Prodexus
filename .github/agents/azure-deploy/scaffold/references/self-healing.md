<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# Self-Healing Loop â€” Error Classification & Auto-Fix

Step 11 runs validation against the generated IaC via CLI commands (`az bicep build` + `az deployment sub what-if`). On failure, classify each error and apply the strategy below. Max 3 attempts before pausing to present a diagnosis (explain pattern â†’ propose fix â†’ ask user). After user approves, 5 more attempts before asking again â€” then every 5 thereafter. See scaffold instructions.md Â§ Self-Healing Loop for the full escalation protocol.

| Error Type | Class | Auto-Fix Strategy |
|------------|-------|-------------------|
| Invalid property name | FIXABLE | Replace with correct property from schema summary |
| Syntax error (HCL/Bicep) | FIXABLE | Re-generate affected module from reference patterns |
| Missing required property | FIXABLE | Add with default value from MCP best practices |
| Wrong API version | FIXABLE | Update to version from schema result |
| Provider version conflict | FIXABLE | Update `required_providers` block |
| Undeclared variable | FIXABLE | Add declaration to `variables.tf` |
| Policy-blocked SKU | FIXABLE | Substitute with next-best from `rejectedAlternatives[]` |
| Circular dependency | FIXABLE | Refactor module references â€” break cycle |
| Permission/RBAC insufficient | BLOCKING | Surface required role + `az role assignment create` command |
| State backend inaccessible | BLOCKING | Surface `az storage account create` instructions |
| Region unsupported for resource | BLOCKING | Suggest alternate regions â€” requires user decision |
| Quota exhaustion (ALL tiers in ALL regions) | PLAN_LEVEL_CHANGE | â›” Service type pivot required â€” see scaffold instructions.md Â§ Self-Healing Loop. Update `prepare-plan.json` â†’ present re-approval gate â†’ regenerate IaC. Counts as 1 healing attempt |
| Quota exhaustion (single region) | PLAN_LEVEL_CHANGE | â›” Region pivot required â€” read `prepare-plan.json.quotaValidation.checkedRegions` and `failedResources` to skip already-failed regions. After checking new regions, append results back to these fields. See scaffold instructions.md Â§ Self-Healing Loop. Update plan region â†’ present re-approval â†’ regenerate IaC |
| Policy blocks planned service entirely | PLAN_LEVEL_CHANGE | â›” Alternative service required â€” see scaffold instructions.md Â§ Self-Healing Loop. Map to next-best from `rejectedAlternatives[]` â†’ present re-approval â†’ regenerate IaC |

