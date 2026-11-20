'use client';

/**
 * Presupuestos (api-spec §15, functional-spec §12): listado con filtros de
 * cliente/estado/fechas, creación y edición sólo en borrador (snapshot
 * BR-063) y transiciones enviar/rechazar/anular con permisos granulares
 * (BR-003). La aceptación con pedido automático llega en la fase de
 * aceptación (BR-065).
 */

import { useCallback, useState } from 'react';
import { useAuth } from '@/features/auth/auth-provider';
import { PresupuestoDetailModal } from '@/features/presupuestos/presupuesto-detail-modal';
import { PresupuestoFormModal } from '@/features/presupuestos/presupuesto-form-modal';
import { getQuote, listQuotes } from '@/services/presupuestos';
import { usePagedList, type PagedQuery } from '@/hooks/usePagedList';
import { formatDate, formatMoney } from '@/lib/format';
import {
  ESTADO_PRESUPUESTO_BADGE,
  ESTADO_PRESUPUESTO_LABELS,
  type EstadoPresupuesto,
  type Quote,
  type QuoteListItem,
} from '@/types/api';
import { PanelCard } from '@/components/ui/cards';
import { DataTable } from '@/components/ui/data-table';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { EmptyState } from '@/components/ui/empty-state';
import { LoadingSpinner } from '@/components/ui/loading-spinner';
import { Pagination } from '@/components/ui/pagination';

const ESTADOS: EstadoPresupuesto[] = [0, 1, 2, 3, 4, 5];

export default function PresupuestosPage() {
  const { can } = useAuth();

  const [statusFilter, setStatusFilter] = useState<EstadoPresupuesto | ''>('');

  const fetcher = useCallback(
    (query: PagedQuery, signal?: AbortSignal) =>
      listQuotes(
        {
          page: query.page,
          pageSize: query.pageSize,
          search: query.search,
          status: statusFilter === '' ? undefined : statusFilter,
        },
        signal
      ),
    [statusFilter]
  );

  const list = usePagedList<QuoteListItem>({
    fetcher,
    fetcherKey: String(statusFilter),
  });

  const [detailId, setDetailId] = useState<number | null>(null);
  const [editQuote, setEditQuote] = useState<Quote | null>(null);
  const [editOpen, setEditOpen] = useState(false);
  const [editError, setEditError] = useState<string | null>(null);

  if (!can('PRESUPUESTOS_VIEW')) {
    return (
      <AppAlert variant="warning" title="Sin permiso">
        No tenés permiso para ver los presupuestos (<code>PRESUPUESTOS_VIEW</code>).
      </AppAlert>
    );
  }

  function openCreate() {
    setEditQuote(null);
    setEditError(null);
    setEditOpen(true);
  }

  async function openEdit(row: QuoteListItem) {
    setEditError(null);
    try {
      const quote = await getQuote(row.id);
      setEditQuote(quote);
      setEditOpen(true);
    } catch (err) {
      setEditError(err instanceof Error ? err.message : 'No se pudo cargar el presupuesto.');
    }
  }

  function onSaved() {
    setEditOpen(false);
    list.reload();
  }

  return (
    <>
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-2">
        <div>
          <h1 className="h4 mb-1">Presupuestos</h1>
          <p className="text-muted mb-0 small">
            {list.totalItems} presupuesto{list.totalItems === 1 ? '' : 's'} · los totales los
            calcula el backend (BR-062)
          </p>
        </div>
        {can('PRESUPUESTOS_CREATE') ? (
          <AppButton
            onClick={openCreate}
            icon={<i className="bi bi-file-earmark-plus me-1" aria-hidden="true" />}
          >
            Nuevo presupuesto
          </AppButton>
        ) : null}
      </div>

      {editError ? (
        <AppAlert variant="danger" onClose={() => setEditError(null)}>
          {editError}
        </AppAlert>
      ) : null}

      <PanelCard
        title="Listado"
        actions={
          <div className="d-flex flex-wrap gap-2 align-items-center">
            <div className="input-group input-group-sm" style={{ width: '15rem' }}>
              <span className="input-group-text bg-white">
                <i className="bi bi-search" aria-hidden="true" />
              </span>
              <input
                type="search"
                className="form-control"
                placeholder="Buscar por cliente..."
                value={list.search}
                onChange={(event) => list.setSearch(event.target.value)}
                aria-label="Buscar presupuestos por cliente"
              />
            </div>
            <select
              className="form-select form-select-sm"
              style={{ width: '10rem' }}
              value={statusFilter === '' ? '' : String(statusFilter)}
              aria-label="Filtrar por estado"
              onChange={(event) =>
                setStatusFilter(event.target.value === '' ? '' : (Number(event.target.value) as EstadoPresupuesto))
              }
            >
              <option value="">Todos</option>
              {ESTADOS.map((value) => (
                <option key={value} value={value}>
                  {ESTADO_PRESUPUESTO_LABELS[value]}
                </option>
              ))}
            </select>
          </div>
        }
      >
        {list.loading ? (
          <LoadingSpinner label="Cargando presupuestos..." />
        ) : list.forbidden ? (
          <div className="p-3">
            <AppAlert variant="warning" title="Sin permiso">
              Tu usuario no tiene permiso para consultar presupuestos.
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
            icon="bi-file-earmark-text"
            title="Sin presupuestos"
            description={
              list.search || statusFilter !== ''
                ? 'Ningún presupuesto coincide con los filtros.'
                : 'Creá el primer presupuesto para cotizar un trabajo.'
            }
          />
        ) : (
          <>
            <DataTable
              columns={[
                {
                  header: '#',
                  render: (row: QuoteListItem) => (
                    <span className="font-monospace small">#{row.id}</span>
                  ),
                  className: 'text-center',
                },
                {
                  header: 'Cliente',
                  render: (row: QuoteListItem) => <span className="fw-semibold">{row.customerName}</span>,
                },
                {
                  header: 'Creado',
                  render: (row: QuoteListItem) => formatDate(row.creationDate),
                },
                {
                  header: 'Validez',
                  render: (row: QuoteListItem) =>
                    row.expirationDate ? (
                      <>
                        {formatDate(row.expirationDate)}
                        {row.expired ? (
                          <span className="badge text-bg-warning ms-1">Vencido</span>
                        ) : null}
                      </>
                    ) : (
                      '—'
                    ),
                },
                {
                  header: 'Estado',
                  render: (row: QuoteListItem) => (
                    <span className={`badge ${ESTADO_PRESUPUESTO_BADGE[row.status]}`}>
                      {ESTADO_PRESUPUESTO_LABELS[row.status]}
                    </span>
                  ),
                  className: 'text-center',
                },
                {
                  header: 'Ítems',
                  render: (row: QuoteListItem) => row.itemsCount,
                  className: 'text-center',
                },
                {
                  header: 'Total',
                  render: (row: QuoteListItem) => (
                    <span className="font-monospace">{formatMoney(row.total)}</span>
                  ),
                  className: 'text-end',
                },
              ]}
              rows={list.items}
              rowKey={(row) => row.id}
              actions={(row) => (
                <div className="btn-group btn-group-sm">
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    title="Ver detalle y acciones"
                    onClick={() => setDetailId(row.id)}
                  >
                    <i className="bi bi-eye" aria-hidden="true" />
                  </button>
                  {can('PRESUPUESTOS_UPDATE') && row.status === 0 ? (
                    <button
                      type="button"
                      className="btn btn-outline-primary"
                      title="Editar (sólo en borrador)"
                      onClick={() => openEdit(row)}
                    >
                      <i className="bi bi-pencil" aria-hidden="true" />
                    </button>
                  ) : null}
                </div>
              )}
            />
            <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 border-top p-3">
              <span className="small text-muted">
                Página {list.page} de {Math.max(list.totalPages, 1)} · {list.totalItems} resultados
              </span>
              <Pagination
                page={list.page}
                totalPages={list.totalPages}
                onPageChange={list.setPage}
              />
            </div>
          </>
        )}
      </PanelCard>

      {detailId !== null ? (
        <PresupuestoDetailModal
          open
          quoteId={detailId}
          onClose={() => setDetailId(null)}
          onChanged={() => list.reload()}
        />
      ) : null}

      {editOpen ? (
        <PresupuestoFormModal
          open
          quote={editQuote}
          onClose={() => setEditOpen(false)}
          onSaved={onSaved}
        />
      ) : null}
    </>
  );
}
