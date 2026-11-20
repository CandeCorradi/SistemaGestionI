# Deployment en Render

Guía operativa para publicar el Sistema de Gestión en **Render** con
PostgreSQL en **Supabase**. Complementa `render.yaml` (Blueprint) y
`docs/technical-architecture.md` §12.

```text
Navegador
    │  HTTPS
    ├──────────────► sistema-gestion-web.onrender.com   (Next.js, Docker)
    │                        │ fetch + Firebase ID Token
    │                        ▼
    └──────────────► sistema-gestion-api.onrender.com   (ASP.NET Core, Docker)
                             │ Npgsql (SSL)
                             ▼
                     Supabase PostgreSQL
                             │
              Firebase Authentication (identidad)
              Supabase Storage (imágenes)
```

- La API y el frontend son **Web Services Docker** independientes del plan
  free. El dominio `*.onrender.com` y el HTTPS los provee Render.
- La base de datos **no** vive en Render: se usa el proyecto de Supabase.
- No hay `preDeployCommand` en el plan free: las migraciones se aplican al
  arrancar la API con `Database__MigrateOnStartup=true`.

---

## 1. Requisitos previos

- Repositorio en GitHub con este código (incluidos `render.yaml` y los
  `Dockerfile`).
- Cuenta de Render conectada a GitHub.
- Proyecto de Supabase creado.
- Proyecto de Firebase con Authentication habilitado.

> Los nombres de servicio de Render son **únicos a nivel global**. Si
> `sistema-gestion-api` ya está tomado, elegí otro nombre: las URLs cambian
> y hay que reflejarlas en CORS y en `NEXT_PUBLIC_API_URL`.

---

## 2. Supabase (PostgreSQL + Storage)

### 2.1 Base de datos

1. Crear el proyecto. Elegir una región cercana a la de Render
   (`virginia` en el Blueprint) para reducir latencia.
2. **Project Settings → Database → Connection string → Session pooler**.
   Es la cadena IPv4 recomendada para servicios en la nube (la conexión
   directa de Supabase es IPv6).
3. Copiar la cadena y completar la contraseña. Debe quedar con esta forma:

```text
Host=aws-0-us-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<project-ref>;Password=<password>;SSL Mode=Require
```

Se carga en Render como `ConnectionStrings__DefaultConnection`.

> Usar el **Session pooler** (puerto 5432). El Transaction pooler (6543) no
> es adecuado para migraciones de EF Core.

### 2.2 Storage

1. Crear un bucket llamado `productos`.
2. Marcarlo como **público**: la API expone las imágenes con
   `/storage/v1/object/public/...` y no firma URLs.
3. Copiar `Project URL` y la `service_role key` (Project Settings → API).
   La `service_role key` es un secreto: solo va en Render.

---

## 3. Firebase

### 3.1 Credenciales del backend (Firebase Admin)

La vía más simple y sin problemas de saltos de línea es el **JSON completo
del service account** en una sola línea:

1. Firebase Console → Project Settings → Service accounts → Generate new
   private key. Se descarga un `.json`.
2. Minificarlo a una línea (por ejemplo con `jq -c . service-account.json`).
3. Cargarlo como `Firebase__ServiceAccountJson`.

Alternativa (tres variables): `Firebase__ProjectId`,
`Firebase__ClientEmail` y `Firebase__PrivateKey`. En este caso la clave
privada debe conservar los saltos de línea reales; si se pega con `\n`
literales, `RSA.ImportFromPem` falla y Firebase queda deshabilitado.

`Firebase__StorageBucket` solo es necesario si se usan archivos privados de
Firebase; las imágenes del catálogo van a Supabase Storage.

### 3.2 Configuración pública del frontend

Firebase Console → Project Settings → General → Your apps → Web app. Copiar
`apiKey`, `authDomain`, `projectId`, `storageBucket`, `messagingSenderId` y
`appId` a las variables `NEXT_PUBLIC_FIREBASE_*`. No son secretos: viajan
al navegador.

### 3.3 Dominios autorizados y proveedores

1. Authentication → Settings → **Authorized domains** → agregar el host del
   frontend, por ejemplo `sistema-gestion-web.onrender.com` (sin `https://`).
   Sin esto, Google/email falla con `auth/unauthorized-domain`.
2. Authentication → Sign-in method → habilitar **Email/Password** y
   **Google** según lo que se vaya a usar.
3. Si se usa Google, configurar el consentimiento y el client ID en Firebase.

---

## 4. Render

### 4.1 Crear los servicios con el Blueprint

1. Render Dashboard → **New → Blueprint** → seleccionar el repositorio.
2. Render lee `render.yaml` y crea:
   - `sistema-gestion-api` (Docker, contexto raíz).
   - `sistema-gestion-web` (Docker, contexto `./frontend`).
3. Completar las variables marcadas con `sync: false` (ver §5). Render las
   pide en el asistente y no las guarda en Git.

### 4.2 Orden de carga recomendado

1. **API**: conexión de Supabase, credenciales de Firebase, Supabase
   Storage y `Database__MigrateOnStartup=true`. Desplegar.
2. Esperar a que `/health` responda 200 y las migraciones terminen.
3. **Frontend**: `NEXT_PUBLIC_API_URL` con la URL real de la API y la
   configuración pública de Firebase. Desplegar.
4. Volver a la **API** y cargar `Cors__AllowedOrigins__0` con la URL real
   del frontend (`https://…onrender.com`, sin barra final). Guardar: Render
   redepliega.

> Las `NEXT_PUBLIC_*` se inlinean en el build. Si cambia
> `NEXT_PUBLIC_API_URL`, hay que reconstruir la imagen: **Manual Deploy →
> Clear build cache & deploy**.

---

## 5. Variables de entorno

### API (`sistema-gestion-api`)

| Clave | Obligatoria | Origen |
| --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | Sí | `Production` |
| `ConnectionStrings__DefaultConnection` | Sí | Supabase, Session pooler |
| `Database__MigrateOnStartup` | Sí | `true` |
| `Firebase__ServiceAccountJson` | Sí* | JSON del service account en una línea |
| `Firebase__ProjectId` | Sí* | Alternativa a la anterior |
| `Firebase__ClientEmail` | Sí* | Alternativa a la anterior |
| `Firebase__PrivateKey` | Sí* | Alternativa a la anterior |
| `Firebase__StorageBucket` | No | Bucket de Firebase |
| `SUPABASE_URL` | Sí | `https://<ref>.supabase.co` |
| `SUPABASE_STORAGE_BUCKET` | Sí | `productos` |
| `SUPABASE_SERVICE_ROLE_KEY` | Sí | Supabase → API |
| `Cors__AllowedOrigins__0` | Sí | URL del frontend |
| `Gemini__ApiKey` | No | Google AI Studio |

\* El backend necesita `ServiceAccountJson` **o** el trío
`ProjectId` + `ClientEmail` + `PrivateKey`. La ausencia de credenciales no
impide arrancar, pero la autenticación responde 401.

### Frontend (`sistema-gestion-web`)

| Clave | Obligatoria |
| --- | --- |
| `NEXT_PUBLIC_API_URL` | Sí (`https://…api.onrender.com`) |
| `NEXT_PUBLIC_FIREBASE_API_KEY` | Sí |
| `NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN` | Sí |
| `NEXT_PUBLIC_FIREBASE_PROJECT_ID` | Sí |
| `NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET` | No |
| `NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID` | No |
| `NEXT_PUBLIC_FIREBASE_APP_ID` | No |
| `NEXT_PUBLIC_WHATSAPP_NUMBER` | No |
| `NEXT_PUBLIC_CURRENCY` | No (default `ARS`) |

Render inyecta `PORT` automáticamente. No hay que definirla.

---

## 6. Primer acceso (SuperAdmin)

Las migraciones crean **roles y permisos**, pero **no usuarios**. El
autorregistro y el ingreso con Google crean usuarios con rol `Cliente`.

Para habilitar el panel administrativo:

1. Abrir el frontend publicado e iniciar sesión una vez con la cuenta que
   será SuperAdmin (así se crea la fila en `Usuarios`).
2. En Supabase → SQL Editor:

```sql
UPDATE "Usuarios"
SET "RolId" = 1
WHERE "Email" = 'tu-correo@ejemplo.com';
```

3. Volver a iniciar sesión. El backend resuelve el rol en cada request, por
   lo que el menú administrativo queda disponible.

Alternativa: conservar los datos locales con `pg_dump`/`pg_restore` (incluye
la fila SuperAdmin con su Firebase UID). No copiar usuarios de prueba a
producción sin revisar sus roles.

---

## 7. Verificación posterior al deploy

```text
GET https://<api>.onrender.com/health
→ 200 {"postgres":"Healthy","status":"Healthy"}
```

- `503` / `Unhealthy`: revisar la cadena de Supabase (sesión pooler, SSL,
  contraseña) en los logs del servicio.
- Preflight CORS:

```http
OPTIONS /api/v1/productos HTTP/1.1
Origin: https://<web>.onrender.com
Access-Control-Request-Method: GET
→ 204 con Access-Control-Allow-Origin: https://<web>.onrender.com
```

- Login en el frontend, apertura del catálogo y una compra de prueba.
- Subida de una imagen para confirmar Supabase Storage.

---

## 8. Limitaciones del plan free

- El servicio **duerme** tras ~15 minutos sin tráfico: el primer request
  tarda 30–60 s (cold start).
- 512 MB de RAM y 0.1 CPU por servicio. Suficiente para una carpintería,
  no para cargas altas.
- Una sola instancia: no hay concurrencia horizontal.
- Supabase free **pausa** el proyecto tras una semana sin actividad. Se
  reactiva desde el dashboard.
- Los servicios free se recrean con cada deploy; no guardar archivos en el
  filesystem del contenedor.

---

## 9. Seguridad

- Cargar todos los secretos con `sync: false`; nunca en `render.yaml` ni en
  el repositorio (AGENTS.md §46/§47).
- `.env`, service accounts y claves privadas están en `.gitignore`.
- La clave de Gemini que estaba en `appsettings.json` **no se commiteó**
  (el archivo estaba sin trackear), pero conviene **rotarla** en Google AI
  Studio por haber estado en texto plano en el árbol de trabajo.
- El endpoint `POST /api/gemini/probar` ahora exige autenticación; aun así,
  no es un endpoint de negocio y conviene retirarlo cuando no se use.
- Revisar los logs de Render: no incluyen tokens ni claves, pero sí errores
  de Firebase/PostgreSQL útiles para diagnóstico.

---

## 10. Troubleshooting

| Síntoma | Causa probable | Solución |
| --- | --- | --- |
| `No open ports detected` | La app no escucha en `$PORT` | El `Dockerfile` ya usa `--urls http://+:${PORT:-8080}`; no fijar `ASPNETCORE_URLS` a mano |
| `/health` 503 | PostgreSQL inaccesible | Revisar Session pooler, `SSL Mode=Require`, contraseña y que el proyecto no esté pausado |
| API 401 en todo | Firebase Admin sin credenciales | Cargar `Firebase__ServiceAccountJson` (una línea) y revisar el log `Firebase no está configurado` |
| `auth/unauthorized-domain` | Dominio no autorizado | Agregar `*.onrender.com` en Firebase → Authorized domains |
| Error CORS en el navegador | Origen distinto o con barra final | `Cors__AllowedOrigins__0` exacto y redeploy de la API |
| El frontend llama a `localhost:7100` | `NEXT_PUBLIC_API_URL` ausente en el build | Cargar la variable y hacer *Clear build cache & deploy* |
| Imágenes 404 | Bucket privado o mal nombrado | Bucket `productos` público y `SUPABASE_STORAGE_BUCKET` correcto |
| Migraciones fallan al arrancar | Cadena con Transaction pooler | Usar Session pooler (5432) |
| El panel no muestra opciones | Usuario con rol `Cliente` | Actualizar `RolId = 1` (§6) |
