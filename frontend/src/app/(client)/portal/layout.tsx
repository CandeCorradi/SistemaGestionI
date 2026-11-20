'use client';

/**
 * Layout del Portal Cliente (functional-spec §23, roadmap Phase 20):
 * guarda de sesión + guard de rol (sólo el rol Cliente entra al portal;
 * el equipo va al panel administrativo), barra superior con navegación,
 * notificaciones, contacto por WhatsApp (link, AGENTS.md §21) y salida.
 */

import { useEffect, useState, type ReactNode } from 'react';
import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { useAuth } from '@/features/auth/auth-provider';
import { NotificationsBell } from '@/features/notificaciones/notifications-bell';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { LoadingSpinner } from '@/components/ui/loading-spinner';

interface PortalNavLink {
  href: string;
  label: string;
  icon: string;
}

const NAV_LINKS: PortalNavLink[] = [
  { href: '/portal', label: 'Inicio', icon: 'bi-house-door' },
  { href: '/catalogo', label: 'Catálogo', icon: 'bi-grid' },
  { href: '/portal/presupuestos', label: 'Presupuestos', icon: 'bi-file-earmark-text' },
  { href: '/portal/pedidos', label: 'Pedidos', icon: 'bi-box-seam' },
  { href: '/portal/pagos', label: 'Pagos', icon: 'bi-cash-coin' },
  { href: '/portal/notificaciones', label: 'Notificaciones', icon: 'bi-bell' },
  { href: '/portal/perfil', label: 'Perfil', icon: 'bi-person' },
];

/** Número internacional en formato wa.me (ej. 5491122334455); vacío oculta el botón. */
const WHATSAPP_NUMBER = process.env.NEXT_PUBLIC_WHATSAPP_NUMBER;

export default function PortalLayout({ children }: { children: ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const {
    configured,
    initializing,
    firebaseUser,
    profile,
    account,
    profileLoading,
    accessDenied,
    error,
    logout,
  } = useAuth();
  const [menuOpen, setMenuOpen] = useState(false);

  const isCliente = account?.rol === 'Cliente';

  useEffect(() => {
    if (!configured || initializing) {
      return;
    }
    if (!firebaseUser) {
      router.replace('/login');
    }
  }, [configured, initializing, firebaseUser, router]);

  // El equipo no usa el portal: vuelve al panel administrativo (AGENTS.md §9).
  useEffect(() => {
    if (firebaseUser && !profileLoading && account && !isCliente) {
      router.replace('/dashboard');
    }
  }, [firebaseUser, profileLoading, account, isCliente, router]);

  if (!configured) {
    return (
      <div className="container py-5" style={{ maxWidth: '40rem' }}>
        <AppAlert variant="warning" title="Firebase sin configurar">
          Falta la configuración pública de Firebase (<code>NEXT_PUBLIC_FIREBASE_*</code>) en{' '}
          <code>frontend/.env.local</code>. Copiá las claves de la consola de Firebase y reiniciá{' '}
          <code>npm run dev</code>.
        </AppAlert>
        <div className="text-center">
          <Link href="/login" className="btn btn-outline-secondary">
            Ir al login
          </Link>
        </div>
      </div>
    );
  }

  if (initializing || (firebaseUser && (profileLoading || !profile && !accessDenied && !error))) {
    return (
      <div className="min-vh-100 d-flex align-items-center justify-content-center">
        <LoadingSpinner label="Verificando sesión..." />
      </div>
    );
  }

  if (!firebaseUser) {
    return (
      <div className="min-vh-100 d-flex align-items-center justify-content-center">
        <LoadingSpinner label="Redirigiendo al login..." />
      </div>
    );
  }

  if (accessDenied) {
    return (
      <div className="container py-5" style={{ maxWidth: '40rem' }}>
        <AppAlert variant="danger" title="Cuenta sin acceso">
          Tu cuenta está autenticada pero no tiene un cliente vinculado en el sistema. Contactá a la
          carpintería.
        </AppAlert>
        <div className="text-center">
          <AppButton variant="outline-secondary" onClick={() => void logout()}>
            Cerrar sesión
          </AppButton>
        </div>
      </div>
    );
  }

  if (account && !isCliente) {
    return (
      <div className="min-vh-100 d-flex align-items-center justify-content-center">
        <LoadingSpinner label="Redirigiendo al panel..." />
      </div>
    );
  }

  if (!isCliente) {
    return (
      <div className="container py-5" style={{ maxWidth: '40rem' }}>
        <AppAlert variant="danger" title="No se pudo cargar el perfil">
          {error ?? 'Error inesperado.'}
        </AppAlert>
        <div className="text-center">
          <AppButton variant="outline-secondary" onClick={() => void logout()}>
            Cerrar sesión
          </AppButton>
        </div>
      </div>
    );
  }

  return (
    <div className="min-vh-100 d-flex flex-column bg-light">
      <nav className="navbar navbar-expand-lg navbar-light bg-white border-bottom">
        <div className="container">
          <Link href="/portal" className="navbar-brand fw-semibold">
            <i className="bi bi-hammer me-2" aria-hidden="true" />
            Mi carpintería
          </Link>
          <button
            type="button"
            className="navbar-toggler"
            aria-label="Abrir menú"
            aria-expanded={menuOpen}
            onClick={() => setMenuOpen((open) => !open)}
          >
            <i className="bi bi-list" aria-hidden="true" />
          </button>

          <div className={`collapse navbar-collapse ${menuOpen ? 'show' : ''}`}>
            <ul className="navbar-nav me-auto mb-2 mb-lg-0">
              {NAV_LINKS.map((link) => (
                <li className="nav-item" key={link.href}>
                  <Link
                    href={link.href}
                    className={`nav-link${pathname === link.href ? ' active' : ''}`}
                    aria-current={pathname === link.href ? 'page' : undefined}
                    onClick={() => setMenuOpen(false)}
                  >
                    <i className={`${link.icon} me-1`} aria-hidden="true" />
                    {link.label}
                  </Link>
                </li>
              ))}
            </ul>

            <div className="d-flex align-items-center gap-2">
              <NotificationsBell />
              {WHATSAPP_NUMBER ? (
                <a
                  className="btn btn-outline-success btn-sm"
                  href={`https://wa.me/${WHATSAPP_NUMBER}`}
                  target="_blank"
                  rel="noreferrer"
                  title="Contactar por WhatsApp"
                >
                  <i className="bi bi-whatsapp" aria-hidden="true" />
                  <span className="d-none d-sm-inline ms-1">WhatsApp</span>
                </a>
              ) : null}
              <span className="small text-muted d-none d-md-inline">
                <i className="bi bi-person-circle me-1" aria-hidden="true" />
                {account?.nombre} {account?.apellido}
              </span>
              <AppButton
                variant="outline-secondary"
                className="btn-sm"
                onClick={() => void logout()}
                icon={<i className="bi bi-box-arrow-right me-1" aria-hidden="true" />}
              >
                Salir
              </AppButton>
            </div>
          </div>
        </div>
      </nav>

      <main className="container py-4 flex-grow-1">{children}</main>

      <footer className="border-top bg-white py-3">
        <div className="container d-flex flex-wrap justify-content-between small text-muted gap-2">
          <span>Sistema de Gestión · Portal del cliente</span>
          <span>¿Dudas? Escribinos por WhatsApp.</span>
        </div>
      </footer>
    </div>
  );
}
