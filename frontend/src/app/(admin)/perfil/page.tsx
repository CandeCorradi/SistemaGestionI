'use client';

/**
 * Perfil del usuario autenticado (roadmap §18 — Profile): cuenta interna,
 * sesión de Firebase y permisos vigentes. Cerrar sesión revoca la sesión
 * de Firebase y devuelve al login.
 */

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/features/auth/auth-provider';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { LoadingSpinner } from '@/components/ui/loading-spinner';
import { PanelCard } from '@/components/ui/cards';
import { StatusBadge } from '@/components/ui/status-badge';

export default function PerfilPage() {
  const router = useRouter();
  const { firebaseUser, profile, account, permissions, profileLoading, logout } = useAuth();
  const [loggingOut, setLoggingOut] = useState(false);

  async function onLogout() {
    setLoggingOut(true);
    try {
      await logout();
      router.replace('/login');
    } finally {
      setLoggingOut(false);
    }
  }

  if (profileLoading) {
    return <LoadingSpinner label="Cargando perfil..." />;
  }

  if (!profile) {
    return (
      <AppAlert variant="warning" title="Sin perfil">
        No se pudieron cargar los datos de la cuenta. Recargá la página o volvé a iniciar sesión.
      </AppAlert>
    );
  }

  const nombreCompleto = account ? `${account.nombre} ${account.apellido}` : '—';

  return (
    <div>
      <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-4">
        <div>
          <h1 className="h3 mb-1">Mi perfil</h1>
          <p className="text-muted mb-0">Datos de la cuenta y permisos vigentes.</p>
        </div>
        <AppButton
          variant="outline-danger"
          loading={loggingOut}
          icon={<i className="bi bi-box-arrow-right" aria-hidden="true" />}
          onClick={onLogout}
        >
          Cerrar sesión
        </AppButton>
      </div>

      <div className="row g-3">
        <div className="col-lg-6">
          <PanelCard title="Cuenta interna">
            <table className="table table-borderless align-middle mb-0">
              <tbody>
                <tr>
                  <th className="text-body-secondary small text-uppercase w-40">Nombre</th>
                  <td>{nombreCompleto}</td>
                </tr>
                <tr>
                  <th className="text-body-secondary small text-uppercase">Email</th>
                  <td>{account?.email ?? '—'}</td>
                </tr>
                <tr>
                  <th className="text-body-secondary small text-uppercase">Rol</th>
                  <td>
                    {account ? (
                      <span className="badge text-bg-primary">{account.rol}</span>
                    ) : (
                      '—'
                    )}
                  </td>
                </tr>
                <tr>
                  <th className="text-body-secondary small text-uppercase">Estado</th>
                  <td>{account ? <StatusBadge active={account.activo} /> : '—'}</td>
                </tr>
              </tbody>
            </table>
          </PanelCard>
        </div>

        <div className="col-lg-6">
          <PanelCard title="Sesión">
            <table className="table table-borderless align-middle mb-0">
              <tbody>
                <tr>
                  <th className="text-body-secondary small text-uppercase w-40">Email (Firebase)</th>
                  <td>{profile.email ?? '—'}</td>
                </tr>
                <tr>
                  <th className="text-body-secondary small text-uppercase">Email verificado</th>
                  <td>
                    {profile.emailVerified ? (
                      <span className="badge text-bg-success">Sí</span>
                    ) : (
                      <span className="badge text-bg-secondary">No</span>
                    )}
                  </td>
                </tr>
                <tr>
                  <th className="text-body-secondary small text-uppercase">UID</th>
                  <td className="text-break font-monospace small">{profile.uid}</td>
                </tr>
              </tbody>
            </table>
          </PanelCard>
        </div>

        <div className="col-12">
          <PanelCard title={`Permisos vigentes (${permissions.length})`}>
            <div className="p-3">
              {permissions.length === 0 ? (
                <p className="text-muted mb-0">
                  Tu usuario no tiene permisos asignados. Contactá al administrador.
                </p>
              ) : (
                <div className="d-flex flex-wrap gap-2">
                  {permissions.map((codigo) => (
                    <code key={codigo} className="badge text-bg-light border">
                      {codigo}
                    </code>
                  ))}
                </div>
              )}
              <p className="text-muted small mt-3 mb-0">
                Estos permisos provienen del backend y solo se muestran aquí; la autorización real
                siempre la valida la API.
              </p>
            </div>
          </PanelCard>
        </div>
      </div>

      {firebaseUser ? null : (
        <AppAlert variant="warning" title="Sesión de Firebase">
          No hay sesión de Firebase activa en este navegador.
        </AppAlert>
      )}
    </div>
  );
}
