'use client';

/**
 * Inventario (api-spec §14, functional-spec §11): stock actual de los
 * productos activos con filtro de stock bajo, historial de movimientos
 * (BR-051) y registro de movimientos con permisos STOCK_ENTRY/STOCK_ADJUST
 * (BR-003). La autorización real es del backend (BR-003).
 */

import { useCallback, useState } from 'react';
import { useAuth } from '@/features/auth/auth-provider';
import { MovimientoFormModal } from '@/features/stock/movimiento-form-modal';
import { listMovements, listStock } from '@/services/inventario';
import { usePagedList, type PagedQuery } from '@/hooks/usePagedList';
import { formatDateTime } from '@/lib/format';
import {
  TIPO_MOVIMIENTO_EFECTO,
  TIPO_MOVIMIENTO_STOCK_LABELS,
  TIPO_PRODUCTO_LABELS,
  type MovimientoStock,
  type StockList,
} from '@/types/api';
import { PanelCard } from '@/components/ui/cards';
import { DataTable } from '@/components/ui/data-table';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { EmptyState } from '@/components/ui/empty-state';
import { LoadingSpinner } from '@/components/ui/loading-spinner';
import { Pagination } from '@/components/ui/pagination';

/** Signo del efecto de un movimiento sobre el stock (+1 / −1). */
function efectoDe(movimiento: MovimientoStock): number {
  const efecto = TIPO_MOVIMIENTO_EFECTO[movimiento.tipo];
  return efecto === 0 ? Math.sign(movimiento.cantidad) || 1 : efecto;
}

export default function StockPage() {
  const { can } = useAuth();

  const puedeMovimiento = can('STOCK_ENTRY') || can('STOCK_ADJUST');

  const [tab, setTab] = useState<'stock' | 'history'>('stock');
  const [lowStockOnly, setLowStockOnly] = useState(false);
  const [historyFilter, setHistoryFilter] = useState<{ id: number; nombre: string } | null>(null);

  const stockFetcher = useCallback(
    (query: PagedQuery, signal?: AbortSignal) =>
      listStock(
        {
          page: query.page,
          pageSize: query.pageSize,
          search: query.search,
          lowStock: lowStockOnly ? true : undefined,
        },
        signal
      ),
    [lowStockOnly]
  );

  const stockList = usePagedList<StockList>({
    fetcher: stockFetcher,
    fetcherKey: String(lowStockOnly),
  });

  const historyFetcher = useCallback(
    (query: PagedQuery, signal?: AbortSignal) =>
      listMovements(
        {
          page: query.page,
          pageSize: query.pageSize,
          productId: historyFilter?.id,
        },
        signal
      ),
    [historyFilter]
  );

  const historyList = usePagedList<MovimientoStock>({
    fetcher: historyFetcher,
    fetcherKey: String(historyFilter?.id ?? 'all'),
  });

  const [modalOpen, setModalOpen] = useState(false);
  const [movementProduct, setMovementProduct] = useState<StockList | null>(null);

  if (!can('STOCK_VIEW')) {
    return (
      <AppAlert variant="warning" title="Sin permiso">
        No tenés permiso para ver el stock (<code>STOCK_VIEW</code>).
      </AppAlert>
    );
  }

  function openMovement(producto: StockList | null) {
    setMovementProduct(producto);
    setModalOpen(true);
  }

  function openHistory(row: StockList) {
    stockList.setPage(1);
    historyList.setPage(1);
    setHistoryFilter({ id: row.id, nombre: row.nombre });
    setTab('history');
  }

  function clearHistoryFilter() {
    historyList.setPage(1);
    setHistoryFilter(null);
  }

  function onSaved() {
    setModalOpen(false);
    stockList.reload();
    historyList.reload();
  }

  return (
    <>
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-2">
        <div>
          <h1 className="h4 mb-1">Stock / Inventario</h1>
          <p className="text-muted mb-0 small">
            {tab === 'stock'
              ? `${stockList.totalItems} producto${stockList.totalItems === 1 ? '' : 's'} con stock registrado`
              : `${historyList.totalItems} movimiento${historyList.totalItems === 1 ? '' : 's'} en el historial`}
          </p>
        </div>
        {puedeMovimiento ? (
          <AppButton
            onClick={() => openMovement(null)}
            icon={<i className="bi bi-arrow-left-right me-1" aria-hidden="true" />}
          >
            Registrar movimiento
          </AppButton>
        ) : null}
      </div>

      <div className="btn-group mb-3" role="tablist" aria-label="Vistas de inventario">
        <button
          type="button"
          role="tab"
          aria-selected={tab === 'stock'}
          className={`btn btn-sm ${tab === 'stock' ? 'btn-dark' : 'btn-outline-dark'}`}
          onClick={() => setTab('stock')}
        >
          <i className="bi bi-box-seam me-1" aria-hidden="true" />
          Stock actual
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={tab === 'history'}
          className={`btn btn-sm ${tab === 'history' ? 'btn-dark' : 'btn-outline-dark'}`}
          onClick={() => setTab('history')}
        >
          <i className="bi bi-clock-history me-1" aria-hidden="true" />
          Historial
        </button>
      </div>

      {tab === 'stock' ? (
        <PanelCard
          title="Stock actual"
          actions={
            <div className="d-flex flex-wrap gap-2 align-items-center">
              <div className="input-group input-group-sm" style={{ width: '15rem' }}>
                <span className="input-group-text bg-white">
                  <i className="bi bi-search" aria-hidden="true" />
                </span>
                <input
                  type="search"
                  className="form-control"
                  placeholder="Buscar por nombre, SKU..."
                  value={stockList.search}
                  onChange={(event) => stockList.setSearch(event.target.value)}
                  aria-label="Buscar productos en stock"
                />
              </div>
              <div className="form-check form-switch small">
                <input
                  id="filter-low-stock"
                  className="form-check-input"
                  type="checkbox"
                  checked={lowStockOnly}
                  onChange={(event) => setLowStockOnly(event.target.checked)}
                />
                <label className="form-check-label" htmlFor="filter-low-stock">
                  Stock bajo
                </label>
              </div>
            </div>
          }
        >
          {stockList.loading ? (
            <LoadingSpinner label="Cargando stock..." />
          ) : stockList.forbidden ? (
            <div className="p-3">
              <AppAlert variant="warning" title="Sin permiso">
                Tu usuario no tiene permiso para consultar el stock.
              </AppAlert>
            </div>
          ) : stockList.error ? (
            <div className="p-3">
              <AppAlert variant="danger" title="Error al cargar">
                {stockList.error}
              </AppAlert>
            </div>
          ) : stockList.items.length === 0 ? (
            <EmptyState
              icon="bi-boxes"
              title="Sin stock para mostrar"
              description={
                stockList.search || lowStockOnly
                  ? 'Ningún producto coincide con los filtros.'
                  : 'Los productos activos aparecerán aquí con su stock.'
              }
            />
          ) : (
            <>
              <DataTable
                columns={[
                  {
                    header: 'Producto',
                    render: (row: StockList) => (
                      <>
                        <div className="fw-semibold">{row.nombre}</div>
                        <div className="small text-muted font-monospace">{row.codigoSku}</div>
                      </>
                    ),
                  },
                  {
                    header: 'Tipo',
                    render: (row: StockList) => (
                      <>
                        {TIPO_PRODUCTO_LABELS[row.tipo]}
                        {row.esPersonalizado ? (
                          <span className="badge text-bg-info ms-1" title="Personalizado / a medida">
                            A medida
                          </span>
                        ) : null}
                      </>
                    ),
                  },
                  {
                    header: 'Stock',
                    render: (row: StockList) => (
                      <span
                        className={`badge ${row.abajoDeMinimo ? 'text-bg-danger' : 'text-bg-light border'}`}
                      >
                        {row.stockActual}
                        {row.abajoDeMinimo ? (
                          <i
                            className="bi bi-exclamation-triangle-fill ms-1"
                            aria-hidden="true"
                            title="Stock bajo"
                          />
                        ) : null}
                      </span>
                    ),
                    className: 'text-center',
                  },
                  {
                    header: 'Mínimo',
                    render: (row: StockList) =>
                      row.stockMinimo > 0 ? row.stockMinimo : '—',
                    className: 'text-center',
                  },
                ]}
                rows={stockList.items}
                rowKey={(row) => row.id}
                actions={(row) => (
                  <div className="btn-group btn-group-sm">
                    {puedeMovimiento ? (
                      <button
                        type="button"
                        className="btn btn-outline-secondary"
                        title="Registrar movimiento"
                        onClick={() => openMovement(row)}
                      >
                        <i className="bi bi-arrow-left-right" aria-hidden="true" />
                      </button>
                    ) : null}
                    <button
                      type="button"
                      className="btn btn-outline-primary"
                      title="Ver historial de movimientos"
                      onClick={() => openHistory(row)}
                    >
                      <i className="bi bi-clock-history" aria-hidden="true" />
                    </button>
                  </div>
                )}
              />
              <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 border-top p-3">
                <span className="small text-muted">
                  Página {stockList.page} de {Math.max(stockList.totalPages, 1)} ·{' '}
                  {stockList.totalItems} resultados
                </span>
                <Pagination
                  page={stockList.page}
                  totalPages={stockList.totalPages}
                  onPageChange={stockList.setPage}
                />
              </div>
            </>
          )}
        </PanelCard>
      ) : (
        <PanelCard
          title="Historial de movimientos"
          actions={
            historyFilter ? (
              <div className="d-flex align-items-center gap-2">
                <span className="badge text-bg-secondary" title="Filtro por producto">
                  {historyFilter.nombre}
                </span>
                <AppButton
                  variant="outline-secondary"
                  onClick={clearHistoryFilter}
                  icon={<i className="bi bi-x-lg me-1" aria-hidden="true" />}
                >
                  Quitar filtro
                </AppButton>
              </div>
            ) : undefined
          }
        >
          {historyList.loading ? (
            <LoadingSpinner label="Cargando historial..." />
          ) : historyList.forbidden ? (
            <div className="p-3">
              <AppAlert variant="warning" title="Sin permiso">
                Tu usuario no tiene permiso para ver el historial de stock.
              </AppAlert>
            </div>
          ) : historyList.error ? (
            <div className="p-3">
              <AppAlert variant="danger" title="Error al cargar">
                {historyList.error}
              </AppAlert>
            </div>
          ) : historyList.items.length === 0 ? (
            <EmptyState
              icon="bi-clock-history"
              title="Sin movimientos"
              description={
                historyFilter
                  ? 'Este producto todavía no tiene movimientos registrados.'
                  : 'Los movimientos de stock aparecerán aquí con su historial (BR-051).'
              }
            />
          ) : (
            <>
              <DataTable
                columns={[
                  {
                    header: 'Fecha',
                    render: (row: MovimientoStock) => formatDateTime(row.fecha),
                  },
                  {
                    header: 'Producto',
                    render: (row: MovimientoStock) => (
                      <>
                        <div className="fw-semibold">{row.productoNombre}</div>
                        <div className="small text-muted font-monospace">#{row.productoId}</div>
                      </>
                    ),
                  },
                  {
                    header: 'Tipo',
                    render: (row: MovimientoStock) => TIPO_MOVIMIENTO_STOCK_LABELS[row.tipo],
                  },
                  {
                    header: 'Efecto',
                    render: (row: MovimientoStock) => {
                      const signo = efectoDe(row);
                      return (
                        <span className={`badge ${signo > 0 ? 'text-bg-success' : 'text-bg-danger'}`}>
                          {signo > 0 ? '+' : '−'}
                          {row.cantidad}
                        </span>
                      );
                    },
                    className: 'text-center',
                  },
                  {
                    header: 'Stock',
                    render: (row: MovimientoStock) => (
                      <span className="font-monospace small">
                        {row.stockAnterior} → {row.stockPosterior}
                      </span>
                    ),
                    className: 'text-center',
                  },
                  {
                    header: 'Usuario',
                    render: (row: MovimientoStock) => row.usuarioNombre ?? '—',
                  },
                  {
                    header: 'Motivo / Ref.',
                    render: (row: MovimientoStock) => (
                      <>
                        {row.motivo ? <div>{row.motivo}</div> : null}
                        {row.referencia ? (
                          <div className="small text-muted font-monospace">{row.referencia}</div>
                        ) : null}
                        {!row.motivo && !row.referencia ? '—' : null}
                      </>
                    ),
                  },
                ]}
                rows={historyList.items}
                rowKey={(row) => row.id}
              />
              <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 border-top p-3">
                <span className="small text-muted">
                  Página {historyList.page} de {Math.max(historyList.totalPages, 1)} ·{' '}
                  {historyList.totalItems} resultados
                </span>
                <Pagination
                  page={historyList.page}
                  totalPages={historyList.totalPages}
                  onPageChange={historyList.setPage}
                />
              </div>
            </>
          )}
        </PanelCard>
      )}

      {modalOpen ? (
        <MovimientoFormModal
          open
          producto={movementProduct}
          onClose={() => setModalOpen(false)}
          onSaved={onSaved}
        />
      ) : null}
    </>
  );
}
