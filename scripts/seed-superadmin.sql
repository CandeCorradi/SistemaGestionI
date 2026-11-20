-- =============================================================================
-- Vinculación del primer usuario interno (SuperAdmin) a un Firebase UID
-- =============================================================================
-- El sistema deriva la identidad del Firebase ID Token (AGENTS.md §3.5) y
-- los permisos del usuario interno vinculado. Sin este paso, el login en la
-- web responde "Usuario sin acceso" porque el UID no existe en Usuarios.
--
-- Uso:
--   1. Crear el usuario de correo en Firebase Authentication (consola).
--   2. Copiar su UID (Authentication > Users).
--   3. Reemplazar los valores <...> de abajo.
--   4. Ejecutar contra la base del sistema:
--        psql "$DATABASE_CONNECTION_STRING" -f scripts/seed-superadmin.sql
--
-- RolId: 1 = SuperAdmin (BR-001, seed de la migración AddRoles).
-- =============================================================================

BEGIN;

INSERT INTO "Usuarios" (
    "FirebaseUid",
    "Email",
    "Nombre",
    "Apellido",
    "RolId",
    "Activo",
    "FechaCreacion"
)
VALUES (
    '<FIREBASE_UID>',          -- UID real de Firebase Authentication
    'superadmin@example.com',  -- Email del usuario (único)
    'Super',
    'Admin',
    1,                         -- Rol SuperAdmin: los 37 permisos
    TRUE,
    now()
)
ON CONFLICT ("FirebaseUid") DO UPDATE
SET
    "Email" = EXCLUDED."Email",
    "RolId" = EXCLUDED."RolId",
    "Activo" = TRUE;

COMMIT;
