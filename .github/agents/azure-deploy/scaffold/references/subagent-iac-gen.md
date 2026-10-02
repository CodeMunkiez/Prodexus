<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# Subagent Template â€” IaC Generation (Steps 5â€“8)

Generate deployment-ready IaC from `prepare-plan.json`. Follow the workflow below â€” each step specifies which reference to read and what to do with it.

## Critical Rules

- â›” **Do NOT invoke any other agents or hand off** â€” no external agent calls of any kind. Use the procedures in THIS file only.
- â›” **Do NOT generate `azure.yaml`**
- â›” **Do NOT modify app source code** â€” only write files under `infra/` (and `Dockerfile.azure` if needed)
- â›” **Do NOT run app build/test/lint commands** (`npm test`, `npm run build`, `pnpm build`, `python -m pytest`, `dotnet build`, etc.). Only validate generated IaC via `az bicep build`.

## Input (provided by caller)

| Field | Source | Required |
|-------|--------|----------|
| `prepare-plan.json` content | Session folder â€” services, naming, quotas, cost, deploymentVariables | YES (verbatim) |
| `context.json.overrides` | `iacFormat`, `detectedInfraProvider` | YES |
| `buildRequirements` | From `prereq-output.json` â€” runtime, deps, Dockerfiles | YES |
| `warnings[]` | From `prereq-output.json` â€” prereq warnings requiring IaC fixes (env var overrides, config changes). Applied during Steps 3â€“4. | YES |
| Compute targets | App Service/Functions, Container Apps, or both + whether PostgreSQL/Redis present | YES |
| `apiVersions` | Map of `resourceType â†’ latestGAVersion` from main-thread MCP lookup. Use these versions in generated Bicep â€” do NOT use versions from training data. If `"MCP unavailable"` â€” see Step 1 for fallback. | YES |

## Output

| Artifact | Location |
|----------|----------|
| `infra/main.bicep` (or `main.tf`) | Workspace `infra/` |
| `infra/main.parameters.json` (or `variables.tf`) | Workspace `infra/` |
| `infra/modules/{service}.bicep` per service | Workspace `infra/modules/` |
| File list | Return to caller for `scaffold-manifest.json.files[]` |

## Workflow

### Step 1 â€” Read skeleton + tag patterns

Read [bicep-patterns.md](bicep-patterns.md) (Bicep) OR [terraform-patterns.md](terraform-patterns.md) (Terraform) â€” NOT both.

**Do:** Extract the `main.bicep` skeleton structure (targetScope, parameters, variables, resource group, module calls). Extract the 5-tag block definition. Use `prepare-plan.json.naming` for all resource names â€” never derive names with `take()`, `substring()`, `uniqueString()`, or string manipulation. The 4-char session suffix in the plan names already provides uniqueness. â›” For each `resource 'Type@Version'` declaration, use the version from `apiVersions` input. If type missing from map, use version from reference file examples.

> â›” **If `apiVersions` is `"MCP unavailable"` or missing a resource type:** run `az provider show --namespace {ns} --query "resourceTypes[?resourceType=='{type}'].apiVersions[?!contains(@, 'preview')] | [0][0]" -o tsv` for each missing provider â€” this filters to GA-only and picks the latest. NEVER fall back to training data â€” hallucinated API versions cause multiple deploy healing cycles.

### Step 2 â€” Read compute-target patterns

Read ONLY the compute-target reference(s) matching the plan, if the plan has multiple compute targets, read each matching file.:
- If plan has App Service/Functions â†’ read [bicep-app-service.md](bicep-app-service.md).
- If plan has Container Apps â†’ read [bicep-container-apps.md](bicep-container-apps.md).
- If plan has Static Web Apps â†’ read [bicep-swa.md](bicep-swa.md).
- If plan has BOTH â†’ read both.

**Do:** Generate compute module(s) using the patterns from each reference file. F1/D1 App Service: do NOT generate Dockerfile, do NOT add managed identity (OOM). App Service health probe: if `prereq-output.json.healthEndpoint` is non-null, set `siteConfig.healthCheckPath` to that value; otherwise omit (do NOT default to `/`).

### Step 3 â€” Read security patterns

â›” **You MUST read [bicep-patterns-security.md](bicep-patterns-security.md).** It contains Key Vault config, managed identity, HTTPS/TLS, and credential hygiene rules. Apply to every generated module.

### Step 4 â€” Read generation rules

Read [iac-generation-rules.md](iac-generation-rules.md).

**Do:** Apply ALL rules from the reference file to every generated module. The file contains mandatory tag definitions, naming constraints, security patterns, env var completeness checks, and Dockerfile generation rules. Do NOT skip any section â€” every rule applies.

### Step 5 â€” Read env var + secrets wiring

Read [env-var-secrets.md](env-var-secrets.md).

**Do:** For each component in the plan, read `.env.example` (or `.env.sample`, config files like Pydantic `Settings`, Django `settings.py`) from the workspace. Map every env var to either: (1) a Bicep parameter, (2) an **`@secure()` param stored on the compute resource** (App Service `appSettings` / Container Apps native `secrets`) for app-internal secrets that are NOT Azure resource credentials (e.g. `SECRET_KEY`, JWT key), or (3) a value derived from other resources. â›” **No Key Vault.** â›” **Database/cache/storage access is managed-identity + token â€” never a connection-string password or access key.** Wire DB/cache **connection parameters** (host, db name, MI username, `sslmode=require`) as plain `appSettings`/`env` values. See [bicep-patterns-security.md](bicep-patterns-security.md) Â§ Data Services and Â§ No Key Vault.

### Step 6 â€” Generate data modules (if needed)

ONLY if PostgreSQL, MySQL, or Redis is in the plan. Skip if none are present.

**PostgreSQL Flexible Server module** â€” **Entra-only** (`authConfig: { activeDirectoryAuth: 'Enabled', passwordAuth: 'Disabled', tenantId }`). NO `administratorLogin`/`administratorLoginPassword`. Add an `administrators` child resource setting the **deploying principal** as Entra admin (pass `entraAdminObjectId`/`entraAdminName`/`entraAdminType`). Include the `AllowAllAzureServicesAndResourcesWithinAzureIps` (`0.0.0.0`) firewall rule, extension allow-list (`azure.extensions` config: `uuid-ossp,pgcrypto,pg_trgm`), storage config (default 32 GB). Set the server `version` from `prepare-plan.json.services[].version` (capabilities-verified) â€” do NOT hardcode or guess. The app MI is granted a DB role post-deploy (see [database-post-deploy.md](../../deploy/references/database-post-deploy.md)).

**MySQL Flexible Server module** â€” **Entra-only**: a **user-assigned managed identity (REQUIRED for MySQL AAD auth)** + `identity: { type: 'UserAssigned', â€¦ }` on the server + an `administrators` child resource (deploying principal) with `identityResourceId` set to that UAMI (â›” `null` fails to deploy) + `aad_auth_only: ON` config. NO password params. Emit a `postDeployRecommendation` to grant the UAMI **Directory Readers** in Entra. Include the MySQL-only deltas: `require_secure_transport: ON` config, the server `version` from `prepare-plan.json.services[].version` (ARM rejects major-only strings like `'8.0'` â€” needs an exact patch such as `'8.0.21'`), and a `flexibleServers/databases` child resource for the compose-declared DB name (e.g. `MYSQLDB_DATABASE`) so the app's schema DB exists in IaC before the container starts. See [bicep-patterns-data.md Â§ MySQL Flexible Server Module](bicep-patterns-data.md).

**Redis Cache module** â€” Basic SKU, `enableNonSslPort: false`, `minimumTlsVersion: '1.2'`, **`disableAccessKeyAuthentication: true`** + `redisConfiguration: { 'aad-enabled': 'true' }` + an `accessPolicyAssignments` child granting the app MI Data Owner. â›” **No access key in Key Vault.** â›” Known Bicep type issue: `sku` property may cause BCP035/BCP187 warnings â€” these are false positives. If deploy fails with `InvalidRequestBody` for `properties.sku.name`, create via `az redis create --sku Basic --vm-size c0` then reference with `existing` keyword in Bicep.

Wire DB/cache **connection parameters** (host, db name, MI username, SSL required) as plain `appSettings`/`env` values â€” NOT secrets. The driver fetches an Entra token at runtime. App-internal (non-Azure) secrets are stored on the compute resource via an `@secure()` param (App Service `appSettings` / Container Apps native `secrets`) â€” â›” **no Key Vault**.

### Step 7 â€” Generate all files

> â›” **Before writing ANY file, verify:** (1) â›” **NO `Microsoft.KeyVault/vaults` resource anywhere** â€” app-internal secrets live on the compute resource. (2) No secrets in module outputs â€” `@secure()` params flow in at deploy, never out. (3) API versions from `apiVersions` input, not memory. (4) Container Apps: no `revisionSuffix`, placeholder image as default, `isPlaceholder` conditionals on registries only (native secrets need no gating).

**Do:** Create the `infra/` directory and write all files:
1. `infra/bicepconfig.json` â€” write `{ "formatting": { "newlineKind": "LF" } }` if it doesn't already exist (user's repo may have one). LF is critical because Bicep triple-quoted strings pass content literally to ARM, and `\r` bytes crash `/bin/sh` in containers.
2. `infra/main.bicep` â€” subscription scope, RG creation with tags, module calls for all services + `role-assignments` module (AcrPull + app-to-resource data-plane RBAC â€” NO Key Vault roles), all unconditional.
3. `infra/main.parameters.json` â€” ARM JSON format (NOT `.bicepparam`). Include `environmentName`, `location`, `sessionId`, `deployedBy`, `createdAt`, and `entraAdminObjectId`/`entraAdminName` (Entra DB admin = deployer). â›” **`createdAt` value:** run `Get-Date -Format "o"` in terminal to get the current ISO 8601 timestamp â€” NEVER use a hardcoded or placeholder date. Do NOT include `@secure()` app-internal secret params (passed at deploy time). Deploy phase passes the deployer object id via `az ad signed-in-user show --query id -o tsv`.
4. `infra/modules/{service}.bicep` â€” one module per service from the plan, PLUS `role-assignments.bicep` (AcrPull for Container Apps â†’ ACR, and app-MI â†’ resource data-plane roles per [rbac-roles.md](rbac-roles.md)). â›” **No Key Vault module and no Key Vault role assignments.**
5. If `buildRequirements.hasBuildKitSyntax == true`: â›” create `{component}/Dockerfile.azure` per [dockerfile-generation.md Â§ ACR Build Compatibility](dockerfile-generation.md).
6. If Container Apps and component has NO Dockerfile: read [dockerfile-generation.md](dockerfile-generation.md) and generate one. Follow the layer ordering, port alignment, and security defaults from that reference â€” do NOT generate from memory.

> â›” **Health probes for Container Apps:** Probe path priority: (1) `prereq-output.json.healthEndpoint` if non-null, (2) first detected GET route from the app, (3) `/` only if the app has a root handler. Do NOT default to `/` for APIs that only serve sub-paths â€” returns 404, blocks activation. For DB apps: use `/healthz` not `/readyz` (DB not wired in Phase 1).

### Step 8 â€” Validate syntax

**Do:** Run `az bicep build --file infra/main.bicep --stdout > $null`. If errors, fix and re-run (max 2 attempts). Do NOT use the `azure-validate` agent.

### Step 9 â€” Return results

**Do:** Return the list of generated files and any validation notes to the caller. Keep status report â‰¤1500 tokens.

