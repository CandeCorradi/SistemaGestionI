# AGENTS.md

## Sistema de Gestión — Guía Operativa para OpenCode

Este archivo define las reglas, restricciones, criterios técnicos y metodología que **OpenCode debe seguir obligatoriamente** al desarrollar, modificar, refactorizar, probar o documentar el proyecto **Sistema de Gestión**.

El sistema está orientado a la gestión integral de **una única carpintería**.

NOTAS DE ENTORNO (2026-09-12):
- SDK .NET 10 instalado a nivel usuario en `%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe` (versión 10.0.400). Los comandos deben usar esa ruta o añadirla a `PATH`.
- Solución actual: `SistemaGestion.sln` incluye 9 proyectos (Shared, Domain, Application, Infrastructure, API y 4 proyectos de test). Código legacy bajo `legacy/`.
- PostgreSQL no está corriendo localmente; `/health` regresará 503/Degraded hasta configurar una instancia local.
- Frontend (Next.js) está por implementarse en `./frontend/` (próxima fase).
- Variables de entorno definidas en `.env.example`; nunca subir `.env` ni credenciales reales al repositorio.

Este archivo no reemplaza la documentación funcional ni técnica del proyecto. Define **cómo debe trabajar el agente** y establece las decisiones que no deben ser modificadas sin autorización explícita.

---

# 1. MISIÓN DEL AGENTE

OpenCode debe actuar como un **desarrollador senior full-stack**, priorizando:

- mantenibilidad;
- seguridad;
- claridad;
- simplicidad;
- testabilidad;
- separación de responsabilidades;
- evolución incremental;
- calidad del código;
- consistencia arquitectónica;
- documentación actualizada.

No debe limitarse a hacer que una funcionalidad "funcione".

Toda implementación debe integrarse correctamente con la arquitectura existente y evitar introducir deuda técnica innecesaria.

---

# 2. FUENTE DE VERDAD

Antes de implementar una funcionalidad, revisar la documentación disponible en:

```text
docs/
├── technical-architecture.md
├── functional-specification.md
├── business-rules.md
├── api-specification.md
└── development-roadmap.md
```

Además:

```text
AGENTS.md
```

define las reglas operativas del agente.

## Jerarquía de decisiones

En caso de contradicción:

1. Decisiones explícitas y recientes del proyecto.
2. `AGENTS.md`.
3. `docs/business-rules.md`.
4. `docs/functional-specification.md`.
5. `docs/technical-architecture.md`.
6. `docs/api-specification.md`.
7. `docs/development-roadmap.md`.
8. Código existente.

El código legado **no tiene prioridad sobre la arquitectura y especificaciones nuevas**.

Si existe una contradicción importante que no pueda resolverse razonablemente, detener la implementación y explicar el conflicto antes de realizar cambios destructivos.

---

# 3. REGLAS INNEGOCIABLES

Las siguientes decisiones son obligatorias.

## 3.1 Aplicación

El sistema es para:

```text
UNA ÚNICA CARPINTERÍA
```

No implementar arquitectura multitenant ni SaaS salvo decisión futura explícita.

---

## 3.2 Backend

Utilizar:

```text
C#
.NET 10 LTS
ASP.NET Core 10
Entity Framework Core 10
Npgsql
PostgreSQL
```

No introducir .NET 8, .NET 9 u otra versión como target principal.

---

## 3.3 Frontend

Utilizar:

```text
Next.js
React
TypeScript
Bootstrap 5.3.8
Bootstrap Icons
```

Utilizar App Router.

No introducir Tailwind CSS como framework visual principal.

No mezclar deliberadamente Tailwind y Bootstrap.

---

## 3.4 Base de datos

La base de datos oficial es:

```text
PostgreSQL
```

El acceso se realiza mediante:

```text
Entity Framework Core
Npgsql
```

No utilizar SQL Server ni Azure SQL como base de datos del proyecto.

La configuración de EF Core debe utilizar:

```text
UseNpgsql(...)
```

---

## 3.5 Autenticación

Utilizar:

```text
Firebase Authentication
```

Firebase administra la identidad externa.

El backend debe verificar los Firebase ID Tokens mediante Firebase Admin SDK.

Firebase Authentication **no reemplaza al usuario interno del sistema**.

---

## 3.6 Storage

Utilizar:

```text
Supabase Storage
```

Las imágenes y archivos no deben almacenarse directamente en PostgreSQL.

PostgreSQL debe almacenar únicamente la metadata y las referencias necesarias.

NOTA: el storage de imágenes es **Supabase Storage** (decisión del
proyecto, gana sobre la referencia histórica a Firebase Storage que
aparece en versiones anteriores de este documento). Firebase sigue
siendo la solución de **autenticación** (§3.5) y de acceso a archivos
privados cuando corresponda; para el bucket de imágenes del catálogo se
utiliza Supabase (`SUPABASE_URL`, `SUPABASE_STORAGE_BUCKET`,
`SUPABASE_SERVICE_ROLE_KEY` en `.env`).

---

## 3.7 Arquitectura

Utilizar un **monolito modular**, no microservicios.

Backend:

```text
Api
Application
Domain
Infrastructure
```

Frontend organizado por features/módulos.

No introducir microservicios, Kafka, RabbitMQ, Redis u otra infraestructura distribuida sin una necesidad concreta y aprobación explícita.

---

# 4. OBJETIVO FUNCIONAL DEL SISTEMA

El sistema debe cubrir el flujo principal:

```text
Catálogo
    ↓
Cliente
    ↓
Presupuesto
    ↓
Aceptación
    ↓
Pedido
    ↓
Producción
    ↓
Pago
    ↓
Venta / Facturación
    ↓
Entrega
```

El sistema debe centralizar:

- clientes;
- proveedores;
- usuarios;
- roles;
- permisos;
- productos;
- muebles;
- melaminas;
- accesorios;
- insumos;
- inventario;
- presupuestos;
- pedidos;
- pagos;
- facturación;
- reportes;
- trabajos realizados;
- catálogo;
- showroom/vidriera;
- notificaciones;
- dashboard.

---

# 5. FUNCIONALIDADES FUERA DE ALCANCE

No implementar ni reintroducir:

```text
Turnos
Agenda de turnos
Calendario de citas
```

El código legado puede contener estas funcionalidades, pero deben considerarse **legacy/deprecadas**.

No copiar estas funcionalidades al nuevo sistema salvo autorización explícita.

---

# 6. ROLES

El sistema debe trabajar con exactamente estos roles iniciales:

```text
SuperAdmin
Owner
Admin
Empleado
Cliente
```

No crear nuevos roles arbitrariamente.

La autorización no debe depender únicamente del nombre del rol.

Debe existir un sistema de permisos granulares.

---

# 7. PERMISOS

Los permisos deben ser configurables.

Ejemplos:

```text
clientes.ver
clientes.crear
clientes.editar
clientes.eliminar

proveedores.ver
proveedores.crear
proveedores.editar
proveedores.eliminar

productos.ver
productos.crear
productos.editar
productos.eliminar

stock.ver
stock.ingresar
stock.ajustar

presupuestos.ver
presupuestos.crear
presupuestos.editar
presupuestos.aceptar
presupuestos.anular

pedidos.ver
pedidos.crear
pedidos.editar
pedidos.cancelar

pagos.ver
pagos.crear
pagos.anular

facturas.ver
facturas.crear

reportes.ver
reportes.exportar

trabajos.ver
trabajos.crear
trabajos.editar
trabajos.eliminar

usuarios.ver
usuarios.crear
usuarios.editar
usuarios.eliminar

roles.ver
roles.editar
```

Los nombres concretos deben mantenerse consistentes con `business-rules.md`.

---

# 8. AUTORIZACIÓN

El frontend puede ocultar funcionalidades según los permisos.

Pero esto solamente mejora la experiencia de usuario.

La seguridad real debe implementarse en backend.

Flujo:

```text
Firebase UID
      ↓
Usuario interno
      ↓
Roles
      ↓
Permisos
      ↓
Autorización
      ↓
Endpoint / operación
```

Nunca confiar en:

```text
role
permission
userId
```

enviados por el frontend.

El backend debe determinar la identidad y permisos reales.

---

# 9. PORTAL CLIENTE

El rol `Cliente` debe disponer de funcionalidades propias.

Como mínimo:

```text
Autenticación
Perfil
Catálogo
Showroom / trabajos realizados
Solicitud de presupuesto
Consulta de presupuestos
Aceptación de presupuesto
Consulta de pedidos
Seguimiento de pedido
Consulta de pagos
Consulta de saldo
Notificaciones
Contacto por WhatsApp
```

El cliente no debe tener acceso a funcionalidades administrativas.

No debe poder:

- modificar precios;
- modificar stock;
- administrar usuarios;
- administrar permisos;
- acceder a información de otros clientes;
- modificar pedidos ajenos;
- acceder a información interna de proveedores.

La autorización debe comprobar también la propiedad de los recursos cuando corresponda.

---

# 10. REGLA CRÍTICA: PRESUPUESTOS

Los presupuestos deben conservar un **snapshot comercial**.

Cuando se crea un presupuesto deben preservarse los datos necesarios para reconstruirlo históricamente:

- descripción;
- cantidades;
- precios;
- descuentos;
- subtotales;
- total;
- productos;
- muebles;
- materiales relevantes.

Los cambios futuros en productos o precios no deben modificar presupuestos históricos.

---

# 11. REGLA CRÍTICA: ACEPTACIÓN DE PRESUPUESTO

Cuando un cliente acepta un presupuesto válido:

```text
Presupuesto
    ↓
Aceptado
    ↓
Creación automática del Pedido
```

La aceptación y creación del pedido debe realizarse de forma atómica.

Evitar estados como:

```text
Presupuesto aceptado
pero Pedido no creado
```

si ambos pasos forman parte de la misma operación de negocio.

Utilizar transacciones cuando la operación involucre múltiples cambios coordinados.

La operación debe ser idempotente cuando sea necesario para evitar pedidos duplicados.

---

# 12. PEDIDOS

Los pedidos deben tener un ciclo de vida definido.

Ejemplo:

```text
Pendiente
Confirmado
EnProduccion
Listo
Entregado
Cancelado
```

Los estados definitivos deben coincidir con `business-rules.md`.

Toda modificación importante del estado debe conservar historial.

No eliminar información histórica necesaria para reconstruir el pedido.

---

# 13. MUEBLES

El sistema debe soportar:

```text
Mueble estándar
Mueble personalizado / a medida
```

Los muebles personalizados pueden contener:

- medidas;
- descripción;
- materiales;
- melaminas;
- accesorios;
- observaciones;
- especificaciones particulares;
- precio calculado;
- imágenes/documentación cuando corresponda.

No asumir que todos los muebles son productos estandarizados.

---

# 14. PRODUCTOS

Los productos pueden representar:

- muebles;
- melaminas;
- accesorios;
- insumos;
- otros materiales definidos por el dominio.

Mantener una separación clara entre:

```text
Producto comercial
Material
Mueble
```

cuando el dominio lo requiera.

No duplicar información innecesariamente.

---

# 15. INVENTARIO

El stock debe ser trazable.

No modificar simplemente un número de stock sin registrar el movimiento cuando corresponda.

Debe existir historial de movimientos.

Conceptualmente:

```text
StockMovement
├── Producto
├── Tipo
├── Cantidad
├── StockAnterior
├── StockPosterior
├── Motivo
├── Usuario
└── Fecha
```

Los tipos de movimiento pueden incluir:

```text
Ingreso
Egreso
Ajuste
Devolución
Reserva
Liberación
```

Los valores definitivos deben surgir de las reglas de negocio.

Las operaciones críticas de inventario deben ser atómicas.

Considerar concurrencia cuando dos operaciones puedan afectar el mismo stock.

---

# 16. PRECIOS Y DINERO

Nunca utilizar:

```text
float
double
```

para representar dinero.

Utilizar:

```text
decimal
```

en backend y una representación equivalente apropiada en frontend.

Los cálculos comerciales deben ser deterministas.

Los totales deben calcularse en backend.

El frontend puede mostrar cálculos preliminares, pero no es la autoridad comercial.

---

# 17. FECHAS Y HORAS

Para instantes de tiempo utilizar UTC.

Evitar depender directamente de:

```text
DateTime.Now
```

en lógica de negocio.

Cuando sea necesario para testing, utilizar una abstracción de reloj.

La presentación al usuario puede convertir el instante a la zona horaria correspondiente.

---

# 18. HISTORIAL Y AUDITORÍA

Las operaciones importantes deben poder auditarse.

Especialmente:

- pagos;
- stock;
- pedidos;
- presupuestos;
- usuarios;
- roles;
- permisos;
- facturación;
- cambios críticos.

Registrar, cuando corresponda:

```text
Usuario
Fecha
Acción
Entidad
EntityId
Datos relevantes
```

No registrar secretos ni información sensible innecesaria.

---

# 19. FACTURACIÓN

La facturación inicial será **simulada/interna**.

Debe existir una arquitectura preparada para una futura integración con facturación electrónica/ARCA.

No implementar una integración real con ARCA salvo requerimiento explícito.

La arquitectura no debe quedar acoplada a una futura implementación específica.

---

# 20. NOTIFICACIONES

El sistema debe contemplar notificaciones.

Pueden estar asociadas a:

- cambios de estado;
- presupuestos;
- aceptación de presupuestos;
- pedidos;
- pagos;
- saldos;
- eventos relevantes.

La implementación concreta debe respetar la arquitectura definida en la documentación.

No introducir servicios externos de mensajería innecesarios.

---

# 21. WHATSAPP

WhatsApp debe funcionar inicialmente como:

```text
Botón / enlace de contacto
```

No implementar un chat interno tipo WhatsApp.

La funcionalidad debe permitir facilitar el contacto entre cliente y carpintería.

---

# 22. CATÁLOGO Y SHOWROOM

El catálogo debe consumir datos reales de la API.

No duplicar manualmente productos o trabajos en el frontend.

El sistema debe soportar:

- imágenes;
- galerías;
- categorías;
- filtros;
- búsqueda;
- destacados;
- detalles;
- materiales;
- medidas;
- precios cuando corresponda.

Utilizar componentes visuales reutilizables.

Bootstrap debe ser el sistema visual principal.

---

# 23. VIDRIERA DE TRABAJOS REALIZADOS

La vidriera debe utilizar los mismos datos del sistema.

No crear una base de datos paralela.

Conceptualmente:

```text
PostgreSQL
     ↓
ASP.NET Core API
     ↓
Next.js
     ↓
Showroom / Vidriera
```

Un trabajo realizado puede incluir:

```text
Nombre
Tipo de mueble
Cliente
Fecha
Descripción
Medidas
Melamina
Accesorios
Imagen principal
Galería
Categoría
Tags
Destacado
Publicado
```

En una futura exposición pública deben poder ocultarse datos personales del cliente.

---

# 24. STORAGE DE IMÁGENES (SUPABASE)

Utilizar Supabase Storage para:

- muebles;
- trabajos realizados;
- galerías;
- melaminas;
- accesorios;
- perfiles;
- imágenes del catálogo.

Flujo recomendado:

```text
Frontend
    ↓
ASP.NET Core API
    ↓
Storage Service (IStorageService)
    ↓
Supabase Storage
```

Validar:

- extensión;
- MIME type;
- tamaño;
- autorización;
- nombre/ruta;
- eliminación;
- archivos huérfanos.

No almacenar archivos binarios directamente en PostgreSQL salvo una decisión explícita y justificada.

---

# 25. BACKEND

Arquitectura:

```text
backend/
├── Api/
├── Application/
├── Domain/
└── Infrastructure/
```

## Api

Contiene:

- controllers;
- endpoints;
- middleware;
- configuración;
- autenticación;
- autorización;
- filtros.

Los controllers deben ser delgados.

No colocar lógica de negocio compleja en controllers.

---

## Application

Contiene:

- casos de uso;
- servicios de aplicación;
- DTOs;
- validadores;
- interfaces;
- mappings;
- consultas;
- comandos.

La capa Application coordina los casos de uso.

---

## Domain

Contiene:

- entidades;
- value objects;
- enums;
- reglas de negocio;
- invariantes;
- abstracciones propias del dominio.

El dominio no debe depender de infraestructura.

---

## Infrastructure

Contiene implementaciones relacionadas con:

- EF Core;
- PostgreSQL;
- Npgsql;
- Firebase;
- Storage;
- servicios externos;
- persistencia.

---

# 26. ENTITY FRAMEWORK CORE

Utilizar EF Core 10.

Reglas:

- no exponer entidades EF directamente desde la API;
- utilizar DTOs;
- configurar relaciones explícitamente cuando sea necesario;
- definir índices;
- definir constraints;
- definir foreign keys;
- utilizar migrations;
- evitar queries innecesariamente costosas;
- utilizar `AsNoTracking()` en consultas de solo lectura cuando corresponda;
- evitar lazy loading salvo justificación;
- evitar cargar relaciones completas innecesariamente.

Las migraciones deben mantenerse versionadas en Git.

Nunca editar manualmente una base de datos de producción para solucionar un cambio estructural sin una migración correspondiente.

---

# 27. POSTGRESQL

PostgreSQL es la única base de datos oficial.

Utilizar:

```text
Npgsql.EntityFrameworkCore.PostgreSQL
```

Preferir tipos y características de PostgreSQL únicamente cuando aporten una ventaja real al dominio o rendimiento.

No incorporar SQL específico del proveedor innecesariamente.

Los IDs principales serán normalmente:

```text
INTEGER
BIGINT
```

con generación automática.

No utilizar UUID como PK principal salvo que exista una justificación técnica explícita.

---

# 28. DTOs

Nunca devolver directamente entidades de persistencia.

Utilizar contratos específicos:

```text
CreateDto
UpdateDto
ResponseDto
ListDto
```

Los DTOs representan el contrato público de la API.

No utilizar entidades EF como request models.

No aceptar campos sensibles o internos desde el cliente.

---

# 29. API REST

La API debe estar versionada:

```text
/api/v1/
```

Ejemplos:

```text
/api/v1/clientes
/api/v1/proveedores
/api/v1/productos
/api/v1/melaminas
/api/v1/accesorios
/api/v1/muebles
/api/v1/presupuestos
/api/v1/pedidos
/api/v1/pagos
/api/v1/facturas
/api/v1/trabajos-realizados
/api/v1/reportes
/api/v1/dashboard
/api/v1/usuarios
/api/v1/roles
/api/v1/permisos
```

La API debe mantener convenciones consistentes.

---

# 30. PAGINACIÓN

Los endpoints de listados deben paginarse cuando el volumen pueda crecer.

Ejemplo:

```http
GET /api/v1/clientes?page=1&pageSize=20
```

Respuesta conceptual:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 100,
  "totalPages": 5
}
```

Agregar cuando corresponda:

```text
search
filters
sorting
```

No permitir tamaños de página ilimitados.

Definir límites razonables.

---

# 31. VALIDACIÓN

Las validaciones de negocio deben existir en backend.

Utilizar FluentValidation cuando resulte apropiado.

El frontend puede validar para mejorar UX.

Pero:

```text
Frontend validation != Security
```

Toda validación crítica debe ejecutarse nuevamente en backend.

---

# 32. MANEJO DE ERRORES

Implementar manejo global de errores.

Utilizar:

```text
Problem Details
```

Status codes esperados:

```text
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
422 Unprocessable Entity
500 Internal Server Error
```

No devolver stack traces en producción.

Los errores deben ser consistentes y útiles para frontend y debugging.

---

# 33. LOGGING

Utilizar logging estructurado.

Serilog puede utilizarse como implementación principal.

Registrar eventos relevantes, por ejemplo:

```text
LOGIN_SUCCESS
LOGIN_FAILED
CLIENT_CREATED
ORDER_CREATED
ORDER_UPDATED
PAYMENT_REGISTERED
STOCK_UPDATED
IMAGE_UPLOADED
REPORT_GENERATED
USER_CREATED
AUTHORIZATION_DENIED
API_ERROR
```

Nunca registrar:

```text
Passwords
Firebase ID Tokens
Access Tokens
Private Keys
Secrets
```

No registrar información sensible innecesariamente.

---

# 34. HEALTH CHECKS

Implementar:

```http
GET /health
```

Debe permitir determinar si la aplicación y sus dependencias críticas están operativas.

Como mínimo evaluar:

- API;
- PostgreSQL;
- dependencias críticas configuradas.

---

# 35. FRONTEND

Utilizar:

```text
Next.js
React
TypeScript
Bootstrap 5.3.8
Bootstrap Icons
```

Utilizar TypeScript estricto.

Aplicar:

- componentes reutilizables;
- separación de UI y lógica;
- servicios para API;
- hooks;
- manejo de errores;
- loading states;
- empty states;
- responsive design;
- formularios validados;
- protección de rutas;
- manejo correcto de sesión.

No colocar toda la lógica dentro de `app/`.

---

# 36. ESTRUCTURA FRONTEND

Mantener una estructura similar a:

```text
frontend/
└── src/
    ├── app/
    ├── components/
    │   ├── ui/
    │   ├── forms/
    │   ├── tables/
    │   └── layout/
    ├── features/
    │   ├── auth/
    │   ├── clientes/
    │   ├── proveedores/
    │   ├── productos/
    │   ├── muebles/
    │   ├── melaminas/
    │   ├── accesorios/
    │   ├── stock/
    │   ├── presupuestos/
    │   ├── pedidos/
    │   ├── pagos/
    │   ├── facturacion/
    │   ├── trabajos/
    │   ├── catalogo/
    │   ├── notificaciones/
    │   └── dashboard/
    ├── services/
    ├── hooks/
    ├── types/
    ├── lib/
    └── config/
```

La estructura puede evolucionar si existe una razón técnica.

No reorganizar grandes partes del frontend sin necesidad.

---

# 37. PWA

El frontend debe estar preparado como PWA.

Considerar:

- Web App Manifest;
- instalación;
- responsive design;
- service worker;
- estrategia de caché;
- comportamiento offline limitado cuando corresponda.

No asumir que toda la aplicación debe funcionar completamente offline.

Las operaciones comerciales críticas deben requerir conexión con la API.

---

# 38. RESPONSIVE DESIGN

La aplicación debe funcionar correctamente en:

- desktop;
- notebook;
- tablet;
- móvil.

Utilizar el sistema responsive de Bootstrap.

No crear interfaces diferentes innecesariamente para cada dispositivo.

---

# 39. TESTING

Testing es obligatorio.

Utilizar según corresponda:

```text
xUnit
WebApplicationFactory
PostgreSQL
Playwright
```

EF Core InMemory puede utilizarse para determinados unit/API tests, pero **no debe utilizarse como sustituto de los tests reales contra PostgreSQL**.

Los tests de integración deben utilizar PostgreSQL en un entorno aislado.

Nunca utilizar:

```text
Development DB
Production DB
```

para tests.

---

# 40. UNIT TESTS

Probar especialmente:

- reglas de negocio;
- validaciones;
- cálculos;
- pagos;
- saldos;
- stock;
- transición de estados;
- permisos;
- casos límite.

Los tests deben ser rápidos y deterministas.

---

# 41. TESTS DE API

Utilizar `WebApplicationFactory` cuando corresponda.

Probar:

- requests válidos;
- validaciones;
- status codes;
- autorización;
- errores;
- DTOs;
- endpoints;
- paginación.

---

# 42. TESTS DE INTEGRACIÓN

Utilizar PostgreSQL real en un entorno aislado.

Comprobar:

- EF Core;
- Npgsql;
- queries;
- foreign keys;
- constraints;
- migrations;
- transactions;
- persistencia;
- comportamiento real de PostgreSQL.

---

# 43. TESTS E2E

Utilizar Playwright para los flujos principales.

Ejemplos:

```text
Login
Crear cliente
Crear presupuesto
Aceptar presupuesto
Crear pedido automáticamente
Registrar pago
Consultar saldo
Actualizar estado de pedido
Crear trabajo realizado
Visualizar showroom
```

Los flujos deben probarse desde la perspectiva del usuario.

---

# 44. REGRESIÓN

Cada bug importante corregido debe generar un test que reproduzca el problema.

Flujo:

```text
Bug
 ↓
Test que reproduce el bug
 ↓
Test falla
 ↓
Corrección
 ↓
Test pasa
 ↓
Se conserva como regresión
```

No eliminar tests simplemente porque una implementación haya cambiado.

---

# 45. SEGURIDAD

Aplicar como mínimo:

- HTTPS;
- Firebase Authentication;
- Firebase Admin SDK;
- autorización backend;
- CORS restrictivo;
- validación de entrada;
- validación de archivos;
- protección de endpoints administrativos;
- secrets mediante variables de entorno;
- rate limiting cuando corresponda;
- headers de seguridad cuando correspondan;
- no exponer secretos;
- no confiar en datos de autorización provenientes del frontend.

---

# 46. VARIABLES DE ENTORNO

Nunca hardcodear secretos.

Utilizar variables de entorno.

Ejemplo:

```text
DATABASE_CONNECTION_STRING

FIREBASE_PROJECT_ID
FIREBASE_CLIENT_EMAIL
FIREBASE_PRIVATE_KEY
FIREBASE_STORAGE_BUCKET
```

Mantener:

```text
.env.example
```

sin valores reales.

Nunca subir:

```text
.env
service-account.json
private keys
tokens
credentials
```

al repositorio.

---

# 47. SECRETOS

Antes de realizar un commit verificar que no existan:

- claves privadas;
- tokens;
- contraseñas;
- credenciales;
- archivos de service account;
- connection strings sensibles.

Si se detecta un secreto accidentalmente incluido en Git, detener el proceso y reportarlo.

No limitarse a borrar el archivo si el secreto ya fue expuesto: debe considerarse potencialmente comprometido.

---

# 48. REPORTES

El sistema debe soportar generación de reportes.

Formatos previstos:

```text
PDF
XLSX
CSV
```

Reportes posibles:

- clientes;
- pedidos;
- ventas;
- pagos;
- productos;
- stock;
- presupuestos.

Utilizar una librería adecuada para cada formato.

ClosedXML puede utilizarse para XLSX.

Los servicios de generación de reportes no deben estar implementados directamente dentro de controllers.

---

# 49. SWAGGER / OPENAPI

Documentar la API mediante OpenAPI.

Documentar:

- endpoints;
- requests;
- responses;
- DTOs;
- parámetros;
- status codes;
- autenticación;
- autorización;
- paginación;
- errores;
- ejemplos relevantes.

La documentación debe actualizarse junto con el código.

---

# 50. AUDITORÍA

La auditoría es transversal.

Cuando se modifiquen datos críticos, considerar registrar:

```text
Who
What
When
Where
Entity
EntityId
Relevant change
```

No utilizar auditoría como sustituto del logging.

Son mecanismos diferentes:

```text
Logging → diagnóstico técnico

Auditoría → trazabilidad funcional
```

---

# 51. LEGACY

El proyecto puede contener código legado proveniente de:

```text
Windows Forms
Blazor
MAUI
ASP.NET Core anterior
```

Este código debe utilizarse como **referencia funcional y de negocio**, no como arquitectura obligatoria.

Antes de reutilizar código legado:

1. Analizar qué problema resuelve.
2. Determinar si sigue perteneciendo al alcance.
3. Compararlo con la arquitectura nueva.
4. Reimplementar cuando sea necesario.

No realizar portaciones mecánicas.

No reintroducir funcionalidades eliminadas.

---

# 52. CAMBIOS INCREMENTALES

No modificar grandes partes del sistema sin necesidad.

Antes de implementar:

```text
1. Analizar requerimiento.
2. Revisar documentación.
3. Revisar arquitectura existente.
4. Revisar código relacionado.
5. Diseñar solución.
6. Implementar cambios mínimos necesarios.
7. Crear/actualizar tests.
8. Ejecutar tests.
9. Corregir errores.
10. Actualizar documentación.
11. Verificar regresiones.
```

Priorizar cambios pequeños y verificables.

---

# 53. NO SOBREENINGENIERÍA

No introducir tecnologías solamente porque sean populares.

Evitar inicialmente:

```text
Microservices
Kafka
RabbitMQ
Redis
Event Sourcing
Distributed CQRS
Kubernetes
```

si no existe una necesidad real.

La arquitectura debe ser suficientemente robusta sin ser innecesariamente compleja.

---

# 54. CQRS

Puede utilizarse una separación conceptual entre:

```text
Commands
Queries
```

cuando mejore la organización.

No implementar infraestructura distribuida de CQRS.

No agregar MediatR u otra librería solamente por seguir una moda arquitectónica.

---

# 55. TRANSACCIONES

Las operaciones que modifiquen múltiples entidades relacionadas deben mantener consistencia.

Ejemplos:

```text
Aceptar presupuesto
        ↓
Crear pedido
```

o:

```text
Registrar pago
        ↓
Actualizar saldo
        ↓
Registrar auditoría
```

Cuando corresponda utilizar transacciones de EF Core/PostgreSQL.

No asumir que dos operaciones independientes son automáticamente atómicas.

---

# 56. CONCURRENCIA

Considerar concurrencia especialmente en:

- stock;
- pagos;
- aceptación de presupuestos;
- creación de pedidos;
- cambios de estado.

Evitar condiciones de carrera.

Cuando una operación pueda ejecutarse simultáneamente, analizar:

```text
¿Qué ocurre si dos usuarios hacen esto al mismo tiempo?
```

antes de implementarla.

---

# 57. SOFT DELETE

Utilizar baja lógica únicamente cuando tenga sentido.

No aplicar soft delete indiscriminadamente.

Los datos históricos que sean necesarios para:

- pedidos;
- pagos;
- facturación;
- auditoría;

deben preservarse.

Nunca eliminar físicamente información histórica crítica solamente para simplificar un CRUD.

---

# 58. GIT

Utilizar Git de manera consistente.

Los commits deben ser pequeños y descriptivos.

Preferir mensajes del tipo:

```text
feat: add customer management
fix: prevent duplicate order creation
test: add payment balance tests
refactor: simplify inventory service
docs: update API specification
```

No mezclar en un mismo commit:

```text
feature + refactor masivo + cambios no relacionados
```

---

# 59. PULL REQUEST / QUALITY GATE

Antes de considerar terminado un cambio importante:

```text
Restore
 ↓
Build
 ↓
Lint / Static checks
 ↓
Unit Tests
 ↓
API Tests
 ↓
Integration Tests
 ↓
E2E cuando corresponda
 ↓
Production Build
 ↓
Quality Gate
```

Un cambio que rompe tests no debe considerarse terminado.

---

# 60. CI/CD

La integración continua debe verificar como mínimo:

```text
Build
Tests
Quality
Security checks
Production build
```

El deployment no debe ejecutarse si falla un quality gate crítico.

---

# 61. DEPLOYMENT

El deployment objetivo será **Render** (Web Services Docker para la API y el
frontend) con **PostgreSQL en Supabase**. El detalle operativo está en
`docs/deployment-render.md` y `render.yaml`.

La base de datos **no** se aprovisiona en Render: PostgreSQL es el proyecto
gestionado de Supabase. El storage de imágenes también es Supabase Storage.

No asumir todavía un servicio específico de Render para cada componente
(Background Worker, Cron Job, etc.) si no fue decidido explícitamente.

La configuración debe permitir separar:

```text
Development
Testing
Staging
Production
```

Nunca utilizar credenciales de producción durante desarrollo local.

---

# 62. PERFORMANCE

Los endpoints normales deben diseñarse para responder en aproximadamente:

```text
< 2 segundos
```

bajo la carga esperada del proyecto.

Objetivo inicial aproximado:

```text
50 usuarios concurrentes
```

Esto es un objetivo de diseño, no una garantía absoluta.

Evitar:

- N+1 queries;
- consultas sin paginar;
- cargar grandes colecciones innecesariamente;
- endpoints que devuelvan datos excesivos;
- procesamiento pesado dentro de controllers.

---

# 63. IMÁGENES

Las imágenes deben optimizarse para web.

Considerar:

- tamaño;
- formato;
- compresión;
- thumbnails;
- lazy loading;
- dimensiones máximas.

No cargar imágenes originales gigantes innecesariamente en listados.

---

# 64. DOCUMENTACIÓN

Cuando una implementación cambie una decisión arquitectónica, funcional o de negocio:

```text
Código
 +
Documentación
```

deben actualizarse en el mismo cambio.

Nunca dejar documentación deliberadamente desactualizada.

Mantener especialmente sincronizados:

```text
AGENTS.md
technical-architecture.md
functional-specification.md
business-rules.md
api-specification.md
development-roadmap.md
```

---

# 65. DECISIONES TÉCNICAS

Si existen varias soluciones técnicamente válidas:

1. Identificar las alternativas.
2. Recomendar una.
3. Explicar brevemente el motivo.
4. Priorizar:
   - mantenibilidad;
   - seguridad;
   - simplicidad;
   - testabilidad;
   - rendimiento;
   - evolución futura.
5. Evitar sobreingeniería.

No introducir una tecnología nueva sin justificarla.

---

# 66. COMPORTAMIENTO ANTE AMBIGÜEDADES

Si un requerimiento es ambiguo pero puede resolverse razonablemente con las reglas existentes:

- utilizar la opción más simple;
- respetar la arquitectura;
- documentar la decisión.

Si la ambigüedad puede provocar una decisión arquitectónica importante o afectar datos existentes:

**no asumir silenciosamente.**

Explicar:

```text
Problema
Opciones
Recomendación
Impacto
```

y solicitar decisión cuando sea necesario.

---

# 67. REGLA DE NO REGRESIÓN

Toda nueva funcionalidad debe preservar las funcionalidades existentes.

Antes de finalizar:

```text
¿El cambio rompe algo existente?
```

Verificar:

- backend;
- frontend;
- base de datos;
- API;
- autenticación;
- autorización;
- tests;
- documentación.

---

# 68. DEFINITION OF DONE

Una funcionalidad se considera terminada solamente cuando corresponde:

```text
☑ Código implementado
☑ Arquitectura respetada
☑ DTOs
☑ Validaciones
☑ Autorización
☑ Manejo de errores
☑ Logging
☑ Swagger/OpenAPI
☑ Unit Tests
☑ API Tests
☑ Integration Tests cuando corresponda
☑ E2E cuando corresponda
☑ Frontend integrado
☑ Loading states
☑ Error states
☑ Empty states
☑ Responsive design
☑ Documentación actualizada
☑ Migraciones actualizadas cuando corresponda
☑ CI verde
☑ Sin secretos expuestos
☑ Sin regresiones conocidas
```

Una funcionalidad que funciona manualmente pero no cumple los criterios correspondientes **no está terminada**.

---

# 69. ORDEN DE IMPLEMENTACIÓN

Seguir preferentemente este orden:

```text
1. Repository / Git
2. Arquitectura
3. Backend base
4. PostgreSQL
5. EF Core / migrations
6. Firebase Authentication
7. Usuarios
8. Roles
9. Permisos
10. Clientes
11. Proveedores
12. Productos
13. Muebles
14. Melaminas
15. Accesorios
16. Insumos
17. Inventario
18. Presupuestos
19. Aceptación de presupuesto
20. Pedidos
21. Producción / estados
22. Pagos
23. Facturación simulada
24. Notificaciones
25. Reportes
26. Frontend administrativo
27. Portal Cliente
28. Catálogo
29. Showroom / Vidriera
30. PWA
31. Testing integral
32. Seguridad
33. Performance
34. CI/CD
35. Deployment
36. Documentación final
```

El roadmap oficial contiene el detalle de cada fase.

---

# 70. REGLA FINAL PARA OPENCODE

Antes de escribir código, OpenCode debe preguntarse:

```text
¿Esta implementación respeta la arquitectura?
¿Respeta las reglas de negocio?
¿Respeta PostgreSQL?
¿Respeta .NET 10?
¿Respeta Next.js + Bootstrap?
¿Respeta Firebase?
¿Respeta los cinco roles?
¿Respeta la autorización granular?
¿Respeta el Portal Cliente?
¿Respeta el historial?
¿Respeta la seguridad?
¿Puede ser testeada?
¿Puede producir regresiones?
¿La documentación seguirá siendo correcta?
```

Si la respuesta es negativa en un punto importante, corregir el diseño antes de implementar.

---

# 71. PRINCIPIO GENERAL

El objetivo no es construir la mayor cantidad de código posible.

El objetivo es construir un sistema:

```text
Simple
      +
Seguro
      +
Mantenible
      +
Testeable
      +
Documentado
      +
Evolutivo
```

La prioridad es una **base sólida** que permita que el Sistema de Gestión crezca sin tener que rehacer su arquitectura.

**No implementar por implementar.**

**No sobreingenierizar.**

**No romper funcionalidades existentes.**

**No ignorar las reglas de negocio.**

**No confiar en el frontend para seguridad.**

**No introducir tecnologías innecesarias.**

**No dejar documentación desactualizada.**

Cada cambio debe ser pequeño, razonado, verificable y coherente con el sistema completo.