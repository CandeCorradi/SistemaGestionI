'use client';

/**
 * Gestión de proveedores (functional-spec §7, api-spec §11): listado,
 * alta/edición y desactivación lógica (BR-021: nunca borrado físico).
 */

import { useCallback, useState } from 'react';
import { useAuth } from '@/features/auth/auth-provider';
import { ProveedorFormModal } from '@/features/proveedores/proveedor-form-modal';
import { deactivateProveedor, getProveedor, listProveedores } from '@/services/proveedores';
import { usePagedList, type PagedQuery } from '@/hooks/usePagedList';
import type { Proveedor, ProveedorList } from '@/types/api';
import { PanelCard } from '@/components/ui/cards';
import { DataTable } from '@/components/ui/data-table';
import { StatusBadge } from '@/components/ui/status-badge';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { EmptyState } from '@/components/ui/empty-state';
import { LoadingSpinner } from '@/components/ui/loading-spinner';
import { Pagination } from '@/components/ui/pagination';

export default function ProveedoresPage() {
  const { can } = useAuth();

  const fetcher = useCallback(
    (query: PagedQuery, signal?: AbortSignal) =>
      listProveedores(
        { page: query.page, pageSize: query.pageSize, search: query.search, active: query.active },
        signal
      ),
    []
  );

  const list = usePagedList<ProveedorList>({ fetcher });

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<Proveedor | null>(null);
  const [confirmTarget, setConfirmTarget] = useState<ProveedorList | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  if (!can('PROVEEDORES_VIEW')) {
    return (
      <AppAlert variant="warning" title="Sin permiso">
        No tenés permiso para ver proveedores (<code>PROVEEDORES_VIEW</code>).
      </AppAlert>
    );
  }

  function openCreate() {
    setEditing(null);
    setFormOpen(true);
  }

  async function openEdit(row: ProveedorList) {
    setActionError(null);
    try {
      const full = await getProveedor(row.id);
      setEditing(full);
      setFormOpen(true);
    } catch {
      setActionError('No se pudo cargar el proveedor para editar.');
    }
  }

  async function confirmDeactivate() {
    if (!confirmTarget) {
      return;
    }
    setSaving(true);
    setActionError(null);
    try {
      await deactivateProveedor(confirmTarget.id);
      setConfirmTarget(null);
      list.reload();
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'No se pudo desactivar el proveedor.');
      setConfirmTarget(null);
    } finally {
      setSaving(false);
    }
  }

  return (
    <>
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-2">
        <div>
          <h1 className="h4 mb-1">Proveedores</h1>
          <p className="text-muted mb-0 small">
            {list.totalItems} proveedor{list.totalItems === 1 ? '' : 'es'} registrado
            {list.totalItems === 1 ? '' : 's'}
          </p>
        </div>
        {can('PROVEEDORES_CREATE') ? (
          <AppButton
            onClick={openCreate}
            icon={<i className="bi bi-plus-lg me-1" aria-hidden="true" />}
          >
            Nuevo proveedor
          </AppButton>
        ) : null}
      </div>

      {actionError ? (
        <AppAlert variant="danger" onClose={() => setActionError(null)}>
          {actionError}
        </AppAlert>
      ) : null}

      <PanelCard
        title="Listado"
        actions={
          <div className="d-flex flex-wrap gap-2 align-items-center">
            <div className="input-group input-group-sm" style={{ width: '16rem' }}>
              <span className="input-group-text bg-white">
                <i className="bi bi-search" aria-hidden="true" />
              </span>
              <input
                type="search"
                className="form-control"
                placeholder="Buscar por nombre, contacto, email..."
                value={list.search}
                onChange={(event) => list.setSearch(event.target.value)}
                aria-label="Buscar proveedores"
              />
            </div>
            <select
              className="form-select form-select-sm"
              style={{ width: '9rem' }}
              value={list.active === undefined ? '' : String(list.active)}
              onChange={(event) =>
                list.setActive(event.target.value === '' ? undefined : event.target.value === 'true')
              }
              aria-label="Filtrar por estado"
            >
              <option value="">Todos</option>
              <option value="true">Activos</option>
              <option value="false">Inactivos</option>
            </select>
          </div>
        }
      >
        {list.loading ? (
          <LoadingSpinner label="Cargando proveedores..." />
        ) : list.forbidden ? (
          <div className="p-3">
            <AppAlert variant="warning" title="Sin permiso">
              Tu usuario no tiene permiso para consultar proveedores.
            </AppAlert>
          </div>
        ) : list.error ? (
          <div className="p-3">
            <AppAlert variant="danger" title="Error al cargar">
              {list.error}
            </AppAlert>
          </div>
        ) : list.items.length === 0 ? (
          <EmptyState
            icon="bi-truck"
            title="No hay proveedores"
            description={
              list.search
                ? 'Ningún proveedor coincide con la búsqueda.'
                : 'Registrá el primer proveedor con el botón "Nuevo proveedor".'
            }
          />
        ) : (
          <>
            <DataTable
              columns={[
                {
                  header: 'Proveedor',
                  render: (row: ProveedorList) => (
                    <>
                      <div className="fw-semibold">{row.nombre}</div>
                      <div className="small text-muted">{row.contacto ?? 'Sin contacto'}</div>
                    </>
                  ),
                },
                {
                  header: 'Email',
                  render: (row: ProveedorList) => row.email ?? '—',
                },
                { header: 'Teléfono', render: (row: ProveedorList) => row.telefono ?? '—' },
                {
                  header: 'Estado',
                  render: (row: ProveedorList) => <StatusBadge active={row.activo} />,
                },
              ]}
              rows={list.items}
              rowKey={(row) => row.id}
              actions={(row) => (
                <div className="btn-group btn-group-sm">
                  {can('PROVEEDORES_UPDATE') ? (
                    <button
                      type="button"
                      className="btn btn-outline-secondary"
                      title="Editar"
                      onClick={() => void openEdit(row)}
                    >
                      <i className="bi bi-pencil" aria-hidden="true" />
                    </button>
                  ) : null}
                  {can('PROVEEDORES_DELETE') && row.activo ? (
                    <button
                      type="button"
                      className="btn btn-outline-danger"
                      title="Desactivar"
                      onClick={() => setConfirmTarget(row)}
                    >
                      <i className="bi bi-x-octagon" aria-hidden="true" />
                    </button>
                  ) : null}
                </div>
              )}
            />
            <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 border-top p-3">
              <span className="small text-muted">
                Página {list.page} de {Math.max(list.totalPages, 1)} · {list.totalItems} resultados
              </span>
              <Pagination page={list.page} totalPages={list.totalPages} onPageChange={list.setPage} />
            </div>
          </>
        )}
      </PanelCard>

      {formOpen ? (
        <ProveedorFormModal
          open
          proveedor={editing}
          onClose={() => setFormOpen(false)}
          onSaved={() => {
            setFormOpen(false);
            list.reload();
          }}
        />
      ) : null}

      <ConfirmDialog
        open={confirmTarget !== null}
        title="Desactivar proveedor"
        message={
          confirmTarget
            ? `Se desactivará "${confirmTarget.nombre}". El historial de productos y pedidos se conserva (BR-021).`
            : ''
        }
        confirmLabel="Desactivar"
        confirmVariant="danger"
        loading={saving}
        onConfirm={() => void confirmDeactivate()}
        onCancel={() => setConfirmTarget(null)}
      />
    </>
  );
}
