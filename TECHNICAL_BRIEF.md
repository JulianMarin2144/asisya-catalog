# TECHNICAL BRIEF — Asisya DEV II

Contrato de desarrollo para la prueba técnica Asisya (Desarrollador II).  

---

## 1. Objetivo

Exponer información de productos y categorías mediante una API REST robusta, escalable y segura, más una SPA React que consuma esa API con autenticación JWT.

La solución debe demostrar habilidades técnicas y criterios arquitectónicos **sin sobreingeniería**.

---

## 2. Stack cerrado

| Área | Elección |
|------|----------|
| Backend | .NET 8, C# |
| Arquitectura | Clean Architecture en capas: `Api` / `Application` / `Domain` / `Infrastructure` |
| Persistencia | PostgreSQL + EF Core |
| Auth | JWT (Bearer) |
| Bulk 100k | `POST /Product` con inserts por lotes (`AddRange` + `SaveChanges` por batch) |
| Cache | `IMemoryCache` solo donde aporte (p. ej. categorías / listados); sin Redis en MVP |
| Frontend | React + Vite + TypeScript |
| Formularios / rutas | React Hook Form + React Router (equivalente a Reactive Forms / AppRoutingModule del enunciado) |
| Tests | xUnit (unitarios) + al menos 1 integración con `WebApplicationFactory` |
| DevOps | Dockerfile + `docker-compose.yml` + GitHub Actions (build/test) |

---

## 3. Arquitectura

```
Client (React SPA) --JWT--> Api --> Application --> Domain
                              \--> Infrastructure --> PostgreSQL
```

### Capas

| Capa | Responsabilidad | No debe |
|------|-----------------|--------|
| **Domain** | Entidades (`Product`, `Category`), reglas de dominio mínimas | Referenciar EF, ASP.NET, DTOs de API |
| **Application** | Casos de uso, interfaces, DTOs, validación | Acceder a HttpContext ni DbContext concreto |
| **Infrastructure** | EF Core, repos, JWT helpers, implementaciones | Contener lógica de negocio de presentación |
| **Api** | Controllers, auth, Problem Details | Contener reglas de negocio ni acceso directo a SQL |

### Patrones permitidos

- Repository + Unit of Work ligero (o `DbContext` como UoW)
- Mapping explícito DTO ↔ entidad (manual o Mapster; no AutoMapper complejo)
- Validación en Application (FluentValidation o DataAnnotations, una sola estrategia)
- Resultado tipado / excepciones de dominio controladas → Problem Details en Api

### Anti-overkill (prohibido en MVP)

- CQRS / MediatR / event sourcing
- Microservicios, gRPC, message bus (RabbitMQ, etc.)
- Redis, multi-tenant, hexagonal “puro” o DDD ceremonial
- Exponer entidades EF directamente en la API
- Código muerto, comentarios obsoletos, abstracciones “por si acaso”

---

## 4. Endpoints requeridos

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/Category/` | Crear categorías |
| `POST` | `/Product/` | Generar y guardar productos aleatorios (carga masiva) |
| `GET` | `/Products/` | Listar con paginación, filtros y búsqueda |
| `GET` | `/Products/{id}` | Detalle de producto con foto de la categoría |
| `PUT` / `DELETE` | Productos | Actualizar / borrar productos |

**Datos de negocio:** categorías `SERVIDORES` y `CLOUD` vía API; carga eficiente de ~100.000 productos.

**Seguridad:** JWT en endpoints críticos (escritura y, según diseño, lecturas protegidas para alinear con el AuthGuard del frontend).

---

## 5. Frontend (SPA)

- Login (usuario/contraseña) → token JWT desde la API
- Guardar token en `localStorage`
- Interceptor que adjunta el token en cada request
- AuthGuard / ruta protegida para productos
- Página de listado de productos
- Formularios crear/editar con validaciones
- Enrutamiento modular (React Router)

---

## 6. Fases y Definition of Done

No adelantar fases. Cada fase solo se cierra si cumple su DoD.

### Fase 0 — Gobernanza

- [x] Este `TECHNICAL_BRIEF.md`

**DoD:** Brief versionado en el repo; sin código de API/SPA aún.

### Fase 1 — Esqueleto backend

- [x] Solution .NET 8 con proyectos por capa
- [x] PostgreSQL cableado, migraciones iniciales
- [x] Seed mínimo de usuario para JWT
- [x] Arranque local documentado a alto nivel

**DoD:** Solución compila; DB se crea/migra; health o endpoint mínimo responde.

### Fase 2 — Dominio API

- [x] `POST /Category`, CRUD de producto
- [x] `GET /Products` con paginación, filtros y búsqueda
- [x] `GET /Products/{id}` incluyendo foto de categoría
- [x] DTOs y mapeo explícito; JWT en endpoints críticos

**DoD:** Endpoints operativos vía Swagger/curl con token.

### Fase 3 — Carga masiva

- [x] `POST /Product` genera productos aleatorios en batch
- [x] Asociación a categorías `SERVIDORES` / `CLOUD`
- [x] Capacidad demostrable de ~100k registros de forma eficiente

**DoD:** Carga masiva completa sin timeouts inaceptables; índices básicos si hacen falta.

### Fase 4 — Calidad

- [x] Pruebas unitarias de Application (o servicios clave)
- [x] Al menos 1 prueba de integración
- [x] Hardening de auth en endpoints críticos

**DoD:** `dotnet test` en verde en local.

### Fase 5 — DevOps

- [x] Dockerfile funcional
- [x] `docker-compose.yml` (API + PostgreSQL; frontend opcional en compose)
- [x] Pipeline mínimo `.github/workflows` (build + test)

**DoD:** `docker compose up` levanta el stack; Actions válidas en repo.

### Fase 6 — Frontend

- [x] Login, `localStorage`, interceptor, AuthGuard
- [x] Listado + crear/editar productos con validación

**DoD:** Flujo login → listado → CRUD usable contra la API.

### Fase 7 — Entrega

- [x] `README.md`: arquitectura, decisiones, escalado horizontal en cloud, clone/build/run
- [x] Repo listo para GitHub público
- [x] Checklist de correo: **asunto = nombre completo**, correo nuevo (no responder el hilo)

**DoD:** Un tercero puede clonar, levantar y validar la prueba con el README.

---

## 7. Escalabilidad (documentar en README, no sobre-implementar)

En el README explicar cómo escalar horizontalmente, por ejemplo:

- API stateless + JWT → múltiples réplicas detrás de un load balancer
- PostgreSQL con connection pooling; lecturas/replicas si crece la carga
- Batch inserts y paginación ya en la app; cache distribuida (Redis) solo si las réplicas lo exigen
- Contenedores en cloud (ECS/AKS/Cloud Run) con health checks

---

## 8. Estructura de repositorio

```
/
  TECHNICAL_BRIEF.md
  README.md
  backend/src/...
  backend/tests/...
  frontend/
  docker-compose.yml
  .github/workflows/ci.yml
```

---

## 9. Entrega (correo)

Según instrucciones del proceso de selección:

1. Enviar la prueba en un **correo nuevo** (no responder el hilo de la invitación).
2. **Asunto = nombre completo**.
3. Incluir enlace al repositorio público y, si aplica, notas de decisión/observaciones (también en README).

---

## 10. Gobernanza de cambios

- Si un requerimiento del PDF entra en conflicto con este brief, prevalece el **PDF + correo**; se documenta la excepción en el README.
- Preferir cambios pequeños, fase a fase, con DoD verificable.
