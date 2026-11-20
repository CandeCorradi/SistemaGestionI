# API Specification — Sistema de Gestión

## 1. Purpose

This document defines the REST API conventions and initial endpoint contract for the Sistema de Gestión.

The API is implemented with:

```text
ASP.NET Core 10
C#
EF Core 10
PostgreSQL
Firebase Authentication
```

The API MUST be versioned.

Base path:

```text
/api/v1
```

---

# 2. API Architecture

The API follows:

```text
HTTP Request
     ↓
Controller
     ↓
Application Use Case
     ↓
Domain
     ↓
Infrastructure
     ↓
PostgreSQL / Firebase / External Services
```

Controllers MUST remain thin.

Business logic MUST NOT be implemented directly inside controllers.

---

# 3. Authentication

Protected endpoints require a Firebase ID token.

Expected header:

```http
Authorization: Bearer <firebase-id-token>
```

The backend MUST:

1. extract the token;
2. validate the token;
3. identify the Firebase UID;
4. resolve the internal application user;
5. evaluate roles and permissions.

## Self-registration

```http
POST /api/v1/auth/register
```

Public endpoint (no token). Creates the account in Firebase
Authentication and the internal user with the fixed `Cliente` role
(BR-001). Rate limited per IP: 20 requests/minute → `429 Too Many
Requests`.

Request:

```json
{
  "email": "cliente@example.com",
  "password": "secret-password",
  "nombre": "Ana",
  "apellido": "Garcia"
}
```

Rules:

- `email`, `password`, `nombre` and `apellido` are required.
- `password`: minimum 6 characters, maximum 128.
- The internal user is always created with the `Cliente` role; the
  request cannot choose a role.
- If the internal user cannot be persisted, the Firebase account is
  deleted (best-effort rollback).

Responses:

```text
200 OK  → account created (body below)
400 Bad Request → Validation.Error (missing or invalid data)
409 Conflict    → Conflict.Error (email already registered)
429 Too Many Requests → rate limit exceeded
```

Response:

```json
{
  "id": 4,
  "email": "cliente@example.com",
  "nombre": "Ana",
  "apellido": "Garcia",
  "rol": "Cliente"
}
```

## External identity provisioning (Google)

```http
POST /api/v1/auth/google
```

Authenticated endpoint (`Authorization: Bearer <firebase-id-token>`). The
frontend signs the user in with the Firebase Google provider and then calls
this endpoint to provision the internal account. The backend derives the
UID, email and display name from the verified token — never from the
request body — and is idempotent:

- if the UID is already linked, returns the existing profile;
- if an internal user exists with the same email, links the UID (no
  duplicate);
- otherwise creates the internal user with the fixed `Cliente` role and
  its customer record (BR-010).

Rate limited per IP: 20 requests/minute → `429 Too Many Requests`.

Responses:

```text
200 OK          → internal account provisioned (AuthProfileDto)
400 Bad Request → Validation.Error (missing email / inactive account)
401 Unauthorized → invalid or missing Firebase ID token
409 Conflict    → the email is already linked to another identity
429 Too Many Requests → rate limit exceeded
```

Response:

```json
{
  "email": "cliente@example.com",
  "nombre": "Ana",
  "apellido": "Garcia",
  "rol": "Cliente",
  "activo": true
}
```

---

# 4. Authorization

Endpoints MUST specify required permissions.

Example:

```text
GET /api/v1/customers
```

may require:

```text
CLIENTES_VIEW
```

while:

```text
POST /api/v1/customers
```

may require:

```text
CLIENTES_CREATE
```

Authorization MUST be enforced server-side.

Behavior:

```text
No token / invalid token     → 401 Unauthorized
Valid token, missing permiso → 403 Forbidden
```

## Permission endpoints

```http
GET /api/v1/permissions
```

Permission catalog (all granular codes). Required permission:

```text
ROLES_VIEW
```

---

```http
GET /api/v1/auth/permissions
```

Effective permissions of the authenticated user (union of role
permissions and explicit user permissions). Used by the frontend to hide
functionality; the backend remains the authority.

Required: valid token only.

Response:

```json
{
  "codigos": ["CLIENTES_VIEW", "PRESUPUESTOS_VIEW"]
}
```

---

```http
GET /api/v1/auth/me
```

Identity extracted from the Firebase ID Token (uid, email, emailVerified).
Required: valid token only.

Response:

```json
{
  "uid": "firebase-uid",
  "email": "user@example.com",
  "emailVerified": true
}
```

---

```http
GET /api/v1/auth/profile
```

Internal profile of the authenticated user (roadmap §18 — Profile): email,
name, last name and role name of the linked internal user.

Required: valid token only.

Responses:

```text
200 OK  → profile
404 Not Found → uid has no active internal user (same rule as /auth/permissions)
```

Response:

```json
{
  "email": "admin@example.com",
  "nombre": "Ada",
  "apellido": "Lopez",
  "rol": "SuperAdmin",
  "activo": true
}
```

---

# 5. HTTP Methods

Use standard HTTP semantics:

```text
GET     Read
POST    Create/action
PUT     Full update
PATCH   Partial update
DELETE  Delete/deactivate where applicable
```

Business actions SHOULD use explicit endpoints where the operation represents a domain action.

Example:

```text
POST /api/v1/quotes/{id}/accept
```

instead of manipulating the status directly:

```text
PATCH /api/v1/quotes/{id}
{
  "status": "Accepted"
}
```

---

# 6. Standard Response

Collection endpoints SHOULD return:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 0,
  "totalPages": 0
}
```

---

# 7. Error Format

Errors SHOULD follow RFC 7807 Problem Details.

Example:

```json
{
  "type": "https://example.com/problems/validation-error",
  "title": "Validation error",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/v1/customers",
  "errors": {
    "email": [
      "The email format is invalid."
    ]
  }
}
```

The production API MUST NOT expose stack traces or internal implementation details.

---

# 8. HTTP Status Codes

Use:

```text
200 OK
201 Created
204 No Content

400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
422 Unprocessable Entity

500 Internal Server Error
```

Do not use `200 OK` for operations that actually failed.

---

# 9. Users

## GET

```http
GET /api/v1/users
```

List users.

Required permission:

```text
USUARIOS_VIEW
```

---

## GET by ID

```http
GET /api/v1/users/{id}
```

Required permission:

```text
USUARIOS_VIEW
```

---

## POST

```http
POST /api/v1/users
```

Create internal user.

Required permission:

```text
USUARIOS_CREATE
```

---

## PATCH

```http
PATCH /api/v1/users/{id}
```

Update permitted user properties.

Required permission:

```text
USUARIOS_UPDATE
```

---

## GET permissions

```http
GET /api/v1/users/{id}/permissions
```

Explicit permissions of a user (additional to role permissions).

Required permission:

```text
USUARIOS_VIEW
```

---

## PUT permissions

```http
PUT /api/v1/users/{id}/permissions
```

Replace the explicit permissions of an employee. Codes must belong to the
permission catalog. An empty list clears explicit permissions.

Required permission:

```text
USUARIOS_UPDATE
```

Request:

```json
{
  "codigos": ["CLIENTES_VIEW", "CLIENTES_CREATE"]
}
```

---

# 9A. Roles

## GET

```http
GET /api/v1/roles
```

List the five fixed roles (BR-001).

Required permission:

```text
ROLES_VIEW
```

---

# 9B. Client Portal (own resources)

All endpoints under `/api/v1/my` serve the authenticated **Cliente**
role (roadmap Phase 20, functional-spec §23). Identity comes only from
the Firebase ID Token (BR-010): the backend resolves the linked
customer record. They require an authenticated session (`401`
otherwise) but **no granular permissions** — the Cliente role has none
(AGENTS.md §9).

Ownership is strict (BR-004): any resource that does not belong to the
linked customer returns `404 Not Found` with the same body as an
unknown id, so existence is never leaked.

## Profile object

```json
{
  "id": 7,
  "email": "cliente@example.com",
  "nombre": "Ana",
  "apellido": "Gómez",
  "telefono": "+54 9 11 5555-5555",
  "direccion": "Av. Siempreviva 742",
  "documento": "30111222",
  "fechaCreacion": "2026-09-20T12:00:00Z"
}
```

## Summary object

```json
{
  "totalPedidos": 5000,
  "totalPagado": 1500,
  "saldo": 3500,
  "pedidosCount": 3,
  "presupuestosPendientes": 1
}
```

Totals and `saldo` are always computed by the backend (BR-083);
`totalPedidos` excludes cancelled orders.

## Get profile

```http
GET /api/v1/my/profile
```

Response: `200` with the profile object. `401` without a valid token;
`404` when the uid has no linked customer.

## Update profile

```http
PATCH /api/v1/my/profile
```

```json
{
  "nombre": "Ana",
  "apellido": "Gómez",
  "telefono": "+54 9 11 5555-5555",
  "direccion": "Av. Siempreviva 742",
  "documento": "30111222"
}
```

All fields are optional. Response: `200` with the updated profile
object; `400` for invalid values (e.g. blank name). The `email` is not
modifiable from the portal: it identifies the account (BR-010).

## Summary

```http
GET /api/v1/my/summary
```

Response: `200` with the summary object.

## List quotes (own)

```http
GET /api/v1/my/quotes?page=1&pageSize=20&search=&status=1
```

Parameters: `page`, `pageSize` (default 20, max 100), `search`,
`status` (EstadoPresupuesto). Response: paged result of quote objects
(§15) restricted to the caller's customer.

## Get quote

```http
GET /api/v1/my/quotes/{id}
```

Response: `200` with the quote object; `404` when unknown or foreign.

## Request quote

```http
POST /api/v1/my/quotes
```

```json
{
  "description": "Ropero a medida 2x2,10 m, melamina blanca",
  "cantidad": 1
}
```

Creates the quote in `Borrador` with a single descriptive line priced
at `0` for the team to price, and publishes the
`PresupuestoSolicitado` notification to the internal team. Response:
`201 Created` with `Location: /api/v1/my/quotes/{id}` and the quote
object; `400` when `description` is empty or `cantidad <= 0`.

## Accept quote

```http
POST /api/v1/my/quotes/{id}/accept
```

Accepts the caller's own quote and creates the order atomically
(BR-064/BR-065). Response: `200` with the quote object including
`orderId`; `404` when unknown or foreign; `409 Conflict` when the
quote is not in the `Enviado` state (already accepted, draft,
expired…).

## List orders (own)

```http
GET /api/v1/my/orders?page=1&pageSize=20&search=&status=2
```

Parameters: `page`, `pageSize`, `search`, `status` (EstadoPedido).
Response: paged result of order objects (§16) restricted to the
caller's customer.

## Get order

```http
GET /api/v1/my/orders/{id}
```

Response: `200` with the order object; `404` when unknown or foreign.

## Order history

```http
GET /api/v1/my/orders/{id}/history
```

Response: `200` with the array of history entries (§16); `404` when
the order is unknown or foreign. Enables order tracking (BR-075).

## Order payments

```http
GET /api/v1/my/orders/{id}/payments
```

Response: `200` with `{ items[], totalPaid, balance }` (§17);
`404` when the order is unknown or foreign. `balance` is computed by
the backend (BR-083).

## List payments (own)

```http
GET /api/v1/my/payments?page=1&pageSize=20
```

Parameters: `page`, `pageSize`. Response: paged result of payment
objects (§17) for the caller's orders.

---

# 10. Customers

## GET

```http
GET /api/v1/customers
```

Query parameters SHOULD include:

```text
page
pageSize
search
active
```

Required permission:

```text
CLIENTES_VIEW
```

---

## GET by ID

```http
GET /api/v1/customers/{id}
```

Required permission:

```text
CLIENTES_VIEW
```

---

## POST

```http
POST /api/v1/customers
```

Create a customer. Duplicate email returns `409 Conflict`.

Required permission:

```text
CLIENTES_CREATE
```

---

## PATCH

```http
PATCH /api/v1/customers/{id}
```

Partial update of permitted customer properties.

Required permission:

```text
CLIENTES_UPDATE
```

---

## DELETE/Deactivate

```http
DELETE /api/v1/customers/{id}
```

The implementation SHOULD deactivate instead of physically deleting when historical relationships exist.

Required permission:

```text
CLIENTES_DELETE
```

---

# 11. Suppliers

Use the term `suppliers` for the API surface and `Proveedor` for the
domain terminology (BR-020).

## GET

```http
GET /api/v1/suppliers
```

Query parameters SHOULD include:

```text
page
pageSize
search
active
```

Required permission:

```text
PROVEEDORES_VIEW
```

---

## GET by ID

```http
GET /api/v1/suppliers/{id}
```

Required permission:

```text
PROVEEDORES_VIEW
```

---

## POST

```http
POST /api/v1/suppliers
```

Create a supplier. `nombre` is required; `email` is optional (when
present, duplicate email returns `409 Conflict`).

Required permission:

```text
PROVEEDORES_CREATE
```

---

## PATCH

```http
PATCH /api/v1/suppliers/{id}
```

Partial update of permitted supplier properties.

Required permission:

```text
PROVEEDORES_UPDATE
```

---

## DELETE/Deactivate

```http
DELETE /api/v1/suppliers/{id}
```

The implementation MUST deactivate instead of physically deleting so
that supplier history related to orders and products is preserved
(BR-021).

Required permission:

```text
PROVEEDORES_DELETE
```

---

The API MUST NOT expose new endpoints named:

```text
mayoristas
```

---

# 12. Products

## GET

```http
GET /api/v1/products
```

Query parameters SHOULD include:

```text
page
pageSize
search
type
category
active
showroom
lowStock
isCustom
```

`type` accepts the `TipoProducto` values (`0` Mueble, `1` Melamina,
`2` Accesorio, `3` Insumo, `4` Otro).

`lowStock=true` returns products whose `stockActual` is at or below a
configured `stockMinimo` (greater than zero).

`isCustom` filters standard (`false`) vs custom/made-to-measure
(`true`) furniture, so the catalog can distinguish both
(functional-spec §9, roadmap §13.2).

Required permission:

```text
PRODUCTOS_VIEW
```

---

## GET by ID

```http
GET /api/v1/products/{id}
```

Required permission:

```text
PRODUCTOS_VIEW
```

---

## POST

```http
POST /api/v1/products
```

Create a product. `nombre`, `codigoSku`, `tipo` and `precio` are
required. Duplicate `codigoSku` (case-insensitive) returns
`409 Conflict` (BR-030). Negative price or cost returns
`400 Bad Request` (AGENTS.md §16).

`esPersonalizado` (default `false`) marks the product as
custom/made-to-measure so the catalog can distinguish standard and
custom furniture (functional-spec §9). The detailed specifications of
a piece made for a specific quote are preserved in the quote itself,
not here (BR-041).

Required permission:

```text
PRODUCTOS_CREATE
```

---

## PATCH

```http
PATCH /api/v1/products/{id}
```

Partial update of permitted product properties. The current price change
never affects existing quotes, accepted orders or historical sales
(BR-031). `stockActual` is not modifiable here: stock is managed with
inventory movements (AGENTS.md §15).

Required permission:

```text
PRODUCTOS_UPDATE
```

---

## DELETE/Deactivate

```http
DELETE /api/v1/products/{id}
```

The implementation MUST deactivate instead of physically deleting
products used in historical transactions (BR-032).

Required permission:

```text
PRODUCTOS_DELETE
```

---

# 12A. Public Catalog

Anonymous, read-only catalog for the storefront and showroom
(functional-spec §9, roadmap Phase 8 §13.4). The endpoints do not
require authentication (`[AllowAnonymous]`).

```http
GET /api/v1/catalog/products
GET /api/v1/catalog/products/{id}
```

Query parameters for the list endpoint:

```text
page      (default 1)
pageSize  (default 20, max 100)
search    (name, SKU, description)
type      (TipoProducto enum value)
category  (exact match, case-insensitive)
```

Only active products are returned; inactive products respond `404` on
the detail endpoint so their existence is not disclosed (BR-004).

Response DTOs expose exhibition data only. They MUST NOT include
`costo`, `stockActual`, `stockMinimo`, `proveedorId` or `activo`
(BR-004). Availability is exposed as the boolean `disponible`
(`stockActual > 0`).

List item (`CatalogoItemDto`):

```json
{
  "id": 1,
  "nombre": "Escritorio Ejecutivo 1800",
  "tipo": 0,
  "categoria": "Oficina",
  "precio": 1800.00,
  "esPersonalizado": false,
  "showroom": false,
  "disponible": true,
  "imagenUrl": "https://<project>.supabase.co/storage/v1/object/public/productos/productos/1/<guid>.png"
}
```

`imagenUrl` is the first image by `orden` (cover image) or `null` when
the product has no images.

Detail (`CatalogoProductoDto`) adds `descripcion`, `material`,
`acabado`, `color`, `dimensiones` and the `imagenes` array
(`{ id, url, orden }`, ordered ascending by `orden`).

Status codes:

```text
200 OK
400 Bad Request (invalid type value)
404 Not Found (unknown or inactive product)
```

---

# 12B. Public Works Showcase (Showroom)

Anonymous, read-only showcase of finished works (functional-spec §13,
roadmap Phase 9 §13.4, AGENTS.md §23). Endpoints are `[AllowAnonymous]`
under `ShowroomController`:

```http
GET /api/v1/showroom/works
GET /api/v1/showroom/works/{id}
```

Query parameters for the list endpoint:

```text
page      (default 1)
pageSize  (default 20, max 100)
search    (name, type, description, client name)
categoria (exact match, case-insensitive)
destacado (true/false)
```

Only works with `publicado = true` are returned (BR-190); drafts
respond `404` on the detail endpoint so their existence is not
disclosed. Ordering: `destacado` descending, then `fechaCreacion`
descending.

Response DTOs expose exhibition data only. They MUST NOT include
`publicado`, `publicarNombreCliente`, `fechaCreacion` or
`fechaActualizacion` (BR-191). `clienteNombre` is `null` unless the
work was saved with `publicarNombreCliente = true`. `fecha` is the
optional commercial date (`DateOnly`, `null` when not informed).

List item (`ShowroomItemDto`):

```json
{
  "id": 1,
  "nombre": "Cocina a medida",
  "tipoMueble": "Cocina",
  "categoria": "Cocinas",
  "fecha": "2026-08-15",
  "clienteNombre": null,
  "destacado": true,
  "imagenUrl": "https://<project>.supabase.co/storage/v1/object/public/productos/trabajos/1/<guid>.png"
}
```

`imagenUrl` is the first image by `orden` (cover) or `null`.

Detail (`ShowroomTrabajoDto`) adds `descripcion`, `medidas`,
`melamina`, `accesorios`, `tags` and the `imagenes` array
(`{ id, url, orden }`, ascending by `orden`).

Status codes:

```text
200 OK
404 Not Found (unknown or unpublished work)
```

---

# 12C. Works (Trabajos Realizados)

Admin CRUD for the showroom content (AGENTS.md §23). Base path:

```http
GET    /api/v1/trabajos-realizados
GET    /api/v1/trabajos-realizados/{id}
POST   /api/v1/trabajos-realizados
PATCH  /api/v1/trabajos-realizados/{id}
DELETE /api/v1/trabajos-realizados/{id}
```

All endpoints require authentication and the granular permissions:

```text
GET      TRABAJOS_VIEW
POST     TRABAJOS_CREATE
PATCH    TRABAJOS_UPDATE
DELETE   TRABAJOS_DELETE
```

(`TRABAJOS_VIEW` is required for detail/images as well.) The `Cliente`
role has no work permissions (BR-002).

Query parameters for the list endpoint:

```text
page      (default 1)
pageSize  (default 20, max 100)
search    (nombre, tipoMueble, clienteNombre, categoria)
categoria (exact match, case-insensitive)
publicado (true/false)
featured  → destacado (true/false)
```

Create request (`CreateTrabajoRealizadoDto`, `nombre` required,
max 200):

```json
{
  "nombre": "Cocina a medida",
  "tipoMueble": "Cocina",
  "clienteNombre": "Cliente S.A.",
  "publicarNombreCliente": false,
  "fecha": "2026-08-15",
  "descripcion": "Cocina a medida con isla y alacena alta.",
  "medidas": "3200 x 600 x 2200 mm",
  "melamina": "Blanco mate",
  "accesorios": "Cajones DTC",
  "categoria": "Cocinas",
  "tags": ["cocina", "melamina"],
  "destacado": false,
  "publicado": false
}
```

`tags`: max 10 entries of max 30 characters each, trimmed,
case-insensitive duplicates removed (domain rule `NormalizarEtiquetas`).
`fecha` accepts `YYYY-MM-DD` or `null`. Returns `201 Created` with the
`TrabajoRealizadoDto`.

Update request (`UpdateTrabajoRealizadoDto`, `PATCH`): every field is
optional with product-style semantics — `null` = unchanged, `""` =
clears the value, `true/false` = sets booleans, `"limpiarFecha": true`
clears `fecha` (to distinguish "clear" from "no change" for dates).
Validations (including length limits) run on the merged snapshot.

List/detail responses expose the full management DTO, including
`publicado`, `publicarNombreCliente`, `fechaCreacion` and
`fechaActualizacion`.

Delete removes the work, its image rows and the storage objects
(best-effort; a storage failure does not orphan rows). Returns
`204 No Content`.

Status codes:

```text
200 OK (list/detail)
201 Created
204 No Content
400 Bad Request (validation failed — ProblemDetails)
401 Unauthorized (missing/invalid Firebase ID Token)
403 Forbidden (missing permission)
404 Not Found (unknown work)
```

### Work images

Stored in Supabase Storage (BR-100/BR-101, AGENTS.md §24); PostgreSQL
only stores `TrabajoRealizadoImagenes` (`Id`, `Path`,
`TrabajoRealizadoId`, `Orden`, `FechaCreacion`).

```http
GET    /api/v1/trabajos-realizados/{id}/images
POST   /api/v1/trabajos-realizados/{id}/images
DELETE /api/v1/trabajos-realizados/{id}/images/{imageId}
```

`GET` requires `TRABAJOS_VIEW`; `POST`/`DELETE` require
`TRABAJOS_UPDATE`. The form field, validations, cover assignment and
orphan handling mirror §13 (extension `.jpg .jpeg .png .webp .gif`,
MIME allow-list, max 5 MB). Storage path:
`trabajos/{workId}/{guid}.{ext}` inside the `productos` bucket.
`POST` returns `201 Created`; `DELETE` returns `204 No Content`.

---

# 13. Product Images

Product images are stored in Supabase Storage (BR-100); PostgreSQL only
stores the `ProductoImagenes` reference rows (BR-101: `Id`, `Path`,
`ProductoId`, `Orden`, `FechaCreacion`). Public read URLs use the
`/storage/v1/object/public/{bucket}/{path}` form of the storage.

Endpoints (api-spec §12 base path):

```http
GET    /api/v1/products/{id}/images
POST   /api/v1/products/{id}/images
DELETE /api/v1/products/{id}/images/{imageId}
```

### GET images

```text
PRODUCTOS_VIEW
```

Returns the gallery ordered by `orden`:

```json
[
  { "id": 4, "url": "https://<project>.supabase.co/storage/v1/object/public/productos/productos/1/<guid>.png", "orden": 0 }
]
```

`404` when the product does not exist.

### POST images (multipart/form-data)

```text
PRODUCTOS_UPDATE
```

Form field `file`. Validations are enforced in the backend:

```text
extension   .jpg .jpeg .png .webp .gif
MIME type   image/jpeg image/png image/webp image/gif
max size    5 MB (request form limit = 5 MB + 64 KB)
```

The service uploads to Supabase Storage at
`productos/{productId}/{guid}.{ext}` and inserts the reference in the
same flow; if persistence fails, the uploaded object is removed
(no orphan files, AGENTS.md §24). Returns `201 Created` with the
`ProductoImagenDto`. `orden` is assigned as the current image count of
the product (cover first).

Status codes:

```text
400 Bad Request   (missing file, invalid extension/MIME, size > 5 MB)
401 Unauthorized  (missing/invalid Firebase ID Token)
403 Forbidden     (missing PRODUCTOS_UPDATE)
404 Not Found     (unknown product)
503 Service Unavailable (storage not configured or unreachable —
        ProblemDetails title "Storage.Error")
```

### DELETE image

```text
PRODUCTOS_UPDATE
```

Deletes the object from Supabase Storage (idempotent: a missing object
counts as success) and then the reference row. Returns `204 No Content`,
`404 Not Found` (unknown image or image of another product — existence
is not disclosed) or `503` when the storage delete fails.

The implementation MUST NOT upload binary files into PostgreSQL.

---

# 14. Inventory

Inventory endpoints manage current stock, per-product stock, movement
history and stock movements (business-rules BR-050..BR-053,
functional-spec §11). Stock is modified exclusively through movements;
`PATCH /api/v1/products/{id}` does not change `stockActual`.

## Current stock

Lists the current stock of active products (paginated).

```http
GET /api/v1/inventory?page=1&pageSize=20&search=&lowStock=true
```

Permission: `STOCK_VIEW`. Query parameters: `page`, `pageSize`,
`search`, `lowStock` (`true` returns only products with
`stockMinimo > 0` and `stockActual <= stockMinimo`).

200 response (paginated result):

```json
{
  "items": [
    {
      "id": 1,
      "nombre": "Melamina Blanca 18mm",
      "codigoSku": "MEL-001",
      "tipo": 1,
      "stockActual": 3,
      "stockMinimo": 5,
      "esPersonalizado": false,
      "abajoDeMinimo": true
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 1,
  "totalPages": 1
}
```

---

## Product stock

```http
GET /api/v1/inventory/products/{productId}
```

Permission: `STOCK_VIEW`. Returns `200` with the stock detail of the
product (the current stock fields plus `activo`) or `404` if the
product does not exist.

---

## Stock movement

Registers a stock movement atomically (BR-050): the stock update and
the movement record are applied in a single transaction, or neither is
applied.

```http
POST /api/v1/inventory/movements
```

Example:

```json
{
  "productId": 1,
  "type": 0,
  "quantity": 5,
  "reason": "Recepción de compra",
  "reference": "OC-2026-001"
}
```

`type` values:

| Value | Type | Effect on stock |
|---|---|---|
| 0 | Ingreso | `+` |
| 1 | Egreso | `−` |
| 2 | Ajuste | `±` (signed `quantity`) |
| 3 | Devolucion | `+` |
| 4 | Reserva | `−` |
| 5 | Liberacion | `+` |

`quantity` is always greater than zero, except for `Ajuste` (2), which
accepts a signed value. The resulting stock can never be negative
(BR-052).

Permission depends on the movement type:

| Types | Required permission |
|---|---|
| Ingreso (0), Devolucion (3) | `STOCK_ENTRY` |
| Egreso (1), Ajuste (2), Reserva (4), Liberacion (5) | `STOCK_ADJUST` |

Status codes:

- `201` — movement created; the body is the movement DTO
  (`stockAnterior`, `stockPosterior`, `usuarioNombre`, `fecha`).
- `400` — invalid quantity for the type, or the operation would leave
  the stock negative (BR-052).
- `403` — the caller lacks the permission required for that type
  (BR-003).
- `404` — the product does not exist (an inactive product returns
  `400`).
- `409` — the stock changed concurrently; nothing was modified
  (BR-053).

Movement DTO (response of `POST` and item of the history):

```json
{
  "id": 10,
  "productoId": 1,
  "productoNombre": "Mesa Roble",
  "tipo": 0,
  "cantidad": 5,
  "stockAnterior": 10,
  "stockPosterior": 15,
  "motivo": "Recepción de compra",
  "referencia": "OC-2026-001",
  "usuarioId": 3,
  "usuarioNombre": "Juan Pérez",
  "fecha": "2026-10-02T12:00:00Z"
}
```

---

## Inventory history

```http
GET /api/v1/inventory/movements?page=1&pageSize=20&productId=1
```

Permission: `STOCK_VIEW`. Returns the movement history (newest first),
optionally filtered by `productId` (BR-051).

---

# 15. Quotes

Quotes endpoints preserve a historical commercial snapshot (BR-063):
totals are always calculated by the backend (BR-062) and line prices are
frozen at creation time. `status` is serialized as a number:

```text
0 Borrador
1 Enviado
2 Aceptado
3 Rechazado
4 Anulado
5 Expirado
```

State changes: `send` (0→1), `reject` (1→3), `cancel` (0/1→4), `accept`
(1→2 plus atomic order creation — BR-064/BR-065). Accepted quotes carry
the id of the generated order in `orderId` (`null` otherwise).

---

## List

```http
GET /api/v1/quotes?page=1&pageSize=20
```

Permission: `PRESUPUESTOS_VIEW`.

Possible filters:

```text
customerId
status          (enum number, see above)
dateFrom        (creation date, inclusive)
dateTo          (creation date, exclusive of the next day)
search          (customer name, surname or email)
page
pageSize       (default 20, max 100)
```

`400` when `dateFrom > dateTo` or `customerId <= 0`.

Response: paged result with `items[]`:

```json
{
  "items": [
    {
      "id": 1,
      "customerId": 7,
      "customerName": "Ana García",
      "creationDate": "2026-10-02T12:00:00Z",
      "expirationDate": "2026-12-31T00:00:00Z",
      "status": 0,
      "total": 1050.00,
      "itemsCount": 2,
      "expired": false
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 1,
  "totalPages": 1
}
```

`expired` is computed server-side: true when `expirationDate` is in the
past and the status is still Borrador/Enviado (BR-064).

---

## Detail

```http
GET /api/v1/quotes/{id}
```

Permission: `PRESUPUESTOS_VIEW`. Returns the quote with its frozen lines
(`404` when unknown).

```json
{
  "id": 1,
  "customerId": 7,
  "customerName": "Ana García",
  "creationDate": "2026-10-02T12:00:00Z",
  "expirationDate": null,
  "sendDate": null,
  "status": 0,
  "subtotal": 1100.00,
  "discount": 50.00,
  "total": 1050.00,
  "notes": "Entrega en 15 días",
  "conditions": "50% de seña",
  "stateReason": null,
  "expired": false,
  "orderId": null,
  "items": [
    {
      "id": 1,
      "productId": 12,
      "description": "Melamina Blanca 18mm",
      "quantity": 2,
      "unitPrice": 150.00,
      "discount": 50.00,
      "subtotal": 250.00
    }
  ]
}
```

---

## Create

```http
POST /api/v1/quotes
```

Permission: `PRESUPUESTOS_CREATE`.

```json
{
  "customerId": 7,
  "expirationDate": "2026-12-31",
  "notes": "Entrega en 15 días",
  "conditions": "50% de seña",
  "items": [
    {
      "productId": 12,
      "description": null,
      "quantity": 2,
      "unitPrice": null,
      "discount": 50.00
    },
    {
      "productId": null,
      "description": "Mueble a medida",
      "quantity": 1,
      "unitPrice": 800.00,
      "discount": null
    }
  ]
}
```

Rules:

- `customerId` must exist and be active (BR-060): `404` unknown,
  `400` inactive.
- At least one item (BR-061): `400` otherwise.
- Lines with `productId` take name and price from the current product
  (snapshot BR-063); the declared `unitPrice` is ignored. Product must
  exist (`404`) and be active (`400`).
- Lines without `productId` require `description` and `unitPrice`
  (custom furniture, BR-041): `400` otherwise.
- `quantity > 0`, `unitPrice >= 0`, `discount >= 0` and
  `discount <= quantity * unitPrice`: `400` otherwise.
- `expirationDate` cannot be in the past: `400`.
- Totals (`subtotal`, `discount`, `total`) are computed by the backend
  (BR-062); the response returns them (`201 Created` with `Location`).

---

## Update

```http
PATCH /api/v1/quotes/{id}
```

Permission: `PRESUPUESTOS_UPDATE`. Only allowed in status `Borrador`
(`409` otherwise). Same validation rules as Create.

- `items: null` keeps the current lines; a non-empty array replaces all
  lines (totals recalculated).
- `customerId: null` keeps the current customer.

---

## Send

```http
POST /api/v1/quotes/{id}/send
```

Permission: `PRESUPUESTOS_UPDATE`. Borrador → Enviado (`409` if the
status is not Borrador; `400` if the quote has no items — BR-061).

---

## Reject

```http
POST /api/v1/quotes/{id}/reject
```

Permission: `PRESUPUESTOS_UPDATE`. Enviado → Rechazado (`409` otherwise).

Body (optional):

```json
{ "reason": "Precio fuera de presupuesto" }
```

---

## Cancel

```http
POST /api/v1/quotes/{id}/cancel
```

Permission: `PRESUPUESTOS_ANULATE`. Borrador/Enviado → Anulado (`409`
otherwise). Body (optional): `{ "reason": "..." }`.

---

## Accept

```http
POST /api/v1/quotes/{id}/accept
```

Permission: `PRESUPUESTOS_ACCEPT`. Domain action (BR-064/BR-065): accepts
the quote and creates its order in a single transaction.

```text
Validate quote (Enviado, not expired)
      ↓
CAS: status Enviado → Aceptado
      ↓
Create order (snapshot total, BR-071)
      ↓
Commit transaction
```

`200 OK` returns the quote with `status = 2` and the new `orderId`:

```json
{
  "id": 1,
  "status": 2,
  "orderId": 4,
  "total": 1050.00
}
```

Status codes:

| Code | Meaning |
|------|---------|
| `400` | Invalid id |
| `403` | Caller lacks `PRESUPUESTOS_ACCEPT` |
| `404` | Quote does not exist |
| `409` | Quote is not Enviado, is expired (BR-064), or was accepted concurrently by another process (BR-065 — no order is created) |

The operation is idempotent from the client's point of view: a repeated
accept returns `409` and never produces a second order (a unique index on
`Pedidos.PresupuestoId` enforces this at the database level).

---

# 16. Orders

Orders are never created through a `POST /orders` endpoint: an order is
generated atomically when a quote is accepted (BR-065/BR-070) and keeps
the commercial snapshot of that quote (BR-071). `status` is serialized
as a number:

```text
0 Pendiente
1 Confirmado
2 EnProduccion
3 Listo
4 Entregado
5 Cancelado
6 Completado
```

Allowed transitions (BR-072/BR-073):

```text
Pendiente     → Confirmado, Cancelado
Confirmado    → EnProduccion, Cancelado
EnProduccion  → Listo, Cancelado
Listo         → Entregado, Cancelado
Entregado     → Completado
Completado    → terminal (BR-074)
Cancelado     → terminal (BR-074)
```

Every successful change writes an immutable history row (BR-075) and
records tracking timestamps on the order (BR-076): `productionStartDate`
when entering EnProduccion, `productionEndDate` when entering Listo and
`deliveryDate` when entering Entregado.

---

## List

```http
GET /api/v1/orders?page=1&pageSize=20
```

Permission: `PEDIDOS_VIEW`.

Possible filters:

```text
customerId
status          (enum number, see above)
dateFrom        (creation date, inclusive)
dateTo          (creation date, exclusive of the next day)
search          (customer name, surname or email)
page
pageSize       (default 20, max 100)
```

`400` when `dateFrom > dateTo` or `customerId <= 0`.

Response: paged result with `items[]`:

```json
{
  "items": [
    {
      "id": 1,
      "customerId": 7,
      "customerName": "Ana García",
      "quoteId": 4,
      "creationDate": "2026-10-03T14:00:00Z",
      "status": 0,
      "total": 1050.00,
      "estimatedDeliveryDate": null
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 1,
  "totalPages": 1
}
```

`quoteId` is the originating quote (BR-070); `estimatedDeliveryDate`
carries the planned delivery date for scheduling (BR-076).

---

## Detail

```http
GET /api/v1/orders/{id}
```

Permission: `PEDIDOS_VIEW`. Returns `404` when unknown. The `items[]`
array holds the frozen lines of the accepted quote (BR-071) using the
same line contract as quotes:

```json
{
  "id": 1,
  "customerId": 7,
  "customerName": "Ana García",
  "quoteId": 4,
  "creationDate": "2026-10-03T14:00:00Z",
  "status": 1,
  "total": 1050.00,
  "productionStartDate": null,
  "productionEndDate": null,
  "estimatedDeliveryDate": "2026-10-25T00:00:00Z",
  "deliveryDate": null,
  "notes": "Entrega coordinada con el cliente",
  "items": [
    {
      "id": 1,
      "productId": 12,
      "description": "Melamina Blanca 18mm",
      "quantity": 2,
      "unitPrice": 150.00,
      "discount": 50.00,
      "subtotal": 250.00
    }
  ]
}
```

Tracking fields (`productionStartDate`, `productionEndDate`,
`estimatedDeliveryDate`, `deliveryDate`, `notes`) follow BR-076: the
timestamp fields are `null` until their transition happens, and the
editable ones (`estimatedDeliveryDate`, `notes`) change through `PATCH`
below.

---

## Update

```http
PATCH /api/v1/orders/{id}
```

Permission: `PEDIDOS_UPDATE`. Edits the editable tracking information
(BR-076); terminal orders are immutable (BR-074).

Request (both fields optional; `null` clears the value):

```json
{
  "notes": "Entrega coordinada con el cliente",
  "estimatedDeliveryDate": "2026-10-25"
}
```

`estimatedDeliveryDate` accepts a date-only string (`YYYY-MM-DD`,
interpreted as UTC) or a full ISO instant.

Response: `200` with the updated order (`OrderDto`).

| Status | When |
|--------|------|
| 200 | Information updated |
| 400 | `notes` exceeds 2000 characters, or the estimated date is invalid/earlier than creation |
| 401 | Missing/invalid token |
| 403 | Missing `PEDIDOS_UPDATE` |
| 404 | Unknown order |
| 409 | Order is in a terminal status (Completado/Cancelado) — BR-074 |

---

## Change status

```http
POST /api/v1/orders/{id}/status
```

Permission (evaluated per request): `PEDIDOS_UPDATE` for every target
status except `5` (Cancelado), which requires `PEDIDOS_CANCEL`
(BR-003). The acting user is resolved from the Firebase token; the
transition itself is validated by the domain (BR-072).

Request:

```json
{
  "status": 1,
  "comment": "Confirmado con el cliente"
}
```

`comment` is optional (max 500 characters, stored in the history row).

Response: `200` with the updated order (`OrderDto`).

| Status | When |
|--------|------|
| 200 | Transition applied |
| 400 | `status` is not a defined enum value |
| 401 | Missing/invalid token |
| 403 | Missing `PEDIDOS_UPDATE`/`PEDIDOS_CANCEL` |
| 404 | Unknown order, or unknown/inactive acting user |
| 409 | Invalid transition (e.g. jump, terminal state) — BR-072/BR-074 |

---

## Order history

```http
GET /api/v1/orders/{id}/history
```

Permission: `PEDIDOS_VIEW`. Returns `404` when the order is unknown.
Rows are ordered newest first:

```json
[
  {
    "id": 3,
    "previousStatus": 0,
    "newStatus": 1,
    "date": "2026-10-03T15:00:00Z",
    "userName": "Ana García",
    "comment": "Confirmado con el cliente"
  },
  {
    "id": 2,
    "previousStatus": null,
    "newStatus": 0,
    "date": "2026-10-03T14:00:00Z",
    "userName": null,
    "comment": "Creado al aceptar el presupuesto #4"
  }
]
```

`previousStatus: null` marks the creation record; `userName: null`
means the change was made by the system (BR-075).

---

# 17. Payments

Payments record money received against an order. The payment history is
immutable: rows are only added, never edited or deleted (BR-082); the
annulment mechanism (`PAGOS_ANULATE`) is reserved for a future phase.
`paymentMethod` is serialized as a number:

```text
0 Efectivo
1 Transferencia
2 Tarjeta
3 Cheque
4 Otro
```

The outstanding balance is always calculated by the backend as
`order.total − sum(payments)` and is never accepted from the client
(BR-083). All timestamps are UTC instants (AGENTS.md §17).

---

## Payment object

```json
{
  "id": 12,
  "orderId": 3,
  "customerId": 7,
  "customerName": "Ana García",
  "amount": 250000.00,
  "date": "2026-10-03T15:00:00Z",
  "paymentMethod": 1,
  "reference": "TRX-12345",
  "notes": "Initial payment",
  "userName": "Ana García"
}
```

`customerId`/`customerName` are `null` when the row is returned without
its order context; `userName` is the internal user who registered the
payment (audit, AGENTS.md §18). `reference` max 100 chars, `notes` max
500. `amount` must be positive with at most 2 decimals (BR-080).

---

## List

```http
GET /api/v1/payments?page=1&pageSize=20
```

Permission: `PAGOS_VIEW`.

Possible filters:

```text
orderId
customerId
dateFrom        (payment date, inclusive)
dateTo          (payment date, exclusive of the next day)
search          (reference or notes)
page
pageSize       (default 20, max 100)
```

`400` when `dateFrom > dateTo`, `orderId <= 0` or `customerId <= 0`.

Response: paged result with `items[]` (payment object above), ordered
newest first.

---

## Order payments

```http
GET /api/v1/orders/{orderId}/payments
```

Permission: `PAGOS_VIEW`. Returns `404` when the order is unknown.

Response combines the payment history with the financial summary
computed by the backend (BR-083):

```json
{
  "items": [],
  "totalPaid": 150000.00,
  "balance": 100000.00
}
```

`balance` is never negative.

---

## Register payment

```http
POST /api/v1/orders/{orderId}/payments
```

Permission: `PAGOS_CREATE`. Example:

```json
{
  "amount": 250000.00,
  "paymentMethod": 1,
  "reference": "TRX-12345",
  "notes": "Initial payment"
}
```

The backend validates the balance and persists the payment inside a
serializable transaction, so two concurrent registrations cannot overdraw
the order (AGENTS.md §56). Response `200` with the payment object.

Status codes:

| Code | Meaning |
| ---- | ------- |
| 400 | Invalid order id, non-positive/3-decimal amount (BR-080), undefined payment method, overlong reference/notes |
| 401/403 | Missing token / missing `PAGOS_CREATE` |
| 404 | Unknown order, or unknown/inactive acting user |
| 409 | Amount exceeds the outstanding balance (BR-081), cancelled order, or concurrent-registration conflict |

There is intentionally no `DELETE /payments/{id}`: financial history must
never be silently removed (BR-082).

---

# 17A. Invoices (Simulated)

Billing is simulated/internal (BR-130): the backend emits one document
per delivered or completed order behind the `IBillingService`
abstraction, so a future ARCA integration does not require rewriting the
domain (BR-131). `isSimulated` is always `true` in this phase and the
document MUST NOT be presented as an authorized fiscal invoice. Invoice
rows are immutable snapshots (BR-141): customer name, total and issue
date are frozen at emission and later product/customer changes never
rewrite them. There is intentionally no `PATCH`/`DELETE` for invoices.
All timestamps are UTC instants (AGENTS.md §17).

## Invoice object

```json
{
  "id": 1,
  "orderId": 3,
  "customerId": 7,
  "customerName": "Ana García",
  "number": "SIM-000001",
  "issueDate": "2026-10-06T14:00:00Z",
  "total": 480000.00,
  "status": 0,
  "isSimulated": true,
  "notes": "Entrega completa",
  "userName": "Carlos Dueño"
}
```

`status` is serialized as a number:

```text
0 Emitida
1 Anulada
```

(State `1` is reserved for a future annulment phase; rows created today
are always `0`.) `number` follows the internal format `SIM-######`
generated by `IBillingService`. `notes` max 500 chars. `total` is copied
from the order at emission and must be positive with at most 2 decimals
(AGENTS.md §16). `userName` is the internal user who issued the invoice
(audit, AGENTS.md §18).

## List

```http
GET /api/v1/facturas?page=1&pageSize=20
```

Permission: `FACTURAS_VIEW`.

Possible filters:

```text
orderId
customerId
dateFrom        (issue date, inclusive)
dateTo          (issue date, exclusive of the next day)
search          (invoice number or customer name)
page
pageSize       (default 20, max 100)
```

`400` when `dateFrom > dateTo`, `orderId <= 0` or `customerId <= 0`.

Response: paged result with `items[]` (invoice object above), ordered
newest first.

## Detail

```http
GET /api/v1/facturas/{id}
```

Permission: `FACTURAS_VIEW`. `400` for an invalid id, `404` when the
invoice is unknown.

## Create

```http
POST /api/v1/facturas
```

Permission: `FACTURAS_CREATE`.

Request:

```json
{
  "orderId": 3,
  "notes": "Entrega completa"
}
```

Only `orderId` is required; `notes` is optional (max 500 chars). The
backend resolves the number (`SIM-######`), the simulated flag, the
customer/total snapshot and the issue date (BR-130/BR-131).

Response: `200` with the invoice object.

| Status | Cause |
| ------ | ----- |
| 400 | Invalid `orderId`, overlong notes, or snapshot validation failure |
| 401/403 | Missing token / missing `FACTURAS_CREATE` |
| 404 | Unknown order, or unknown/inactive acting user |
| 409 | Order not in `Entregado`/`Completado` status, or the order already has an invoice |

The row and its number are persisted in a single transaction (BR-151):
emission is atomic and a rejected request never leaves an unnumbered
invoice behind.

---

# 18. Notifications

Notifications belong to the authenticated internal user: the backend
resolves the recipient from the Firebase token and every endpoint only
returns or mutates the caller's own rows (BR-090). Read state is
Leída/No leída (BR-091). Notifications are generated best-effort from
business events (BR-092) and never require a dedicated permission.

```http
GET  /api/v1/notifications
GET  /api/v1/notifications/unread-count
POST /api/v1/notifications/{id}/read
POST /api/v1/notifications/read-all
```

## Notification object

```json
{
  "id": 5,
  "type": 1,
  "title": "Presupuesto aceptado",
  "message": "El presupuesto #3 fue aceptado y se creó el pedido #2.",
  "entityType": "Pedido",
  "entityId": 2,
  "read": false,
  "createdAt": "2026-10-09T14:30:00Z"
}
```

`type` is serialized as a number:

```text
0 PresupuestoEnviado
1 PresupuestoAceptado
2 PedidoEstadoCambiado
3 PagoRegistrado
4 PedidoListo
5 PresupuestoSolicitado
```

`entityType`/`entityId` reference the originating business entity
(`Presupuesto` or `Pedido`) when applicable. All timestamps are UTC
instants (AGENTS.md §17).

## List

```http
GET /api/v1/notifications?page=1&pageSize=20&unreadOnly=false
```

Possible parameters:

```text
page
pageSize       (default 20, max 100)
unreadOnly     (true → only unread rows)
```

Response: paged result with `items[]` (notification object above), most
recent first, restricted to the caller's own notifications.

## Unread count

```http
GET /api/v1/notifications/unread-count
```

Response: `{ "count": 3 }`. Used by the UI badge.

## Mark as read

```http
POST /api/v1/notifications/{id}/read
```

Response: `204 No Content`. `400` for an invalid id; `404` when the
notification is unknown **or belongs to another user** (ownership is
never leaked).

## Mark all as read

```http
POST /api/v1/notifications/read-all
```

Response: `{ "updated": 4 }` with the quantity of rows changed.

---

# 19. Dashboard

Administrative dashboard (functional-spec §22). Requires an authenticated
internal user. Each business block is only returned when the requesting
user holds the corresponding effective permission; the rest of the blocks
are `null` (the API only returns information authorized for the requesting
user). The `notifications` block is always present: it counts the user's
own unread notifications (BR-090).

```http
GET /api/v1/dashboard
```

Authorization: authenticated internal user (no single granular permission;
blocks are filtered per permission).

Block requirements:

| Block          | Required permission                              |
| -------------- | ------------------------------------------------ |
| `orders`       | `PEDIDOS_VIEW`                                   |
| `quotes`       | `PRESUPUESTOS_VIEW`                              |
| `sales`        | `FACTURAS_VIEW` **or** `REPORTES_VIEW`           |
| `payments`     | `PAGOS_VIEW`                                     |
| `inventory`    | `STOCK_VIEW`                                     |
| `notifications`| always (own unread count)                        |

Example response:

```json
{
  "orders": {
    "pendientes": 3,
    "confirmados": 2,
    "enProduccion": 1,
    "listos": 4,
    "entregadosMes": 7,
    "canceladosMes": 0
  },
  "quotes": {
    "borradores": 1,
    "enviados": 5,
    "aceptadosMes": 4,
    "rechazadosMes": 1
  },
  "sales": {
    "facturadoMes": 450000.00,
    "facturasMes": 6
  },
  "payments": {
    "cobradoMes": 320000.00,
    "pagosMes": 9
  },
  "inventory": {
    "productosActivos": 10,
    "stockBajo": 2,
    "enShowroom": 4
  },
  "notifications": {
    "unreadCount": 2
  },
  "generatedAtUtc": "2026-10-09T12:00:00Z"
}
```

Monthly aggregates (`*Mes`) cover the current UTC calendar month.
`404 Not Found` when the token has no active internal user.

---

# 20. Reports

Operational reports (functional-spec §21, BR-140/BR-141). Visualization
requires `REPORTES_VIEW`; export requires `REPORTES_EXPORT` (BR-140).

```http
GET /api/v1/reports/orders
GET /api/v1/reports/payments
GET /api/v1/reports/quotes
GET /api/v1/reports/sales
GET /api/v1/reports/inventory
GET /api/v1/reports/{key}/export?format=xlsx|csv|pdf
```

## Common query parameters

```text
from          ISO date (inclusive, UTC day start)
to            ISO date (inclusive, up to the end of the day)
customerId    filter by customer (orders, payments, quotes, sales)
status        numeric enum: order state (orders) or quote state (quotes)
method        numeric enum: payment method (payments)
type          numeric enum: product type (inventory)
active        boolean: active/inactive products (inventory)
```

Invalid enum values or `from > to` return `400 Bad Request`. An unknown
report key returns `404 Not Found`.

## Reports

```text
orders      Orders in the range: number, customer, date, state, total
            (frozen snapshot BR-141), paid amount and balance.
payments    Payments in the range: date, order, customer, amount,
            method, reference and the user who registered it.
quotes      Quotes in the range: number, customer, date, state, subtotal,
            discount and total (backend-calculated snapshot BR-062/BR-141).
sales       Issued invoices in the range (invoiced revenue): document
            number, date, order, customer name snapshot, total and
            condition (Simulada/Emitida).
inventory   Current stock per product: SKU, name, type, category, stock,
            minimum, cost, price and stock value (at cost; no estimate
            when the product has no cost).
```

## Preview document

`GET /api/v1/reports/{key}` returns the document consumed by the UI and
by the exporters:

```json
{
  "key": "orders",
  "title": "Reporte de pedidos",
  "generatedAt": "2026-10-09T14:30:00Z",
  "appliedFilters": [
    { "key": "Período", "value": "2026-10-01 al 2026-10-31" }
  ],
  "columns": [
    { "key": "numero", "header": "Pedido", "align": "left" },
    { "key": "total", "header": "Total", "align": "right" }
  ],
  "rows": [
    {
      "cells": {
        "numero": "#1",
        "total": 1950.00,
        "estado": "En producción"
      }
    }
  ],
  "summary": [
    { "label": "Pedidos", "value": 3 },
    { "label": "Total", "value": 6950.00 }
  ]
}
```

Cell values are strings (resolved enum labels included), numbers
(decimals with 2 fractional digits) or null/empty when not applicable.

## Export

```http
GET /api/v1/reports/orders/export?format=xlsx&from=2026-10-01&to=2026-10-31
```

Possible formats:

```text
xlsx    application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
csv     text/csv; charset=utf-8 (UTF-8 with BOM, RFC 4180 escaping)
pdf     application/pdf (A4 landscape)
```

Response: the file with a `Content-Disposition` filename of the form
`reporte-{key}-{yyyyMMdd-HHmmss}.{ext}`. `format` is required; an unknown
value returns `400`. Export authorization matches visualization (BR-140).

---

# 21. Profile

Authenticated user:

```http
GET /api/v1/profile
PATCH /api/v1/profile
```

The API MUST derive the current user from the authenticated identity.

The client MUST NOT be able to impersonate another user by sending a different user ID.

---

# 22. WhatsApp

The backend MAY expose business contact configuration:

```http
GET /api/v1/contact
```

The frontend can generate the external WhatsApp link.

Frontend implementation: the link is built from
`NEXT_PUBLIC_WHATSAPP_NUMBER` (`https://wa.me/{number}`); the contact
button is hidden when the variable is not configured.

No internal chat endpoints are required.

---

# 23. Health Checks

The API MUST expose:

```http
GET /health
```

Optional:

```http
GET /health/live
GET /health/ready
```

Health endpoints MUST NOT expose secrets or sensitive infrastructure details.

---

# 24. OpenAPI

The API MUST expose an OpenAPI specification in development and appropriate environments.

Every endpoint SHOULD document:

- authentication;
- required permission;
- request schema;
- response schema;
- possible errors;
- status codes.

---

# 25. DTO Policy

Entities MUST NOT be returned directly from controllers.

Use DTOs:

```text
Request DTO
Response DTO
Command DTO
Query DTO
```

Example:

```text
Customer
    ↓
CustomerResponseDto
```

The API contract MUST remain independent from EF Core entity structure.

---

# 26. Validation

Input validation MUST occur before executing the use case.

Validation SHOULD include:

- required fields;
- lengths;
- formats;
- numeric ranges;
- business constraints.

Business rules MUST still be validated in the application/domain layers.

---

# 27. Pagination

Potentially large collections MUST be paginated.

Default:

```text
page = 1
pageSize = 20
```

The API SHOULD enforce a maximum page size, for example:

```text
100
```

The exact maximum can be configured.

---

# 28. Sorting

Endpoints MAY support controlled sorting:

```text
sortBy
sortDirection
```

The backend MUST whitelist sortable fields.

Clients MUST NOT inject arbitrary SQL fragments through sorting parameters.

---

# 29. Filtering

Filtering parameters MUST be converted into structured application queries.

The API MUST NOT concatenate user-provided values into raw SQL.

---

# 30. Concurrency

Operations susceptible to concurrent modification SHOULD use optimistic concurrency where appropriate.

Examples:

- inventory;
- order status;
- payments;
- user permissions.

A concurrency conflict SHOULD return:

```http
409 Conflict
```

---

# 31. API Security Rules

The API MUST:

- require HTTPS in production;
- validate Firebase tokens;
- enforce permissions;
- validate input;
- avoid exposing internal exceptions;
- avoid exposing stack traces;
- protect sensitive endpoints;
- validate uploaded files;
- apply CORS restrictions;
- avoid logging secrets or authentication tokens.

---

# 32. API Testing

The API MUST have automated tests for:

- authentication;
- authorization;
- customer isolation;
- CRUD operations;
- validation;
- quote acceptance;
- order creation;
- order status transitions;
- payments;
- inventory;
- reports;
- error responses.

Critical endpoints SHOULD have integration tests against PostgreSQL.

---

# 33. API Versioning

The first public version is:

```text
v1
```

Breaking API changes MUST result in a new version rather than silently changing existing contracts.

---

# 34. API Implementation Rule

OpenCode MUST NOT create endpoints merely because a database table exists.

Endpoints MUST correspond to:

- a business capability;
- a use case;
- a required query;
- a required domain action.

Database CRUD MUST NOT automatically become public API CRUD.
