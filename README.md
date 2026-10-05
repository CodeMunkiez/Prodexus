# Prodexus

A .NET Azure Functions API built to ingest flat-file content, normalize rows into a canonical structure, and persist them to SQL using Entity Framework.

## Local development

1. Restore dependencies:
   - `dotnet restore`
2. Start local dependencies:
   - `docker compose up -d sql azurite`
3. Apply the database schema from the repository root:
   - `dotnet ef database update --project src/Prodexus.Functions/Prodexus.Functions.csproj --startup-project src/Prodexus.Functions/Prodexus.Functions.csproj`
4. Configure `SQL_CONNECTION_STRING` for the local SQL Server and `AzureWebJobsStorage` as `UseDevelopmentStorage=true`, then run the function app:
   - `func start --verbose` (from `src/Prodexus.Functions`)
5. Test the API:
   - `GET /api/health`
   - `POST /api/ingest/file` with raw text or multipart file upload

## Architecture

- `src/Prodexus.Functions`: Prodexus Azure Function app
- `src/Prodexus.Tests`: Prodexus xUnit tests for parser and mapper logic

