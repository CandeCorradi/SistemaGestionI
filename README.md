# Sistema de Gestión

Sistema de gestión integral para **una única carpintería**: clientes, proveedores, productos, muebles, inventario, presupuestos, pedidos, pagos, facturación simulada, notificaciones, reportes, catálogo/showroom y portal cliente.

El proyecto sigue las reglas operativas de `AGENTS.md` y la documentación de `docs/`.

## Stack

- **Backend:** C# / .NET 10 LTS / ASP.NET Core 10 / EF Core 10 / PostgreSQL (Npgsql) / Firebase Authentication / Supabase Storage / Serilog / OpenAPI
- **Frontend:** Next.js 16 (App Router) / React / TypeScript estricto / Bootstrap 5.3.8 / Bootstrap Icons / PWA

> Nota: no se permite .NET 8/9, MySQL/SQL Server ni Tailwind como framework visual principal (ver `AGENTS.md` §3).

## Estructura del repositorio

```text
/
├── src/            → Código nuevo (backend, monolito modular)
│   ├── SistemaGestion.Domain/
│   ├── SistemaGestion.Application/
│   ├── SistemaGestion.Infrastructure/
│   ├── SistemaGestion.API/
│   └── SistemaGestion.Shared/
├── tests/          → Proyectos de test (xUnit, WebApplicationFactory)
├── frontend/       → Aplicación Next.js
├── docs/           → Documentación técnica y funcional (fuente de verdad)
├── legacy/         → Código anterior (Windows Forms, Blazor, MAUI, API vieja) — SOLO referencia
├── scripts/
├── .github/        → Workflows CI/CD
├── AGENTS.md       → Reglas operativas obligatorias
└── global.json     → Pin del SDK .NET 10
```

## Requisitos del entorno

- SDK **.NET 10** (`global.json` fija la versión y exige la familia 10.0).
  Si no está instalado de forma global, se puede instalar sin administrador:

  ```powershell
  Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile "$env:TEMP\dotnet-install.ps1"
  & "$env:TEMP\dotnet-install.ps1" -Channel 10.0 -InstallDir "$env:LOCALAPPDATA\Microsoft\dotnet"
  ```

- **PostgreSQL** local (puerto 5432). El archivo `.env.example` documenta las variables necesarias.
- Node.js 20+ para el frontend.
- **Firebase** (proyecto de desarrollo) para autenticación y storage. Sin credenciales la API arranca igual; la autenticación queda inhabilitada.

## Puesta en marcha

```powershell
# Backend: restaurar, compilar y testear
dotnet restore SistemaGestion.sln --disable-parallel
dotnet build SistemaGestion.sln -m:1 -nodeReuse:false
dotnet test SistemaGestion.sln

# Levantar la API (compila y ejecuta en Development)
powershell -ExecutionPolicy Bypass -File scripts\run-api.ps1

# Frontend
cd frontend
npm install
npm run dev        # http://localhost:3000
```

API local: `http://localhost:7100` (Swagger/OpenAPI en `/openapi/v1.json` en Development). Health check en `/health`.

> **Nota (MSB4166):** si `dotnet run`/`dotnet build` termina con
> `MSB4166: el nodo secundario se cerró antes de tiempo`, compilá con
> `-m:1 -nodeReuse:false` (o usá `scripts\run-api.ps1`). Es un problema de
> MSBuild con varios nodos en equipos con recursos limitados, no de la API.

```powershell
# Crear la base de datos y aplicar migraciones (requiere PostgreSQL local)
dotnet ef database update --project src/SistemaGestion.Infrastructure --startup-project src/SistemaGestion.API
```

## Convenciones importantes

- La API usa el prefijo `/api/v1` y terminología nueva: `Proveedor` (no `Mayorista`).
- No exponer entidades EF directamente: usar DTOs.
- Dinero siempre en `decimal` (nunca `float`/`double`).
- Instantes de tiempo en UTC (desconectar de `DateTime.Now` en lógica de negocio).
- `Turnos`/`Agenda` está fuera de alcance (ver `AGENTS.md` §5).
- Ver detector de errores obligatorio al trabajar: consultar `AGENTS.md` §70 antes de escribir código.

## Documentación

```text
docs/
├── technical-architecture.md
├── functional-specification.md
├── business-rules.md
├── api-specification.md
├── development-roadmap.md
└── deployment-render.md
```

## Legacy

`legacy/` contiene el sistema anterior (ASP.NET Core 8 + MySQL, Windows Forms, Blazor WASM y .NET MAUI). Su función es de **referencia funcional y de negocio**, no define la arquitectura nueva. No migrarlo mecánicamente. Tampoco está incluido en la solución `SistemaGestion.sln`.