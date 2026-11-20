# Business Rules — Sistema de Gestión

## 1. Purpose

This document defines the business rules and invariants of the Sistema de Gestión.

These rules MUST be enforced by the backend regardless of the frontend behavior.

The frontend MUST NOT be considered a security or business-rule enforcement layer.

---

# 2. Core Business Principle

The system manages the commercial lifecycle:

```text
Customer
   ↓
Quote
   ↓
Acceptance
   ↓
Order
   ↓
Production
   ↓
Payment
   ↓
Delivery
   ↓
Completion
```

Each transition MUST comply with the rules defined in this document.

---

# 3. User and Permission Rules

## BR-001 — Fixed Roles

The system MUST contain exactly:

```text
SuperAdmin
Owner
Admin
Empleado
Cliente
```

New roles MUST NOT be created dynamically unless this requirement is explicitly changed.

---

## BR-002 — Granular Permissions

Authorization MUST support permissions independently of roles.

A role is a collection of permissions.

Example:

```text
Empleado
 ├── CLIENTES_VIEW
 ├── CLIENTES_CREATE
 ├── PRESUPUESTOS_VIEW
 └── PRESUPUESTOS_CREATE
```

---

## BR-003 — Backend Authorization

Every protected operation MUST be authorized by the backend.

Hiding a button in the frontend MUST NOT be considered authorization.

---

## BR-004 — Customer Isolation

A Cliente MUST only be able to access:

- their own profile;
- their own quotes;
- their own orders;
- their own payments;
- their own notifications.

A customer MUST NOT access another customer's data by modifying an ID in a request.

---

## BR-005 — Self-Registration

A new user MAY register through the public endpoint
`POST /api/v1/auth/register`.

Self-registration MUST always assign the fixed `Cliente` role. The
request MUST NOT be able to choose a role.

The account is created in Firebase Authentication and the internal user
is linked to it. If the internal user cannot be persisted, the Firebase
account MUST be removed (best-effort rollback).

---

# 4. Customer Rules

## BR-010 — Customer Identity

A customer account MAY be linked to a Firebase UID.

The Firebase UID MUST be unique.

---

## BR-011 — Customer Deactivation

Deactivating a customer MUST NOT delete historical quotes, orders or payments.

---

# 5. Supplier Rules

## BR-020 — Supplier Terminology

The new system MUST use:

```text
Proveedor
```

instead of:

```text
Mayorista
```

The old terminology MUST NOT be introduced in new APIs, entities, database tables or UI labels.

---

## BR-021 — Supplier History

A supplier referenced by historical records MUST NOT be physically deleted in a way that breaks historical relationships.

---

# 6. Product Rules

## BR-030 — Product Identity

Every active product MUST have a unique business identity according to its product code/reference.

---

## BR-031 — Product Price Changes

Changing the current product price MUST NOT modify:

- existing quotes;
- accepted orders;
- historical sales.

Historical commercial data MUST remain immutable.

---

## BR-032 — Product Deactivation

Products used in historical transactions MUST be deactivated instead of physically deleted.

---

# 7. Custom Furniture Rules

## BR-040 — Custom Product Independence

A custom furniture quote MAY exist without a predefined catalog product.

---

## BR-041 — Custom Specifications

Custom dimensions and specifications MUST be preserved with the quote/order.

Changing a catalog product MUST NOT alter the specifications of an already-created custom quote.

---

# 8. Inventory Rules

## BR-050 — Atomic Stock Operations

Inventory modifications MUST be atomic.

A stock operation must either:

```text
complete successfully
```

or:

```text
not modify the stock
```

---

## BR-051 — Stock History

Every manual stock adjustment MUST create a movement record.

---

## BR-052 — Negative Stock

Negative stock MUST NOT be allowed unless an explicit future business requirement enables it.

---

## BR-053 — Concurrency

Concurrent inventory modifications MUST not overwrite each other's changes.

The implementation MUST use appropriate database concurrency/transaction mechanisms.

---

# 9. Quote Rules

## BR-060 — Quote Ownership

Every quote MUST belong to exactly one customer.

---

## BR-061 — Quote Items

A quote MUST contain at least one item before it can be sent or accepted.

---

## BR-062 — Quote Total

The backend MUST calculate quote totals.

The frontend-provided total MUST NOT be trusted.

Conceptually:

```text
Subtotal
- Discounts
+ Taxes
= Total
```

The exact tax configuration is a business parameter.

---

## BR-063 — Historical Price Snapshot

When a quote is issued, the applicable commercial price MUST be stored as a snapshot.

Changing the current product price MUST NOT modify the quote.

---

## BR-064 — Quote Acceptance

Only a valid quote can be accepted.

A quote MUST NOT be accepted if it is:

```text
Expired
Rejected
Cancelled
Already Accepted
```

---

## BR-065 — Quote Acceptance Atomicity

Quote acceptance and order creation MUST be treated as one transactional business operation.

The system MUST guarantee:

```text
Accepted Quote
+
Created Order
```

or:

```text
No acceptance
+
No order
```

---

# 10. Order Rules

## BR-070 — Order Origin

An order MAY originate from:

- an accepted quote;
- an authorized internal operation where explicitly supported.

---

## BR-071 — Order Commercial Snapshot

An order MUST preserve the commercial information that was agreed with the customer.

Later changes to:

- product;
- price;
- material;
- supplier;
- catalog information

MUST NOT modify the historical order.

---

## BR-072 — Order Status

Order statuses MUST follow valid transitions.

The system MUST NOT allow arbitrary jumps between states.

---

## BR-073 — Suggested Order Lifecycle

Default lifecycle:

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

Cancellation MAY occur from permitted states.

---

## BR-074 — Completed Orders

A completed order MUST NOT be modified in ways that alter its historical commercial meaning.

Corrections MUST use controlled mechanisms.

---

## BR-075 — Order History

Every relevant status transition MUST create an immutable history record.

---

## BR-076 — Order Production and Delivery Tracking

Successful status transitions MUST record tracking timestamps in UTC:

```text
→ EnProduccion    production start
→ Listo           production end
→ Entregado       actual delivery
```

Orders MUST support editable tracking information while they are active:

```text
Notes
Estimated delivery date
```

Editable tracking information MUST NOT change on terminal orders
(BR-074).

Notes MUST NOT exceed 2000 characters.

An estimated delivery date MUST NOT precede the order creation date.

---

# 11. Payment Rules

## BR-080 — Payment Amount

Payment amounts MUST be positive.

---

## BR-081 — Payment Validation

The backend MUST verify:

```text
payment <= outstanding balance
```

unless an explicit overpayment business rule is introduced.

---

## BR-082 — Payment History

Registered payments MUST remain auditable.

Deleting a payment MUST NOT silently alter historical financial records.

Corrections SHOULD use reversal/adjustment mechanisms.

---

## BR-083 — Balance

Outstanding balance:

```text
Order Total - Confirmed Payments
```

MUST be calculated consistently.

---

# 12. Notification Rules

## BR-090 — Notification Ownership

A notification MUST belong to a user when it is user-specific.

---

## BR-091 — Notification Read State

Notifications MUST support at least:

```text
Unread
Read
```

---

## BR-092 — Business Events

The system SHOULD generate notifications for:

- quote requested by the customer (portal);
- quote sent;
- quote accepted;
- order created;
- order status changed;
- payment registered;
- order ready.

---

# 13. Image/File Rules

## BR-100 — Storage

Images MUST be stored in Supabase Storage (project decision replacing
Firebase Storage — AGENTS.md §3.6). The bucket and credentials are
configured with the `SUPABASE_URL`, `SUPABASE_STORAGE_BUCKET` and
`SUPABASE_SERVICE_ROLE_KEY` environment variables; the service role key
MUST NOT leave the backend. Public read access uses the bucket's public
object URLs.

---

## BR-101 — Database Storage

The relational database MUST NOT store image binary data for normal product/profile images.

---

## BR-102 — Access Control

Files MUST be accessible only according to authorization rules.

A user MUST NOT obtain unauthorized files merely by guessing their storage path.

---

# 14. Audit Rules

## BR-110 — Critical Operations

The following operations SHOULD be audited:

```text
User changes
Permission changes
Quote acceptance
Order status changes
Payments
Inventory adjustments
Important configuration changes
```

---

## BR-111 — Audit Immutability

Audit records MUST NOT be editable by normal users.

---

# 15. Financial Rules

## BR-120 — Decimal Money

Monetary values MUST use decimal-compatible representations.

The system MUST NOT use floating-point types such as:

```text
float
double
```

for monetary persistence/calculation.

---

## BR-121 — Rounding

The system MUST define a consistent rounding strategy for monetary calculations.

The same strategy MUST be used by backend reports, quotes, orders and payments.

---

# 16. Invoice Rules

## BR-130 — Simulated Invoicing

The initial invoice functionality is simulated.

It MUST NOT falsely represent a document as an officially authorized fiscal invoice.

An invoice can only be issued for an order in `Entregado` or `Completado` status, and each order may have at most one invoice. Emission and internal numbering (`SIM-######`) are atomic: a failed emission never leaves an unnumbered document.

---

## BR-131 — Future ARCA Integration

The invoicing architecture SHOULD isolate fiscal integration behind an abstraction so future ARCA integration does not require rewriting the domain model.

---

# 17. Reporting Rules

## BR-140 — Report Authorization

Reports MUST be restricted according to user permissions.

---

## BR-141 — Historical Data

Reports MUST use historical transaction snapshots where necessary.

Changing current product information MUST NOT rewrite historical sales reports.

---

# 18. Data Integrity

## BR-150 — Referential Integrity

Foreign-key relationships MUST be enforced by PostgreSQL wherever appropriate.

---

## BR-151 — Transactional Operations

Operations affecting multiple business entities MUST use transactions when partial completion could create inconsistent business state.

Examples:

```text
Accept Quote
Create Order
Register related stock movement
Register related payment operation
```

when these operations are implemented as one business action.

---

# 19. Deletion Policy

## BR-160 — Prefer Deactivation

Entities with historical significance SHOULD use:

```text
Active / Inactive
```

instead of physical deletion.

---

## BR-161 — Physical Deletion

Physical deletion is only permitted when:

1. the entity has no relevant historical references;
2. no business rule requires preservation;
3. the operation is explicitly authorized.

---

# 20. Date and Time

## BR-170 — Stored Time

System timestamps representing instants MUST use UTC internally.

Presentation layers MAY convert timestamps to the user's local timezone.

---

# 21. Legacy Rules

## BR-180 — Legacy Is Reference

Legacy Windows Forms, Blazor and MAUI code MUST be treated as reference material.

It MUST NOT automatically dictate the architecture of the new system.

---

## BR-181 — Turnos

The old Turnos/appointments functionality is explicitly excluded.

It MUST NOT be migrated into the new system.

---

# 21A. Showroom Rules

## BR-190 — Published Works Only

The public showcase (`GET /api/v1/showroom/works` and detail) MUST
only return works with `publicado = true`.

Drafts MUST respond `404` on the detail endpoint so their existence is
not disclosed. Publishing/unpublishing is restricted to
`TRABAJOS_UPDATE`.

---

## BR-191 — Client Privacy in the Showcase

Works store `clienteNombre` for internal reference. The name is only
exposed publicly when the work was saved with
`publicarNombreCliente = true`.

Public DTOs MUST NOT include `publicado`, `publicarNombreCliente` or
the audit timestamps. Internal management DTOs remain available to
users with `TRABAJOS_VIEW`.

---

## BR-192 — Showcase Gallery

Work images follow the storage rules BR-100/BR-101: binaries live in
Supabase Storage (`trabajos/{workId}/{guid}.{ext}`) and PostgreSQL
stores only `TrabajoRealizadoImagenes` references.

Upload validation mirrors product images (extension/MIME allow-list,
max 5 MB). The cover image is the lowest `orden`. Deleting a work
removes its image rows and storage objects (best-effort cleanup; no
orphan files).

---

# 22. Business Rule Enforcement

Every business rule MUST be enforced at the appropriate backend layer.

Recommended responsibility:

```text
Domain
 └── Core invariants

Application
 └── Use-case rules

Infrastructure
 └── Persistence constraints

API
 └── Request/authentication concerns
```

The frontend MAY reproduce validation for user experience but MUST NOT be the only enforcement mechanism.

---

# 23. Business Rule Testing

Critical business rules MUST have automated tests.

At minimum, tests MUST cover:

- unauthorized access;
- customer data isolation;
- quote acceptance;
- quote expiration;
- quote-to-order atomicity;
- invalid order transitions;
- payment over balance;
- inventory concurrency;
- negative stock prevention;
- historical price preservation;
- permission enforcement.