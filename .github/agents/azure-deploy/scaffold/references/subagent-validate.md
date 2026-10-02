<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# Subagent Template â€” Validation & Manifest (Steps 10â€“12)

Validate IaC syntax, write `scaffold-manifest.json`, and generate deploy checklist. Follow the workflow below.

## Critical Rules

- â›” **Do NOT invoke any other agents or hand off** â€” no external agent calls of any kind. Use the procedures in THIS file only.
- â›” **Do NOT create or modify Azure resources** â€” validation is syntax-only (`bicep build` / `terraform validate`), never `az deployment sub create`.
- â›” **Do NOT run `what-if` or `terraform plan`** â€” deploy runs the mandatory what-if with real secret params. Scaffold validates syntax only.

## Input (provided by caller)

| Field | Required |
|-------|----------|
| IaC file paths (all generated `.bicep` or `.tf` files) | YES |
| Self-review findings from Steps 6â€“9 | YES |
| `prepare-plan.json` â€” services, naming, region, subscriptionId | YES |
| `prereq-output.json.warnings[]` â€” prereq warnings with `fixPhase` | YES |
| `prereq-output.json.healthEndpoint` â€” detected health path (or `null`) | YES |
| Conformance result JSON (from main-thread Step 10a-conf) | YES |

## Output

| Artifact | Location |
|----------|----------|
| `scaffold-manifest.json` | Session folder |
| `deploy-result.json` skeleton | Session folder |
| Validation result status | Return to caller: `Validated` or `Failed` |
| Deploy checklist | `.copilot-azure/sessions/{id}/deploy-checklist.md` |

## Workflow

### Step 1 â€” Read validation + manifest rules

Read [validation-and-manifest.md](validation-and-manifest.md) and [scaffold-schemas.ts](scaffold-schemas.ts).

**Do:** Understand the `ScaffoldManifest` interface (field names, types, required fields) and the validation sequence.

### Step 2 â€” Format and validate IaC

**Do:**
1. Run `az bicep build --file infra/main.bicep --stdout > $null` (Bicep) or `terraform validate` (TF). Process output for BCP errors and warnings. Record pass/fail.

### Step 3 â€” Check RBAC completeness

**Do:** Verify every managed identity â†” resource pair in the IaC has a corresponding `Microsoft.Authorization/roleAssignments` resource with the correct role GUID. Cross-reference with the review findings from Steps 6â€“9 input.

### Step 3b â€” Azure runtime constraint check (Container Apps)

> Skip if no Container Apps in the plan.

For each Container App resource in the generated Bicep:

1. **cpu/memory combo** â€” verify `cpu` (must be type `string`) + `memory` is one of: `0.25/0.5Gi`, `0.5/1Gi`, `0.75/1.5Gi`, `1/2Gi`, `1.25/2.5Gi`, `1.5/3Gi`, `1.75/3.5Gi`, `2/4Gi`. FIXABLE: adjust to nearest valid combo (use smallest valid combo for sidecars/companions).
2. **secretRef coverage** â€” every `secretRef` in container env vars must have a matching entry in `configuration.secrets[]` (native `name` + `value`). â›” **No `keyVaultUrl` / Key Vault** â€” a `keyVaultUrl` or any `Microsoft.KeyVault/vaults` resource is a BLOCK. Missing native secret entry â†’ FIXABLE: add `{ name, value: <secureParam> }` to the CA's `secrets[]`.
3. **probe path** â€” if `prereq-output.json.healthEndpoint` is non-null, verify `probePath` matches it. If null AND any `plainEnvVars` entry is named `BASE` or `PATH_PREFIX`, verify `probePath` starts with that value (not bare `/`). If null AND no BASE var: verify the app has a route handler for the probe path (check source entry point for `app.get('/')` or framework root handler) â€” bare `/` on a REST API with only sub-path routes (e.g., `/users`, `/messages`) returns 404 and blocks revision activation. FIXABLE: update `probePath` to a known GET endpoint from the app.

FIXABLE errors: fix the Bicep â†’ re-run `az bicep build` â†’ proceed to Step 3c.

### Step 3b2 â€” App Service security constraint check

> Skip if no App Service or Functions in the plan.

For each App Service resource, and each **Premium (EP1)** Functions resource, in the generated Bicep:

1. **basicPublishingCredentialsPolicies** â€” verify both child resources exist: `basicPublishingCredentialsPolicies/scm` (with `allow: true`) and `basicPublishingCredentialsPolicies/ftp` (with `allow: false`). Missing â†’ FIXABLE: add the child resources per [bicep-patterns-security.md](bicep-patterns-security.md) Â§ Publishing Credential Lockdown. â›” **Skip Flex Consumption function apps** (`kind: 'functionapp,linux'` with `functionAppConfig`) â€” they must NOT have these policies; if present, FIXABLE: remove them (see [bicep-functions-flex.md](bicep-functions-flex.md)).
2. **uniqueString in naming** â€” verify the App Service name uses `uniqueString()` or a unique suffix (not a hardcoded literal). Hardcoded names cause global collisions. Missing â†’ FIXABLE: wrap name with `uniqueString(resourceGroup().id)`.

FIXABLE errors: fix the Bicep â†’ re-run `az bicep build` â†’ proceed to Step 3c.

### Step 3c â€” Record plan conformance result

The main thread (instructions.md Step 10a-conf) already ran the conformance script and passed you its JSON. Record it in `scaffold-manifest.json.conformance` = `{ passed, failures, source: "script" }`. â›” Do NOT set `validationResult.status: "Validated"` while any BLOCK failure is unresolved.

**Fallback** (only if the caller passed NO result AND `infra/main.bicep` exists â€” the gate is Bicep-only, skip for Terraform): run `{scaffoldDir}/scripts/scaffold-conformance.ps1 -SessionPath "{sessionPath}" -InfraPath infra` (or `.sh` on bash) yourself, then record with `source: "script"`. Never hand-judge when a shell is available.

### Step 4 â€” Write scaffold-manifest.json

Read [scaffold-schemas.ts](scaffold-schemas.ts) for exact field names.

**Do:** Write `scaffold-manifest.json` to the session folder with: `sessionId`, `scaffoldCompletedUtc`, `iacFormat`, `targetScope`, `entryPoint`, `parametersFile`, `files[]`, `deployCommand`, `twoPhaseWiring` (if Container Apps), `phase2Steps` (if applicable), `selfReview` (from caller input), `validationResult` (from Steps 2â€“3). Use the exact field names from [scaffold-schemas.ts](scaffold-schemas.ts) Â§ `ScaffoldManifest`.

### Step 5 â€” Handle failures (if any)

Read [scaffold-healing-rules.md](scaffold-healing-rules.md) ONLY if validation failed.

**Do:** Classify errors as FIXABLE or BLOCKING. FIXABLE: auto-fix IaC â†’ re-validate (max 3 attempts before asking user). BLOCKING: surface to user and halt. PLAN_LEVEL_CHANGE: requires re-approval â€” do NOT auto-fix.

### Step 6 â€” Verify deploy checklist exists

**Do:** Check that `.copilot-azure/sessions/{id}/deploy-checklist.md` exists (written by the parallel checklist subagent at scaffold Step 5b). If missing, read [`deploy-checklist-template.md`](../../deploy/references/deploy-checklist-template.md) and write it now as a fallback â€” fill `{placeholders}` from `prepare-plan.json`, delete non-applicable sections. This file survives conversation compaction

### Step 7 â€” Create deploy-result.json skeleton (MANDATORY)

Read [`deploy-schemas.ts`](../../deploy/references/deploy-schemas.ts) â€” specifically the `DeployResult` interface.

**Do:** Create `.copilot-azure/sessions/{id}/deploy-result.json` conforming to the `DeployResult` interface. Populate fields from all session artifacts already written (`prepare-plan.json`, `context.json`, `scaffold-manifest.json`, `prereq-output.json`). Use sensible defaults for fields the deploy phase will fill later. The deploy main agent updates this file in-place at Step 8 with real values.

### Step 8 â€” Return results

**Do:** Return validation status (`Validated` or `Failed`) to the caller. Confirm deploy-checklist.md exists. Keep status report â‰¤500 tokens.

