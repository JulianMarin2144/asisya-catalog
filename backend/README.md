# Asisya Catalog — Backend

## Requisitos
- .NET 8 SDK (arranque local)
- Docker / Docker Compose (stack completo)

## Secretos (importante)

Valores en `appsettings.json` y `docker-compose.yml` son **demos** para desarrollo:

- `CHANGE_ME_IN_PRODUCTION` — password de PostgreSQL
- `CHANGE_ME_IN_PRODUCTION_USE_A_LONG_SECRET_32PLUS` — clave JWT

No uses estos valores en un entorno real. Sobrescribe con variables de entorno o un `.env` local no versionado.

## Arranque con Docker (recomendado)

```bash
# Desde la raíz del repo
docker compose down -v
docker compose up --build -d
```

- API: `http://localhost:5192` (host) → contenedor `8080`
- Health: `GET http://localhost:5192/health`
- Login seed: `POST /auth/login` con `{ "username": "admin", "password": "Admin123!" }`

Las migraciones EF se aplican al arrancar la API (`MigrateAsync` en el seeder).

## Arranque local (SDK)

```bash
docker compose up -d postgres
cd backend/src/Asisya.Api
dotnet run --launch-profile http
```

- Health: `GET http://localhost:5192/health`
- Swagger: `http://localhost:5192/swagger` (Development)

## Endpoints

| Método | Ruta | Auth |
|--------|------|------|
| `POST` | `/auth/login` | No |
| `POST` | `/Category` | JWT |
| `GET` | `/Products?page=&pageSize=&categoryId=&search=` | No |
| `GET` | `/Products/{id}` | No |
| `POST` | `/Product` | JWT (individual o `{ "count": N }` bulk) |
| `PUT` | `/Product/{id}` | JWT |
| `DELETE` | `/Product/{id}` | JWT |

## Tests

```bash
dotnet test backend/Asisya.Catalog.sln
```

- Unitarios: `backend/tests/Asisya.Application.Tests` (Moq)
- Integración: `backend/tests/Asisya.Api.Tests` (Testcontainers PostgreSQL)

## CI

Pipeline en [`.github/workflows/ci.yml`](../.github/workflows/ci.yml): build + test en push/PR a `main`.

## Estructura

```
backend/
  Dockerfile
  Asisya.Catalog.sln
  src/
    Asisya.Api
    Asisya.Application
    Asisya.Domain
    Asisya.Infrastructure
  tests/
    Asisya.Application.Tests
    Asisya.Api.Tests
```
