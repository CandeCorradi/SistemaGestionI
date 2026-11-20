'use client';

/**
 * Panel lateral del carrito de la vidriera. Al finalizar, exige sesión de
 * cliente: si no hay sesión redirige al login conservando el carrito y el
 * destino; si la hay, convierte la selección en una solicitud de
 * presupuesto (POST /api/v1/my/quotes), que es el punto de entrada del
 * flujo comercial (presupuesto → aceptación → pedido).
 */

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/features/auth/auth-provider';
import { useCart } from '@/features/carrito/cart-provider';
import { requestQuote } from '@/services/portal';
import { ApiError } from '@/services/api-client';
import { AppAlert } from '@/components/ui/app-alert';

export function CartDrawer() {
  const router = useRouter();
  const { firebaseUser, profile, profileLoading, account, initializing } = useAuth();
  const { items, count, open, closeCart, removeItem, clear } = useCart();

  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Bloquea el scroll del fondo mientras el panel está abierto.
  useEffect(() => {
    if (!open) {
      return;
    }
    const previous = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => {
      document.body.style.overflow = previous;
    };
  }, [open]);

  // Al cerrar se descarta el error anterior para que no reaparezca al
  // volver a abrir el panel.
  function handleClose() {
    setError(null);
    closeCart();
  }

  async function checkout() {
    if (items.length === 0) {
      return;
    }
    setError(null);

    // Mientras Firebase restaura la sesión no se puede saber si hay usuario:
    // redirigir al login en ese momento expulsaría a un cliente con sesión.
    if (initializing) {
      return;
    }

    // Sin sesión: al login y de vuelta a la vidriera con el carrito abierto.
    if (!firebaseUser) {
      closeCart();
      router.push(`/login?redirect=${encodeURIComponent('/showroom?cart=1')}`);
      return;
    }

    if (profileLoading) {
      return;
    }

    if (!profile) {
      setError('Tu cuenta todavía no tiene acceso al portal. Contactá al administrador.');
      return;
    }

    if (account?.rol !== 'Cliente') {
      setError('Para comprar necesitás ingresar con una cuenta de cliente.');
      return;
    }

    const descripcion = [
      'Compra desde la vidriera:',
      ...items.map(
        (item) => `- ${item.nombre}${item.categoria ? ` (${item.categoria})` : ''}`
      ),
    ].join('\n');

    setSubmitting(true);
    try {
      const quote = await requestQuote({ description: descripcion, cantidad: 1 });
      clear();
      closeCart();
      router.push(`/portal/presupuestos?created=${quote.id}`);
    } catch (err) {
      setError(
        err instanceof ApiError
          ? err.message
          : 'No se pudo enviar la solicitud de compra. Intentá de nuevo.'
      );
    } finally {
      setSubmitting(false);
    }
  }

  if (!open) {
    return null;
  }

  return (
    <>
      <div className="offcanvas-backdrop fade show" onClick={handleClose} aria-hidden="true" />
      <div
        className="offcanvas offcanvas-end show"
        tabIndex={-1}
        role="dialog"
        aria-modal="true"
        aria-labelledby="cart-title"
        style={{ visibility: 'visible' }}
      >
        <div className="offcanvas-header border-bottom">
          <h5 className="offcanvas-title" id="cart-title">
            <i className="bi bi-cart3 me-2" aria-hidden="true" />
            Mi selección ({count})
          </h5>
          <button
            type="button"
            className="btn-close"
            onClick={handleClose}
            aria-label="Cerrar carrito"
          />
        </div>

        <div className="offcanvas-body d-flex flex-column">
          {items.length === 0 ? (
            <div className="text-center text-muted my-5">
              <i className="bi bi-cart-x fs-1 d-block mb-2" aria-hidden="true" />
              <p className="mb-0">Todavía no agregaste trabajos.</p>
            </div>
          ) : (
            <ul className="list-group list-group-flush">
              {items.map((item) => (
                <li
                  key={item.id}
                  className="list-group-item px-0 d-flex align-items-center gap-3"
                >
                  {item.imagenUrl ? (
                    // eslint-disable-next-line @next/next/no-img-element -- URL pública del storage
                    <img
                      src={item.imagenUrl}
                      alt={item.nombre}
                      className="rounded object-fit-cover"
                      style={{ width: '4rem', height: '4rem' }}
                    />
                  ) : (
                    <div
                      className="rounded bg-secondary-subtle d-flex align-items-center justify-content-center"
                      style={{ width: '4rem', height: '4rem' }}
                    >
                      <i className="bi bi-image text-secondary" aria-hidden="true" />
                    </div>
                  )}
                  <div className="flex-grow-1">
                    <div className="fw-semibold small">{item.nombre}</div>
                    <div className="text-muted small">
                      {item.tipoMueble ?? 'Trabajo a medida'}
                      {item.categoria ? ` · ${item.categoria}` : ''}
                    </div>
                  </div>
                  <button
                    type="button"
                    className="btn btn-outline-danger btn-sm"
                    title="Quitar"
                    onClick={() => removeItem(item.id)}
                  >
                    <i className="bi bi-trash" aria-hidden="true" />
                  </button>
                </li>
              ))}
            </ul>
          )}

          <div className="mt-auto border-top pt-3">
            {error ? (
              <AppAlert variant="danger" onClose={() => setError(null)}>
                {error}
              </AppAlert>
            ) : null}
            <p className="small text-muted">
              El equipo cotiza tu selección y te deja el presupuesto disponible en tu portal.
            </p>
            <button
              type="button"
              className="btn btn-primary w-100"
              onClick={() => void checkout()}
              disabled={items.length === 0 || submitting || profileLoading || initializing}
            >
              {submitting ? (
                <span className="spinner-border spinner-border-sm me-2" aria-hidden="true" />
              ) : initializing ? (
                <span className="spinner-border spinner-border-sm me-2" aria-hidden="true" />
              ) : (
                <i className="bi bi-bag-check me-2" aria-hidden="true" />
              )}
              {initializing
                ? 'Verificando sesión...'
                : firebaseUser
                  ? 'Finalizar compra'
                  : 'Ingresar para comprar'}
            </button>
          </div>
        </div>
      </div>
    </>
  );
}
