'use client';

/**
 * Gestión de productos (functional-spec §8, api-spec §12): listado con
 * búsqueda y filtros (tipo, estado, stock bajo), alta/edición y
 * desactivación lógica (BR-032).
 */

import { useCallback, useState } from 'react';
import { useAuth } from '@/features/auth/auth-provider';
import { ProductoFormModal } from '@/features/productos/producto-form-modal';
import { ProductImagesModal } from '@/features/productos/product-images-modal';
import { deactivateProducto, getProducto, listProductos } from '@/services/productos';
import { usePagedList, type PagedQuery } from '@/hooks/usePagedList';
import { formatMoney } from '@/lib/format';
import { TIPO_PRODUCTO_LABELS, type Producto, type ProductoList, type TipoProducto } from '@/types/api';
import { PanelCard } from '@/components/ui/cards';
import { DataTable } from '@/components/ui/data-table';
import { StatusBadge } from '@/components/ui/status-badge';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { EmptyState } from '@/components/ui/empty-state';
import { LoadingSpinner } from '@/components/ui/loading-spinner';
import { Pagination } from '@/components/ui/pagination';

function StockCell({ row }: { row: ProductoList }) {
  const low = row.stockMinimo > 0 && row.stockActual <= row.stockMinimo;
  return (
    <span className={`badge ${low ? 'text-bg-danger' : 'text-bg-light border'}`}>
      {row.stockActual}
      {low ? (
        <i className="bi bi-exclamation-triangle-fill ms-1" aria-hidden="true" title="Stock bajo" />
      ) : null}
    </span>
  );
}

export default function ProductosPage() {
  const { can } = useAuth();

  const [tipo, setTipo] = useState<TipoProducto | undefined>(undefined);
  const [lowStockOnly, setLowStockOnly] = useState(false);
  const [isCustom, setIsCustom] = useState<boolean | undefined>(undefined);

  const fetcher = useCallback(
    (query: PagedQuery, signal?: AbortSignal) =>
      listProductos(
        {
          page: query.page,
          pageSize: query.pageSize,
          search: query.search,
          active: query.active,
          tipo,
          lowStock: lowStockOnly ? true : undefined,
          isCustom,
        },
        signal
      ),
    [tipo, lowStockOnly, isCustom]
  );

  const list = usePagedList<ProductoList>({
    fetcher,
    fetcherKey: `${String(tipo)}|${String(lowStockOnly)}|${String(isCustom)}`,
  });

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<Producto | null>(null);
  const [imagesTarget, setImagesTarget] = useState<ProductoList | null>(null);
  const [confirmTarget, setConfirmTarget] = useState<ProductoList | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  if (!can('PRODUCTOS_VIEW')) {
    return (
      <AppAlert variant="warning" title="Sin permiso">
        No tenés permiso para ver productos (<code>PRODUCTOS_VIEW</code>).
      </AppAlert>
    );
  }

  function openCreate() {
    setEditing(null);
    setFormOpen(true);
  }

  async function openEdit(row: ProductoList) {
    setActionError(null);
    try {
      const full = await getProducto(row.id);
      setEditing(full);
      setFormOpen(true);
    } catch {
      setActionError('No se pudo cargar el producto para editar.');
    }
  }

  async function confirmDeactivate() {
    if (!confirmTarget) {
      return;
    }
    setSaving(true);
    setActionError(null);
    try {
      await deactivateProducto(confirmTarget.id);
      setConfirmTarget(null);
      list.reload();
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'No se pudo desactivar el producto.');
      setConfirmTarget(null);
    } finally {
      setSaving(false);
    }
  }

  return (
    <>
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-2">
        <div>
          <h1 className="h4 mb-1">Productos</h1>
          <p className="text-muted mb-0 small">
            {list.totalItems} producto{list.totalItems === 1 ? '' : 's'} en el catálogo
          </p>
        </div>
        {can('PRODUCTOS_CREATE') ? (
          <AppButton
            onClick={openCreate}
            icon={<i className="bi bi-plus-lg me-1" aria-hidden="true" />}
          >
            Nuevo producto
          </AppButton>
        ) : null}
      </div>

      {actionError ? (
        <AppAlert variant="danger" onClose={() => setActionError(null)}>
          {actionError}
        </AppAlert>
      ) : null}

      <PanelCard
        title="Catálogo"
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
                value={list.search}
                onChange={(event) => list.setSearch(event.target.value)}
                aria-label="Buscar productos"
              />
            </div>
            <select
              className="form-select form-select-sm"
              style={{ width: '9rem' }}
              value={tipo === undefined ? '' : String(tipo)}
              onChange={(event) =>
                setTipo(event.target.value === '' ? undefined : (Number(event.target.value) as TipoProducto))
              }
              aria-label="Filtrar por tipo"
            >
              <option value="">Todos los tipos</option>
              {(
                Object.keys(TIPO_PRODUCTO_LABELS) as unknown as TipoProducto[]
              ).map((value) => (
                <option key={value} value={value}>
                  {TIPO_PRODUCTO_LABELS[value]}
                </option>
              ))}
            </select>
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
            <select
              className="form-select form-select-sm"
              style={{ width: '9.5rem' }}
              value={isCustom === undefined ? '' : String(isCustom)}
              onChange={(event) =>
                setIsCustom(event.target.value === '' ? undefined : event.target.value === 'true')
              }
              aria-label="Filtrar estándar o personalizado"
            >
              <option value="">Estándar y a medida</option>
              <option value="false">Estándar</option>
              <option value="true">A medida</option>
            </select>
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
        {list.loading ? (
          <LoadingSpinner label="Cargando productos..." />
        ) : list.forbidden ? (
          <div className="p-3">
            <AppAlert variant="warning" title="Sin permiso">
              Tu usuario no tiene permiso para consultar productos.
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
            icon="bi-box-seam"
            title="No hay productos"
            description={
              list.search || tipo !== undefined || lowStockOnly || isCustom !== undefined
                ? 'Ningún producto coincide con los filtros.'
                : 'Registrá el primer producto con el botón "Nuevo producto".'
            }
          />
        ) : (
          <>
            <DataTable
              columns={[
                {
                  header: 'Producto',
                  render: (row: ProductoList) => (
                    <>
                      <div className="fw-semibold">{row.nombre}</div>
                      <div className="small text-muted font-monospace">{row.codigoSku}</div>
                    </>
                  ),
                },
                {
                  header: 'Tipo',
                  render: (row: ProductoList) => (
                    <>
                      {TIPO_PRODUCTO_LABELS[row.tipo]}
                      {row.esPersonalizado ? (
                        <span
                          className="badge text-bg-info ms-1"
                          title="Personalizado / a medida"
                        >
                          A medida
                        </span>
                      ) : null}
                    </>
                  ),
                },
                {
                  header: 'Precio',
                  render: (row: ProductoList) => formatMoney(row.precio),
                  className: 'text-end',
                },
                {
                  header: 'Stock',
                  render: (row: ProductoList) => <StockCell row={row} />,
                  className: 'text-center',
                },
                {
                  header: 'Estado',
                  render: (row: ProductoList) => <StatusBadge active={row.activo} />,
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
                  {can('PRODUCTOS_UPDATE') ? (
                    <button
                      type="button"
                      className="btn btn-outline-secondary"
                      title="Editar"
                      onClick={() => void openEdit(row)}
                    >
                      <i className="bi bi-pencil" aria-hidden="true" />
                    </button>
                  ) : null}
                  {can('PRODUCTOS_DELETE') && row.activo ? (
                    <button
                      type="button"
                      className="btn btn-outline-danger"
                      title="Desactivar"
                      onClick={() => setConfirmTarget(row)}
                    >
                      <i className="bi bi-archive" aria-hidden="true" />
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
        <ProductoFormModal
          open
          producto={editing}
          onClose={() => setFormOpen(false)}
          onSaved={() => {
            setFormOpen(false);
            list.reload();
          }}
        />
      ) : null}

      {imagesTarget ? (
        <ProductImagesModal
          open
          producto={imagesTarget}
          canUpload={can('PRODUCTOS_UPDATE')}
          onClose={() => setImagesTarget(null)}
          onChanged={() => list.reload()}
        />
      ) : null}

      <ConfirmDialog
        open={confirmTarget !== null}
        title="Desactivar producto"
        message={
          confirmTarget
            ? `Se desactivará "${confirmTarget.nombre}" (${confirmTarget.codigoSku}). El historial de presupuestos y pedidos se conserva (BR-032).`
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
