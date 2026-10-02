<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

ï»¿Dependency compatibility checks for Azure. Part of the [deployability check](deployability-check.md).

## EOL / Unsupported Runtimes

| Verdict | Condition |
|---------|----------|
| ðŸ”¶ Major Migration | .NET Framework 4.x, ASP.NET Core 2.1, Python 2.x |
| âŒ FAIL or ðŸ”¶ | Node.js < 18, Java < 11 â€” config-only upgrade â†’ âŒ FAIL (fixable), API changes needed â†’ ðŸ”¶ |

> **Quick test:** ONE config value change, no import changes? â†’ âŒ FAIL. Otherwise â†’ ðŸ”¶.

**Ecosystem-era check (Python):** ALL pinned deps pre-2018, no Python 3.10+ wheels â†’ âŒ FAIL. Signals: `flask_script`, `werkzeug<1.0`, `itsdangerous<1.0`, imports from `werkzeug.contrib.*` / `flask.ext.*`.

## EOL / Unmaintained Frameworks

| Verdict | Condition |
|---------|-----------|
| ðŸ”¶ Major Migration | Flask < 2.0, Django < 3.2, Express < 4.0, Rails < 6.0, Spring Boot < 2.7 |
| âš ï¸ WARN | React < 16, Angular < 14, Next.js < 13 |

## Archived / Abandoned Repositories

| Signal | Verdict |
|--------|--------|
| `archived: true` + EOL stack | ðŸ”¶ Major Migration |
| `archived: true` (current runtime) | âš ï¸ WARN |
| README "deprecated"/"unmaintained" + EOL | ðŸ”¶ Major Migration |
| README "deprecated"/"unmaintained" (current) | âš ï¸ WARN |

> **Remediation scope:** Fixing all blockers requires major upgrade OR >5 files â†’ ðŸ”¶ Major Migration.

## Intentionally Vulnerable Applications

Detect via **code structure first**, metadata second. These apps are designed to be exploited â€” vulnerability IS the product.

**Code signals (check first):**
- Directory `vulnerabilities/` with subdirs like `sqli/`, `xss/`, `csrf/`, `fi/` (file inclusion)
- Security-level config toggling vulnerability severity (`default_security_level`, `security.level`, `difficulty`)
- Source files with intentionally unsanitized `$_GET`/`$_POST`/`$_REQUEST` passed to SQL queries, shell commands (`exec`, `shell_exec`, `system `), or `eval`/`include` -- that are systematic across 3+ files (not a single bug)
- `hackable/`/`exploit/` directories, or files with names like `command_injection.php`, `brute_force.php`

**Metadata signals (secondary check):**
- Project/package description or repo About containing: "deliberately vulnerable", "intentionally insecure", "vulnerable by design", "security training", "penetration testing", "do not deploy to production"
- README/License warning against internet-facing deployment

**Verdict:** â‰¥2 code signals OR 1 code + 1 metadata â†’ ðŸ›‘ HALT. Single metadata only â†’ âš ï¸ WARN (could be a disclosure).

> â›” **ðŸ›‘ HALT is a verdict, not an exit.** On ðŸ›‘ HALT your NEXT action MUST be the Step-4 write â€” persist all 3 artifacts (`prereq-output.json`, `context.json`, `readiness-report.md`) with `overallHealth: "blocked"` via the `create` tool, then read them back, BEFORE printing any halt summary to the user. A halt message with no `prereq-output.json` on disk is a failure â€” the deterministic `blocked` verdict must be persisted first, because the halt message can end the turn.

## Non-Azure Cloud SDK Dependencies

Functional cloud SDK deps â†’ ðŸ”¶ blockers; classification and observability carve-out in [cloud-sdk-migration.md](cloud-sdk-migration.md). The redirect gate (instructions.md Step 2) and the deploy-blocking stop (instructions.md Step 8 Row 2) own all routing â€” no `routeToSkill` decision happens here.

## Platform-Specific Dependencies

| Dependency Type | Verdict |
|----------------|---------|
| Native OS binaries (`.so`, `.dll`) | âš ï¸ WARN |
| Native Node.js addons on free/low-tier SKUs | âš ï¸ WARN |
| GPU-required libraries (CUDA) | âš ï¸ WARN |
| Local file system writes, file-based DBs | âš ï¸ WARN â€” ephemeral on PaaS |
| BuildKit Dockerfile syntax (`--mount`, `# syntax=`) | âš ï¸ WARN `W-BUILDKIT` â€” set `buildRequirements.hasBuildKitSyntax: true`. **ACR `az acr build` does NOT support BuildKit** â€” scaffold must generate `Dockerfile.azure`. **fix:** "Generate ACR-compatible Dockerfile.azure" **fixPhase:** `scaffold` |
| Jib container build (no Dockerfile) | âš ï¸ WARN â€” note Jib path for scaffold |
| Redis client without TLS config | âš ï¸ WARN `W-REDIS-TLS` â€” **fix:** "Add TLS config" **fixPhase:** `prereq`. **Config key registration:** if the app uses a config library that requires keys to be pre-registered before env var override (Go/Viper `Unmarshal()`, Spring `@ConfigurationProperties`), the config file must also declare the TLS key (e.g., add `tlsEnabled: false` to YAML) â€” otherwise the env var is silently ignored. Detection: grep for `viper.Unmarshal`, `mapstructure`, `@ConfigurationProperties`. |
| PostgreSQL client with SSL disabled | âš ï¸ WARN `W-PG-SSL` â€” **fix:** "Set SSL mode env var" **fixPhase:** `scaffold` |
| MySQL client without TLS config | âš ï¸ WARN `W-MYSQL-SSL` â€” **fix:** "Enable client TLS for Azure MySQL" **fixPhase:** `prereq`. Most MySQL drivers/ORMs need an in-code SSL option (no SSL env var like Postgres has), so this is a client-config change â†’ prereq remediation batch (like `W-REDIS-TLS`), not IaC-only scaffold. Detection: MySQL in the plan (`mysql:*` in compose, or a MySQL driver/ORM â€” e.g. `mysql2`, `sequelize` dialect mysql, `typeorm`, `prisma`, `knex`) with no SSL/TLS option in the client config. |
| Go Viper without env key replacer | âš ï¸ WARN `W-VIPER-ENV` â€” **fix:** "Add SetEnvKeyReplacer call" **fixPhase:** `prereq` |
| Licensed/proprietary SDKs | âš ï¸ WARN |

## Hardcoded Localhost URLs

| Pattern | Verdict | ID | fixPhase |
|---|---|---|---|
| `localhost`, `127.0.0.1`, `0.0.0.0` in API base URLs, CORS origins, webhook URLs, service discovery | âš ï¸ WARN | `W-LOCALHOST-URL` | `scaffold` |

> **Exclude:** database connection strings, dev-only files, test files.

## Dockerfile Analysis

| Pattern | Verdict | ID | fixPhase |
|---|---|---|---|
| `CMD.*uv run` / `ENTRYPOINT.*uv run` | âš ï¸ WARN | `W-UV-RUN` | `prereq` |
| `CMD.*poetry run` / `ENTRYPOINT.*poetry run` | âš ï¸ WARN | `W-POETRY-RUN` | `prereq` |

> Also check `docker-compose.yml` `command:` overrides.

## Native Module Detection

Static lockfile analysis only.

| Language | Signal | Where |
|----------|--------|-------|
| Node.js | `"node-gyp"` in lockfile | `package-lock.json`, `yarn.lock`, `pnpm-lock.yaml` |
| Python | `numpy`, `pandas`, `grpcio`, `Pillow`, `bcrypt`, `psycopg2` (not `-binary`), `cryptography`, `lxml`, `scipy`, `scikit-learn` | `requirements.txt`, `pyproject.toml` |
| .NET | `[DllImport]`, `NativeLibrary.Load` | `.cs` files |
| Go | `import "C"` (cgo) | `.go` files |
| PHP | `ext-*` requirements | `composer.json` |

> â›” **Lockfile grep is the ONLY valid evidence.** Do NOT set `hasNativeModules` from package name alone. `prebuild-install` without `node-gyp` = prebuilt â†’ `hasNativeModules: false`. Key: `bcrypt` âœ… native, `bcryptjs` âŒ JS. `psycopg2` âœ…, `psycopg2-binary` âŒ. `sharp` v0.33+ âŒ prebuilt. `canvas` âœ… always. No lockfile â†’ `"unknown"`, âš ï¸ WARN.

When `hasNativeModules: true`: `f1Viable: false`, `f1BlockReason: "native modules ({signal})"`. (F1/D1/Free are never offered regardless â€” see below.)

## SKU Sizing Signals â€” Beyond Native Modules

> â›” **F1/D1/Free are never selected** (managed identity is mandatory and the free-tier MI sidecar OOMs). The floor is always **B1**. `f1Viable` is retained for back-compat but is effectively always `false`; treat these signals as "size **up** from B1" (B2/S1), never as "drop to free".

| Condition | f1BlockReason / sizing note |
|-----------|---------------|
| Large dep tree (Python >10, Node lockfile >500KB, .NET >20 NuGet) | `"large dependency tree"` â€” consider B2 |
| Build-time compilation (`tsconfig.json` + `"build"` script) | `"build-time compilation"` |
| WSGI/ASGI server (`gunicorn`/`uvicorn`/`daphne`) | `"WSGI/ASGI server"` |
| ðŸ”¶ Major Migration (>5 files) | `"major migration"` |

Prepare always selects **B1 (~$13/mo) minimum**; the signals above only push sizing higher.

## First-Run Initialization

Detect init steps needed before first HTTP request â€” run automatically in dev but cause 500s on Azure.

| Framework | Signal | Init command |
|-----------|--------|-------------|
| Flask-Migrate/Alembic | `flask_migrate`/`alembic` + `migrations/` | `flask db upgrade` |
| Django | `django` + `manage.py` + `*/migrations/` | `python manage.py migrate` |
| Prisma | `@prisma/client` + `prisma/schema.prisma` | `npx prisma migrate deploy` |
| TypeORM | `typeorm` + `migrations/`/`ormconfig` | `npx typeorm migration:run` |
| Sequelize | `sequelize` + `migrations/` | `npx sequelize-cli db:migrate` |
| EF Core | `EntityFrameworkCore.Tools` in csproj | `dotnet ef database update` |

**Seed/bootstrap:** Flask `@app.cli.command('deploy')` â†’ `flask deploy`. Django `fixtures/` â†’ `manage.py loaddata`. Seed commands: `required: false`.

Migration framework + `migrations/` dir â†’ âš ï¸ WARN, write to `prereq-output.json.initCommands[]` (schema in [`prereq-schemas.ts`](prereq-schemas.ts)). ORM without `migrations/` dir â†’ âœ… PASS. Prepare prepends required `initCommands` to `deployStrategy.startupCommand`; scaffold encodes in `appCommandLine`. Migrations are idempotent â€” safe on every cold start.

## Database & Storage

| Found | Assessment |
|-------|-----------|
| File-based DB (SQLite, LevelDB, DuckDB) | âš ï¸ WARN â€” ephemeral on PaaS. Deploy as-is, suggest managed DB in `postDeployRecommendations[]` |
| Local file storage | Needs Azure Blob Storage |
| In-memory cache only | Consider Redis |
| Managed DB connection string | Ready â€” update connection |

