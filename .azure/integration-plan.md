# Integration Hand-off Plan

## Backend
- Project: `src/AchorPoint.Functions`
- Run command: `func start --verbose`
- Build command: `dotnet build src/AchorPoint.Functions/Prodexus.Functions.csproj`
- Port: `7071`
- Health endpoint: `GET /api/health`

## Frontend
- Frontend: none (API-only project)
- API seam: not applicable

## API Routes
- `GET /api/health`
- `POST /api/ingest/file`

## Database
- Type: Azure SQL / SQL Server compatible
- Migration tool: `dotnet ef migrations add <name> --project src/AchorPoint.Functions/Prodexus.Functions.csproj`
- Connection env vars: `SQL_CONNECTION_STRING`, `STORAGE_CONNECTION_STRING`
- No seed data: required

## Services
- Essential: Blob Storage, Azure SQL
- Enhancement: none

## Shared Types
- None. Keep model and EF types local to `src/AchorPoint.Functions` for this API-only project.

## Integration Results
- SQL Server schema migrations created under `src/AchorPoint.Functions/Data/Migrations` and applied to LocalDB: `ParsedRecords` with a status check constraint; no seed data.
- Backend built and started with Functions Core Tools. `GET /api/health` returned 200; `POST /api/ingest/file` returned 200 and persisted one row; empty input returned 400. The smoke-test row was removed afterward.
- The project is API-only, so frontend wiring and mock-data removal are not applicable.
- End-to-end proof: `POST /api/ingest/file` -> Functions host -> LocalDB, with the inserted `Accepted` record confirmed in SQL before cleanup.

