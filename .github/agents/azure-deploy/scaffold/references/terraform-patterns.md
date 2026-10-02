<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# Terraform Patterns

Alternative-path patterns for AppOnboard scaffold. Used when `.tf` files detected or user overrides `iacFormat`. Per-resource config comes from `mcp_azure_mcp_azureterraformbestpractices` at runtime â€” this file covers layout, provider, naming, state, tagging, and wiring.

## File Structure

### Default (greenfield or user override)

```
infra/
â”œâ”€â”€ main.tf              # Root module â€” provider, resource group, module calls
â”œâ”€â”€ variables.tf         # Input variables (all configurable values)
â”œâ”€â”€ outputs.tf           # Exported values (endpoints, resource IDs)
â”œâ”€â”€ backend.tf           # State backend config (local default, Azure Storage for prod)
â”œâ”€â”€ terraform.tfvars     # Default variable values (from prepare-plan.json)
â””â”€â”€ modules/
    â”œâ”€â”€ app-service/
    â”‚   â”œâ”€â”€ main.tf
    â”‚   â”œâ”€â”€ variables.tf
    â”‚   â””â”€â”€ outputs.tf
    â”œâ”€â”€ container-app/
    â”œâ”€â”€ sql-database/
    â”œâ”€â”€ log-analytics/
    â””â”€â”€ ...
```

### Non-Azure IaC coexistence (GCP/AWS TF already in repo)

When `detectedInfraProvider.terraform` is `"gcp"`, `"aws"`, or `"multi"` (without `azurerm`), write Azure TF to a **separate directory** from existing non-Azure TF. Never overwrite or modify existing IaC files.

**Output directory rule:** If existing TF is NOT at `infra/`, write to `infra/`. If existing TF IS at `infra/` (or any path containing `infra`), write to `infra-azure/`. Same module structure as default layout.

Each Azure service gets its own module. `main.tf` orchestrates resource group + module calls.

## Provider Configuration

```hcl
terraform {
  required_version = ">= 1.5"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }
}

provider "azurerm" {
  features {}
  subscription_id                 = var.subscription_id
  resource_provider_registrations = "none"
}
```

> â›” Never pin to exact patch versions (e.g., `= 4.1.0`). Use `~> 4.0` to allow minor/patch updates. `azurerm` manages API versions internally â€” if a resource isn't available in `azurerm`, use `azapi_resource` with the latest stable ARM API version.

> **Conditional access (AADSTS530084):** `azurerm` provider re-requests tokens that violate device-binding policies. Fix: (1) switch to Bicep, or (2) use service principal auth (`ARM_CLIENT_ID` + `ARM_CLIENT_SECRET` + `ARM_TENANT_ID`).

## variables.tf

Required variables: `environment_name` (string, default "dev"), `location` (string, default "eastus"), `subscription_id` (string), `session_id` (string), `deployed_by` (string). All configurable values MUST be variables â€” no hardcoded regions, names, or SKUs.

## terraform.tfvars

Populate from `prepare-plan.json`: `environment_name`, `location`, `subscription_id`, `session_id`. See naming-patterns.md for naming convention.

## Backend

Local backend by default: `backend "local" { path = "terraform.tfstate" }`. Recommend Azure Storage backend in `postDeployRecommendations[]` for production.

## Resource Group

Use `rg-${var.project_name}-${var.environment_name}-${random_string.suffix.result}` with `tags = local.tags`. Suffix prevents collisions across AppOnboard sessions.

## Naming Convention

```hcl
resource "random_string" "suffix" {
  length  = 4
  special = false
  upper   = false
}

locals {
  # Pattern: {type}-{appname}-{env}-{suffix}
  app_name    = "app-${var.environment_name}-${random_string.suffix.result}"
  sql_name    = "sql-${var.environment_name}-${random_string.suffix.result}"
  # Storage/ACR: alphanumeric only, no hyphens
  storage_name = "st${replace(var.environment_name, "-", "")}${random_string.suffix.result}"
  acr_name     = "cr${replace(var.environment_name, "-", "")}${random_string.suffix.result}"
}
```

Cross-reference naming with [prepare/references/naming-patterns.md](../../prepare/references/naming-patterns.md) â€” Terraform names must match `prepare-plan.json.naming.resources[]`.

## Resource Tags â€” Mandatory

Apply all 5 AppOnboard tags via `local.tags` â€” see [iac-generation-rules.md Â§ Session Tags](iac-generation-rules.md) for tag names and values.

```hcl
locals {
  tags = {
    "app-onboard-skill"       = "true"
    "app-onboard-session-id"  = var.session_id
    "created-at"      = timestamp()
    "environment"     = var.environment_name
    "deployed-by"     = var.deployed_by
  }
}
```

> âš ï¸ `timestamp()` changes on every plan. Add `lifecycle { ignore_changes = [tags["created-at"]] }` on every resource.

## Secrets â€” App-Internal Only, Stored On-Compute (No Key Vault)

Use `random_password` ONLY for app-internal secrets that are NOT an Azure resource credential (e.g. a Django `SECRET_KEY`, JWT signing key), and store the value **directly on the compute resource** as an app setting / container secret. â›” **No Key Vault.** Azure resource auth (database, cache, storage, Cosmos) is **managed-identity + token â€” never a generated password**.

```hcl
resource "random_password" "app_secret_key" {
  length  = 50
  special = true
  lifecycle { ignore_changes = [result] }
}

# App Service â€” store directly as an app setting (no Key Vault):
resource "azurerm_linux_web_app" "app" {
  # ...
  app_settings = {
    SECRET_KEY = random_password.app_secret_key.result
  }
}
# Container Apps â€” use a native `secret { name, value }` block + `env { secret_name = ... }`.
```

> â›” NEVER use `random_string` for secrets â€” it is not marked `sensitive` in state. Always use `random_password`.
> â›” **NEVER generate a database/cache/storage password or access key** (no `administrator_login_password`, no `random_password` for a DB). Those services use `azuread_authentication_only`/`password_auth_enabled = false`/`shared_access_key_enabled = false` + managed identity. See [bicep-patterns-security.md](bicep-patterns-security.md) Â§ Data Services.

## Container Apps â€” Two-Phase Wiring

Same circular dependency as Bicep â€” see [bicep-container-apps.md](bicep-container-apps.md). Phase 1: placeholder image, no ACR refs. Phase 2: build + push, assign AcrPull, update via `az containerapp update --image` outside Terraform.

```hcl
# Phase 1: Placeholder image
resource "azurerm_container_app" "app" {
  template {
    container {
      name   = "app"
      image  = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
      cpu    = 0.25
      memory = "0.5Gi"
    }
  }
  identity {
    type = "SystemAssigned"
  }
}
```

## outputs.tf

Export: `resource_group_name`, `app_url` (https://${hostname}), `resource_ids` (list of all deployed resource IDs for deploy-result.json).

## Security Defaults

Apply same security rules as Bicep â€” see [bicep-patterns-security.md](bicep-patterns-security.md). Terraform-specific syntax:

| Rule | Terraform HCL |
|------|---------------|
| Managed identity (MANDATORY â€” all compute) | `identity { type = "SystemAssigned" }` |
| â›” No DB admin password (SQL/PG/MySQL) | SQL: `azuread_authentication_only = true`; PostgreSQL: `authentication { password_auth_enabled = false, active_directory_auth_enabled = true, tenant_id = ... }` + `azurerm_postgresql_flexible_server_active_directory_administrator`; MySQL: `azurerm_mysql_flexible_server_active_directory_administrator` + `aad_auth_only`. NEVER `administrator_login_password`. |
| â›” No storage/cache keys | Storage: `shared_access_key_enabled = false`; Redis: `azurerm_redis_cache` with `access_keys_authentication_enabled = false` + `azurerm_redis_cache_access_policy_assignment` for the app MI |
| â›” No Key Vault | Do NOT create `azurerm_key_vault` / `azurerm_key_vault_secret`. App-internal secrets go directly into `app_settings` (App Service) or a native container `secret` block |
| App-internal secret (on-compute) | `app_settings = { SECRET_KEY = random_password.app_secret_key.result }` â€” no `@Microsoft.KeyVault(...)` |
| HTTPS only | `https_only = true`, `minimum_tls_version = "1.2"` |
| Storage | `https_traffic_only_enabled = true`, `allow_nested_items_to_be_public = false`, `min_tls_version = "TLS1_2"` |
| â›” Cosmos DB RBAC | `azurerm_cosmosdb_sql_role_assignment`, NOT `azurerm_role_assignment` + `local_authentication_disabled = true` â€” see [rbac-roles.md](rbac-roles.md) |
| RBAC assignments | `principal_type = "ServicePrincipal"` REQUIRED â€” see [rbac-roles.md](rbac-roles.md) |
| SCM/FTP auth | `scm.allow: true` (scaffold), `ftp.allow: false` (always) â€” use `azapi_resource` |

