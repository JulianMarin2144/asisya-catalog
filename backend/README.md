# Asisya Catalog — Backend

## Requisitos
- .NET 8 SDK (arranque local)
- Docker / Docker Compose (stack completo)

## Secretos (importante)

`docker-compose.yml` no trae secretos ni valores por defecto: exige `POSTGRES_PASSWORD` y `JWT_KEY`, y deja apagados el usuario demo y Swagger. Los valores de demo local están en [`.env.example`](../.env.example), que se copia a `.env` (ignorado por git). `appsettings.json` solo tiene placeholders `CHANGE_ME_IN_PRODUCTION…` para arrancar con el SDK.

No uses estos valores en un entorno real: inyecta secretos propios por variables de entorno o un secret manager, y deja `SEED_DEFAULT_ADMIN` y `SWAGGER_ENABLED` en `false`.

## Arranque con Docker (recomendado)

```bash
# Desde la raíz del repo
cp .env.example .env   # PowerShell: Copy-Item .env.example .env
docker compose down -v
docker compose up --build -d
```

- API: `http://localhost:5192` (host) → contenedor `8080`
- Health: `GET http://localhost:5192/health`
- Swagger: `http://localhost:5192/swagger` (solo con `SWAGGER_ENABLED=true`, activo en el `.env` de demo)
- Login seed: `POST /auth/login` con `{ "username": "admin", "password": "Admin123!" }` (solo con `SEED_DEFAULT_ADMIN=true`)

Las migraciones EF se aplican al arrancar la API (`MigrateAsync` en el seeder).

## Arranque local (SDK)

```bash
# Desde la raíz, con el .env de demo creado
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
| `GET` | `/Category` | JWT |
| `POST` | `/Category` | JWT |
| `GET` | `/Products?page=&pageSize=&categoryId=&search=` | JWT |
| `GET` | `/Products/{id}` | JWT |
| `POST` | `/Product` | JWT (individual o `{ "count": N }` bulk) |
| `PUT` | `/Product/{id}` | JWT |
| `DELETE` | `/Product/{id}` | JWT |

## Tests

```bash
dotnet test backend/Asisya.Catalog.sln                      # unitarios + integración (requiere Docker)
dotnet test backend/tests/Asisya.Application.Tests          # solo unitarios, sin Docker
```

- Unitarios: `backend/tests/Asisya.Application.Tests` (Moq)
- Integración: `backend/tests/Asisya.Api.Tests` (Testcontainers PostgreSQL). Si Docker no es accesible fallan con un mensaje explícito.
- Si el build falla por archivos bloqueados: `dotnet build-server shutdown` y reintentar.

## CI

Pipeline en [`.github/workflows/ci.yml`](../.github/workflows/ci.yml) en push/PR a `main`: build + test + cobertura del backend; lint + build + `npm audit` del frontend.

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
