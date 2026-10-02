# Project Plan

**Status**: Integrated
**Created**: 2026-10-02
**Mode**: NEW

---

## 1. Project Overview

**Goal**: Build a .NET Azure Function that accepts flat-file content over HTTP, maps raw row data into a standard canonical model, validates the payload, and persists parsed records to Azure SQL using Entity Framework. The project is designed so that every module is independently testable.

**App Type**: API only

**API Login**: Yes

**Mode**: NEW

**Deployment Plan**: No deployment plan found

---

## 2. Backend â€” Azure Functions

| Component | Technology |
|-----------|-----------|
| **Language** | C# |
| **Runtime** | .NET |
| **Package Manager** | dotnet (NuGet) |
| **Test Runner** | xUnit |
| **Mocking Library** | **NSubstitute** (.NET â€” never Moq, see runtimes/dotnet.md) |
| **Test Command** | dotnet test |
| **Orchestration** | docker-compose |

> **Language vs Runtime**: `Language` is the source language the user picked in this service's `language` question. `Runtime` is the execution runtime â€” default `Node` for TypeScript/JavaScript, `CPython` for Python, `.NET` for C#. Only deviate from the default (e.g. `Bun`, `Deno`, `PyPy`) when the user explicitly asks. **Package Manager and Test Runner are language-dependent** â€” match them to this service's Language (e.g. C# â†’ `dotnet (NuGet)` + `xUnit`/`NUnit`/`MSTest`). The `Orchestration` row is recorded for the scaffold step but hidden in the plan UI â€” always keep it set to `docker-compose`.

---

## 3. Services Required

| Azure Service | Role in App | Environment Variable | Default Value (Local) | Classification |
|---------------|------------|---------------------|----------------------|----------------|
| Blob Storage | Azure Functions runtime storage and upload staging for flat-file ingestion | STORAGE_CONNECTION_STRING | UseDevelopmentStorage=true | Essential |
| Azure SQL | Primary relational data store for parsed records and mapping results | SQL_CONNECTION_STRING | Server=(localdb)\\mssqllocaldb;Database=ProdexusDb;Trusted_Connection=True; | Essential |

---

## 4. Prerequisites

Identify the required tools, then inventory them by following [prerequisites.md](../shared-references/prerequisites.md). Always produce **both** groups â€” `### Run` and `### Debug` â€” as two sub-tables under this section. The plan webview shows the Run group always and the Debug group only when the user turns on the Autopilot toggle, so do not omit either group yourself.

### Run

| Tool | Service(s) | Installed | Version |
|------|------------|-----------|---------|
| .NET SDK | ingestion-function | âœ… | 10.0.401 |
| Azure Functions Core Tools | ingestion-function | â“ | Unconfirmed |
| Azure CLI | * | â“ | Unconfirmed |
| Docker Desktop | ingestion-function | âœ… | 29.6.2 |

### Debug

| Tool | Service(s) | Installed | Version |
|------|------------|-----------|---------|
| ms-azuretools.vscode-azurefunctions | ingestion-function | â“ | Unconfirmed |

---

## 5. Project Structure

```
Prodexus/
â”œâ”€ .azure/
â”‚  â”œâ”€ project-plan.md
â”‚  â””â”€ requirements.json
â”œâ”€ src/
â”‚  â”œâ”€ Prodexus.Functions/
â”‚  â”‚  â”œâ”€ Functions/
â”‚  â”‚  â”‚  â””â”€ FlatFileIngestionFunction.cs
â”‚  â”‚  â”œâ”€ Data/
â”‚  â”‚  â”‚  â”œâ”€ AppDbContext.cs
â”‚  â”‚  â”‚  â””â”€ Models/
â”‚  â”‚  â”œâ”€ Mapping/
â”‚  â”‚  â”‚  â”œâ”€ FieldMapConfig.cs
â”‚  â”‚  â”‚  â””â”€ RowMapper.cs
â”‚  â”‚  â”œâ”€ Services/
â”‚  â”‚  â”‚  â”œâ”€ FlatFileParser.cs
â”‚  â”‚  â”‚  â””â”€ RecordPersistor.cs
â”‚  â”‚  â”œâ”€ Program.cs
â”‚  â”‚  â”œâ”€ appsettings.json
â”‚  â”‚  â””â”€ host.json
â”‚  â””â”€ Prodexus.Tests/
â”‚     â”œâ”€ Ingestion/
â”‚     â”‚  â”œâ”€ FlatFileIngestionFunctionTests.cs
â”‚     â”‚  â””â”€ RowMapperTests.cs
â”‚     â””â”€ Fixtures/
â”‚        â””â”€ sample-flat-file.txt
â”œâ”€ docker-compose.yml
â”œâ”€ README.md
â”œâ”€ .gitignore
â””â”€ Directory.Build.props
```

---

## 6. Route Definitions

| # | Method | Path | Description | Request Body | Response Body | Status Codes |
|---|--------|------|-------------|-------------|--------------|-------------|
| 1 | GET | `/api/health` | Health check for the ingestion service | â€” | `{ status, services }` | 200, 503 |
| 2 | POST | `/api/ingest/file` | Accept flat-file content as a stream and validate/parse row records before persisting them to SQL | Raw text, CSV-like content, or multipart file upload | `{ status, rowsProcessed, inserted, errors }` | 200, 202, 400, 500 |

---

## 7. Next Steps

1. Run **azure-project-scaffold** to execute this plan
2. Run **azure-project-integrate** to wire the frontend to live data, smoke-test the backend, and create the migrations
3. Run **azure-debug-plan** â†’ **azure-debug-generate** for Docker emulators and VS Code debugging
4. Run the **azure-deploy** agent when ready; it uses **azure-app-onboard** for architecture, cost estimation, IaC generation, provisioning, and health verification

