'use client';

/**
 * Pedidos del Portal Cliente (functional-spec §23): listado propio paginado
 * con filtro de estado y acceso al detalle con seguimiento (BR-075).
 */

import { useCallback, useState } from 'react';
import Link from 'next/link';
import { listMyOrders } from '@/services/portal';
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

export default function PortalOrdersPage() {
  const [statusFilter, setStatusFilter] = useState<EstadoPedido | ''>('');

  const fetcher = useCallback(
    (query: PagedQuery, signal?: AbortSignal) =>
      listMyOrders(
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

  return (
    <>
      <div className="mb-4">
        <h1 className="h4 mb-1">Mis pedidos</h1>
        <p className="text-muted mb-0 small">
          {list.totalItems} pedido{list.totalItems === 1 ? '' : 's'} · seguí el estado de la
          producción y la entrega
        </p>
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
                placeholder="Buscar..."
                value={list.search}
                onChange={(event) => list.setSearch(event.target.value)}
                aria-label="Buscar pedidos"
              />
            </div>
            <select
              className="form-select form-select-sm"
              style={{ width: '10rem' }}
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
        ) : list.error ? (
          <div className="p-3">
            <AppAlert variant="danger" title="Error al cargar">
              {list.error}
            </AppAlert>
          </div>
        ) : list.items.length === 0 ? (
          <EmptyState
            icon="bi-box-seam"
            title="Sin pedidos"
            description={
              list.search || statusFilter !== ''
                ? 'Ningún pedido coincide con los filtros.'
                : 'Todavía no tenés pedidos. Aceptá un presupuesto para generar el primero.'
            }
            action={
              !list.search && statusFilter === '' ? (
                <Link href="/portal/presupuestos" className="btn btn-primary">
                  Ver mis presupuestos
                </Link>
              ) : null
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
                  header: 'Solicitado',
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
                  header: 'Entrega estimada',
                  render: (row: OrderListItem) =>
                    row.estimatedDeliveryDate ? formatDate(row.estimatedDeliveryDate) : '—',
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
                <Link
                  href={`/portal/pedidos/${row.id}`}
                  className="btn btn-outline-secondary btn-sm"
                  title="Ver detalle"
                >
                  <i className="bi bi-eye" aria-hidden="true" />
                </Link>
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
    </>
  );
}
