import type { ReactNode } from 'react';
import { CartProvider } from '@/features/carrito/cart-provider';
import { CartDrawer } from '@/features/carrito/cart-drawer';
import { PublicNav } from '@/components/layout/public-nav';

/**
 * Layout de áreas públicas (functional-spec §9, roadmap §13.4):
 * catálogo anónimo con barra de navegación hacia el ingreso y el portal.
 * No requiere sesión: los endpoints del catálogo no llevan token. Incluye
 * el carrito de la vidriera (localStorage) y su panel lateral.
 */
export default function PublicLayout({ children }: { children: ReactNode }) {
  return (
    <CartProvider>
      <div className="min-vh-100 d-flex flex-column bg-light">
        <PublicNav />

        <main className="container py-4 flex-grow-1">{children}</main>

        <footer className="border-top bg-white py-3">
          <div className="container small text-muted">
            Sistema de Gestión · Catálogo de productos
          </div>
        </footer>

        <CartDrawer />
      </div>
    </CartProvider>
  );
}
