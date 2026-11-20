'use client';

/**
 * Detalle del catálogo público (functional-spec §9): ficha del producto
 * con galería de imágenes desde el storage (BR-100), especificaciones y
 * disponibilidad. Los productos inactivos responden 404 en la API y aquí
 * se muestran como inexistentes.
 */

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useParams } from 'next/navigation';
import { ApiError } from '@/services/api-client';
import { getCatalogo } from '@/services/catalogo';
import { formatMoney } from '@/lib/format';
import { TIPO_PRODUCTO_LABELS, type CatalogoProducto } from '@/types/api';
import { PanelCard } from '@/components/ui/cards';
import { AppAlert } from '@/components/ui/app-alert';
import { LoadingSpinner } from '@/components/ui/loading-spinner';

export default function CatalogoDetailPage() {
  const params = useParams<{ id: string }>();
  const id = Number(params.id);

  const [producto, setProducto] = useState<CatalogoProducto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [mainIndex, setMainIndex] = useState(0);

  const validId = Number.isInteger(id) && id > 0;

  // Cambio de producto en la misma ruta: recorrer la galería al inicio
  // y no mostrar la ficha anterior mientras carga la nueva.
  const [prevId, setPrevId] = useState(id);
  if (prevId !== id) {
    setPrevId(id);
    setMainIndex(0);
    setProducto(null);
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
        const result = await getCatalogo(id, controller.signal);
        if (!cancelled) {
          setProducto(result);
          setError(null);
        }
      } catch (err) {
        if (!cancelled && (err as { name?: string }).name !== 'AbortError') {
          setError(
            err instanceof ApiError && err.isNotFound
              ? 'Este producto no existe o no está disponible.'
              : err instanceof Error
                ? err.message
                : 'No se pudo cargar el producto.'
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
      <AppAlert variant="danger" title="Producto inexistente">
        El identificador del producto no es válido.{' '}
        <Link href="/catalogo">Volver al catálogo</Link>.
      </AppAlert>
    );
  }

  if (error) {
    return (
      <AppAlert variant="danger" title="No se pudo cargar">
        {error}
        <div className="mt-2">
          <Link href="/catalogo">Volver al catálogo</Link>
        </div>
      </AppAlert>
    );
  }

  if (!producto) {
    return <LoadingSpinner label="Cargando producto..." />;
  }

  const imagenes = producto.imagenes;
  const imagenPrincipal = imagenes.length > 0 ? imagenes[Math.min(mainIndex, imagenes.length - 1)] : null;

  return (
    <>
      <nav aria-label="breadcrumb" className="mb-3">
        <ol className="breadcrumb small mb-0">
          <li className="breadcrumb-item">
            <Link href="/catalogo">Catálogo</Link>
          </li>
          <li className="breadcrumb-item active" aria-current="page">
            {producto.nombre}
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
                alt={`${producto.nombre} · imagen ${imagenPrincipal.orden + 1}`}
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
              <h1 className="h4 mb-1">{producto.nombre}</h1>
              {producto.disponible ? (
                <span className="badge text-bg-success">Disponible</span>
              ) : (
                <span className="badge text-bg-secondary">A consultar</span>
              )}
            </div>

            <div className="mb-2">
              <span className="badge text-bg-light border me-1">
                {TIPO_PRODUCTO_LABELS[producto.tipo]}
              </span>
              {producto.categoria ? (
                <span className="badge text-bg-light border me-1">{producto.categoria}</span>
              ) : null}
              {producto.esPersonalizado ? (
                <span className="badge text-bg-info">A medida</span>
              ) : null}
            </div>

            <p className="fs-4 fw-semibold mb-3">{formatMoney(producto.precio)}</p>

            {producto.descripcion ? <p className="text-muted">{producto.descripcion}</p> : null}

            <dl className="row mb-0 small">
              {producto.material ? (
                <>
                  <dt className="col-5 text-muted">Material</dt>
                  <dd className="col-7">{producto.material}</dd>
                </>
              ) : null}
              {producto.acabado ? (
                <>
                  <dt className="col-5 text-muted">Acabado</dt>
                  <dd className="col-7">{producto.acabado}</dd>
                </>
              ) : null}
              {producto.color ? (
                <>
                  <dt className="col-5 text-muted">Color</dt>
                  <dd className="col-7">{producto.color}</dd>
                </>
              ) : null}
              {producto.dimensiones ? (
                <>
                  <dt className="col-5 text-muted">Medidas</dt>
                  <dd className="col-7">{producto.dimensiones}</dd>
                </>
              ) : null}
            </dl>

            <div className="border-top mt-3 pt-3 small text-muted">
              ¿Te interesa?{' '}
              <Link href="/portal/presupuestos">Solicitá un presupuesto</Link> desde tu cuenta.
            </div>
          </PanelCard>
        </div>
      </div>
    </>
  );
}
