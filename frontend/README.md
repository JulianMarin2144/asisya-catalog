# Asisya Catalog — Frontend

SPA React + Vite + TypeScript para el catálogo Asisya.

## Arranque local

```bash
cp .env.example .env   # VITE_API_BASE_URL=http://localhost:5192
npm install
npm run dev
```

Abre `http://localhost:5173`. La API debe estar en `http://localhost:5192` (Docker Compose).

## Docker

Incluido en el `docker-compose.yml` de la raíz:

- Frontend: `http://localhost:5173`
- API (host): `http://localhost:5192` → contenedor `:8080`

`VITE_API_BASE_URL` se inyecta en **build time** (el navegador habla con el host, no con el nombre Docker interno).

## Credenciales demo

- Usuario: `admin`
- Password: `Admin123!`

Solo existen si la API arranca con `SEED_DEFAULT_ADMIN=true` (lo activa el `.env` de demo de la raíz; ver README principal).
