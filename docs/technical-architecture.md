# Arquitectura Técnica — Sistema de Gestión

## 1. Propósito

Define la arquitectura del sistema: monolito modular, backend en .NET 10 con separación en capas y frontend Next.js orientado por features. Es la referencia técnica oficial junto con `AGENTS.md`.

## 2. Stack

```text
Backend:  C# 14 · .NET 10 LTS · ASP.NET Core 10 · EF Core 10 · Npgsql · PostgreSQL
          Firebase Authentication (Admin SDK) · Supabase Storage · Serilog · OpenAPI
Frontend: Next.js 16 (App Router) · React · TypeScript estricto
          Bootstrap 5.3.8 · Bootstrap Icons · PWA
```

## 3. Principios

- Monolito **modular**; sin microservicios, eventos distribuidos ni infraestructura innecesaria.
- El **backend es la autoridad** en reglas de negocio y seguridad; el frontend solo mejora UX.
- Cambios incrementales pequeños y verificables; documentación y código evolucionan juntos.
- `Turnos`/`Agenda`/`Mayorista` quedan explícitamente fuera del alcance nuevo.

## 4. Arquitectura del backend

Dependencias (solo hacia abajo):

```text
SistemaGestion.Domain
        ↑
SistemaGestion.Application
        ↑
SistemaGestion.Infrastructure
        ↑
SistemaGestion.API
```

`SistemaGestion.Shared` contiene tipos transversales sin dependencias (Result, PagedResult) y es referenciado por Application y API. **Domain no depende de Infrastructure ni de Shared.**

### Capas

| Capa | Responsabilidad |
|------|-----------------|
| `Domain` | Entidades, value objects, enums, invariantes y reglas de negocio core |
| `Application` | Casos de uso, DTOs, validadores, interfaces, consultas/comandos, coordinación |
| `Infrastructure` | EF Core, PostgreSQL/Npgsql, Firebase (auth/storage), servicios externos, persistencia |
| `API` | Controllers delgados, middleware, configuración, autenticación, autorización, filtros, OpenAPI |

Reglas:

- Controllers **delgados**; sin lógica de negocio.
- Nunca exponer entidades EF: siempre DTOs.
- Cálculos comerciales deterministas en backend (`decimal`).
- Validación con FluentValidation en Application cuando corresponda; la API valida entrada antes del caso de uso.
- Errores en formato **Problem Details** (RFC 7807) vía middleware global.

## 5. Persistencia

- PostgreSQL es la única base de datos (`UseNpgsql`).
- DbContext: `SistemaGestionDbContext` en `Infrastructure/Persistence`.
- IDs principales `INTEGER`/`BIGINT` autogenerados (no UUID salvo justificación).
- Migraciones versionadas en Git; nunca editar la base de producción a mano.
- `AsNoTracking()` en consultas de solo lectura; sin lazy loading.
- Auditar operaciones críticas; registrar movimientos de stock; historial de estados de pedidos.
- Baja lógica (soft delete) solo cuando aporta valor histórico; los datos de pedidos/pagos/facturación se preservan.

## 6. Autenticación y autorización

Flujo:

```text
Firebase UID → Usuario interno → Roles → Permisos → Autorización → Endpoint
```

- Firebase administra la identidad externa; el backend valida los ID Tokens con Firebase Admin SDK.
- Roles fijos: `SuperAdmin`, `Owner`, `Admin`, `Empleado`, `Cliente`.
- Permisos granulares configurables (p. ej. `CLIENTES_VIEW`, `CLIENTES_CREATE`). La autorización nunca depende solo del nombre del rol.
- El backend determina siempre la identidad real; nunca confiar en IDs/roles/permissions enviados por el frontend.
- Aislamiento de cliente: propiedad de recursos verificada en backend.

## 7. Storage

- Imágenes en **Supabase Storage** (decisión del proyecto que reemplaza a
  Firebase Storage — AGENTS.md §3.6); PostgreSQL guarda solo las tablas de
  referencias `ProductoImagenes` y `TrabajoRealizadoImagenes` (BR-101, BR-192).
- Implementación: `IStorageService` (Application) →
  `SupabaseStorageService` (Infrastructure) con la API REST del bucket,
  usando `SUPABASE_URL` / `SUPABASE_STORAGE_BUCKET` /
  `SUPABASE_SERVICE_ROLE_KEY` del `.env` (nunca versionado).
- Validación de archivos: extensión, MIME, tamaño, autorización, ruta/eliminación y archivos huérfanos.
- Lectura pública mediante URLs `/storage/v1/object/public/...`;
  errores de storage → ProblemDetails `Storage.Error` (503).
- Consumidores: endpoints de imágenes de producto (api-spec §13),
  imágenes de trabajos realizados (api-spec §12C) y los recursos
  públicos catálogo (§12A) y vidriera (§12B).

## 8. API REST

- Prefijo `/api/v1`, convenciones consistentes, paginación server-side (page/pageSize+search+filtros+orden).
- Endpoints corresponden a capacidades de negocio, no a tablas de la base de datos.
- Health checks: `/health` (API + PostgreSQL + dependencias configuradas).
- OpenAPI actualizado junto al código.

## 9. Arquitectura del frontend

```text
frontend/src/
├── app/            → rutas (grupos: (public), (auth), (admin), (gestion), (cliente))
├── components/     → ui/, layout/, domain/
├── features/       → auth/, customers/, suppliers/, products/, ..., reports/
├── services/       → consumo centralizado de la API
├── hooks/
├── lib/
├── types/
├── config/
└── validators/
```

- Sin `fetch()` disperso: los módulos consumen la API mediante servicios centralizados.
- Bootstrap 5.3.8 + Bootstrap Icons como base visual; componentes reutilizables.
- PWA (implementada, roadmap Phase 21): manifest en `app/manifest.ts`,
  iconos en `public/icons/` y `public/sw.js` que cachea SÓLO assets
  inmutables de `/_next/static` (nunca `/api/` ni HTML); el service
  worker se registra en producción (`ServiceWorkerRegister`). Las
  operaciones comerciales siempre requieren conexión con la API.
- La lógica no vive toda en `app/`.

## 10. Observabilidad

- **Logging estructurado** con Serilog; nunca registrar contraseñas, ID tokens, claves ni secretos.
- **Auditoría funcional** (quién/qué/cuándo) separada del logging técnico.
- Errores globales con Problem Details sin stack traces en producción.

## 11. Testing

```text
Unit tests (xUnit)                    → Domain y Application
API integration tests                 → WebApplicationFactory
Infrastructure integration tests      → PostgreSQL aislado (Fase 4+)
E2E (Playwright)                      → flujos críticos (Fases 22-23)
```

EF InMemory puede usarse en ciertos unit tests, nunca como sustituto de PostgreSQL real.

E2E (implementado, roadmap Phases 22-23): suite Playwright en
`frontend/e2e/` (`npm run test:e2e`) con proyectos Chromium, workers
seriales y reintentos. Requiere API (`:7100`) y Next (`:3000`) activos
y los usuarios demo de `playwright.config.ts`. Flujos cubiertos:
smoke de accesos y vidriera pública, flujo comercial completo
(presupuesto → pedido → pago) y administración de trabajos realizados.

## 12. Deployment

- Entornos separados: Development, Testing, Staging, Production.
- Objetivo: Render (Web Services Docker) + PostgreSQL en Supabase.
- Blueprint `render.yaml`, Dockerfiles y guía operativa en
  `docs/deployment-render.md`.
- Las migraciones se aplican al arrancar la API con
  `Database:MigrateOnStartup` (el plan free de Render no ofrece
  `preDeployCommand`).
- CORS se configura por entorno con `Cors:AllowedOrigins`; el frontend
  llama a la API directamente.