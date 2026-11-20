'use client';

/**
 * Gestión de trabajos realizados / vidriera (AGENTS.md §23, api-spec
 * §12C): listado con búsqueda y filtros (publicado, destacado),
 * alta/edición, publicación y borrado con confirmación. Sólo lo
 * publicado aparece en la vidriera pública (BR-190).
 */

import { useCallback, useState } from 'react';
import { useAuth } from '@/features/auth/auth-provider';
import { TrabajoFormModal } from '@/features/trabajos/trabajo-form-modal';
import { TrabajoImagesModal } from '@/features/trabajos/trabajo-images-modal';
import { deleteTrabajo, getTrabajo, listTrabajos, updateTrabajo } from '@/services/trabajos';
import { usePagedList, type PagedQuery } from '@/hooks/usePagedList';
import type { TrabajoList, TrabajoRealizado } from '@/types/api';
import { PanelCard } from '@/components/ui/cards';
import { DataTable } from '@/components/ui/data-table';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { EmptyState } from '@/components/ui/empty-state';
import { LoadingSpinner } from '@/components/ui/loading-spinner';
import { Pagination } from '@/components/ui/pagination';

export default function TrabajosPage() {
  const { can } = useAuth();

  const [publishedOnly, setPublishedOnly] = useState<boolean | undefined>(undefined);
  const [featuredOnly, setFeaturedOnly] = useState<boolean | undefined>(undefined);

  const fetcher = useCallback(
    (query: PagedQuery, signal?: AbortSignal) =>
      listTrabajos(
        {
          page: query.page,
          pageSize: query.pageSize,
          search: query.search,
          published: publishedOnly,
          featured: featuredOnly,
        },
        signal
      ),
    [publishedOnly, featuredOnly]
  );

  const list = usePagedList<TrabajoList>({
    fetcher,
    fetcherKey: `${String(publishedOnly)}|${String(featuredOnly)}`,
  });

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<TrabajoRealizado | null>(null);
  const [imagesTarget, setImagesTarget] = useState<TrabajoList | null>(null);
  const [confirmTarget, setConfirmTarget] = useState<TrabajoList | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  if (!can('TRABAJOS_VIEW')) {
    return (
      <AppAlert variant="warning" title="Sin permiso">
        No tenés permiso para ver trabajos realizados (<code>TRABAJOS_VIEW</code>).
      </AppAlert>
    );
  }

  function openCreate() {
    setEditing(null);
    setFormOpen(true);
  }

  async function openEdit(row: TrabajoList) {
    setActionError(null);
    try {
      const full = await getTrabajo(row.id);
      setEditing(full);
      setFormOpen(true);
    } catch {
      setActionError('No se pudo cargar el trabajo para editar.');
    }
  }

  async function togglePublicado(row: TrabajoList) {
    setActionError(null);
    try {
      await updateTrabajo(row.id, { publicado: !row.publicado });
      list.reload();
    } catch (err) {
      setActionError(
        err instanceof Error ? err.message : 'No se pudo cambiar la publicación.'
      );
    }
  }

  async function confirmDelete() {
    if (!confirmTarget) {
      return;
    }
    setSaving(true);
    setActionError(null);
    try {
      await deleteTrabajo(confirmTarget.id);
      setConfirmTarget(null);
      list.reload();
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'No se pudo eliminar el trabajo.');
      setConfirmTarget(null);
    } finally {
      setSaving(false);
    }
  }

  return (
    <>
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-2">
        <div>
          <h1 className="h4 mb-1">Trabajos realizados</h1>
          <p className="text-muted mb-0 small">
            {list.totalItems} obra{list.totalItems === 1 ? '' : 's'} en la vidriera
          </p>
        </div>
        {can('TRABAJOS_CREATE') ? (
          <AppButton
            onClick={openCreate}
            icon={<i className="bi bi-plus-lg me-1" aria-hidden="true" />}
          >
            Nuevo trabajo
          </AppButton>
        ) : null}
      </div>

      {actionError ? (
        <AppAlert variant="danger" onClose={() => setActionError(null)}>
          {actionError}
        </AppAlert>
      ) : null}

      <PanelCard
        title="Obras"
        actions={
          <div className="d-flex flex-wrap gap-2 align-items-center">
            <div className="input-group input-group-sm" style={{ width: '15rem' }}>
              <span className="input-group-text bg-white">
                <i className="bi bi-search" aria-hidden="true" />
              </span>
              <input
                type="search"
                className="form-control"
                placeholder="Buscar por nombre, cliente..."
                value={list.search}
                onChange={(event) => list.setSearch(event.target.value)}
                aria-label="Buscar trabajos"
              />
            </div>
            <select
              className="form-select form-select-sm"
              style={{ width: '9.5rem' }}
              value={publishedOnly === undefined ? '' : String(publishedOnly)}
              onChange={(event) =>
                setPublishedOnly(
                  event.target.value === '' ? undefined : event.target.value === 'true'
                )
              }
              aria-label="Filtrar por publicación"
            >
              <option value="">Todos</option>
              <option value="true">Publicados</option>
              <option value="false">Borradores</option>
            </select>
            <select
              className="form-select form-select-sm"
              style={{ width: '9.5rem' }}
              value={featuredOnly === undefined ? '' : String(featuredOnly)}
              onChange={(event) =>
                setFeaturedOnly(
                  event.target.value === '' ? undefined : event.target.value === 'true'
                )
              }
              aria-label="Filtrar destacados"
            >
              <option value="">Todos</option>
              <option value="true">Destacados</option>
              <option value="false">No destacados</option>
            </select>
          </div>
        }
      >
        {list.loading ? (
          <LoadingSpinner label="Cargando trabajos..." />
        ) : list.forbidden ? (
          <div className="p-3">
            <AppAlert variant="warning" title="Sin permiso">
              Tu usuario no tiene permiso para consultar trabajos.
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
            icon="bi-hammer"
            title="No hay trabajos"
            description={
              list.search || publishedOnly !== undefined || featuredOnly !== undefined
                ? 'Ninguna obra coincide con los filtros.'
                : 'Registrá la primera obra con el botón "Nuevo trabajo".'
            }
          />
        ) : (
          <>
            <DataTable
              columns={[
                {
                  header: 'Obra',
                  render: (row: TrabajoList) => (
                    <>
                      <div className="fw-semibold">{row.nombre}</div>
                      <div className="small text-muted">
                        {row.tipoMueble ?? 'A medida'}
                        {row.categoria ? ` · ${row.categoria}` : ''}
                      </div>
                    </>
                  ),
                },
                {
                  header: 'Cliente',
                  render: (row: TrabajoList) => (
                    <>
                      {row.clienteNombre ?? (
                        <span className="text-muted small">Sin informar</span>
                      )}
                    </>
                  ),
                },
                {
                  header: 'Fecha',
                  render: (row: TrabajoList) => (
                    <span className="small">{row.fecha ?? '—'}</span>
                  ),
                },
                {
                  header: 'Estado',
                  render: (row: TrabajoList) => (
                    <div className="d-flex flex-wrap gap-1">
                      <span
                        className={`badge ${row.publicado ? 'text-bg-success' : 'text-bg-secondary'}`}
                      >
                        {row.publicado ? 'Publicado' : 'Borrador'}
                      </span>
                      {row.destacado ? (
                        <span className="badge text-bg-warning" title="Destacado">
                          <i className="bi bi-star-fill" aria-hidden="true" />
                        </span>
                      ) : null}
                    </div>
                  ),
                },
              ]}
              rows={list.items}
              rowKey={(row) => row.id}
              actions={(row) => (
                <div className="btn-group btn-group-sm">
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    title="Imágenes"
                    onClick={() => setImagesTarget(row)}
                  >
                    <i className="bi bi-images" aria-hidden="true" />
                  </button>
                  {can('TRABAJOS_UPDATE') ? (
                    <>
                      <button
                        type="button"
                        className={`btn ${row.publicado ? 'btn-outline-success' : 'btn-outline-secondary'}`}
                        title={row.publicado ? 'Ocultar de la vidriera' : 'Publicar en la vidriera'}
                        onClick={() => void togglePublicado(row)}
                      >
                        <i
                          className={`bi ${row.publicado ? 'bi-eye-fill' : 'bi-eye-slash'}`}
                          aria-hidden="true"
                        />
                      </button>
                      <button
                        type="button"
                        className="btn btn-outline-secondary"
                        title="Editar"
                        onClick={() => void openEdit(row)}
                      >
                        <i className="bi bi-pencil" aria-hidden="true" />
                      </button>
                    </>
                  ) : null}
                  {can('TRABAJOS_DELETE') ? (
                    <button
                      type="button"
                      className="btn btn-outline-danger"
                      title="Eliminar"
                      onClick={() => setConfirmTarget(row)}
                    >
                      <i className="bi bi-trash" aria-hidden="true" />
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
        <TrabajoFormModal
          open
          trabajo={editing}
          onClose={() => setFormOpen(false)}
          onSaved={() => {
            setFormOpen(false);
            list.reload();
          }}
        />
      ) : null}

      {imagesTarget ? (
        <TrabajoImagesModal
          open
          trabajo={imagesTarget}
          canUpload={can('TRABAJOS_UPDATE')}
          onClose={() => setImagesTarget(null)}
          onChanged={() => list.reload()}
        />
      ) : null}

      <ConfirmDialog
        open={confirmTarget !== null}
        title="Eliminar trabajo"
        message={
          confirmTarget
            ? `Se eliminará "${confirmTarget.nombre}" junto con sus imágenes de la vidriera. Esta acción no se puede deshacer.`
            : ''
        }
        confirmLabel="Eliminar"
        confirmVariant="danger"
        loading={saving}
        onConfirm={() => void confirmDelete()}
        onCancel={() => setConfirmTarget(null)}
      />
    </>
  );
}
