<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# IaC Generation Rules â€” Steps 5â€“8

Rules for generating infrastructure code, Dockerfiles, security verification, and telemetry wiring.

## Step 5 â€” Generate IaC

For each service in `services[]`:

> **Sub-agent delegation:** Use [subagent-iac-gen.md](subagent-iac-gen.md) template verbatim. Include full `prepare-plan.json`, `ScaffoldManifest` interface, and compute-target patterns. Self-review (Step 9) remains mandatory after sub-agent returns.

> **PostgreSQL wiring:** Include firewall rule + extension allow-list (see [subagent-iac-gen.md](subagent-iac-gen.md) Step 6 if PostgreSQL in plan). â›” **PostgreSQL config resources** (e.g., `require_secure_transport`) must use `source: 'user-override'` â€” `'system-default'` is read-only and ARM rejects it. â›” **Do NOT create `databases/postgres` child resource** â€” it exists by default and ARM rejects duplicate creation. **BuildKit Dockerfiles:** Generate `Dockerfile.azure` for ACR compatibility â€” see [code-deployment-container-apps.md Â§ BuildKit](../../deploy/references/code-deployment-container-apps.md).

> **Env var completeness:** Read `.env.example` (or `.env.sample`, `config.example`) + config/settings files (Pydantic `Settings`, `@t3-oss/env-nextjs`, Django `settings.py`) for each component to enumerate required env vars before generating IaC. Every env var with a placeholder value (not `localhost`) should map to either: (1) a Bicep parameter, (2) a **managed-identity KV reference** for an app-internal secret (NOT an Azure resource credential), or (3) a value derived from other resources. â›” **Database/cache/storage access is managed-identity + token â€” wire connection *parameters* (host, db name, MI username, `sslmode=require`) as plain app settings, never a connection-string password or access key.** â›” **Container Apps:** KV `secretRef` entries must be gated behind `isPlaceholder` â€” Phase 1 = `secrets: []`, Phase 2 activates KV refs. See [bicep-container-apps.md](bicep-container-apps.md). Flag unmapped vars in selfReview as âš ï¸ WARN. Missing vars cause container crash loops at deploy time.

> â›” **Set `targetScope = 'subscription'` in `main.bicep`.** Subscription-scope Bicep creates the resource group in IaC with all 5 AppOnboard tags (including `created-at`). Do NOT use default resource-group scope â€” it requires imperative `az group create` which consistently misses tags. If the user lacks subscription-level permissions, the deploy phase handles fallback to RG-scope automatically.

> â›” **Native module deploy strategy.** If `prepare-plan.json.deployStrategy` exists, read [bicep-app-service.md Â§ Native Module Deploy Strategy](bicep-app-service.md) and apply the startup command + app settings to the App Service Bicep. The `deployStrategy.startupCommand` goes into `appCommandLine`, and `deployStrategy.requiredAppSettings` goes into `appSettings[]`. When no `deployStrategy` exists, do NOT set `appCommandLine` â€” let Oryx use its default startup.

### Session Tags â€” Mandatory on ALL Resources

â›” Include the 5 AppOnboard tags on every resource and module. See [bicep-patterns.md Â§ tags](bicep-patterns.md) for Bicep code block or [terraform-patterns.md Â§ tags](terraform-patterns.md) for HCL code block.

| Tag key | Value source |
|---------|-------------|
| `app-onboard-skill` | `'true'` |
| `app-onboard-session-id` | `sessionId` param |
| `created-at` | ISO timestamp (scaffold populates, deploy may override) |
| `environment` | `naming.resourcePrefix` |
| `deployed-by` | `context.json.azure.userDisplayName` |

If extending existing IaC, MERGE with existing tags using `union()` / `merge()`.

> â›” **Do NOT generate `azure.yaml`.** Deploy via `az deployment sub create`. See [pipeline-rules.md](../../references/pipeline-rules.md) Â§ azure.yaml prohibition.

### Verification

â›” **API version verification:** Use versions from `apiVersions` input map. If a type is missing from the map, use the latest GA version from reference file examples â€” no `-preview` suffix. `az bicep build` catches invalid versions at compile time.

â›” **Resource property verification:** Training data references deprecated properties. Known traps: `Microsoft.CognitiveServices/accounts/deployments` uses `sku` (name + capacity), NOT `scaleSettings` (deprecated) â€” omit `raiPolicyName`. â›” **Do NOT emit a `Microsoft.KeyVault/vaults` resource at all** (no Key Vault). Fallback: `az bicep build` + `what-if`.

### Output

`infra/main.bicep`, `main.parameters.json`, `modules/{service}.bicep` per service.

### Platform Compatibility

- **Line endings:** Generated `.bicep` files need LF, not CRLF â€” Bicep triple-quoted strings pass content literally to ARM, and `\r` bytes crash `/bin/sh` in containers. The validate subagent runs `mcp_bicep_format_bicep_file` (or `bicep-format_bicep_file`) post-generation to enforce this. If the formatter is unavailable, ensure files use LF manually.
- **Shell compatibility:** Startup scripts in Bicep multiline strings MUST use `set -eu` (POSIX). Do NOT use `set -euo pipefail` â€” Container Apps base images use `/bin/sh` (dash), not bash.
- **Package manager pinning:** When generating or modifying Dockerfiles, pin package manager versions from the project's `packageManager` field (e.g., `pnpm@9.4.0`). Never use `@latest` â€” major version drift breaks builds on older Node.js base images.

### Security Patterns â€” Apply During Generation

â›” Apply ALL patterns from [`bicep-patterns-security.md`](bicep-patterns-security.md) during generation â€” managed identity, SCM/FTP auth policies, on-compute app-internal secrets (NO Key Vault), least-privilege RBAC.

## Step 6b â€” Dockerfile Generation (conditional)

If `prepare-plan.json.services[]` has any entry with `name` containing "Container Apps" AND that component has no existing Dockerfile â†’ â›” **You MUST read [`dockerfile-generation.md`](dockerfile-generation.md)** and generate one. Skip if all Container Apps components already have Dockerfiles, or if no service targets Container Apps.

## Step 7 â€” Secure-by-Default Verification

â›” Read [`bicep-patterns-security.md`](bicep-patterns-security.md) (or `terraform-patterns.md` Â§ Security Defaults) and verify ALL security patterns from Step 5. If deployment includes compute with MI â†” resource RBAC, â›” read [rbac-roles.md](rbac-roles.md) for GUID table. **If a role is NOT in the table, check [Azure built-in roles docs](https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles).** Never guess GUIDs. Skip RBAC for SWA-only.

Tell the user which patterns were applied: managed identity, KV secrets, least-privilege RBAC, SCM/FTP auth policies, private endpoints.

## Step 8 â€” Wire Telemetry

If `prepare-plan.json.instrumentation.appInsightsEnabled` is `true`, add `APPLICATIONINSIGHTS_CONNECTION_STRING` as a plain app setting wired from the App Insights module output. The connection string is not a secret â€” no KV storage needed. For Container Apps: use a plain env var (not `secretRef`).

