<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# Project Architecture

> Testable Azure-centric project structure.

---

## Core Principles

1. **Service boundary isolation** â€” Put every Azure service behind dedicated module + interface. Never scatter SDK calls across handlers.
2. **Dependency injection** â€” Inject services. Handlers receive deps, never import singletons. Swap real services for mocks in tests.
3. **Environment-driven config** â€” Use same code for mocks, emulators, Azure; switch via env vars.
4. **Monorepo by default** â€” Frontend, backend, shared types in one repo with clear boundaries.
5. **Contracts first** â€” Put shared types/schemas in `shared/`; define API contracts before implementation.
6. **One function per file** â€” Match file and function names; test each independently.
7. **Tests next to source** â€” Test directory mirrors source structure.

---

## Canonical Project Structures

> **ðŸ“ Naming the service folders.** The folder names in the trees below (`functions`, `web`) are **role placeholders**. When the project has a clear product name, **prefer domain-specific names for the deployable apps** â€” derive a kebab-case slug from the product name and add a role suffix:
>
> - **Functions backend** â†’ `<project>-api` (e.g. `office-compliance-api`)
> - **Frontend** â†’ `<project>-<type>`, where `<type>` fits the app â€” `-portal`, `-app`, or `-web` (e.g. `office-compliance-portal`)
> - **Shared package** â†’ keep the generic `shared/` (it is internal, never a deployed app)
>
> This is a **SHOULD**, not a mandate. Use generic `functions`/`web` only without a clear project name (e.g. generic internal tooling) or when following existing workspace structure. Apply names **consistently everywhere**: npm `workspaces`, `cd` commands, tsconfig `rootDir`, computed `main` field (e.g. with `rootDir: ".."`, `<project>-api` handlers compile to `dist/<project>-api/src/functions/*.js`). Shared package imports stay `../shared/...`. Plan's Project Structure is source of truth; trees below use generic names only as examples.

> **ðŸ“ Naming the service folders.** The folder names in the trees below (`functions`, `web`) are **role placeholders**. When the project has a clear product name, **prefer domain-specific names for the deployable apps** â€” derive a kebab-case slug from the product name and add a role suffix:
>
> - **Functions backend** â†’ `<project>-api` (e.g. `office-compliance-api`)
> - **Frontend** â†’ `<project>-<type>`, where `<type>` fits the app â€” `-portal`, `-app`, or `-web` (e.g. `office-compliance-portal`)
> - **Shared package** â†’ keep the generic `shared/` (it is internal, never a deployed app)
>
> This is a **SHOULD**, not a mandate. Use generic `functions`/`web` only without a clear project name (e.g. generic internal tooling) or when following existing workspace structure. Apply names **consistently everywhere**: npm `workspaces`, `cd` commands, tsconfig `rootDir`, computed `main` field (e.g. with `rootDir: ".."`, `<project>-api` handlers compile to `dist/<project>-api/src/functions/*.js`). Shared package imports stay `../shared/...`. Plan's Project Structure is source of truth; trees below use generic names only as examples.

### TypeScript â€” SPA + Azure Functions

```
project-root/
â”œâ”€â”€ .azure/
â”‚   â””â”€â”€ project-plan.md             â† Project plan (source of truth)
â”œâ”€â”€ .env.example                    â† Connection string template (checked in)
â”œâ”€â”€ .env                            â† Actual values (gitignored)
â”œâ”€â”€ .gitignore
â”œâ”€â”€ package.json                    â† Root workspace config
â”œâ”€â”€ services/
â”‚   â”œâ”€â”€ functions/                  â† Azure Functions project
â”‚   â”‚   â”œâ”€â”€ host.json
â”‚   â”‚   â”œâ”€â”€ local.settings.json     â† Functions env config (gitignored)
â”‚   â”‚   â”œâ”€â”€ package.json            â† Backend dependencies
â”‚   â”‚   â”œâ”€â”€ tsconfig.json
â”‚   â”‚   â”œâ”€â”€ vitest.config.ts        â† Test runner config
â”‚   â”‚   â”œâ”€â”€ openapi.yaml            â† API contract
â”‚   â”‚   â”œâ”€â”€ src/
â”‚   â”‚   â”‚   â”œâ”€â”€ functions/          â† Function handlers (one per file)
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ getItems.ts
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ createItem.ts
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ getItemById.ts
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ updateItem.ts
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ deleteItem.ts
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ health.ts
â”‚   â”‚   â”‚   â”‚   â””â”€â”€ openapi.ts
â”‚   â”‚   â”‚   â”œâ”€â”€ services/           â† Service abstraction layer
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ interfaces/
â”‚   â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ IStorageService.ts
â”‚   â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ IDatabaseService.ts
â”‚   â”‚   â”‚   â”‚   â”‚   â””â”€â”€ ICacheService.ts
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ storage.ts
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ database.ts
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ cache.ts
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ config.ts       â† Config loader + env validation
â”‚   â”‚   â”‚   â”‚   â””â”€â”€ registry.ts     â† Service factory / DI registry
â”‚   â”‚   â”‚   â”œâ”€â”€ errors/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ AppError.ts     â† Base error class
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ errorTypes.ts   â† NotFoundError, ValidationError, etc.
â”‚   â”‚   â”‚   â”‚   â””â”€â”€ errorHandler.ts â† Global error handler
â”‚   â”‚   â”‚   â”œâ”€â”€ middleware/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ requestLogger.ts
â”‚   â”‚   â”‚   â”‚   â””â”€â”€ validateRequest.ts
â”‚   â”‚   â”‚   â””â”€â”€ logger.ts           â† Structured logger (pino)
â”‚   â”‚   â”œâ”€â”€ tests/
â”‚   â”‚   â”‚   â”œâ”€â”€ fixtures/           â† Mock data (JSON files)
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ items.json
â”‚   â”‚   â”‚   â”‚   â””â”€â”€ users.json
â”‚   â”‚   â”‚   â”œâ”€â”€ mocks/              â† Mock service implementations
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ mockStorage.ts
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ mockDatabase.ts
â”‚   â”‚   â”‚   â”‚   â””â”€â”€ mockCache.ts
â”‚   â”‚   â”‚   â”œâ”€â”€ services/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ config.test.ts
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ storage.test.ts
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ database.test.ts
â”‚   â”‚   â”‚   â”‚   â””â”€â”€ registry.test.ts
â”‚   â”‚   â”‚   â”œâ”€â”€ functions/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ getItems.test.ts
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ createItem.test.ts
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ getItemById.test.ts
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ health.test.ts
â”‚   â”‚   â”‚   â”‚   â””â”€â”€ openapi.test.ts
â”‚   â”‚   â”‚   â”œâ”€â”€ errors/
â”‚   â”‚   â”‚   â”‚   â””â”€â”€ errorHandler.test.ts
â”‚   â”‚   â”‚   â””â”€â”€ validation/
â”‚   â”‚   â”‚       â””â”€â”€ itemSchema.test.ts
â”‚   â”œâ”€â”€ web/                        â† Frontend application
â”‚   â”‚   â”œâ”€â”€ package.json
â”‚   â”‚   â”œâ”€â”€ tsconfig.json
â”‚   â”‚   â”œâ”€â”€ vite.config.ts          â† Dev proxy to Functions
â”‚   â”‚   â”œâ”€â”€ index.html
â”‚   â”‚   â””â”€â”€ src/
â”‚   â”‚       â”œâ”€â”€ App.tsx
â”‚   â”‚       â”œâ”€â”€ main.tsx
â”‚   â”‚       â”œâ”€â”€ api/
â”‚   â”‚       â”‚   â””â”€â”€ client.ts       â† Typed API client
â”‚   â”‚       â”œâ”€â”€ components/
â”‚   â”‚       â”œâ”€â”€ pages/
â”‚   â”‚       â””â”€â”€ hooks/
â”‚   â””â”€â”€ shared/                     â† Shared types and schemas
â”‚       â”œâ”€â”€ package.json
â”‚       â”œâ”€â”€ types/
â”‚       â”‚   â”œâ”€â”€ index.ts
â”‚       â”‚   â”œâ”€â”€ entities.ts         â† Entity types (shared FE + BE)
â”‚       â”‚   â””â”€â”€ api.ts              â† Response contracts + ErrorCode union
â”‚       â””â”€â”€ schemas/
â”‚           â””â”€â”€ validation.ts       â† Zod schemas + inferred request types
â””â”€â”€ data/                           â† Docker volume mounts (gitignored)
```

### Shared Types â€” Single Source of Truth for Request Types

> âš ï¸ **CRITICAL: Do NOT define request types in BOTH `types/api.ts` AND `schemas/validation.ts`.** Causes duplicate export errors.
>
> With Zod, `z.infer<typeof schema>` types ARE canonical request types:
>
> | File | Contains | Does NOT contain |
> |------|----------|-----------------|
> | `types/entities.ts` | Entity interfaces (User, Photo, etc.) | â€” |
> | `types/api.ts` | Response types, `ErrorCode` union, `ErrorResponse` | Request types (LoginRequest, etc.) |
> | `schemas/validation.ts` | Zod schemas + `z.infer` request types | Response types |
> | `index.ts` | `export * from` all three files | â€” |
>
> Thus `export * from './types/api.js'` and `export * from './schemas/validation.js'` never export the same name.

### TypeScript â€” API Only

```
project-root/
â”œâ”€â”€ .azure/
â”‚   â””â”€â”€ project-plan.md
â”œâ”€â”€ .env.example
â”œâ”€â”€ .gitignore
â”œâ”€â”€ package.json
â”œâ”€â”€ services/
â”‚   â”œâ”€â”€ functions/
â”‚   â”‚   â”œâ”€â”€ host.json
â”‚   â”‚   â”œâ”€â”€ local.settings.json
â”‚   â”‚   â”œâ”€â”€ package.json
â”‚   â”‚   â”œâ”€â”€ tsconfig.json
â”‚   â”‚   â”œâ”€â”€ vitest.config.ts
â”‚   â”‚   â”œâ”€â”€ openapi.yaml
â”‚   â”‚   â”œâ”€â”€ src/
â”‚   â”‚   â”‚   â”œâ”€â”€ functions/
â”‚   â”‚   â”‚   â”œâ”€â”€ services/
â”‚   â”‚   â”‚   â”œâ”€â”€ errors/
â”‚   â”‚   â”‚   â”œâ”€â”€ middleware/
â”‚   â”‚   â”‚   â””â”€â”€ logger.ts
â”‚   â”‚   â”œâ”€â”€ tests/
â”‚   â”‚   â”‚   â”œâ”€â”€ fixtures/
â”‚   â”‚   â”‚   â”œâ”€â”€ mocks/
â”‚   â”‚   â”‚   â”œâ”€â”€ services/
â”‚   â”‚   â”‚   â”œâ”€â”€ functions/
â”‚   â”‚   â”‚   â””â”€â”€ errors/
â”‚   â””â”€â”€ shared/
â”‚       â”œâ”€â”€ types/
â”‚       â””â”€â”€ schemas/
â””â”€â”€ data/
```

### Python â€” SPA + Azure Functions

```
project-root/
â”œâ”€â”€ .azure/
â”‚   â””â”€â”€ project-plan.md
â”œâ”€â”€ .env.example
â”œâ”€â”€ .gitignore
â”œâ”€â”€ services/
â”‚   â”œâ”€â”€ functions/                  â† Azure Functions Python project
â”‚   â”‚   â”œâ”€â”€ host.json
â”‚   â”‚   â”œâ”€â”€ local.settings.json
â”‚   â”‚   â”œâ”€â”€ pyproject.toml          â† Python project config
â”‚   â”‚   â”œâ”€â”€ pytest.ini              â† Test config
â”‚   â”‚   â”œâ”€â”€ openapi.yaml
â”‚   â”‚   â”œâ”€â”€ function_app.py         â† Function registration
â”‚   â”‚   â”œâ”€â”€ services/
â”‚   â”‚   â”‚   â”œâ”€â”€ __init__.py
â”‚   â”‚   â”‚   â”œâ”€â”€ interfaces.py       â† ABC / Protocol definitions
â”‚   â”‚   â”‚   â”œâ”€â”€ storage.py
â”‚   â”‚   â”‚   â”œâ”€â”€ database.py
â”‚   â”‚   â”‚   â”œâ”€â”€ cache.py
â”‚   â”‚   â”‚   â”œâ”€â”€ config.py           â† Config loader + validation
â”‚   â”‚   â”‚   â””â”€â”€ registry.py         â† Service factory
â”‚   â”‚   â”œâ”€â”€ errors/
â”‚   â”‚   â”‚   â”œâ”€â”€ __init__.py
â”‚   â”‚   â”‚   â”œâ”€â”€ app_error.py
â”‚   â”‚   â”‚   â”œâ”€â”€ error_types.py
â”‚   â”‚   â”‚   â””â”€â”€ error_handler.py
â”‚   â”‚   â”œâ”€â”€ middleware/
â”‚   â”‚   â”‚   â”œâ”€â”€ __init__.py
â”‚   â”‚   â”‚   â”œâ”€â”€ request_logger.py
â”‚   â”‚   â”‚   â””â”€â”€ validate_request.py
â”‚   â”‚   â”œâ”€â”€ logger.py               â† structlog setup
â”‚   â”‚   â””â”€â”€ tests/
â”‚   â”‚       â”œâ”€â”€ conftest.py          â† Pytest fixtures (mock services)
â”‚   â”‚       â”œâ”€â”€ fixtures/
â”‚   â”‚       â”‚   â”œâ”€â”€ items.json
â”‚   â”‚       â”‚   â””â”€â”€ users.json
â”‚   â”‚       â”œâ”€â”€ test_config.py
â”‚   â”‚       â”œâ”€â”€ test_storage.py
â”‚   â”‚       â”œâ”€â”€ test_database.py
â”‚   â”‚       â”œâ”€â”€ test_get_items.py
â”‚   â”‚       â”œâ”€â”€ test_create_item.py
â”‚   â”‚       â”œâ”€â”€ test_error_handler.py
â”‚   â”‚       â”œâ”€â”€ test_health.py
â”‚   â”‚       â””â”€â”€ test_validation.py
â”‚   â”œâ”€â”€ web/                        â† Frontend
â”‚   â”‚   â””â”€â”€ (same as TypeScript)
â”‚   â””â”€â”€ shared/
â”‚       â”œâ”€â”€ types.py                â† Pydantic models
â”‚       â””â”€â”€ validation.py           â† Validation schemas
â””â”€â”€ data/
```

### C# (.NET 10) â€” SPA + Azure Functions

```
project-root/
â”œâ”€â”€ .azure/
â”‚   â””â”€â”€ project-plan.md
â”œâ”€â”€ .env.example
â”œâ”€â”€ .gitignore
â”œâ”€â”€ ProjectName.sln
â”œâ”€â”€ services/
â”‚   â”œâ”€â”€ Functions/                  â† Azure Functions isolated worker
â”‚   â”‚   â”œâ”€â”€ Functions.csproj
â”‚   â”‚   â”œâ”€â”€ host.json
â”‚   â”‚   â”œâ”€â”€ local.settings.json
â”‚   â”‚   â”œâ”€â”€ Program.cs              â† DI registration + startup
â”‚   â”‚   â”œâ”€â”€ openapi.yaml
â”‚   â”‚   â”œâ”€â”€ Functions/              â† Function handlers
â”‚   â”‚   â”‚   â”œâ”€â”€ GetItems.cs
â”‚   â”‚   â”‚   â”œâ”€â”€ CreateItem.cs
â”‚   â”‚   â”‚   â”œâ”€â”€ GetItemById.cs
â”‚   â”‚   â”‚   â”œâ”€â”€ Health.cs
â”‚   â”‚   â”‚   â””â”€â”€ OpenApi.cs
â”‚   â”‚   â”œâ”€â”€ Services/
â”‚   â”‚   â”‚   â”œâ”€â”€ Interfaces/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ IStorageService.cs
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ IDatabaseService.cs
â”‚   â”‚   â”‚   â”‚   â””â”€â”€ ICacheService.cs
â”‚   â”‚   â”‚   â”œâ”€â”€ StorageService.cs
â”‚   â”‚   â”‚   â”œâ”€â”€ DatabaseService.cs
â”‚   â”‚   â”‚   â”œâ”€â”€ CacheService.cs
â”‚   â”‚   â”‚   â””â”€â”€ Config.cs
â”‚   â”‚   â”œâ”€â”€ Errors/
â”‚   â”‚   â”‚   â”œâ”€â”€ AppException.cs
â”‚   â”‚   â”‚   â”œâ”€â”€ ErrorTypes.cs
â”‚   â”‚   â”‚   â””â”€â”€ ErrorHandler.cs
â”‚   â”‚   â”œâ”€â”€ Middleware/
â”‚   â”‚   â”‚   â”œâ”€â”€ RequestLogger.cs
â”‚   â”‚   â”‚   â””â”€â”€ ValidateRequest.cs
â”‚   â”œâ”€â”€ Functions.Tests/            â† xUnit test project
â”‚   â”‚   â”œâ”€â”€ Functions.Tests.csproj
â”‚   â”‚   â”œâ”€â”€ Fixtures/
â”‚   â”‚   â”‚   â””â”€â”€ ItemFixtures.cs
â”‚   â”‚   â”œâ”€â”€ Mocks/
â”‚   â”‚   â”‚   â”œâ”€â”€ MockStorageService.cs
â”‚   â”‚   â”‚   â”œâ”€â”€ MockDatabaseService.cs
â”‚   â”‚   â”‚   â””â”€â”€ MockCacheService.cs
â”‚   â”‚   â”œâ”€â”€ Services/
â”‚   â”‚   â”‚   â”œâ”€â”€ ConfigTests.cs
â”‚   â”‚   â”‚   â””â”€â”€ StorageTests.cs
â”‚   â”‚   â”œâ”€â”€ Functions/
â”‚   â”‚   â”‚   â”œâ”€â”€ GetItemsTests.cs
â”‚   â”‚   â”‚   â”œâ”€â”€ CreateItemTests.cs
â”‚   â”‚   â”‚   â””â”€â”€ HealthTests.cs
â”‚   â”‚   â”œâ”€â”€ Errors/
â”‚   â”‚   â”‚   â””â”€â”€ ErrorHandlerTests.cs
â”‚   â”‚   â””â”€â”€ Validation/
â”‚   â”‚       â””â”€â”€ ItemValidatorTests.cs
â”‚   â”œâ”€â”€ Shared/
â”‚   â”‚   â”œâ”€â”€ Shared.csproj
â”‚   â”‚   â”œâ”€â”€ Models/
â”‚   â”‚   â”‚   â”œâ”€â”€ Item.cs
â”‚   â”‚   â”‚   â””â”€â”€ ApiContracts.cs
â”‚   â”‚   â””â”€â”€ Validators/
â”‚   â”‚       â””â”€â”€ ItemValidator.cs    â† FluentValidation
â”‚   â””â”€â”€ Web/                        â† Frontend
â”‚       â””â”€â”€ (same as TypeScript)
â””â”€â”€ data/
```

---

## Service Abstraction Layer

The `services/` directory is **critical** for testability. Each file wraps one Azure service behind an interface. Handlers receive services via DI; never import SDKs directly.

> Full service abstraction architecture: see [service-abstraction.md](.github/agents/shared-references/service-abstraction.md).

---

## Function Organization

### One Function Per File (Required)

```
services/functions/src/functions/
â”œâ”€â”€ getItems.ts         â† HTTP GET /api/items
â”œâ”€â”€ createItem.ts       â† HTTP POST /api/items
â”œâ”€â”€ getItemById.ts      â† HTTP GET /api/items/{id}
â”œâ”€â”€ updateItem.ts       â† HTTP PUT /api/items/{id}
â”œâ”€â”€ deleteItem.ts       â† HTTP DELETE /api/items/{id}
â”œâ”€â”€ health.ts           â† HTTP GET /api/health
â””â”€â”€ openapi.ts          â† HTTP GET /api/openapi.json
```

Each function receives deps through service registry:

```typescript
// Example: clean handler with injected services
import { app } from "@azure/functions";
import { getServices } from "../services/registry";

app.http("getItems", {
  methods: ["GET"],
  authLevel: "anonymous",
  route: "items",
  handler: async (request, context) => {
    const { database } = getServices();
    const items = await database.findAll("items");
    return { jsonBody: { items } };
  }
});
```

### Shared Handler Utilities (Required â€” DRY Enforcement)

When 3+ handlers need same helper, extract it to `services/functions/src/utils/`; do NOT duplicate inline.

**Common examples:**

```typescript
// services/functions/src/utils/toPublicUser.ts
import type { User, PublicUser } from '../../../shared/types/entities.js';

export function toPublicUser(user: User): PublicUser {
  return {
    id: user.id,
    email: user.email,
    displayName: user.displayName,
    coupleId: user.coupleId,
    createdAt: user.createdAt,
  };
}
```

```typescript
// Usage in handler â€” import, don't redefine
import { toPublicUser } from '../utils/toPublicUser.js';
```

**Detection**: After Step 6, grep handlers for repeated helper names. Extract when found in 3+ files.

**Enforcement**: Step 12 MUST check for duplicated helpers and extract before finalization.

---

## Frontend Dev Server Configuration (proxy + preview compatibility)

Frontend dev server must (a) proxy `/api` to Functions host and (b) embed in scaffold's **Approve UI** preview, which starts it inside a **VS Code webview iframe** (forwarding port in remote / Codespaces / Dev Container / SSH sessions). Missing (b) hangs preview on "Startingâ€¦" or renders blank despite working in a normal browser, **blocking the approval gate**.

### Vite (React, Vue, Svelte)

```typescript
// vite.config.ts
export default defineConfig({
  server: {
    // (b) Preview compatibility â€” let the webview iframe / forwarded host load the app.
    host: true,          // bind 0.0.0.0 so the webview / port-forwarder can reach it
    allowedHosts: true,  // dev-only: don't 403-block the webview / forwarded origin
    strictPort: false,   // let the preview bind a free port if the default is taken
    // (a) Proxy /api to the Functions host.
    proxy: {
      '/api': {
        target: 'http://localhost:7071',
        changeOrigin: true
      }
    }
  }
});
```

### Angular

```json
// proxy.conf.json
{
  "/api": {
    "target": "http://localhost:7071",
    "secure": false
  }
}
```

> For Angular, serve with host binding + host-check disabled so preview iframe loads (`ng serve --host 0.0.0.0 --disable-host-check`, or equivalent `serve` options in `angular.json`). For Next.js, bind all interfaces (`next dev -H 0.0.0.0`). Goal matches Vite's `host: true` + `allowedHosts: true`.

> **Do NOT frame-bust the dev server.** Never send `X-Frame-Options` from dev server or add a `<meta http-equiv="Content-Security-Policy" content="â€¦ frame-ancestors â€¦">` to `index.html`. These allow normal browser loading but block preview webview iframe embedding: "works in my browser, blank in the preview."

---

## Monorepo Package Management

### npm Workspaces (TypeScript)

```json
{
  "private": true,
  "workspaces": ["services/functions", "services/web", "services/shared"],
  "scripts": {
    "test": "npm test --workspaces",
    "test:functions": "cd services/functions && npm test",
    "test:web": "cd services/web && npm test",
    "build": "npm run build --workspaces"
  }
}
```

### TypeScript Cross-Workspace Import Configuration

When Functions imports from `../shared/`, `tsconfig.json` must set `rootDir` outside workspace:

```jsonc
// services/functions/tsconfig.json
{
  "compilerOptions": {
    "rootDir": "..",        // â† Parent of functions dir (i.e., services/)
    "outDir": "dist",
    // ... other options
  },
  "include": ["src/**/*.ts", "../shared/**/*.ts"],
  "exclude": ["node_modules", "dist", "tests"]
}
```

> â›” **Import the shared code by relative path (`../shared/...`) or by the shared package's `name` â€” NEVER through a tsconfig `paths` alias.**
>
> `tsc` resolves `paths` at compile time and emits the module specifier **unchanged**. `paths` has no runtime counterpart (no `tsconfig-paths`, no bundler, no `imports` map in these projects), so the alias survives into `dist/` and Node cannot resolve it. The build is green, `tsc --noEmit` is green, and `func start` dies on load.
>
> ```jsonc
> // âŒ compiles, fails at runtime â€” tsc emits require('@shared/schemas/index') unchanged
> // â†’ Worker was unable to load entry point: Cannot find module '@shared/schemas/index'
> { "compilerOptions": { "paths": { "@shared/*": ["shared/src/*"] } } }
> ```
>
> ```ts
> // âŒ import { createTaskSchema } from '@shared/schemas/index';
> // âœ… relative form â€” count the levels from the importing FILE, not from the workspace root.
> //    services/functions/src/functions/createTask.ts -> services/shared/schemas/index
> import { createTaskSchema } from '../../../shared/schemas/index';
> // âœ… or the workspace package's own name, exactly as written in services/shared/package.json
> import { createTaskSchema } from '@<project>/shared/schemas';
> ```
>
> These are the only two supported mechanisms. Picking both leaves two competing paths to the same code, and the compile-time-only one wins silently.

> âš ï¸ **Build output nesting â€” `main` MUST match actual dist/ output (Rule 14)**
>
> When `rootDir` is parent dir, `tsc` mirrors full structure under `dist/`. `main` in `package.json` MUST be computed from actual output â€” never hardcoded.
>
> | `rootDir` value | `services/functions/src/functions/register.ts` compiles to | Correct `main` field |
> |-----------------|-------------------------------------------------------|---------------------|
> | `"."` | `dist/src/functions/register.js` | `"dist/src/functions/*.js"` |
> | `".."` (= `services/`) | `dist/functions/src/functions/register.js` | `"dist/functions/src/functions/*.js"` |
> | `"../.."` (= project root) | `dist/services/functions/src/functions/register.js` | `"dist/services/functions/src/functions/*.js"` |
>
> **Verification (MANDATORY after every `tsc` build):**
> 1. Run `tsc` in functions workspace
> 2. List `dist/` â€” find compiled handler `.js` files
> 3. Construct matching glob
> 4. Set `main` to that glob
> 5. `func start` â€” verify functions register. "Found zero files" = wrong `main`.
>
> **#1 cause of "tests pass but app won't start".** Tests use vitest/ts-node (transpile on fly, never read `main`). Only `func start` uses `main` to discover handlers.

### Python (Poetry)

```toml
# pyproject.toml at project root
[tool.poetry]
packages = [
    { include = "services", from = "services/functions" },
    { include = "shared", from = "services" },
]
```

### .NET (Solution)

```xml
<!-- ProjectName.sln references -->
<!-- services/Functions/Functions.csproj -->
<!-- services/Functions.Tests/Functions.Tests.csproj -->
<!-- services/Shared/Shared.csproj -->
```

---

## .gitignore Additions

> â›” **Ignore `.env` and `local.settings.json` before creating them.** They hold real credential
> values. A secret that reaches a commit is compromised the moment it is pushed, and deleting it in
> a later commit does **not** undo that â€” the value stays in history and in every clone and fork
> that already has it. Recovery means rotating the credential, not editing a file. This applies to
> private repositories exactly as it does to public ones: no secret belongs in any commit, ever.

```gitignore
# Environment
.env
local.settings.json

# Data volumes
data/

# Build output
dist/
bin/
obj/
.vite/

# Runtime
node_modules/
__pycache__/
.python_packages/

# Test output
coverage/
.pytest_cache/
TestResults/

# IDE
.vs/
```

---

## Port Allocation Convention

| Service | Port | Notes |
|---------|------|-------|
| Azure Functions host | 7071 | Default `func start` port |
| Frontend dev server (Vite) | 5173 | Default Vite port |
| Frontend dev server (Angular) | 4200 | Default Angular port |
| Azurite Blob | 10000 | |
| Azurite Queue | 10001 | |
| Azurite Table | 10002 | |
| PostgreSQL | 5432 | |
| CosmosDB Emulator | 8081 | |
| Redis | 6379 | |
| Azure SQL Edge | 1433 | |

