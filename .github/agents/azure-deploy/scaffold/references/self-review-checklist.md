<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# Self-Review Checklist

4-layer adversarial review of generated IaC. Run after scaffold generates all files, before `scaffold-manifest.json`.

## Rating System

- **VERIFIED** â€” confirmed correct by inspecting the generated code
- **PLAUSIBLE** â€” likely correct but cannot fully verify (e.g., API version exists but not checked against registry)
- **FLAGGED** â€” incorrect, missing, or contradicts the plan/patterns

**Consume results:** If any finding is FLAGGED â†’ fix the IaC, then re-run validation. If all VERIFIED/PLAUSIBLE â†’ proceed. Write findings to `scaffold-manifest.json.selfReview`.

> â›” **Halt on critical failures** â€” if any finding is FLAGGED at L1 (Security) or L3 (Hallucination), do NOT proceed to deploy. Present findings and ask: **"Fix / Continue with risks / Cancel"**.

> â›” **Step 12 validation remains mandatory** regardless of self-review results â€” IaC may change during FLAGGED fixes.

## Layer 1 â€” Security Claims Extraction

Extract every security claim from the generated IaC and check for internal contradictions.

| Check | Example |
|-------|---------|
| Managed identity declared but secret hardcoded | `identity: { type: 'SystemAssigned' }` but `password: 'hardcoded'` in same file |
| HTTPS enforced but HTTP endpoint exposed | `httpsOnly: true` but ingress allows HTTP |
| Resource accessed via managed identity but matching role not granted | ACR image pull without AcrPull, Storage access without a Storage Data role, or any MIâ†’resource dependency missing its `roleAssignment` â€” access fails at runtime â†’ `FLAGGED` |
| â›” Role assignment scope targets wrong resource | `scope: resourceGroup()` on resource-specific roles â†’ `FLAGGED`. Must scope to specific resource. |
| `principalType` missing on role assignments | Causes intermittent 30s+ delays |
| â›” Identity block missing on compute resource | â›” **MANDATORY FAIL** â€” ALL compute MUST have `identity: { type: 'SystemAssigned' }`. There is NO exception: managed identity is required on every SKU (the compute floor is B1, which supports MI; F1/D1/Free are never generated). Missing identity â†’ `FLAGGED`. |
| SQL firewall `0.0.0.0/0` without private endpoint | Prefer MI + private endpoint. AllowAzureServices genuinely needed â†’ `PLAUSIBLE`. |
| â›” SCM/FTP auth policy missing on App Service | ALL App Service (and Premium/EP1 Functions) MUST have `basicPublishingCredentialsPolicies`: `scm.allow: true`, `ftp.allow: false`. Missing â†’ `FLAGGED`. â›” **Functions on Flex Consumption must NOT have these policies** â€” flag their presence instead. |
| â›” Any `Microsoft.KeyVault/vaults`, `@Microsoft.KeyVault(...)`, or `keyVaultUrl` present | â›” **MANDATORY FAIL** â€” no Key Vault is created; app-internal secrets live on the compute resource. â†’ â›” **FLAGGED** |

**Rating:** Claims that contradict each other â†’ `FLAGGED`. Consistent claims â†’ `VERIFIED`.

## Layer 2 â€” Pattern Validation

Validate against pattern files loaded at Steps 3â€“5 and [rbac-roles.md](rbac-roles.md).

### Bicep

| Check | Source |
|-------|--------|
| File structure: `main.bicep` â†’ `modules/*.bicep` | `bicep-patterns.md` |
| `main.parameters.json` uses ARM JSON (not `.bicepparam`) | `bicep-patterns.md` |
| Naming: `{prefix}{name}{token}` â‰¤32 chars | `bicep-patterns.md` |
| System-assigned managed identity on all services | `bicep-patterns-security.md` |
| No `administratorLogin`/`administratorLoginPassword`/access key on ANY data service (SQL, PG, MySQL, Redis, Storage, Cosmos) â€” Entra/MI-only | `bicep-patterns-security.md` Â§ Data Services |
| PG/MySQL set an Entra admin child resource; PG `authConfig.passwordAuth: 'Disabled'` | `bicep-patterns-data.md` |
| No F1/D1 App Service SKU, no Free Static Web Apps tier | `sku-matrix.md` |
| â›” No `Microsoft.KeyVault/vaults` resource | No Key Vault is created â€” app-internal secrets are stored on the compute resource â†’ any KV resource is â›” **FLAGGED** | `bicep-patterns-security.md` Â§ No Key Vault |
| App-internal secrets passed as `@secure()` params â†’ App Service `appSettings` / CA native `secrets` | `bicep-patterns-security.md` Â§ Secrets |
| Prereq `warnings[]` each have a corresponding IaC fix | Read [`env-var-secrets.md`](env-var-secrets.md) for SSL/TLS fixes |
| Detected Azure Functions topology preserved | Every Azure Functions component in `context.json.components[]` has a planned Azure Functions service and a `Microsoft.Web/sites` Function App resource. If an SWA frontend is also present, no `buildProperties.apiLocation`, SWA-managed API source copy, or `azureStaticWebApps` backend auth/runtime setting appears. Missing or merged Function App â†’ `FLAGGED`. |
| Container Apps: two-phase ACR wiring, `registries` populated when ACR in plan, port alignment | `bicep-container-apps.md` |
| â›” BuildKit Dockerfile without `Dockerfile.azure` | `hasBuildKitSyntax == true` but no `Dockerfile.azure` in `files[]` â†’ â›” **MANDATORY FAIL**. ACR does not support BuildKit. |
| â›” Role assignment `scope` targets specific resource, not `resourceGroup()` | `rbac-roles.md` |
| â›” No `azure.yaml` in `scaffold-manifest.json.files[]` | `pipeline-rules.md` |
| Non-Azure TF in separate dir | `terraform-patterns.md` |

### Cross-Module Reference Validation

Trace references BETWEEN modules â€” per-file checks miss broken cross-module wiring.

| Check | Rating |
|-------|--------|
| **Param wiring** â€” every `module` call in `main.bicep`: verify every param without `= default` is passed | Missing param â†’ `FLAGGED` |
| **Secret ref completeness** â€” every CA `secretRef` has a matching entry in the CA's native `secrets[]` (name + `value`); â›” NO `keyVaultUrl` / Key Vault | Missing native secret or any `keyVaultUrl` â†’ `FLAGGED` |
| **Output ref validity** â€” every `moduleRef.outputs.X` is declared in the referenced module | Missing output â†’ `FLAGGED` |

### Terraform

| Check | Source |
|-------|--------|
| File structure: `main.tf`, `variables.tf`, `outputs.tf`, `backend.tf`, `modules/` | `mcp_azure_mcp_azureterraformbestpractices` |
| Provider: `azurerm ~> 4.0` | Terraform registry |
| System-assigned managed identity, no `administrator_login`, no Key Vault (`shared_access_key_enabled=false` etc.) | `terraform-patterns.md` |
| Container Apps: two-phase ACR wiring | Same pattern as Bicep |

**Rating:** Matches â†’ `VERIFIED`. Reasonable deviation â†’ `PLAUSIBLE`. Violates â†’ `FLAGGED`.

## Layer 3 â€” Hallucination Detection

Catch fabricated resource types, API versions, SKU names, or properties.

### Bicep (default path)

| Check | How |
|-------|-----|
| API versions, resource types, property names valid | `bicep build` â€” errors = `FLAGGED` |
| â›” Deploy-time value validity (`bicep build` blind spot) | The compiler accepts any schema-valid string, but ARM rejects wrong enum-like values (engine versions, region-restricted SKUs), wrong resource `scope`, and empty resource-ID properties. Any such value the generator chose from memory â€” not traceable to `prepare-plan.json` or a pattern file â€” MUST be confirmed against the provider capabilities API; unconfirmed â†’ `FLAGGED`. |
| SKU names match `prepare-plan.json` | Cross-reference `services[].sku` |
| OpenAI deployment uses `scaleSettings` instead of `sku` | `scaleSettings` deprecated â†’ `FLAGGED` |
| Any resource uses `-preview` API version | Use latest GA from MCP tool â†’ `FLAGGED` |
| VNet subnets as separate child resources | Must be inline in `properties.subnets[]` â†’ `FLAGGED` |

Run `bicep build main.bicep --stdout > /dev/null` as syntax + schema validation. Parse errors = `FLAGGED`.

> â›” **`az bicep build` is MANDATORY for L3.** If unavailable, write `FLAGGED` with "bicep build unavailable."

> â›” **Verify `main.bicep` has `targetScope = 'subscription'`.** Missing â†’ FLAGGED (FIXABLE â€” add targetScope, RG resource with tags, `scope: rg` on modules).

> `mcp_bicep_build_bicep` + `az deployment sub what-if` also appropriate at L3. â›” Do NOT use `az deployment sub validate` (known bug).

**Rating:** Passes `bicep build` â†’ `VERIFIED`. Build warning â†’ `PLAUSIBLE`. Build error â†’ `FLAGGED`.

### Terraform (alternative path)

Run `terraform init -backend=false && terraform validate` + `terraform plan -detailed-exitcode`. Same rating criteria as Bicep.

**Rating:** Passes validate + plan â†’ `VERIFIED`. Plan warning â†’ `PLAUSIBLE`. Validate/plan error â†’ `FLAGGED`.

## Layer 4 â€” WAF Alignment

Per-pillar spot check against [Azure Well-Architected Framework](https://learn.microsoft.com/en-us/azure/well-architected/).

| Pillar | Check |
|--------|-------|
| Reliability | Health probes configured. â›” Verify probe path matches `prereq-output.json.healthEndpoint` â€” non-existent or mismatched path = `FLAGGED`. â›” ACA probes do NOT follow HTTP redirects â€” if app has trailing-slash normalization (Express `redirect`), use path WITHOUT trailing `/` (e.g., `/app` not `/app/`). |
| Security | No public blob access, TLS 1.2+, managed identity |
| Cost | SKU matches budget tier from `prepare-plan.json` |
| Ops | App Insights present + connected for APM, 5 AppOnboard tags on resources, all values parameterized. Present = `VERIFIED`. App Insights absent = `PLAUSIBLE`. â›” `diagnostic-settings` is not part of the plan â€” if the generator added one it MUST be gated (`if (enableDiagnostics)`, default `false`) or absent; wired UNCONDITIONALLY in `main.bicep` = `FLAGGED` (blocks first deploy). |
| Performance | Autoscale rules present for production SKUs |
| Reliability | â›” Env var values compatible with app config validation (Pydantic `Settings`, Django `settings.py`). Typed fields reject wrong formats â†’ `FLAGGED` |

**Rating:** Pillar addressed â†’ `VERIFIED`. Not applicable for SKU â†’ `PLAUSIBLE`. Missing for production SKU â†’ `FLAGGED`.

## Output

Write findings to `scaffold-manifest.json.selfReview.findings[]`: `{ "layer": "L1", "claim": "...", "rating": "FLAGGED", "detail": "..." }` (layer is one of `"L1"`|`"L2"`|`"L3"`|`"L4"`). All `FLAGGED` must be resolved or surfaced at deploy gate. `PLAUSIBLE` = informational.

