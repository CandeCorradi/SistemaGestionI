'use client';

/**
 * Catálogo público (functional-spec §9, roadmap §13.4): listado paginado
 * de productos activos con búsqueda, filtro por tipo e imagen de portada
 * desde el storage (BR-100). No expone datos internos de costo ni de
 * stock numérico (BR-004).
 */

import Link from 'next/link';
import { useCallback, useState } from 'react';
import { listCatalogo } from '@/services/catalogo';
import { usePagedList, type PagedQuery } from '@/hooks/usePagedList';
import { formatMoney } from '@/lib/format';
import { TIPO_PRODUCTO_LABELS, type CatalogoItem, type TipoProducto } from '@/types/api';
import { PanelCard } from '@/components/ui/cards';
import { AppAlert } from '@/components/ui/app-alert';
import { EmptyState } from '@/components/ui/empty-state';
import { LoadingSpinner } from '@/components/ui/loading-spinner';
import { Pagination } from '@/components/ui/pagination';

export default function CatalogoPage() {
  const [tipo, setTipo] = useState<TipoProducto | undefined>(undefined);

  const fetcher = useCallback(
    (query: PagedQuery, signal?: AbortSignal) =>
      listCatalogo(
        {
          page: query.page,
          pageSize: query.pageSize,
          search: query.search,
          tipo,
        },
        signal
      ),
    [tipo]
  );

  const list = usePagedList<CatalogoItem>({
    fetcher,
    fetcherKey: String(tipo ?? ''),
  });

  return (
    <>
      <div className="mb-4">
        <h1 className="h4 mb-1">Catálogo</h1>
        <p className="text-muted mb-0 small">
          {list.totalItems} producto{list.totalItems === 1 ? '' : 's'} para descubrir
        </p>
      </div>

      <PanelCard
        title="Productos"
        actions={
          <div className="d-flex flex-wrap gap-2 align-items-center">
            <div className="input-group input-group-sm" style={{ width: '16rem' }}>
              <span className="input-group-text bg-white">
                <i className="bi bi-search" aria-hidden="true" />
              </span>
              <input
                type="search"
                className="form-control"
                placeholder="Buscar por nombre, descripción..."
                value={list.search}
                onChange={(event) => list.setSearch(event.target.value)}
                aria-label="Buscar productos"
              />
            </div>
            <select
              className="form-select form-select-sm"
              style={{ width: '9.5rem' }}
              value={tipo === undefined ? '' : String(tipo)}
              onChange={(event) =>
                setTipo(event.target.value === '' ? undefined : (Number(event.target.value) as TipoProducto))
              }
              aria-label="Filtrar por tipo"
            >
              <option value="">Todos los tipos</option>
              {(Object.keys(TIPO_PRODUCTO_LABELS) as unknown as TipoProducto[]).map((value) => (
                <option key={value} value={value}>
                  {TIPO_PRODUCTO_LABELS[value]}
                </option>
              ))}
            </select>
          </div>
        }
      >
        {list.loading ? (
          <LoadingSpinner label="Cargando catálogo..." />
        ) : list.error ? (
          <div className="p-3">
            <AppAlert variant="danger" title="Error al cargar">
              {list.error}
            </AppAlert>
          </div>
        ) : list.items.length === 0 ? (
          <EmptyState
            icon="bi-grid"
            title="Sin resultados"
            description={
              list.search || tipo !== undefined
                ? 'Ningún producto coincide con los filtros.'
                : 'Pronto vas a encontrar nuestros productos acá.'
            }
          />
        ) : (
          <>
            <div className="row g-3">
              {list.items.map((item) => (
                <div className="col-6 col-md-4 col-lg-3" key={item.id}>
                  <Link
                    href={`/catalogo/${item.id}`}
                    className="card h-100 text-decoration-none border"
                  >
                    {item.imagenUrl ? (
                      // eslint-disable-next-line @next/next/no-img-element -- URL pública del storage
                      <img
                        src={item.imagenUrl}
                        alt={item.nombre}
                        className="card-img-top object-fit-cover"
                        style={{ height: '12rem' }}
                        loading="lazy"
                      />
                    ) : (
                      <div
                        className="card-img-top bg-secondary-subtle d-flex align-items-center justify-content-center"
                        style={{ height: '12rem' }}
                      >
                        <i className="bi bi-image fs-1 text-secondary" aria-hidden="true" />
                      </div>
                    )}
                    <div className="card-body">
                      <div className="d-flex justify-content-between align-items-start gap-2">
                        <span className="fw-semibold text-dark">{item.nombre}</span>
                        {item.disponible ? (
                          <span className="badge text-bg-success" title="Disponible">
                            Disponible
                          </span>
                        ) : (
                          <span className="badge text-bg-secondary" title="Sin stock">
                            A consultar
                          </span>
                        )}
                      </div>
                      <div className="small text-muted mt-1">
                        {TIPO_PRODUCTO_LABELS[item.tipo]}
                        {item.categoria ? ` · ${item.categoria}` : ''}
                      </div>
                      <div className="fw-semibold mt-2">{formatMoney(item.precio)}</div>
                    </div>
                  </Link>
                </div>
              ))}
            </div>
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
