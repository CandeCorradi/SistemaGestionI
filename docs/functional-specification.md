# Functional Specification — Sistema de Gestión

## 1. Purpose

This document defines the functional requirements of the **Sistema de Gestión**, a commercial management system designed for a single carpentry business.

The document is intended to serve as an implementation reference for developers and coding agents.

The system MUST implement the functionality described here without inventing additional business behavior unless explicitly authorized.

---

# 2. System Scope

The system manages the complete commercial lifecycle of the carpentry business:

```text
Catalog / Showroom
        ↓
Customer
        ↓
Quote
        ↓
Quote Acceptance
        ↓
Order
        ↓
Production
        ↓
Payment
        ↓
Sale / Delivery
```

The system also provides:

- user and role management;
- granular permissions;
- customers;
- suppliers;
- products and furniture;
- custom furniture;
- inventory;
- quotes;
- orders;
- payments;
- notifications;
- reports;
- showroom;
- authentication;
- profile management;
- simulated invoicing;
- Firebase image storage.

---

# 3. Users and Roles

The system MUST support exactly five business roles:

```text
SuperAdmin
Owner
Admin
Empleado
Cliente
```

## 3.1 SuperAdmin

Responsible for technical/system-level administration.

Capabilities include:

- manage users;
- manage roles;
- manage permissions;
- configure system parameters;
- access administrative functions;
- access system-wide reports;
- manage or supervise business configuration.

The SuperAdmin is not necessarily the business owner.

---

## 3.2 Owner

Represents the owner of the carpentry business.

Capabilities include:

- manage business information;
- manage employees;
- manage customers;
- manage suppliers;
- manage products;
- manage inventory;
- manage quotes;
- manage orders;
- manage payments;
- access reports;
- access financial information;
- configure commercial parameters.

---

## 3.3 Admin

Responsible for operational administration.

Default capabilities:

- manage customers;
- manage suppliers;
- manage products;
- manage inventory;
- manage quotes;
- manage orders;
- register payments;
- generate reports.

Actual permissions MUST be controlled through the permission system and MUST NOT depend exclusively on role names.

---

## 3.4 Empleado

Represents an employee of the carpentry business.

Employees MUST receive granular permissions.

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
PRESUPUESTOS_UPDATE
PRESUPUESTOS_ACCEPT

PEDIDOS_VIEW
PEDIDOS_UPDATE_STATUS

PAGOS_VIEW
PAGOS_CREATE

REPORTES_VIEW
```

The exact permissions are defined by the authorization model.

---

## 3.5 Cliente

The customer accesses the client portal.

The customer MUST be able to:

- authenticate;
- view and update their profile;
- browse the catalog/showroom;
- view furniture/products;
- request a quote;
- view their quotes;
- accept quotes;
- view orders;
- view order status;
- view payment information;
- receive notifications;
- contact the business through WhatsApp.

Customers MUST only access resources belonging to themselves.

---

# 4. Authentication

Authentication MUST use Firebase Authentication.

The system MUST support:

- login;
- logout;
- account registration where applicable;
- password recovery through Firebase;
- authenticated session management;
- token validation.

The backend MUST verify Firebase ID tokens.

Firebase UID MUST be associated with the application's internal user.

Business roles and permissions MUST be managed by the application's database.

Firebase Authentication MUST NOT be treated as the source of truth for all business authorization rules.

---

# 5. User Management

Authorized administrative users MUST be able to:

- create users;
- view users;
- update users;
- activate/deactivate users;
- assign roles;
- configure employee permissions;
- view account status.

The system MUST NOT physically delete users when doing so would compromise historical information.

---

# 6. Customer Management

Authorized users MUST be able to:

- register customers;
- update customer information;
- search customers;
- filter customers;
- view customer details;
- view customer quotes;
- view customer orders;
- view customer payments.

Customer information MAY include:

- first name;
- last name;
- identification/document number;
- email;
- phone;
- address;
- city;
- province;
- notes;
- Firebase UID when linked to a portal account;
- active/inactive state.

---

# 7. Supplier Management

The system MUST use the term **Proveedor**.

The legacy term **Mayorista** MUST NOT be used in new functionality.

Authorized users MUST be able to:

- create suppliers;
- update suppliers;
- view suppliers;
- search suppliers;
- deactivate suppliers;
- associate products/materials with suppliers.

Supplier history MUST be preserved when relevant to existing orders or products.

---

# 8. Product and Furniture Management

The system MUST support:

```text
Standard Product/Furniture
Custom/Made-to-measure Furniture
```

A product MAY contain:

- name;
- description;
- category;
- type;
- dimensions;
- material;
- finish;
- color;
- price;
- cost;
- stock;
- minimum stock;
- images;
- active state;
- showroom availability.

Images MUST be stored in Supabase Storage (BR-100, AGENTS.md §3.6).

The database MUST store only references/URLs/paths.

---

# 9. Catalog / Showroom

The public/client-facing catalog MUST allow users to:

- browse products;
- search products;
- filter products;
- view product details;
- view images;
- see availability;
- distinguish standard and custom furniture.

Implementation (Phase 8, api-spec §12A): the catalog is exposed
anonymously through `GET /api/v1/catalog/products` and
`GET /api/v1/catalog/products/{id}`, consumed by the public
`/catalogo` area of the frontend (list with search, type filter,
pagination and cover image; detail with gallery, specifications and
availability badge). Only active products are visible and the payloads
exclude internal fields (cost, numeric stock, supplier, active flag);
availability is the boolean `disponible` (BR-004). Inactive or unknown
products respond `404`.

Finished furniture available for display MUST be represented in the showroom.

The administrative dashboard MUST provide visibility over showroom/finished-furniture stock.

## 9A. Finished Works Showcase (Vidriera)

The vidriera presents finished works (Phase 8A, api-spec §12B/§12C,
BR-190…BR-192). Anyone can browse published works anonymously:

- list at `/showroom` with search, category filter, cover image and
  featured badge (featured works first);
- detail at `/showroom/[id]` with gallery, description, dimensions,
  materials (melamine/accessories), tags and optional date;
- client name only appears when staff enabled
  `publicarNombreCliente` for that work;
- drafts (`publicado = false`) are never exposed publicly.

Authorized staff manage the content in `/trabajos`
(`TRABAJOS_VIEW/CREATE/UPDATE/DELETE`): create and edit works, publish
or unpublish, mark featured, and maintain the image gallery (upload,
reorder by insertion, delete). Works are internal records of the
carpentry; the showcase never exposes administrative fields or other
clients' data.

### Cart and purchase intent

Visitors can add published works to a browser-side cart
(`localStorage`, no session required) from the list or the detail page.
When they choose **Finalizar compra**:

- without a session, they are redirected to `/login?redirect=/showroom?cart=1`;
  the cart survives the login and the panel reopens (the `cart` parameter
  is removed afterwards so a reload does not reopen it);
- with a `Cliente` session, the selection becomes a quote request
  (`POST /api/v1/my/quotes`) describing the chosen works. The team quotes
  it and the client can accept it from the portal, which creates the
  order (BR-064/BR-065);
- staff accounts cannot purchase: the cart requires a customer account.

The cart never sends prices, roles or permissions to the backend; the
commercial totals remain server-side.

### Google sign-in

The login and registration pages offer **Continuar con Google**
(Firebase `signInWithPopup`). After the popup, the backend provisions the
internal account through `POST /api/v1/auth/google` (api-spec §3,
idempotent): existing emails are linked, new identities receive the fixed
`Cliente` role. The Google provider must be enabled in the Firebase
project (Authentication → Sign-in method → Google).

---

# 10. Custom Furniture

The system MUST support quote requests for made-to-measure furniture.

A custom request MAY contain:

- furniture type;
- dimensions;
- materials;
- finish;
- color;
- accessories;
- description;
- customer requirements;
- reference images where supported.

Custom furniture MUST NOT necessarily require an existing catalog product.

A quote can therefore represent a product specifically configured for a customer.

---

# 11. Inventory

The inventory module MUST track:

- current stock;
- minimum stock;
- stock increases;
- stock decreases;
- adjustments;
- movement history.

Inventory movements MUST record at least:

- product;
- movement type;
- quantity;
- date/time;
- user;
- reference;
- reason where applicable.

Stock changes MUST be atomic.

The system MUST prevent inconsistent inventory caused by concurrent operations.

---

# 12. Quotes

The system MUST allow authorized users and customers, according to permissions, to create quote requests.

A quote MUST contain:

- customer;
- creation date;
- expiration date where applicable;
- status;
- items;
- quantities;
- unit prices;
- discounts where applicable;
- taxes where applicable;
- subtotal;
- total;
- notes;
- commercial conditions.

Possible quote states SHOULD include:

```text
Draft
Pending
Sent
Viewed
Accepted
Rejected
Expired
Cancelled
```

Implemented states (api-spec §15 serializes them as numbers):

```text
Borrador (0)   → Enviado (1)   → Aceptado (2) / Rechazado (3)
Borrador/Enviado                  → Anulado (4)
Expirado (5): derived from expirationDate (BR-064)
```

`Pending` and `Viewed` are deferred: a quote is created in `Borrador`
and moves to `Enviado` when handed to the customer; view tracking will be
added with the customer portal if required. `Expirado` has no transition
endpoint: it is evaluated against `expirationDate` at acceptance time
(BR-064) and surfaced as the `expired` flag in list/detail responses.

The exact state transition rules are defined in `business-rules.md`.

---

# 13. Quote Items

Each quote item MUST preserve the commercial information applicable when the quote was created.

At minimum:

- product reference when applicable;
- description snapshot;
- quantity;
- unit price;
- discount;
- subtotal;
- custom specifications when applicable.

Changing the current product price MUST NOT modify an already-issued quote.

---

# 14. Quote Acceptance

A customer MUST be able to accept a valid quote through the client portal.

Acceptance MUST:

1. verify that the quote belongs to the customer;
2. verify that the quote can still be accepted;
3. register acceptance;
4. change quote status;
5. create the corresponding order;
6. preserve the relationship between quote and order;
7. generate the relevant notification.

The quote-to-order operation MUST be atomic.

If order creation fails, the quote MUST NOT remain falsely marked as successfully accepted.

---

# 15. Orders

Orders represent confirmed commercial operations.

An order MUST contain:

- customer;
- originating quote when applicable;
- creation date;
- status;
- items;
- quantities;
- commercial snapshots;
- production information;
- payment information;
- delivery information;
- notes.

Suggested lifecycle:

```text
Pending
Confirmed
InProduction
Ready
Delivered
Completed
Cancelled
```

Actual transitions are defined in `business-rules.md`.

---

# 16. Order History

Every relevant order status change MUST be recorded.

History SHOULD contain:

- previous status;
- new status;
- date/time;
- user;
- comment/reason where applicable.

Historical records MUST remain available for auditing.

---

# 17. Payments

Authorized users MUST be able to:

- register payments;
- view payments;
- associate payments with orders;
- view outstanding balances.

A payment SHOULD contain:

- order;
- amount;
- date;
- payment method;
- reference;
- notes;
- registering user.

Payment records are immutable after registration (BR-082): corrections
use reversal/annulment mechanisms (reserved for a future phase), never
silent edits or deletes of financial history.

The system MUST calculate:

```text
Total Order
- Payments Registered
= Outstanding Balance
```

The system MUST prevent payments from producing invalid financial states.

---

# 18. Notifications

The system MUST support notifications for relevant events.

Examples:

- quote created;
- quote sent;
- quote accepted;
- order created;
- order status changed;
- payment registered;
- order ready;
- relevant administrative events.

Notifications MUST belong to a specific user where applicable.

Users MUST be able to identify unread notifications.

---

# 19. WhatsApp Contact

WhatsApp MUST NOT be implemented as an internal messaging/chat system.

The application MUST provide a contact button that generates a WhatsApp link using configured business contact information.

Example conceptual behavior:

```text
Contact via WhatsApp
        ↓
Open WhatsApp
        ↓
External conversation
```

---

# 20. Invoicing

The initial system MUST provide simulated/controlled invoicing functionality.

The architecture MUST allow future integration with ARCA.

The first version MUST NOT depend on direct ARCA integration for normal operation.

Invoice-related information MUST preserve historical commercial data.

Implemented behavior (BR-130/BR-131):

- internal invoice emission per order, via `POST /api/v1/facturas`
  (api-spec §17A), restricted to orders in `Entregado` or `Completado`
  status;
- exactly one invoice per order, enforced transactionally together with
  the internal numbering (`SIM-######`), so a failed emission leaves no
  unnumbered document behind;
- immutable snapshot at emission: customer identity, total and issue
  date are frozen (BR-141); later product or customer changes never
  rewrite existing invoices;
- fiscal integration is isolated behind `IBillingService`, so a future
  ARCA connection does not require rewriting the domain model;
- no annulment or editing endpoints in this phase; the document is
  never presented as an authorized fiscal invoice.

---

# 21. Reports

The system supports operational reports (api-spec §20):

- orders: number, customer, date, state, total (frozen snapshot),
  paid amount and balance;
- payments: date, order, customer, amount, method, reference and
  registering user;
- quotes: number, customer, date, state, subtotal, discount and total;
- sales: issued invoices in the period (invoiced revenue with the
  customer-name snapshot);
- inventory: current stock per product with stock value at cost.

Filters:

```text
Date range (from/to, inclusive by day)
Customer
Status (orders/quotes)
Payment method (payments)
Product type (inventory)
```

Export formats:

```text
PDF
XLSX
CSV
```

Authorization: visualization requires REPORTES_VIEW; export requires
REPORTES_EXPORT (BR-140). Totals are calculated exclusively by the
backend; the UI renders the same document returned by the API.

---

# 22. Dashboard

The administrative dashboard (`GET /api/v1/dashboard`, api-spec §19)
provides aggregated indicators organized in blocks:

- **Orders** (`PEDIDOS_VIEW`): orders in progress by status, delivered and
  cancelled during the current month;
- **Quotes** (`PRESUPUESTOS_VIEW`): sent (awaiting response) and drafts,
  accepted/rejected during the current month;
- **Sales** (`FACTURAS_VIEW` or `REPORTES_VIEW`): invoiced total and
  invoice count for the current month;
- **Payments** (`PAGOS_VIEW`): collected total and payment count for the
  current month;
- **Inventory** (`STOCK_VIEW`): active products, low-stock count and
  showroom count;
- **Notifications**: own unread notifications (always shown, BR-090).

Dashboard information MUST respect the authenticated user's permissions:
blocks the user is not authorized to see are returned as `null` and hidden
in the UI. The dashboard also shows system status (API health, effective
permissions, session) and quick links filtered by permission.

---

# 23. Client Dashboard (Portal Cliente)

The client portal (`/portal`, api-spec §9B) is available only to the
`Cliente` role and reuses the same design system as the administrative
panel (Bootstrap, AGENTS.md §35). All of its data comes from the
`/api/v1/my/*` endpoints, authorized by the Firebase ID Token and
scoped to the linked customer (BR-004, BR-010).

## Access and guards

- Unauthenticated visitors are redirected to `/login`.
- The administrative layout redirects `Cliente` users to `/portal`;
  the portal layout redirects any other authenticated role back to
  `/dashboard`.
- When the uid has no linked customer, the portal shows an
  access-denied message instead of data.
- After login or self-registration, `Cliente` accounts land on
  `/portal` and other roles on `/dashboard`.

## Sections

The client dashboard MUST provide:

- **Inicio** (`/portal`): account summary — pending balance, paid
  total, order total and quotes awaiting response
  (`GET /api/v1/my/summary`, computed by the backend, BR-083) plus
  quick links;
- **Presupuestos** (`/portal/presupuestos`): paginated list with
  search and status filter; detail at `/portal/presupuestos/{id}`
  showing the frozen snapshot (BR-063); the customer requests a quote
  describing the work (`POST /api/v1/my/quotes`, line priced at 0 for
  the team to complete) and can accept a valid quote
  (`POST /api/v1/my/quotes/{id}/accept`), which creates the order
  atomically (BR-064/BR-065);
- **Pedidos** (`/portal/pedidos`): paginated list and detail with
  state-history tracking (BR-075), production/delivery dates and
  per-order payments with backend-computed balance (BR-083);
- **Pagos** (`/portal/pagos`): own payment history and account
  summary;
- **Notificaciones** (`/portal/notificaciones`): own notifications
  with unread filter and mark-as-read actions (BR-090/BR-091);
- **Perfil** (`/portal/perfil`): view and edit name, surname, phone,
  address and document (`PATCH /api/v1/my/profile`); the email is not
  editable from the portal because it identifies the account;
- **WhatsApp contact** as a contact link (AGENTS.md §21), shown when
  `NEXT_PUBLIC_WHATSAPP_NUMBER` is configured;
- **Catalog access**: delivered together with the catalog/showroom
  phase (AGENTS.md §69).

The customer MUST NOT see or modify internal data (prices under
revision, stock, users, suppliers, permissions) nor access other
customers' resources.

---

# 24. Search and Filtering

Main administrative modules SHOULD support:

- free-text search;
- status filters;
- date filters;
- pagination;
- sorting where useful.

Pagination MUST be server-side for potentially large datasets.

---

# 25. Images and Files

Images MUST be stored in Supabase Storage (BR-100; project decision:
Supabase replaces Firebase Storage — AGENTS.md §3.6).

Initial prefixes:

```text
productos/{productId}/{guid}.{ext}
trabajos/{workId}/{guid}.{ext}
```

The SQL database MUST store references instead of binary image data
(BR-101).

File validation MUST consider:

- authenticated user;
- permitted path;
- MIME type;
- extension;
- maximum size (5 MB for product images);
- ownership where applicable.

Product images are managed through api-spec §13
(`GET/POST/DELETE /api/v1/products/{id}/images`): upload requires
`PRODUCTOS_UPDATE`, the gallery requires `PRODUCTOS_VIEW`, and a failed
persistence removes the uploaded object so no orphan files remain
(AGENTS.md §24). Unconfigured or unreachable storage returns `503`
(ProblemDetails title `Storage.Error`).

The public catalog and showroom display images through their public
bucket URLs (`/storage/v1/object/public/...`).

Before uploading, the admin UI optimizes the image in the browser —
redimension to a 1600 px longest side and progressive compression
down to ≤ 900 KB (`frontend/src/lib/image-compress.ts`) — so catalog
thumbnails and galleries load fast. The 5 MB limit remains the API
contract for direct callers; GIF files are uploaded unchanged.

## 25A. Progressive Web App

The frontend is installable as a PWA (roadmap Phase 21, AGENTS.md §37):

- web app manifest served at `/manifest.webmanifest` (name, icons,
  `display: standalone`, theme color);
- PNG icons (192 and 512 px) in `/icons/`;
- a service worker (`/sw.js`) that caches only immutable
  `/_next/static` assets — it never caches `/api/` responses or
  navigations, so commercial operations always require connectivity;
- responsive design (Bootstrap) for desktop, tablet and mobile.

Offline support is deliberately limited: previously loaded static
assets may render, but no business transaction is simulated offline.

---

# 26. Auditability

Critical business operations MUST be auditable.

At minimum:

- user creation/modification;
- role/permission changes;
- quote acceptance;
- order status changes;
- payments;
- inventory adjustments;
- relevant configuration changes.

---

# 27. Functional Exclusions

The following are explicitly outside the new system scope:

- appointment/turn management;
- internal WhatsApp/chat;
- SaaS multi-tenancy;
- microservices;
- distributed event architecture;
- mandatory ARCA integration in the first version;
- migration of obsolete legacy UI as-is.

The old **Turnos** functionality MUST NOT be migrated.

---

# 28. Functional Acceptance Principles

A feature is considered functionally complete only when:

1. authorized users can perform the operation;
2. unauthorized users cannot perform it;
3. validation rules are enforced;
4. relevant history is preserved;
5. related entities remain consistent;
6. errors are communicated clearly;
7. the operation is covered by automated tests where applicable;
8. the frontend and backend enforce the same business expectations;
9. the operation works with real PostgreSQL persistence;
10. the operation does not bypass domain/business rules.