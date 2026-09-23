# Asisya Catalog — Prueba técnica DEV II

API REST de catálogo de **productos y categorías** más una **SPA React** con autenticación JWT.  
Objetivo: solución robusta, escalable y segura, con criterios arquitectónicos claros y **sin sobreingeniería**.

Documento de contrato interno: [`TECHNICAL_BRIEF.md`](TECHNICAL_BRIEF.md).

---

## Arquitectura

```
React SPA  --JWT Bearer-->  Api  -->  Application  -->  Domain
                               \-->  Infrastructure  -->  PostgreSQL
```

| Capa | Responsabilidad |
|------|-----------------|
| **Domain** | Entidades (`Product`, `Category`, `User`); sin EF ni ASP.NET |
| **Application** | Casos de uso, DTOs, validación, interfaces de repos |
| **Infrastructure** | EF Core, repositorios, JWT, hashing BCrypt |
| **Api** | Controllers, auth, CORS, Problem Details, health |

### Decisiones (por qué así)

- **Clean Architecture por capas** (no hexagonal/DDD ceremonial): separación clara Domain → Application → Infrastructure → Api, suficiente para la prueba y fácil de seguir en un monorepo.
- **Sin CQRS / MediatR**: el dominio es CRUD + bulk; MediatR añadiría indirection sin beneficio medible aquí.
- **Sin Redis en el MVP**: un solo nodo / demo local; `IMemoryCache` bastaría si hiciera falta en proceso. Redis se documenta como siguiente paso al escalar horizontalmente (ver abajo).
- **Mapping DTO explícito** (sin AutoMapper complejo): control total y menos magia en una prueba corta.
- **DbContext como Unit of Work** + repositorios simples: batch insert con `AddRange` + `SaveChangesAsync` por lote de 5000 y `ChangeTracker.Clear()`.

---

## Stack

| Área | Tecnología |
|------|------------|
| Backend | .NET 8, C#, ASP.NET Core |
| Persistencia | PostgreSQL 16, EF Core 8 |
| Auth | JWT Bearer + BCrypt |
| Frontend | React 19, Vite, TypeScript, React Router, React Hook Form, Axios |
| Tests | xUnit, Moq, Testcontainers.PostgreSql, WebApplicationFactory |
| DevOps | Docker multi-stage, docker-compose, GitHub Actions (build + test) |

---

## Arranque rápido (tercero / evaluador)

### Requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (obligatorio para `compose` y tests de integración)
- .NET 8 SDK (opcional; solo si desarrollas la API fuera de Docker)
- Node 22+ (opcional; solo si desarrollas el frontend fuera de Docker)

### Clonar y levantar

```bash
git clone <URL_DEL_REPO>
cd Asis   # o el nombre de la carpeta del clone

docker compose down -v
docker compose up --build -d
```

Espera ~30–60 s a que Postgres esté healthy y la API aplique migraciones + seed.

### URLs

| Servicio | URL |
|----------|-----|
| SPA | http://localhost:5173 |
| API | http://localhost:5192 |
| Swagger | http://localhost:5192/swagger |
| Health | http://localhost:5192/health |

> **Nota de puertos:** el contenedor API escucha en `8080` internamente; en el host se publica como **5192** (`5192:8080`) porque en algunos Windows el puerto 8080 está reservado. La SPA usa `VITE_API_BASE_URL=http://localhost:5192`.

### Credenciales seed

| Usuario | Password |
|---------|----------|
| `admin` | `Admin123!` |

Categorías seed: `SERVIDORES`, `CLOUD`.

### Validación manual sugerida

1. Abrir http://localhost:5173 → login con `admin` / `Admin123!`.
2. Crear un producto, verlo en el listado, editarlo, eliminarlo.
3. Sin token, `/products` redirige a `/login`.
4. Opcional: Swagger o curl para bulk (sección Carga masiva).

### Parar

```bash
docker compose down
# con volumen limpio:
docker compose down -v
```

---

## Desarrollo local (sin contenedor de app)

```bash
# Solo DB
docker compose up -d postgres

# API
cd backend/src/Asisya.Api
dotnet run --launch-profile http
# http://localhost:5192

# Frontend
cd frontend
cp .env.example .env
npm install
npm run dev
# http://localhost:5173
```

---

## Tests

Requiere **Docker** en ejecución (Testcontainers levanta PostgreSQL).

```bash
cd backend
dotnet test Asisya.Catalog.sln
```

- Unitarios: `Asisya.Application.Tests` (Moq)
- Integración: `Asisya.Api.Tests` (Testcontainers + `WebApplicationFactory`) — flujo login → Category → Product CRUD + 401 sin JWT

CI: [`.github/workflows/ci.yml`](.github/workflows/ci.yml) (push/PR a `main`).

---

## Endpoints principales

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| `POST` | `/auth/login` | No | Obtiene JWT (`admin` / `Admin123!`) |
| `GET` | `/health` | No | Health check (Postgres) |
| `GET` | `/Category` | No | Lista categorías |
| `POST` | `/Category` | JWT | Crea categoría (`name`, `description?`, `photoUrl`) |
| `GET` | `/Products` | No | Listado paginado (`page`, `pageSize`, `categoryId`, `search`) |
| `GET` | `/Products/{id}` | No | Detalle + `categoryPhotoUrl` |
| `POST` | `/Product` | JWT | Crear individual **o** bulk si `count > 1` |
| `PUT` | `/Product/{id}` | JWT | Actualizar producto |
| `DELETE` | `/Product/{id}` | JWT | Eliminar producto |

### Ejemplo login + crear categoría

```bash
TOKEN=$(curl -s -X POST http://localhost:5192/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin123!"}' | jq -r .accessToken)

curl -s -X POST http://localhost:5192/Category \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"DEMO","photoUrl":"https://cdn.example.com/demo.png"}'
```

---

## Carga masiva (~100.000 productos)

```bash
curl -X POST http://localhost:5192/Product \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"count":100000}'
```

- Genera productos aleatorios asociados a `SERVIDORES` / `CLOUD`.
- Inserts por **lotes de 5.000** (`AddRange` + `SaveChangesAsync`, sin Redis/colas).
- Respuesta incluye `insertedCount`, `batchCount`, `elapsedMilliseconds`.

**Tiempo observado en desarrollo local (Docker/Desktop):** ~**9 s** para 100.000 registros (`elapsedMilliseconds ≈ 9012`).  
Tras la carga, `GET /Products?pageSize=1` devolvió `totalCount=100000` en ~200 ms.

---

## Escalabilidad horizontal en cloud

Cómo se escalaría esta solución (sin implementarlo todo en el MVP):

1. **API stateless + JWT** — cualquier réplica valida el mismo `Jwt:Key`; poner N pods/tareas detrás de un **load balancer** (ALB / nginx / Cloud Load Balancing).
2. **PostgreSQL** — connection pooling (PgBouncer / pool del driver); **réplicas de lectura** para listados pesados si crece el tráfico de lectura.
3. **Ya en la app** — batch inserts y paginación en query (no cargar 100k en memoria). **Cache distribuida (Redis)** solo cuando haya varias réplicas y se necesite invalidación compartida (reemplazo natural de `IMemoryCache`).
4. **Contenedores** — desplegar las imágenes actuales en **ECS / AKS / Cloud Run** con health check en `/health`, autoscaling por CPU/memoria o RPS.
5. **Fotos de categoría** — hoy son URLs; en escala, servir desde **S3 / Blob / CDN** y guardar solo la URL en DB.

---

## Secretos y seguridad del repo

- Placeholders en compose / `appsettings.json`: `CHANGE_ME_IN_PRODUCTION` (password DB) y `CHANGE_ME_IN_PRODUCTION_USE_A_LONG_SECRET_32PLUS` (JWT).
- Usuario demo `admin` / `Admin123!` es **seed de prueba**, no un secreto de producción.
- [`.gitignore`](.gitignore) excluye `bin/`, `obj/`, `node_modules/`, `dist/`, `.env` y overrides locales. Se versiona `.env.example`.

En un entorno real: inyectar secretos por variables de entorno / secret manager y rotar JWT y passwords.

---

## Notas y excepciones respecto al PDF

| PDF / correo | Decisión tomada |
|--------------|-----------------|
| “.NET Core 7 en adelante” | **.NET 8** LTS |
| Frontend “React” + términos Angular (`Reactive Forms`, `AppRoutingModule`) | **React Hook Form + React Router** (equivalente funcional) |
| `POST /Product` carga masiva | Mismo endpoint: body con `count > 1` = bulk; sin `count` = creación individual (CRUD SPA) |
| Foto de categoría | Campo `PhotoUrl` obligatorio en Category; detalle expone `categoryPhotoUrl` |
| Mock “Mockito / Testcontainers” | Ecosistema .NET: **Moq + Testcontainers.PostgreSql** |
| Puerto API 8080 | Contenedor en 8080; host **5192** por restricción frecuente de 8080 en Windows |
| `GET /Category` | Añadido para poblar el select del frontend (no estaba explícito en el PDF) |
| Entrega | Repo público + **correo nuevo** con **asunto = nombre completo** (instrucciones del proceso) |

---

## Estructura del repositorio

```
/
├── TECHNICAL_BRIEF.md          # Contrato por fases
├── README.md                   # Este archivo
├── docker-compose.yml          # postgres + api + frontend
├── .gitignore
├── .github/workflows/ci.yml
├── backend/
│   ├── Dockerfile
│   ├── Asisya.Catalog.sln
│   ├── README.md
│   ├── src/
│   │   ├── Asisya.Api
│   │   ├── Asisya.Application
│   │   ├── Asisya.Domain
│   │   └── Asisya.Infrastructure
│   └── tests/
│       ├── Asisya.Application.Tests
│       └── Asisya.Api.Tests
└── frontend/
    ├── Dockerfile
    ├── nginx.conf
    ├── .env.example
    ├── package.json
    └── src/
        ├── api/
        ├── auth/
        └── pages/
```

---

## Checklist de entrega (correo)

Según instrucciones del proceso de selección:

1. Enviar en un **correo nuevo** (no responder el hilo de la invitación).
2. **Asunto = nombre completo**.
3. Incluir enlace al repositorio público de GitHub.
4. Mencionar (o adjuntar) observaciones/decisiones — también documentadas aquí y en `TECHNICAL_BRIEF.md`.
