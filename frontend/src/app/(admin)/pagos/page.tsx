'use client';

/**
 * Pagos (api-spec §17, functional-spec §17): historial global paginado con
 * filtros de pedido/cliente/fechas y búsqueda en referencia/notas
 * (PAGOS_VIEW). El registro de pagos se hace desde el detalle del pedido
 * (PAGOS_CREATE, BR-080/BR-081); el saldo siempre lo calcula el backend
 * (BR-083).
 */

import { useCallback, useState } from 'react';
import { useAuth } from '@/features/auth/auth-provider';
import { OrderDetailModal } from '@/features/pedidos/order-detail-modal';
import { listPayments } from '@/services/pagos';
import { usePagedList, type PagedQuery } from '@/hooks/usePagedList';
import { formatDateTime, formatMoney } from '@/lib/format';
import { METODO_PAGO_LABELS, type PaymentItem } from '@/types/api';
import { PanelCard } from '@/components/ui/cards';
import { DataTable } from '@/components/ui/data-table';
import { AppAlert } from '@/components/ui/app-alert';
import { EmptyState } from '@/components/ui/empty-state';
import { LoadingSpinner } from '@/components/ui/loading-spinner';
import { Pagination } from '@/components/ui/pagination';

export default function PagosPage() {
  const { can } = useAuth();

  const [orderFilter, setOrderFilter] = useState('');
  const [dateFromFilter, setDateFromFilter] = useState('');
  const [dateToFilter, setDateToFilter] = useState('');

  const fetcher = useCallback(
    (query: PagedQuery, signal?: AbortSignal) =>
      listPayments(
        {
          page: query.page,
          pageSize: query.pageSize,
          search: query.search,
          orderId: orderFilter === '' ? undefined : Number(orderFilter),
          dateFrom: dateFromFilter || undefined,
          dateTo: dateToFilter || undefined,
        },
        signal
      ),
    [orderFilter, dateFromFilter, dateToFilter]
  );

  const list = usePagedList<PaymentItem>({
    fetcher,
    fetcherKey: `${orderFilter}|${dateFromFilter}|${dateToFilter}`,
  });

  const [detailId, setDetailId] = useState<number | null>(null);

  if (!can('PAGOS_VIEW')) {
    return (
      <AppAlert variant="warning" title="Sin permiso">
        No tenés permiso para ver los pagos (<code>PAGOS_VIEW</code>).
      </AppAlert>
    );
  }

  const hayFiltros =
    list.search !== '' || orderFilter !== '' || dateFromFilter !== '' || dateToFilter !== '';

  return (
    <>
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-2">
        <div>
          <h1 className="h4 mb-1">Pagos</h1>
          <p className="text-muted mb-0 small">
            {list.totalItems} pago{list.totalItems === 1 ? '' : 's'} · el saldo por pedido lo
            calcula el backend (BR-083)
          </p>
        </div>
      </div>

      <PanelCard
        title="Historial"
        actions={
          <div className="d-flex flex-wrap gap-2 align-items-center">
            <div className="input-group input-group-sm" style={{ width: '14rem' }}>
              <span className="input-group-text bg-white">
                <i className="bi bi-search" aria-hidden="true" />
              </span>
              <input
                type="search"
                className="form-control"
                placeholder="Referencia o notas..."
                value={list.search}
                onChange={(event) => list.setSearch(event.target.value)}
                aria-label="Buscar pagos por referencia o notas"
              />
            </div>
            <input
              type="number"
              className="form-control form-control-sm"
              style={{ width: '7rem' }}
              placeholder="Pedido"
              min={1}
              value={orderFilter}
              onChange={(event) => setOrderFilter(event.target.value)}
              aria-label="Filtrar por número de pedido"
            />
            <input
              type="date"
              className="form-control form-control-sm"
              style={{ width: '9.5rem' }}
              value={dateFromFilter}
              onChange={(event) => setDateFromFilter(event.target.value)}
              aria-label="Filtrar pagos desde fecha"
            />
            <input
              type="date"
              className="form-control form-control-sm"
              style={{ width: '9.5rem' }}
              value={dateToFilter}
              onChange={(event) => setDateToFilter(event.target.value)}
              aria-label="Filtrar pagos hasta fecha"
            />
          </div>
        }
      >
        {list.loading ? (
          <LoadingSpinner label="Cargando pagos..." />
        ) : list.forbidden ? (
          <div className="p-3">
            <AppAlert variant="warning" title="Sin permiso">
              Tu usuario no tiene permiso para consultar pagos.
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
            icon="bi-cash-coin"
            title="Sin pagos"
            description={
              hayFiltros
                ? 'Ningún pago coincide con los filtros.'
                : 'Los pagos se registran desde el detalle de cada pedido (PAGOS_CREATE).'
            }
          />
        ) : (
          <>
            <DataTable
              columns={[
                {
                  header: 'Fecha',
                  render: (row: PaymentItem) => formatDateTime(row.date),
                },
                {
                  header: 'Pedido',
                  render: (row: PaymentItem) => (
                    <span className="font-monospace small">#{row.orderId}</span>
                  ),
                  className: 'text-center',
                },
                {
                  header: 'Cliente',
                  render: (row: PaymentItem) => row.customerName ?? '—',
                },
                {
                  header: 'Método',
                  render: (row: PaymentItem) => (
                    <span className="badge text-bg-light border">
                      {METODO_PAGO_LABELS[row.paymentMethod]}
                    </span>
                  ),
                  className: 'text-center',
                },
                {
                  header: 'Referencia',
                  render: (row: PaymentItem) => row.reference ?? '—',
                },
                {
                  header: 'Registrado por',
                  render: (row: PaymentItem) => (
                    <span className="small text-muted">{row.userName ?? '—'}</span>
                  ),
                },
                {
                  header: 'Monto',
                  render: (row: PaymentItem) => (
                    <span className="font-monospace">{formatMoney(row.amount)}</span>
                  ),
                  className: 'text-end',
                },
              ]}
              rows={list.items}
              rowKey={(row) => row.id}
              actions={
                can('PEDIDOS_VIEW')
                  ? (row) => (
                      <button
                        type="button"
                        className="btn btn-outline-secondary btn-sm"
                        title="Ver el pedido y su saldo"
                        onClick={() => setDetailId(row.orderId)}
                      >
                        <i className="bi bi-eye" aria-hidden="true" />
                      </button>
                    )
                  : undefined
              }
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
