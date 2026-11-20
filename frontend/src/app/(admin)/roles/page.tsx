'use client';

/**
 * Roles del sistema (api-spec §9, BR-001): los cinco roles son fijos y
 * no se crean ni editan. Esta vista es de consulta; la asignación de un
 * rol a un usuario se hace desde /usuarios y los permisos explícitos
 * adicionales en el modal de permisos de cada usuario.
 */

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useAuth } from '@/features/auth/auth-provider';
import { listRoles } from '@/services/roles';
import type { Rol } from '@/types/api';
import { PanelCard } from '@/components/ui/cards';
import { DataTable } from '@/components/ui/data-table';
import { AppAlert } from '@/components/ui/app-alert';
import { LoadingSpinner } from '@/components/ui/loading-spinner';

const ROL_DESCRIPTIONS: Record<string, string> = {
  SuperAdmin:
    'Acceso total: usuarios, roles, permisos y todas las operaciones del sistema.',
  Owner:
    'Propietario de la carpintería: acceso total menos la administración de SuperAdmin.',
  Admin: 'Administración operativa completa: clientes, pedidos, stock y reportes.',
  Empleado: 'Operación diaria: clientes, productos, pedidos y stock (sin pagos ni usuarios).',
  Cliente: 'Portal del cliente: catálogo, presupuestos propios, pedidos y pagos propios.',
};

export default function RolesPage() {
  const { can } = useAuth();
  const [roles, setRoles] = useState<Rol[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    const controller = new AbortController();
    listRoles(controller.signal)
      .then((data) => {
        if (!cancelled) {
          setRoles(data);
        }
      })
      .catch((err: unknown) => {
        if (!cancelled && (err as { name?: string }).name !== 'AbortError') {
          setError(err instanceof Error ? err.message : 'No se pudieron cargar los roles.');
        }
      });
    return () => {
      cancelled = true;
      controller.abort();
    };
  }, []);

  if (!can('ROLES_VIEW')) {
    return (
      <AppAlert variant="warning" title="Sin permiso">
        Tu perfil no tiene el permiso ROLES_VIEW.
      </AppAlert>
    );
  }

  return (
    <>
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-2">
        <div>
          <h1 className="h4 mb-1">Roles</h1>
          <p className="text-muted mb-0 small">
            Los cinco roles del sistema son fijos (BR-001). Cada usuario tiene exactamente un
            rol; los permisos adicionales se asignan por usuario.
          </p>
        </div>
        {can('USUARIOS_VIEW') ? (
          <Link href="/usuarios" className="btn btn-outline-secondary">
            <i className="bi bi-people me-1" aria-hidden="true" />
            Ir a usuarios
          </Link>
        ) : null}
      </div>

      <PanelCard title="Roles del sistema">
        {error ? (
          <AppAlert variant="danger" title="Error al cargar">
            {error}
          </AppAlert>
        ) : roles === null ? (
          <LoadingSpinner label="Cargando roles..." />
        ) : (
          <DataTable
            columns={[
              { header: 'Código', render: (rol) => rol.codigo },
              { header: 'Nombre', render: (rol) => rol.nombre },
              {
                header: 'Descripción',
                render: (rol) => (
                  <span className="small text-muted">
                    {ROL_DESCRIPTIONS[rol.nombre] ?? 'Sin descripción.'}
                  </span>
                ),
              },
            ]}
            rows={roles}
            rowKey={(rol) => rol.id}
          />
        )}
      </PanelCard>
    </>
  );
}
