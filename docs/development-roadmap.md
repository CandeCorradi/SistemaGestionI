# Development Roadmap — Sistema de Gestión

## 1. Purpose

This document defines the implementation roadmap for the **Sistema de Gestión**.

It is intended to be used by developers and coding agents, particularly OpenCode, as an operational implementation plan.

The roadmap defines:

- implementation phases;
- task dependencies;
- implementation order;
- validation gates;
- testing requirements;
- migration strategy from legacy software;
- Definition of Done;
- technical constraints;
- release criteria.

The coding agent MUST follow the phases in order unless a dependency explicitly allows parallel development.

---

# 2. Project Baseline

The project MUST use the following technology baseline.

## Backend

```text
.NET 10 LTS
ASP.NET Core 10
C# 14
Entity Framework Core 10
```

## Database

```text
PostgreSQL
Npgsql Entity Framework Core Provider
EF Core Migrations
```

## Frontend

```text
Next.js
TypeScript
Bootstrap 5.3.8
Bootstrap Icons
PWA
```

## Authentication and Storage

```text
Firebase Authentication
Supabase Storage
```

## Deployment

```text
Render (Docker: API + frontend)
Supabase PostgreSQL
```

Los servicios concretos se definen en `render.yaml` y se documentan en
`docs/deployment-render.md` antes de cada release.

---

# 3. Architectural Baseline

Backend architecture:

```text
SistemaGestion.Domain
        ↓
SistemaGestion.Application
        ↓
SistemaGestion.Infrastructure
        ↓
SistemaGestion.API
```

Frontend:

```text
Next.js App Router
        ↓
Features
        ↓
Components
        ↓
API Services
        ↓
ASP.NET Core API
```

Database:

```text
PostgreSQL
        ↑
Npgsql
        ↑
EF Core 10
```

---

# 4. Implementation Principles

## 4.1 Build Incrementally

The system MUST be implemented in small, verifiable increments.

The coding agent MUST NOT generate the entire application in one uncontrolled operation.

Each phase MUST finish with validation.

---

## 4.2 Backend Before Frontend Integration

For each major business module:

```text
Domain
    ↓
Application
    ↓
Infrastructure
    ↓
API
    ↓
Tests
    ↓
Frontend
```

The frontend SHOULD consume stable API contracts instead of inventing its own data model.

---

## 4.3 No Premature Overengineering

The project MUST NOT introduce:

- microservices;
- event buses;
- Kafka;
- RabbitMQ;
- Redis;
- distributed CQRS;
- Kubernetes;
- complex service meshes;

unless a future requirement explicitly justifies them.

The initial architecture is a modular monolith.

---

# 5. Phase 0 — Repository and Environment Preparation

## Objective

Create a clean and reproducible development environment.

## Tasks

### 0.1 Repository structure

Create:

```text
/
├── src/
├── tests/
├── frontend/
├── docs/
├── scripts/
├── .github/
├── AGENTS.md
├── README.md
├── global.json
└── .gitignore
```

---

### 0.2 .NET SDK

Install and configure .NET 10 SDK.

Pin the SDK using:

```text
global.json
```

The repository MUST NOT depend on whichever SDK happens to be installed on the developer's machine.

---

### 0.3 Node.js

Configure the Node.js version used by the frontend.

The version MUST be documented and reproducible in development and CI/CD.

---

### 0.4 Git

Configure:

- `.gitignore`;
- branch conventions;
- commit conventions;
- repository documentation.

---

### 0.5 Environment variables

Create environment templates:

```text
.env.example
```

Secrets MUST NOT be committed.

---

## Gate

Phase 0 is complete when:

- .NET 10 builds;
- frontend installs successfully;
- repository structure exists;
- environment configuration is documented;
- no secrets are committed;
- CI can execute a basic build.

---

# 6. Phase 1 — Legacy System Audit

## Objective

Understand the existing system before replacing it.

The legacy system MUST be treated as a source of business knowledge, not as the architecture of the new application.

---

## 6.1 Inventory Legacy Components

Analyze:

```text
ASP.NET Core backend
Windows Forms
Blazor
MAUI
EF Core
Firebase
Existing database
Reports
Authentication
Existing migrations
```

---

## 6.2 Existing Controllers

Review legacy controllers including:

```text
AuthController
ClientesController
MayoristasController
PedidosController
DetallePedidosController
PagosController
PresupuestosController
UsuariosController
```

Determine:

- useful behavior;
- obsolete behavior;
- business rules;
- validation;
- dependencies;
- technical debt.

---

## 6.3 Legacy Entities

Analyze:

```text
Cliente
Mayorista
Usuario
Pedido
DetallePedido
Pago
Presupuesto
DetallePresupuesto
ProductoPresupuesto
```

Map useful concepts into the new domain.

---

## 6.4 Legacy Terminology

Replace:

```text
Mayorista
```

with:

```text
Proveedor
```

in the new system.

---

## 6.5 Legacy Turnos

The legacy Turnos functionality MUST NOT be migrated.

---

## 6.6 Migration Classification

Each relevant legacy component MUST receive one classification:

```text
REUSE
ADAPT
REWRITE
DEPRECATE
DELETE
```

---

## Gate

Do not begin domain implementation until the legacy audit has identified:

- reusable business behavior;
- obsolete behavior;
- data that needs migration;
- functionality explicitly excluded.

---

# 7. Phase 2 — Solution and Architecture Skeleton

## Objective

Create the backend solution according to the defined architecture.

Create:

```text
src/
├── SistemaGestion.Domain/
├── SistemaGestion.Application/
├── SistemaGestion.Infrastructure/
└── SistemaGestion.API/
```

Tests:

```text
tests/
├── SistemaGestion.Domain.UnitTests/
├── SistemaGestion.Application.UnitTests/
├── SistemaGestion.Infrastructure.IntegrationTests/
└── SistemaGestion.API.IntegrationTests/
```

---

## 7.1 Project Dependencies

Allowed dependency direction:

```text
Domain
↑
Application
↑
Infrastructure
↑
API
```

More precisely:

```text
Domain
    ← Application
    ← Infrastructure
    ← API
```

Domain MUST NOT depend on Infrastructure.

---

## 7.2 Initial Infrastructure

Configure:

- dependency injection;
- configuration;
- logging;
- PostgreSQL;
- EF Core;
- API pipeline;
- health checks;
- OpenAPI.

---

## Gate

The solution MUST:

```bash
dotnet restore
dotnet build
dotnet test
```

successfully before continuing.

---

# 8. Phase 3 — Domain Model

## Objective

Implement the core domain independently from infrastructure.

---

## 8.1 Initial Entities

Define entities including, as required:

```text
Usuario
Rol
Permiso
Cliente
Proveedor
Producto
CategoriaProducto
ImagenProducto
Stock
MovimientoStock
Presupuesto
DetallePresupuesto
Pedido
DetallePedido
HistorialPedido
Pago
Notificacion
```

Additional entities MAY be introduced only when justified by a functional requirement.

---

## 8.2 Value Objects

Use value objects where they improve domain integrity.

Possible examples:

```text
Money
Email
PhoneNumber
Address
Dimensions
```

Do not create value objects merely for abstraction.

---

## 8.3 Enumerations

Define domain enums for:

```text
UserRole
QuoteStatus
OrderStatus
PaymentMethod
InventoryMovementType
NotificationStatus
```

---

## 8.4 Domain Rules

Implement core invariants:

- valid money values;
- valid quantities;
- valid state transitions;
- valid stock operations;
- entity identity;
- required relationships.

---

## 8.5 Domain Tests

Every important domain rule MUST have unit tests.

Examples:

```text
Cannot create quote without items.
Cannot accept expired quote.
Cannot move order to invalid status.
Cannot register negative payment.
Cannot reduce stock below zero.
```

---

## Gate

The domain MUST compile and its unit tests MUST pass without requiring PostgreSQL, Firebase or HTTP.

---

# 9. Phase 4 — Database and Persistence

## Objective

Implement PostgreSQL persistence using EF Core 10.

---

## 9.1 DbContext

Create:

```text
SistemaGestionDbContext
```

---

## 9.2 Entity Configurations

Prefer separate configurations:

```text
Configurations/
├── UsuarioConfiguration
├── ClienteConfiguration
├── ProveedorConfiguration
├── ProductoConfiguration
├── PresupuestoConfiguration
├── PedidoConfiguration
├── PagoConfiguration
└── ...
```

---

## 9.3 PostgreSQL

Use:

```csharp
UseNpgsql(connectionString)
```

The project MUST NOT introduce SQL Server EF providers.

---

## 9.4 Database Constraints

Use PostgreSQL constraints for:

- required relationships;
- uniqueness;
- foreign keys;
- appropriate numeric constraints.

Business logic MUST NOT rely exclusively on database constraints.

---

## 9.5 Migrations

Create the initial migration only after the domain and persistence model are sufficiently stable.

Example:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

The production database MUST be updated through controlled migrations.

---

## 9.6 Indexes

Create indexes based on real access patterns.

Potential indexes:

```text
Cliente.Email
Producto.Codigo
Pedido.ClienteId
Pedido.Estado
Pedido.Fecha
Presupuesto.ClienteId
Presupuesto.Estado
Pago.PedidoId
MovimientoStock.ProductoId
Notificacion.UsuarioId
```

The final index strategy MUST be validated against query usage.

---

## Gate

The system MUST be capable of:

```text
Create database
Apply migrations
Insert test data
Query test data
Rollback/recover according to migration strategy
```

using PostgreSQL.

---

# 10. Phase 5 — Authentication and Authorization

## Objective

Implement secure identity and granular authorization.

---

## 10.1 Firebase Authentication

Configure Firebase Authentication.

The frontend authenticates with Firebase.

The backend validates Firebase ID tokens.

---

## 10.2 Internal User

Associate:

```text
Firebase UID
        ↓
Usuario
        ↓
Rol
        ↓
Permisos
```

---

## 10.3 Roles

Implement exactly:

```text
SuperAdmin
Owner
Admin
Empleado
Cliente
```

---

## 10.4 Permissions

Implement granular permissions.

Examples:

```text
CLIENTES_VIEW
CLIENTES_CREATE
CLIENTES_UPDATE

PRODUCTOS_VIEW
PRODUCTOS_CREATE
PRODUCTOS_UPDATE

PRESUPUESTOS_VIEW
PRESUPUESTOS_CREATE
PRESUPUESTOS_ACCEPT

PEDIDOS_VIEW
PEDIDOS_UPDATE_STATUS

PAGOS_VIEW
PAGOS_CREATE

REPORTES_VIEW
```

---

## 10.5 Authorization Tests

Tests MUST verify:

- unauthenticated access;
- authenticated access;
- role restrictions;
- permission restrictions;
- customer isolation;
- direct API access without UI.

---

## Gate

No protected business endpoint may be considered complete until authentication and authorization tests exist.

---

# 11. Phase 6 — Core API Infrastructure

## Objective

Establish consistent API behavior.

Implement:

- API versioning convention;
- Problem Details;
- validation;
- exception handling;
- authentication middleware;
- authorization;
- pagination;
- logging;
- health checks;
- OpenAPI.

Base route:

```text
/api/v1
```

---

## Gate

Create a protected test endpoint and verify:

```text
401 without authentication
403 without permission
200 with valid authorization
```

---

# 12. Phase 7 — Customer and Supplier Modules

## Objective

Implement basic master data.

---

## Customer

Implement:

```text
Create
Read
Update
Search
Filter
Deactivate
```

---

## Supplier

Implement:

```text
Create
Read
Update
Search
Filter
Deactivate
```

The system MUST use `Proveedor`, not `Mayorista`.

---

## Tests

Cover:

- validation;
- duplicate data;
- authorization;
- deactivation;
- customer isolation;
- pagination.

---

## Gate

Customer and supplier APIs MUST be stable enough for frontend integration.

---

# 13. Phase 8 — Product and Catalog

## Objective

Implement products, furniture and showroom functionality.

---

## 13.1 Products

Implement:

```text
Create
Read
Update
Deactivate
Search
Filter
```

---

## 13.2 Product Types

Support:

```text
Standard
Custom
```

---

## 13.3 Images

Integrate Supabase Storage (bucket `productos`, service role key kept
server-side, public object URLs for the catalog).

Database stores:

```text
storage path / URL
```

not binary image data.

---

## 13.4 Catalog

Implement:

- product listing;
- search;
- filters;
- details;
- images;
- showroom visibility.

---

## Gate

A customer must be able to browse catalog information without gaining administrative access.

---

# 13A. Phase 8A — Works Showcase (Showroom)

## Objective

Implement the gallery of finished works (AGENTS.md §23, §69 order
item 29) on top of the catalog infrastructure.

## Works management

- [x] `TrabajoRealizado` / `TrabajoRealizadoImagen` entities with
  domain validation;
- [x] CRUD under `/api/v1/trabajos-realizados` with granular
  permissions `TRABAJOS_VIEW/CREATE/UPDATE/DELETE` (ids 38–41);
- [x] publish/unpublish and featured flags (patch semantics);
- [x] tag normalization (max 10, case-insensitive duplicates);
- [x] image gallery on Supabase Storage (BR-192), cover by `orden`.

## Public showcase

- [x] anonymous endpoints `/api/v1/showroom/works` and
  `/api/v1/showroom/works/{id}` (BR-190);
- [x] client privacy on published works (BR-191);
- [x] `/showroom` and `/showroom/[id]` public pages;
- [x] `/trabajos` admin page with search, filters and permissions.

## Gate

An anonymous visitor can browse published works while drafts and
client names stay hidden, and only authorized staff can manage the
gallery.

---

# 14. Phase 9 — Inventory

## Objective

Implement stock management and history.

---

## Tasks

Implement:

```text
Current stock
Stock entry
Stock exit
Adjustment
Movement history
Low-stock detection
```

---

## Critical Requirements

Inventory changes MUST be:

- atomic;
- auditable;
- concurrency-safe;
- permission-controlled.

---

## Tests

Test:

```text
Valid increase
Valid decrease
Invalid negative stock
Concurrent modification
Unauthorized adjustment
Movement history
```

---

## Gate

Inventory consistency MUST be demonstrated through integration tests against PostgreSQL.

---

# 15. Phase 10 — Quotes

## Objective

Implement the complete quote lifecycle.

---

## Tasks

Implement:

```text
Create
Edit
View
Send
View
Accept
Reject
Cancel
Expire
```

---

## Quote Snapshot

Store historical:

```text
description
quantity
unit price
discount
tax
subtotal
total
custom specifications
```

---

## Customer Quote Requests

Allow customers to request quotes.

---

## Quote Acceptance

Implement:

```text
Validate
    ↓
Accept
    ↓
Create Order
    ↓
Commit Transaction
    ↓
Notify
```

---

## Tests

Mandatory tests:

- quote without items;
- expired quote;
- rejected quote;
- duplicate acceptance;
- customer isolation;
- price snapshot;
- acceptance transaction;
- order creation.

---

## Gate

Quote acceptance MUST reliably produce exactly one corresponding order.

---

# 16. Phase 11 — Orders and Production

## Objective

Implement order lifecycle.

---

## Statuses

Initial lifecycle:

```text
Pending
    ↓
Confirmed
    ↓
InProduction
    ↓
Ready
    ↓
Delivered
    ↓
Completed
```

Cancellation MUST follow explicitly permitted transitions.

---

## Tasks

Implement:

```text
Create (atomically via quote acceptance, BR-070 — no standalone POST)
View
Update permitted information
Change status
History
Production tracking
Delivery information
```

---

## Tests

Verify:

- valid transitions;
- invalid transitions;
- cancellation;
- history;
- authorization;
- customer visibility.

---

## Gate

Every relevant status change MUST generate history.

---

# 17. Phase 12 — Payments

## Objective

Implement payment registration and balances.

---

## Tasks

Implement:

```text
Register payment
View payments
Payment history
Outstanding balance
Payment method
Reference
```

---

## Rules

The backend MUST calculate:

```text
Order Total
-
Confirmed Payments
=
Outstanding Balance
```

The frontend MUST NOT calculate the authoritative balance.

---

## Tests

Verify:

- positive amount;
- overpayment;
- payment association;
- balance calculation;
- authorization;
- historical integrity.

---

# 18. Phase 13 — Notifications

## Objective

Implement business notifications.

Events SHOULD include:

```text
Quote sent
Quote accepted
Order created
Order status changed
Payment registered
Order ready
```

---

## Tasks

Implement:

```text
List notifications
Mark as read
Mark all as read
Unread count
```

---

## Gate

A user MUST only access their own notifications.

---

# 19. Phase 14 — Invoicing

## Objective

Implement initial simulated invoicing.

---

## Requirements

The invoice module MUST:

- generate the required internal document;
- preserve historical information;
- clearly distinguish simulated documentation from official fiscal authorization;
- isolate future fiscal integration.

---

## Architecture

Create an abstraction such as:

```text
IBillingService
```

Possible implementations:

```text
SimulatedBillingService
FutureArcaBillingService
```

The ARCA implementation is outside the first release.

---

# 20. Phase 15 — Reports

## Objective

Implement operational reporting.

Reports:

```text
Quotes
Orders
Payments
Sales
Inventory
```

---

## Filters

Support relevant:

```text
Date
Customer
Status
Product
Payment method
```

---

## Exports

Implement:

```text
PDF
XLSX
CSV
```

---

## Tests

Verify:

- authorization;
- filters;
- totals;
- date ranges;
- historical values;
- exports.

---

# 21. Phase 16 — Frontend Foundation

## Objective

Create the Next.js application.

---

## Structure

Suggested:

```text
frontend/
├── app/
│   ├── (public)/
│   ├── (auth)/
│   ├── (admin)/
│   ├── (gestion)/
│   └── (cliente)/
│
├── components/
│   ├── ui/
│   ├── layout/
│   └── domain/
│
├── features/
│   ├── auth/
│   ├── customers/
│   ├── suppliers/
│   ├── products/
│   ├── inventory/
│   ├── quotes/
│   ├── orders/
│   ├── payments/
│   ├── notifications/
│   └── reports/
│
├── services/
├── hooks/
├── lib/
├── types/
└── validators/
```

---

# 22. Phase 17 — Bootstrap Design System

## Objective

Establish a consistent UI system.

Use:

```text
Bootstrap 5.3.8
Bootstrap Icons
```

Bootstrap MUST be the primary CSS/UI foundation.

Do not introduce additional UI frameworks without explicit approval.

---

## Components

Create reusable components:

```text
Button
Input
Select
Modal
Card
Table
Badge
Alert
Pagination
Spinner
EmptyState
ConfirmDialog
```

---

## Design Principle

Business pages MUST consume reusable components instead of duplicating UI patterns.

---

# 23. Phase 18 — Frontend Authentication

## Objective

Connect Next.js with Firebase Authentication.

Implement:

```text
Login
Logout
Session handling
Password recovery
Profile
Protected routes
```

---

## Authorization

The frontend MAY hide unauthorized functionality for UX.

However:

> Backend authorization remains authoritative.

---

# 24. Phase 19 — Administrative Frontend

Implement modules in this order:

```text
Dashboard
    ↓
Customers
    ↓
Suppliers
    ↓
Products
    ↓
Inventory
    ↓
Quotes
    ↓
Orders
    ↓
Payments
    ↓
Reports
    ↓
Users / Permissions
```

Each module MUST consume the API through centralized services.

Do not scatter arbitrary `fetch()` calls throughout components.

---

# 25. Phase 20 — Client Portal

Implement:

```text
Client Dashboard
Profile
Catalog
Product details
Quote requests
Quotes
Quote acceptance
Orders
Order tracking
Payments
Notifications
WhatsApp contact
```

---

## Customer Isolation

The frontend MUST never assume that a customer can access an arbitrary resource ID.

The backend MUST validate ownership.

---

# 26. Phase 21 — PWA

## Objective

Transform the frontend into a Progressive Web App.

Implement:

- web app manifest;
- icons;
- installability;
- responsive design;
- appropriate caching;
- offline strategy where useful.

The application MUST NOT pretend to support offline business transactions if backend connectivity is required.

---

# 27. Phase 22 — Integration Testing

## Objective

Validate complete workflows.

Use PostgreSQL integration environments.

> **Estado: implementado.** Cobertura backend con xUnit (Domain,
> Application, Infrastructure y API/WebApplicationFactory, 854 tests)
> más la suite E2E de navegador (`frontend/e2e/`) que ejercita los
> flujos A-D contra la API real. Flujo E cubierto por unit tests de
> Application y tests de Infrastructure.

Mandatory flows:

### Flow A

```text
Register Customer
↓
Create Quote
↓
Send Quote
↓
Customer Views Quote
```

### Flow B

```text
Customer Accepts Quote
↓
Quote Accepted
↓
Order Created
```

### Flow C

```text
Order
↓
Production
↓
Ready
↓
Delivered
↓
Completed
```

### Flow D

```text
Order
↓
Payment
↓
Balance Updated
```

### Flow E

```text
Inventory
↓
Movement
↓
Stock Updated
↓
History Created
```

---

# 28. Phase 23 — End-to-End Testing

Use browser-based E2E testing for critical user journeys.

> **Estado: implementado.** Suite Playwright en `frontend/e2e/`
> (`npm run test:e2e`, Chromium, workers seriales, reintentos).
> Requiere API (`:7100`) y Next (`:3000`) activos. Journeys cubiertos:
> login admin y cliente, creación de cliente y presupuesto, aceptación
> en el portal, creación automática del pedido, actualización de
> estado, registro de pago, vidriera pública (showroom) y
> administración de trabajos realizados.

Minimum journeys:

```text
Admin login
Customer login
Create customer
Create product
Create quote
Customer accepts quote
Order status update
Register payment
View notification
Generate report
```

---

# 29. Phase 24 — Security Validation

Perform security checks for:

- authentication;
- authorization;
- customer isolation;
- IDOR;
- invalid tokens;
- expired tokens;
- file upload;
- Supabase Storage bucket policies;
- input validation;
- SQL injection;
- XSS;
- CSRF where applicable;
- CORS;
- secrets;
- logging;
- error disclosure.

---

# 30. Phase 25 — Performance Validation

Initial target:

```text
~50 concurrent users
```

For normal API operations:

```text
Target response time < 2 seconds
```

under expected development/production load.

Performance tests SHOULD identify:

- slow queries;
- missing indexes;
- excessive API requests;
- N+1 queries;
- oversized responses;
- inefficient frontend rendering.

---

# 31. Phase 26 — Production Configuration

Prepare:

```text
Production environment variables
Firebase production project
PostgreSQL production database
Storage configuration
CORS
HTTPS
Logging
Health checks
```

Secrets MUST be stored through secure environment/secret management.

---

# 32. Phase 27 — Render Deployment

Deploy:

```text
Next.js
ASP.NET Core API
PostgreSQL
```

using the Render architecture defined in `render.yaml`.

The exact Render services MUST be documented before production deployment
(`docs/deployment-render.md`).

---

# 33. Phase 28 — CI/CD

CI pipeline MUST execute:

```text
Restore
↓
Build
↓
Lint
↓
Unit tests
↓
Integration tests
↓
Frontend tests
↓
Production build
↓
Security checks
↓
Deploy
```

A failed required stage MUST block deployment.

---

# 34. Phase 29 — Documentation Completion

Update:

```text
README.md
AGENTS.md
technical-architecture.md
functional-specification.md
business-rules.md
api-specification.md
development-roadmap.md
```

Documentation MUST reflect the implemented system.

Documentation and implementation MUST NOT intentionally diverge.

---

# 35. Phase 30 — Final Acceptance

The project is ready for release candidate evaluation when:

## Backend

- [ ] .NET 10 LTS
- [ ] ASP.NET Core 10
- [ ] EF Core 10
- [ ] PostgreSQL
- [ ] migrations working
- [ ] authentication working
- [ ] authorization working
- [ ] permissions working
- [ ] validation working
- [ ] logging working
- [ ] health checks working
- [ ] OpenAPI available

## Business

- [ ] customers
- [ ] suppliers
- [ ] products
- [x] showroom
- [ ] custom furniture
- [ ] inventory
- [ ] quotes
- [ ] quote acceptance
- [ ] automatic order creation
- [ ] order lifecycle
- [ ] order history
- [ ] payments
- [ ] notifications
- [ ] simulated invoicing
- [ ] reports

## Frontend

- [ ] Next.js
- [ ] TypeScript
- [ ] Bootstrap
- [ ] responsive UI
- [ ] administrative portal
- [ ] customer portal
- [ ] protected routes
- [x] PWA

## Infrastructure

- [ ] Firebase Authentication
- [ ] Supabase Storage
- [ ] PostgreSQL production
- [ ] Render deployment
- [ ] environment configuration
- [ ] HTTPS
- [ ] CORS
- [ ] CI/CD

## Testing

- [x] Domain unit tests
- [x] Application unit tests
- [x] Integration tests
- [x] API tests
- [x] E2E tests
- [x] authorization tests
- [ ] storage/security tests
- [ ] performance validation

---

# 36. Definition of Done

A task is NOT considered complete merely because the code compiles.

A task is complete only when:

1. the implementation satisfies its functional requirement;
2. business rules are enforced;
3. authorization is implemented;
4. validation exists;
5. database persistence works where applicable;
6. automated tests exist;
7. relevant integration tests pass;
8. API contracts are documented;
9. frontend behavior is implemented where applicable;
10. errors are handled;
11. logging is appropriate;
12. no secrets are committed;
13. documentation is updated when behavior changes;
14. code follows the established architecture;
15. no unrelated technical debt is introduced.

---

# 37. Coding Agent Rules

OpenCode MUST follow these rules.

## Rule 1 — Respect the architecture

Do not move business logic into controllers.

Do not access PostgreSQL directly from frontend code.

Do not make the Domain depend on Infrastructure.

---

## Rule 2 — Respect technology versions

Backend MUST target:

```text
net10.0
```

Do not downgrade to .NET 8 without explicit authorization.

EF Core packages MUST remain aligned with major version 10.

---

## Rule 3 — PostgreSQL is mandatory

Use:

```text
Npgsql
```

Do not introduce:

```text
Microsoft.EntityFrameworkCore.SqlServer
```

or SQL Server-specific infrastructure.

---

## Rule 4 — Bootstrap is mandatory

Use:

```text
Bootstrap 5.3.8
```

Do not introduce Tailwind, Material UI, Chakra UI or another complete UI framework without explicit authorization.

---

## Rule 5 — Do not invent business rules

If implementation requires an unspecified business decision:

```text
STOP
DOCUMENT THE DECISION
REQUEST CLARIFICATION
```

Do not silently invent behavior.

---

## Rule 6 — Tests are part of implementation

Every significant business feature MUST include tests.

---

## Rule 7 — Do not break existing functionality without checking dependencies

Before modifying shared code:

```text
Search usages
↓
Understand dependencies
↓
Modify
↓
Run tests
```

---

## Rule 8 — Prefer small changes

Avoid massive uncontrolled refactors.

Implement one coherent change at a time.

---

## Rule 9 — Verify before declaring completion

Before marking a task complete:

```text
Build
↓
Test
↓
Inspect diff
↓
Check architecture
↓
Update documentation
```

---

# 38. Recommended Implementation Order

The preferred execution order is:

```text
01 Repository
      ↓
02 Legacy Audit
      ↓
03 Architecture Skeleton
      ↓
04 Domain
      ↓
05 PostgreSQL / EF Core
      ↓
06 Authentication / Authorization
      ↓
07 API Infrastructure
      ↓
08 Customers / Suppliers
      ↓
09 Products / Catalog
      ↓
10 Inventory
      ↓
11 Quotes
      ↓
12 Orders
      ↓
13 Payments
      ↓
14 Notifications
      ↓
15 Invoicing
      ↓
16 Reports
      ↓
17 Frontend Foundation
      ↓
18 Bootstrap Design System
      ↓
19 Frontend Authentication
      ↓
20 Admin Portal
      ↓
21 Client Portal
      ↓
22 PWA
      ↓
23 Integration Tests
      ↓
24 E2E
      ↓
25 Security
      ↓
26 Performance
      ↓
27 Production
      ↓
28 Render
      ↓
29 CI/CD
      ↓
30 Final Acceptance
```

---

# 39. Parallelization Rules

Some tasks may be developed in parallel after their dependencies are stable.

Example:

```text
Backend Customers ──────┐
Backend Suppliers ──────┤
Backend Products ───────┼──→ Frontend integration
Backend Inventory ──────┤
Backend Quotes ─────────┘
```

However:

```text
Domain
↓
Persistence
↓
Application
↓
API
```

dependencies MUST NOT be bypassed merely to accelerate implementation.

---

# 40. Milestones

## M1 — Technical Foundation

Completed when:

```text
.NET 10
PostgreSQL
EF Core 10
Architecture
CI
Tests
```

are operational.

---

## M2 — Security Foundation

Completed when:

```text
Firebase Auth
Users
Roles
Permissions
Authorization
```

are operational.

---

## M3 — Commercial Foundation

Completed when:

```text
Customers
Suppliers
Products
Inventory
```

are operational.

---

## M4 — Commercial Lifecycle

Completed when:

```text
Quotes
Acceptance
Orders
Production
Payments
```

are operational.

---

## M5 — Customer Experience

Completed when:

```text
Catalog
Client Portal
Quotes
Orders
Payments
Notifications
WhatsApp
```

are operational.

---

## M6 — Management and Reporting

Completed when:

```text
Dashboard
Reports
PDF
XLSX
CSV
Simulated invoicing
```

are operational.

---

## M7 — Production Readiness

Completed when:

```text
Security
Performance
E2E
CI/CD
Render
Documentation
```

are validated.

---

# 41. Final Release Criteria

The first production-ready release MUST satisfy all of the following:

```text
Functional requirements
        +
Business rules
        +
Security requirements
        +
Automated testing
        +
Database integrity
        +
API contract
        +
Frontend usability
        +
Deployment reproducibility
        +
Documentation
```

No feature should be considered production-ready if it only works in the developer's local environment.

---

# 42. Change Management

New requirements MUST be evaluated against:

```text
Functional Specification
Business Rules
Technical Architecture
API Specification
Roadmap
```

A requirement that modifies one of these documents MUST trigger a review of dependent documents.

The coding agent MUST identify affected modules before implementing significant requirement changes.

---

# 43. Roadmap Maintenance

This document is a living implementation plan.

When a phase is completed:

- mark the relevant checklist;
- record important deviations;
- update dependencies;
- update documentation;
- record technical decisions when necessary.

The roadmap MUST reflect the actual state of the repository.