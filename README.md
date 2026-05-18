# Foreign Citizen Rules

Monorepo for the foreign citizen rules system.

## Structure

- `backend/` - ASP.NET Core API and integration tests.
- `frontend/` - frontend application placeholder.
- `docker-compose.yml` - local stack for PostgreSQL, backend, and frontend placeholder.
- `docs/` - project documentation.

## Backend

```powershell
dotnet build .\backend\ForeignCitizenRules.sln
dotnet test .\backend\ForeignCitizenRules.sln
```

## Docker

```powershell
docker compose up --build
```

Swagger is available at `http://localhost:5000/swagger`.
