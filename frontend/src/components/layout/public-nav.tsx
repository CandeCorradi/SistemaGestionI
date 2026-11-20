'use client';

/**
 * Barra de navegación de las áreas públicas con acceso al carrito de la
 * vidriera (AGENTS.md §23). Es cliente porque el contador del carrito vive
 * en localStorage.
 */

import Link from 'next/link';
import { useCart } from '@/features/carrito/cart-provider';

export function PublicNav() {
  const { count, openCart } = useCart();

  return (
    <nav className="navbar navbar-expand-lg navbar-light bg-white border-bottom">
      <div className="container">
        <Link href="/catalogo" className="navbar-brand fw-semibold">
          <i className="bi bi-hammer me-2" aria-hidden="true" />
          Nuestra carpintería
        </Link>
        <div className="d-flex align-items-center gap-2">
          <Link href="/showroom" className="btn btn-outline-secondary btn-sm">
            <i className="bi bi-hammer me-1" aria-hidden="true" />
            Trabajos
          </Link>
          <Link href="/catalogo" className="btn btn-outline-secondary btn-sm">
            <i className="bi bi-grid me-1" aria-hidden="true" />
            Catálogo
          </Link>
          <button
            type="button"
            className="btn btn-outline-primary btn-sm position-relative"
            onClick={openCart}
            aria-label={`Abrir carrito${count > 0 ? ` (${count})` : ''}`}
          >
            <i className="bi bi-cart3" aria-hidden="true" />
            {count > 0 ? (
              <span className="position-absolute top-0 start-100 translate-middle badge rounded-pill text-bg-danger">
                {count}
              </span>
            ) : null}
          </button>
          <Link href="/portal" className="btn btn-outline-primary btn-sm">
            <i className="bi bi-person me-1" aria-hidden="true" />
            Mi cuenta
          </Link>
        </div>
      </div>
    </nav>
  );
}
