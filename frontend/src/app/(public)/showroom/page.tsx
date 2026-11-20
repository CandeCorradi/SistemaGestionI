'use client';

/**
 * Vidriera pública de trabajos realizados (AGENTS.md §23, roadmap §13):
 * listado paginado de obras publicadas con búsqueda, filtro por
 * categoría e imagen de portada desde el storage (BR-100). Sólo muestra
 * obras publicadas (BR-190) y nunca expone el nombre del cliente sin
 * consentimiento (BR-191).
 */

import Link from 'next/link';
import { useCallback, useEffect, useState } from 'react';
import { listShowroom } from '@/services/showroom';
import { usePagedList, type PagedQuery } from '@/hooks/usePagedList';
import { useCart } from '@/features/carrito/cart-provider';
import type { ShowroomItem } from '@/types/api';
import { PanelCard } from '@/components/ui/cards';
import { AppAlert } from '@/components/ui/app-alert';
import { EmptyState } from '@/components/ui/empty-state';
import { LoadingSpinner } from '@/components/ui/loading-spinner';
import { Pagination } from '@/components/ui/pagination';

export default function ShowroomPage() {
  const [categoria, setCategoria] = useState('');
  const [addedId, setAddedId] = useState<number | null>(null);
  const { addItem, openCart } = useCart();

  // Al volver del login con ?cart=1 se reabre el carrito para completar la
  // compra sin que el visitante tenga que buscarlo de nuevo. El parámetro se
  // quita de la URL para que un refresco posterior no vuelva a abrirlo.
  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    if (params.get('cart') !== '1') {
      return;
    }
    openCart();
    params.delete('cart');
    const query = params.toString();
    window.history.replaceState(
      null,
      '',
      `${window.location.pathname}${query ? `?${query}` : ''}`
    );
  }, [openCart]);

  function handleAdd(item: ShowroomItem) {
    addItem({
      id: item.id,
      nombre: item.nombre,
      categoria: item.categoria,
      tipoMueble: item.tipoMueble,
      imagenUrl: item.imagenUrl,
    });
    setAddedId(item.id);
    window.setTimeout(
      () => setAddedId((current) => (current === item.id ? null : current)),
      1500
    );
  }

  const fetcher = useCallback(
    (query: PagedQuery, signal?: AbortSignal) =>
      listShowroom(
        {
          page: query.page,
          pageSize: query.pageSize,
          search: query.search,
          category: categoria || undefined,
        },
        signal
      ),
    [categoria]
  );

  const list = usePagedList<ShowroomItem>({
    fetcher,
    fetcherKey: categoria,
  });

  return (
    <>
      <div className="mb-4">
        <h1 className="h4 mb-1">Trabajos realizados</h1>
        <p className="text-muted mb-0 small">
          {list.totalItems} obra{list.totalItems === 1 ? '' : 's'} de nuestra vidriera
        </p>
      </div>

      <PanelCard
        title="Obras"
        actions={
          <div className="d-flex flex-wrap gap-2 align-items-center">
            <div className="input-group input-group-sm" style={{ width: '16rem' }}>
              <span className="input-group-text bg-white">
                <i className="bi bi-search" aria-hidden="true" />
              </span>
              <input
                type="search"
                className="form-control"
                placeholder="Buscar por nombre, categoría..."
                value={list.search}
                onChange={(event) => list.setSearch(event.target.value)}
                aria-label="Buscar trabajos"
              />
            </div>
            <input
              type="search"
              className="form-control form-control-sm"
              style={{ width: '10rem' }}
              placeholder="Categoría"
              value={categoria}
              onChange={(event) => setCategoria(event.target.value)}
              aria-label="Filtrar por categoría"
            />
          </div>
        }
      >
        {list.loading ? (
          <LoadingSpinner label="Cargando trabajos..." />
        ) : list.error ? (
          <div className="p-3">
            <AppAlert variant="danger" title="Error al cargar">
              {list.error}
            </AppAlert>
          </div>
        ) : list.items.length === 0 ? (
          <EmptyState
            icon="bi-hammer"
            title="Sin resultados"
            description={
              list.search || categoria
                ? 'Ninguna obra coincide con los filtros.'
                : 'Pronto vas a encontrar nuestros trabajos acá.'
            }
          />
        ) : (
          <>
            <div className="row g-3">
              {list.items.map((item) => (
                <div className="col-6 col-md-4 col-lg-3" key={item.id}>
                  <div className="card h-100 border">
                    <Link href={`/showroom/${item.id}`} className="text-decoration-none">
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
                    </Link>
                    <div className="card-body d-flex flex-column">
                      <Link href={`/showroom/${item.id}`} className="text-decoration-none">
                        <div className="d-flex justify-content-between align-items-start gap-2">
                          <span className="fw-semibold text-dark">{item.nombre}</span>
                          {item.destacado ? (
                            <span className="badge text-bg-warning" title="Destacado">
                              <i className="bi bi-star-fill" aria-hidden="true" />
                            </span>
                          ) : null}
                        </div>
                        <div className="small text-muted mt-1">
                          {item.tipoMueble ?? 'Trabajo a medida'}
                          {item.categoria ? ` · ${item.categoria}` : ''}
                        </div>
                        <div className="small text-muted mt-1 d-flex align-items-center gap-1">
                          {item.fecha ? (
                            <>
                              <i className="bi bi-calendar3" aria-hidden="true" />
                              {item.fecha}
                            </>
                          ) : null}
                          {item.clienteNombre ? ` · ${item.clienteNombre}` : null}
                        </div>
                      </Link>
                      <button
                        type="button"
                        className={`btn btn-sm mt-3 ${
                          addedId === item.id ? 'btn-success' : 'btn-outline-primary'
                        }`}
                        onClick={() => handleAdd(item)}
                        title="Agregar al carrito"
                      >
                        <i
                          className={`bi ${
                            addedId === item.id ? 'bi-check-lg' : 'bi-cart-plus'
                          } me-1`}
                          aria-hidden="true"
                        />
                        {addedId === item.id ? 'Agregado' : 'Agregar al carrito'}
                      </button>
                    </div>
                  </div>
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
