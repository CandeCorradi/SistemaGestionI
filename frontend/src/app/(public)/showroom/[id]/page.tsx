'use client';

/**
 * Detalle de la vidriera pública (AGENTS.md §23): ficha de la obra con
 * galería desde el storage (BR-100), materiales y medidas. Sólo las
 * obras publicadas responden 200 (BR-190); el nombre del cliente se
 * muestra sólo si la obra lo publica (BR-191).
 */

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useParams } from 'next/navigation';
import { ApiError } from '@/services/api-client';
import { getShowroom } from '@/services/showroom';
import { useCart } from '@/features/carrito/cart-provider';
import type { ShowroomTrabajo } from '@/types/api';
import { PanelCard } from '@/components/ui/cards';
import { AppAlert } from '@/components/ui/app-alert';
import { LoadingSpinner } from '@/components/ui/loading-spinner';

export default function ShowroomDetailPage() {
  const params = useParams<{ id: string }>();
  const id = Number(params.id);

  const [trabajo, setTrabajo] = useState<ShowroomTrabajo | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [mainIndex, setMainIndex] = useState(0);
  const { addItem, openCart } = useCart();

  const validId = Number.isInteger(id) && id > 0;

  // Cambio de obra en la misma ruta: recorrer la galería al inicio
  // y no mostrar la ficha anterior mientras carga la nueva.
  const [prevId, setPrevId] = useState(id);
  if (prevId !== id) {
    setPrevId(id);
    setMainIndex(0);
    setTrabajo(null);
    setError(null);
  }

  useEffect(() => {
    if (!validId) {
      return;
    }
    let cancelled = false;
    const controller = new AbortController();

    const load = async () => {
      try {
        const result = await getShowroom(id, controller.signal);
        if (!cancelled) {
          setTrabajo(result);
          setError(null);
        }
      } catch (err) {
        if (!cancelled && (err as { name?: string }).name !== 'AbortError') {
          setError(
            err instanceof ApiError && err.isNotFound
              ? 'Esta obra no existe o no está publicada.'
              : err instanceof Error
                ? err.message
                : 'No se pudo cargar la obra.'
          );
        }
      }
    };

    void load();
    return () => {
      cancelled = true;
      controller.abort();
    };
  }, [id, validId]);

  if (!validId) {
    return (
      <AppAlert variant="danger" title="Obra inexistente">
        El identificador de la obra no es válido.{' '}
        <Link href="/showroom">Volver a la vidriera</Link>.
      </AppAlert>
    );
  }

  if (error) {
    return (
      <AppAlert variant="danger" title="No se pudo cargar">
        {error}
        <div className="mt-2">
          <Link href="/showroom">Volver a la vidriera</Link>
        </div>
      </AppAlert>
    );
  }

  if (!trabajo) {
    return <LoadingSpinner label="Cargando obra..." />;
  }

  const imagenes = trabajo.imagenes;
  const imagenPrincipal = imagenes.length > 0 ? imagenes[Math.min(mainIndex, imagenes.length - 1)] : null;

  function handleAdd(openAfter: boolean) {
    if (!trabajo) {
      return;
    }
    addItem({
      id: trabajo.id,
      nombre: trabajo.nombre,
      categoria: trabajo.categoria,
      tipoMueble: trabajo.tipoMueble,
      imagenUrl: trabajo.imagenes[0]?.url ?? null,
    });
    if (openAfter) {
      openCart();
    }
  }

  return (
    <>
      <nav aria-label="breadcrumb" className="mb-3">
        <ol className="breadcrumb small mb-0">
          <li className="breadcrumb-item">
            <Link href="/showroom">Trabajos realizados</Link>
          </li>
          <li className="breadcrumb-item active" aria-current="page">
            {trabajo.nombre}
          </li>
        </ol>
      </nav>

      <div className="row g-4">
        <div className="col-lg-7">
          <PanelCard title="Galería">
            {imagenPrincipal ? (
              // eslint-disable-next-line @next/next/no-img-element -- URL pública del storage
              <img
                src={imagenPrincipal.url}
                alt={`${trabajo.nombre} · imagen ${imagenPrincipal.orden + 1}`}
                className="img-fluid rounded border w-100 object-fit-cover"
                style={{ maxHeight: '26rem' }}
              />
            ) : (
              <div
                className="rounded border bg-secondary-subtle d-flex align-items-center justify-content-center w-100"
                style={{ height: '18rem' }}
              >
                <div className="text-center text-secondary">
                  <i className="bi bi-image fs-1" aria-hidden="true" />
                  <div className="small">Sin imágenes</div>
                </div>
              </div>
            )}

            {imagenes.length > 1 ? (
              <div className="d-flex flex-wrap gap-2 mt-3">
                {imagenes.map((imagen, index) => (
                  <button
                    key={imagen.id}
                    type="button"
                    className={`btn p-0 border${index === mainIndex ? ' border-primary border-2' : ''}`}
                    title={`Imagen ${index + 1}`}
                    onClick={() => setMainIndex(index)}
                  >
                    {/* eslint-disable-next-line @next/next/no-img-element -- URL pública del storage */}
                    <img
                      src={imagen.url}
                      alt={`Miniatura ${index + 1}`}
                      className="rounded object-fit-cover"
                      style={{ width: '4.5rem', height: '4.5rem' }}
                      loading="lazy"
                    />
                  </button>
                ))}
              </div>
            ) : null}
          </PanelCard>
        </div>

        <div className="col-lg-5">
          <PanelCard title="Detalle">
            <div className="d-flex flex-wrap justify-content-between align-items-start gap-2">
              <h1 className="h4 mb-1">{trabajo.nombre}</h1>
              {trabajo.destacado ? (
                <span className="badge text-bg-warning">
                  <i className="bi bi-star-fill me-1" aria-hidden="true" />
                  Destacado
                </span>
              ) : null}
            </div>

            <div className="mb-2">
              {trabajo.tipoMueble ? (
                <span className="badge text-bg-light border me-1">{trabajo.tipoMueble}</span>
              ) : null}
              {trabajo.categoria ? (
                <span className="badge text-bg-light border me-1">{trabajo.categoria}</span>
              ) : null}
              {trabajo.clienteNombre ? (
                <span className="badge text-bg-light border me-1">
                  Cliente: {trabajo.clienteNombre}
                </span>
              ) : null}
            </div>

            {trabajo.descripcion ? <p className="text-muted">{trabajo.descripcion}</p> : null}

            <dl className="row mb-0 small">
              {trabajo.fecha ? (
                <>
                  <dt className="col-5 text-muted">Fecha</dt>
                  <dd className="col-7">{trabajo.fecha}</dd>
                </>
              ) : null}
              {trabajo.medidas ? (
                <>
                  <dt className="col-5 text-muted">Medidas</dt>
                  <dd className="col-7">{trabajo.medidas}</dd>
                </>
              ) : null}
              {trabajo.melamina ? (
                <>
                  <dt className="col-5 text-muted">Melamina</dt>
                  <dd className="col-7">{trabajo.melamina}</dd>
                </>
              ) : null}
              {trabajo.accesorios ? (
                <>
                  <dt className="col-5 text-muted">Accesorios</dt>
                  <dd className="col-7">{trabajo.accesorios}</dd>
                </>
              ) : null}
            </dl>

            {trabajo.tags.length > 0 ? (
              <div className="mt-3">
                {trabajo.tags.map((tag) => (
                  <span key={tag} className="badge text-bg-secondary me-1">
                    {tag}
                  </span>
                ))}
              </div>
            ) : null}

            <div className="d-flex flex-wrap gap-2 mt-3">
              <button
                type="button"
                className="btn btn-primary"
                onClick={() => handleAdd(true)}
              >
                <i className="bi bi-bag-check me-1" aria-hidden="true" />
                Comprar ahora
              </button>
              <button
                type="button"
                className="btn btn-outline-primary"
                onClick={() => handleAdd(false)}
              >
                <i className="bi bi-cart-plus me-1" aria-hidden="true" />
                Agregar al carrito
              </button>
            </div>

            <div className="border-top mt-3 pt-3 small text-muted">
              ¿Te gusta este trabajo?{' '}
              <Link href="/catalogo">Mirá el catálogo</Link> o solicitá un presupuesto desde tu
              cuenta.
            </div>
          </PanelCard>
        </div>
      </div>
    </>
  );
}
