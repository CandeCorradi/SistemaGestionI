'use client';

/**
 * Pedidos (api-spec §16, functional-spec §15): listado con filtros de
 * cliente/estado/fechas y detalle con el ciclo de vida validado en backend
 * (BR-072/BR-073) e historial inmutable (BR-075). Los pedidos se crean
 * aceptando un presupuesto (BR-065/BR-070), no desde acá.
 */

import { useCallback, useState } from 'react';
import { useAuth } from '@/features/auth/auth-provider';
import { OrderDetailModal } from '@/features/pedidos/order-detail-modal';
import { listOrders } from '@/services/pedidos';
import { usePagedList, type PagedQuery } from '@/hooks/usePagedList';
import { formatDate, formatMoney } from '@/lib/format';
import {
  ESTADO_PEDIDO_BADGE,
  ESTADO_PEDIDO_LABELS,
  type EstadoPedido,
  type OrderListItem,
} from '@/types/api';
import { PanelCard } from '@/components/ui/cards';
import { DataTable } from '@/components/ui/data-table';
import { AppAlert } from '@/components/ui/app-alert';
import { EmptyState } from '@/components/ui/empty-state';
import { LoadingSpinner } from '@/components/ui/loading-spinner';
import { Pagination } from '@/components/ui/pagination';

const ESTADOS: EstadoPedido[] = [0, 1, 2, 3, 4, 5, 6];

export default function PedidosPage() {
  const { can } = useAuth();

  const [statusFilter, setStatusFilter] = useState<EstadoPedido | ''>('');

  const fetcher = useCallback(
    (query: PagedQuery, signal?: AbortSignal) =>
      listOrders(
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

  const list = usePagedList<OrderListItem>({
    fetcher,
    fetcherKey: String(statusFilter),
  });

  const [detailId, setDetailId] = useState<number | null>(null);

  if (!can('PEDIDOS_VIEW')) {
    return (
      <AppAlert variant="warning" title="Sin permiso">
        No tenés permiso para ver los pedidos (<code>PEDIDOS_VIEW</code>).
      </AppAlert>
    );
  }

  return (
    <>
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-2">
        <div>
          <h1 className="h4 mb-1">Pedidos</h1>
          <p className="text-muted mb-0 small">
            {list.totalItems} pedido{list.totalItems === 1 ? '' : 's'} · se crean al aceptar un
            presupuesto (BR-070)
          </p>
        </div>
      </div>

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
                aria-label="Buscar pedidos por cliente"
              />
            </div>
            <select
              className="form-select form-select-sm"
              style={{ width: '11rem' }}
              value={statusFilter === '' ? '' : String(statusFilter)}
              aria-label="Filtrar por estado"
              onChange={(event) =>
                setStatusFilter(
                  event.target.value === '' ? '' : (Number(event.target.value) as EstadoPedido)
                )
              }
            >
              <option value="">Todos</option>
              {ESTADOS.map((value) => (
                <option key={value} value={value}>
                  {ESTADO_PEDIDO_LABELS[value]}
                </option>
              ))}
            </select>
          </div>
        }
      >
        {list.loading ? (
          <LoadingSpinner label="Cargando pedidos..." />
        ) : list.forbidden ? (
          <div className="p-3">
            <AppAlert variant="warning" title="Sin permiso">
              Tu usuario no tiene permiso para consultar pedidos.
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
            icon="bi-cart-check"
            title="Sin pedidos"
            description={
              list.search || statusFilter !== ''
                ? 'Ningún pedido coincide con los filtros.'
                : 'Los pedidos nacen al aceptar un presupuesto (BR-070).'
            }
          />
        ) : (
          <>
            <DataTable
              columns={[
                {
                  header: '#',
                  render: (row: OrderListItem) => (
                    <span className="font-monospace small">#{row.id}</span>
                  ),
                  className: 'text-center',
                },
                {
                  header: 'Cliente',
                  render: (row: OrderListItem) => (
                    <span className="fw-semibold">{row.customerName}</span>
                  ),
                },
                {
                  header: 'Presupuesto',
                  render: (row: OrderListItem) =>
                    row.quoteId !== null ? (
                      <span className="font-monospace small">#{row.quoteId}</span>
                    ) : (
                      '—'
                    ),
                  className: 'text-center',
                },
                {
                  header: 'Creado',
                  render: (row: OrderListItem) => formatDate(row.creationDate),
                },
                {
                  header: 'Estado',
                  render: (row: OrderListItem) => (
                    <span className={`badge ${ESTADO_PEDIDO_BADGE[row.status]}`}>
                      {ESTADO_PEDIDO_LABELS[row.status]}
                    </span>
                  ),
                  className: 'text-center',
                },
                {
                  header: 'Total',
                  render: (row: OrderListItem) => (
                    <span className="font-monospace">{formatMoney(row.total)}</span>
                  ),
                  className: 'text-end',
                },
              ]}
              rows={list.items}
              rowKey={(row) => row.id}
              actions={(row) => (
                <button
                  type="button"
                  className="btn btn-outline-secondary btn-sm"
                  title="Ver detalle, avanzar estado e historial"
                  onClick={() => setDetailId(row.id)}
                >
                  <i className="bi bi-eye" aria-hidden="true" />
                </button>
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
        <OrderDetailModal
          open
          orderId={detailId}
          onClose={() => setDetailId(null)}
          onChanged={() => list.reload()}
        />
      ) : null}
    </>
  );
}
