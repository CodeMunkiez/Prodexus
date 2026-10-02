<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# Environment Variables & Secrets â€” Cross-Cutting Rules

Applies to ALL compute targets (App Service, Container Apps, Functions). For Container Apps-specific Bicep patterns (secretRef, identity), see [bicep-container-apps.md](bicep-container-apps.md).

## Environment Variable Value Derivation

> â›” **Never invent env var values â€” derive from the app's config class.** Cross-reference `.env.example`, `.env.sample`, `docker-compose.yml`, and the app's config module. Verify each value:
> 1. **Type validation:** URL-typed fields need valid URLs â€” not `*` or placeholders
> 2. **Defaults:** Use app defaults unless overriding with deployed URL
> 3. **Required:** Fields without defaults must be provided
>
> **Pitfalls:** `CORS_ORIGINS=["*"]` â†’ invalid for strict validators (use actual URLs). `DATABASE_URL=changethis` â†’ use a managed-identity connection (no password; wire plain connection params, driver fetches a token). JSON array env vars need Bicep variable escaping:
> ```bicep
> var corsOrigins = '["https://${containerApp.properties.configuration.ingress.fqdn}"]'
> { name: 'CORS_ORIGINS', value: corsOrigins }
> ```

## App-Internal Secret Storage (No Key Vault)

> â›” **No Key Vault is created.** Database/cache/storage access is managed-identity + token â€” there is NO connection-string password or access key to store. App-internal secrets that are NOT an Azure resource credential (e.g. `SECRET_KEY`, JWT signing key, third-party API keys) are stored **directly on the compute resource** from an `@secure()` param generated at deploy time.

**Where app-internal secrets go:**
- **App Service / Functions:** `@secure()` param â†’ `siteConfig.appSettings` (platform-encrypted at rest), or set at deploy via `az webapp config appsettings set`.
- **Container Apps:** `@secure()` param â†’ the CA's **native** `secrets: [{ name, value }]` â†’ `secretRef`. No `keyVaultUrl`. Native secrets have no RBAC dependency, so they need no `isPlaceholder` gating (two-phase wiring is still needed for ACR registries + real image only).

```bicep
// Container Apps â€” native secret (NOT keyVaultUrl)
secrets: [
  { name: 'secret-key', value: secretKey }  // value from an @secure() param
]
```

> â›” **Do NOT hardcode secrets in committed files** â€” not in `main.parameters.json`, `terraform.tfvars`, env vars, or any generated file. (`@secure()` Bicep params ARE the correct way to pass an **app-internal** secret at deploy time â€” the ban is on committing the value, not on the parameter. Databases/caches/storage have NO secret param â€” they use managed identity.) The deploy phase generates each app-internal secret ONCE and passes it as an `@secure()` param â€” see [code-deployment-appservice.md](../../deploy/references/code-deployment-appservice.md) or [code-deployment-container-apps.md](../../deploy/references/code-deployment-container-apps.md).

## Azure Managed Service SSL/TLS Requirements

> â›” **Azure managed databases and caches enforce TLS. Local docker-compose configs typically don't.** This mismatch causes container crashes post-deploy.

Check `prereq-output.json.warnings[]` for warnings with `fixPhase: "scaffold"`. Each warning's `fix` field describes the required IaC change. Read the app's config loader for the actual env var name.

> â›” **Prefer env var override over code change.** Only modify source if no env override path exists AND user approves.
> â›” **Self-review:** If any `fixPhase: "scaffold"` warning exists and IaC lacks the fix â†’ flag as FLAGGED.

## App-Internal Secret Naming

> â›” **Container Apps native secret names allow only lowercase alphanumeric characters and hyphens.** Map env var names: `SECRET_KEY` â†’ `secret-key`, `JWT_SECRET` â†’ `jwt-secret`. Do NOT use underscores or uppercase. (App Service app settings keep the original env var name, e.g. `SECRET_KEY`.)

## Compose â†’ Azure PaaS Credential Mapping (Entra-only)

> â›” **Azure managed databases are provisioned Entra-only â€” there is no username/password to map.** Docker-compose `POSTGRES_USER`/`POSTGRES_PASSWORD` / `MYSQL_USER`/`MYSQL_PASSWORD` are DROPPED, not translated. The app connects with its **managed identity**: the DB username is the app MI's principal name and the "password" is an Entra token fetched at runtime. Map the compose DB **name** (`POSTGRES_DB`/`MYSQL_DATABASE`) to the app DB; do NOT emit any password env var. See [database-post-deploy.md](../../deploy/references/database-post-deploy.md) for granting the MI a DB role.

