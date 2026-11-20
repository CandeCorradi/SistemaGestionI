'use client';

/**
 * Layout del panel administrativo (roadmap §16/§18): guarda de sesión,
 * shell con sidebar y barra superior. Si Firebase no está configurado o el
 * usuario no tiene usuario interno habilitado, muestra el aviso
 * correspondiente en lugar de datos protegidos.
 */

import { useEffect, useState, type ReactNode } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { useAuth } from '@/features/auth/auth-provider';
import { Sidebar } from '@/components/layout/sidebar';
import { NotificationsBell } from '@/features/notificaciones/notifications-bell';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { LoadingSpinner } from '@/components/ui/loading-spinner';

export default function AdminLayout({ children }: { children: ReactNode }) {
  const router = useRouter();
  const {
    configured,
    initializing,
    firebaseUser,
    profile,
    account,
    profileLoading,
    accessDenied,
    error,
    can,
    logout,
  } = useAuth();
  const [menuOpen, setMenuOpen] = useState(false);

  const isAuthenticated = Boolean(firebaseUser && profile && !accessDenied);
  const isCliente = account?.rol === 'Cliente';

  useEffect(() => {
    if (!configured || initializing) {
      return;
    }
    if (!firebaseUser) {
      router.replace('/login');
    }
  }, [configured, initializing, firebaseUser, router]);

  // El rol Cliente no usa el panel administrativo: va al portal (AGENTS.md §9).
  useEffect(() => {
    if (firebaseUser && !profileLoading && isCliente) {
      router.replace('/portal');
    }
  }, [firebaseUser, profileLoading, isCliente, router]);

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
    // Redirigiendo a /login; mostrar cargando evita un destello.
    return (
      <div className="min-vh-100 d-flex align-items-center justify-content-center">
        <LoadingSpinner label="Redirigiendo al login..." />
      </div>
    );
  }

  if (accessDenied) {
    return (
      <div className="container py-5" style={{ maxWidth: '40rem' }}>
        <AppAlert variant="danger" title="Usuario sin acceso">
          Tu cuenta está autenticada en Firebase pero no tiene un usuario interno habilitado en el
          sistema. Contactá al administrador.
        </AppAlert>
        <div className="text-center">
          <AppButton variant="outline-secondary" onClick={() => void logout()}>
            Cerrar sesión
          </AppButton>
        </div>
      </div>
    );
  }

  if (isCliente) {
    // Redirigiendo al portal; mostrar cargando evita un destello.
    return (
      <div className="min-vh-100 d-flex align-items-center justify-content-center">
        <LoadingSpinner label="Redirigiendo al portal..." />
      </div>
    );
  }

  if (!isAuthenticated) {
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
    <div className="admin-shell d-flex">
      <Sidebar can={can} open={menuOpen} onClose={() => setMenuOpen(false)} />

      <div className="admin-content flex-grow-1 d-flex flex-column">
        <header className="bg-white border-bottom px-3 py-2 d-flex align-items-center gap-2">
          <button
            type="button"
            className="btn btn-outline-secondary btn-sm d-md-none"
            aria-label="Abrir menú"
            onClick={() => setMenuOpen((open) => !open)}
          >
            <i className="bi bi-list" aria-hidden="true" />
          </button>
          <div className="ms-auto d-flex align-items-center gap-3">
            <NotificationsBell />
            <span className="small text-muted d-none d-sm-inline">
              <i className="bi bi-person-circle me-1" aria-hidden="true" />
              {profile?.email}
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
        </header>

        <main className="p-3 p-lg-4 flex-grow-1">{children}</main>
      </div>
    </div>
  );
}
