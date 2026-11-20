'use client';

/**
 * Gestión de clientes (functional-spec §6, api-spec §10): listado con
 * paginación/búsqueda/filtro de activos + alta/edición en modal +
 * desactivación lógica (BR-011). Oculta la UI sin permiso (BR-003).
 */

import { useCallback, useState } from 'react';
import { useAuth } from '@/features/auth/auth-provider';
import { ClienteFormModal } from '@/features/clientes/cliente-form-modal';
import { deactivateCliente, getCliente, listClientes } from '@/services/clientes';
import { usePagedList, type PagedQuery } from '@/hooks/usePagedList';
import { fullName } from '@/lib/format';
import type { Cliente, ClienteList } from '@/types/api';
import { PanelCard } from '@/components/ui/cards';
import { DataTable } from '@/components/ui/data-table';
import { StatusBadge } from '@/components/ui/status-badge';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { EmptyState } from '@/components/ui/empty-state';
import { LoadingSpinner } from '@/components/ui/loading-spinner';
import { Pagination } from '@/components/ui/pagination';

export default function ClientesPage() {
  const { can } = useAuth();

  const fetcher = useCallback(
    (query: PagedQuery, signal?: AbortSignal) =>
      listClientes(
        { page: query.page, pageSize: query.pageSize, search: query.search, active: query.active },
        signal
      ),
    []
  );

  const list = usePagedList<ClienteList>({ fetcher });

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<Cliente | null>(null);
  const [confirmTarget, setConfirmTarget] = useState<ClienteList | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  if (!can('CLIENTES_VIEW')) {
    return (
      <AppAlert variant="warning" title="Sin permiso">
        No tenés permiso para ver clientes (<code>CLIENTES_VIEW</code>).
      </AppAlert>
    );
  }

  function openCreate() {
    setEditing(null);
    setFormOpen(true);
  }

  async function openEdit(row: ClienteList) {
    setActionError(null);
    try {
      const full = await getCliente(row.id);
      setEditing(full);
      setFormOpen(true);
    } catch {
      setActionError('No se pudo cargar el cliente para editar.');
    }
  }

  async function confirmDeactivate() {
    if (!confirmTarget) {
      return;
    }
    setSaving(true);
    setActionError(null);
    try {
      await deactivateCliente(confirmTarget.id);
      setConfirmTarget(null);
      list.reload();
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'No se pudo desactivar el cliente.');
      setConfirmTarget(null);
    } finally {
      setSaving(false);
    }
  }

  return (
    <>
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-2">
        <div>
          <h1 className="h4 mb-1">Clientes</h1>
          <p className="text-muted mb-0 small">
            {list.totalItems} cliente{list.totalItems === 1 ? '' : 's'} registrado{list.totalItems === 1 ? '' : 's'}
          </p>
        </div>
        {can('CLIENTES_CREATE') ? (
          <AppButton
            onClick={openCreate}
            icon={<i className="bi bi-plus-lg me-1" aria-hidden="true" />}
          >
            Nuevo cliente
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
                placeholder="Buscar por nombre, email, doc..."
                value={list.search}
                onChange={(event) => list.setSearch(event.target.value)}
                aria-label="Buscar clientes"
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
          <LoadingSpinner label="Cargando clientes..." />
        ) : list.forbidden ? (
          <div className="p-3">
            <AppAlert variant="warning" title="Sin permiso">
              Tu usuario no tiene permiso para consultar clientes.
            </AppAlert>
          </div>
        ) : list.error ? (
          <div className="p-3">
            <AppAlert variant="danger" title="Error al cargar" onClose={() => list.setSearch('')}>
              {list.error}
            </AppAlert>
          </div>
        ) : list.items.length === 0 ? (
          <EmptyState
            icon="bi-people"
            title="No hay clientes"
            description={
              list.search
                ? 'Ningún cliente coincide con la búsqueda.'
                : 'Registrá el primer cliente con el botón "Nuevo cliente".'
            }
          />
        ) : (
          <>
            <DataTable
              columns={[
                {
                  header: 'Cliente',
                  render: (row: ClienteList) => (
                    <>
                      <div className="fw-semibold">{fullName(row.nombre, row.apellido)}</div>
                      <div className="small text-muted">{row.email}</div>
                    </>
                  ),
                },
                { header: 'Teléfono', render: (row: ClienteList) => row.telefono ?? '—' },
                { header: 'Documento', render: (row: ClienteList) => row.documento ?? '—' },
                {
                  header: 'Estado',
                  render: (row: ClienteList) => <StatusBadge active={row.activo} />,
                },
              ]}
              rows={list.items}
              rowKey={(row) => row.id}
              actions={(row) => (
                <div className="btn-group btn-group-sm">
                  {can('CLIENTES_UPDATE') ? (
                    <button
                      type="button"
                      className="btn btn-outline-secondary"
                      title="Editar"
                      onClick={() => void openEdit(row)}
                    >
                      <i className="bi bi-pencil" aria-hidden="true" />
                    </button>
                  ) : null}
                  {can('CLIENTES_DELETE') && row.activo ? (
                    <button
                      type="button"
                      className="btn btn-outline-danger"
                      title="Desactivar"
                      onClick={() => setConfirmTarget(row)}
                    >
                      <i className="bi bi-person-dash" aria-hidden="true" />
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
        <ClienteFormModal
          open
          cliente={editing}
          onClose={() => setFormOpen(false)}
          onSaved={() => {
            setFormOpen(false);
            list.reload();
          }}
        />
      ) : null}

      <ConfirmDialog
        open={confirmTarget !== null}
        title="Desactivar cliente"
        message={
          confirmTarget
            ? `Se desactivará a ${fullName(confirmTarget.nombre, confirmTarget.apellido)}. El historial se conserva (no se borra).`
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
