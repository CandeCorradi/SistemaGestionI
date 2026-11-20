'use client';

/**
 * Inicio del Portal Cliente (functional-spec §23): resumen con saldo,
 * totales y contadores calculados por el backend, y accesos rápidos a
 * presupuestos, pedidos y pagos.
 */

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useAuth } from '@/features/auth/auth-provider';
import { getSummary } from '@/services/portal';
import { formatMoney } from '@/lib/format';
import type { ClienteSummary } from '@/types/api';
import { StatCard, PanelCard } from '@/components/ui/cards';
import { AppAlert } from '@/components/ui/app-alert';
import { LoadingSpinner } from '@/components/ui/loading-spinner';

interface QuickLink {
  href: string;
  label: string;
  description: string;
  icon: string;
  tone: string;
}

const QUICK_LINKS: QuickLink[] = [
  {
    href: '/portal/presupuestos',
    label: 'Presupuestos',
    description: 'Consultá, solicitá o aceptá presupuestos',
    icon: 'bi-file-earmark-text',
    tone: 'bg-primary',
  },
  {
    href: '/portal/pedidos',
    label: 'Pedidos',
    description: 'Seguí el estado de tus pedidos',
    icon: 'bi-box-seam',
    tone: 'bg-warning',
  },
  {
    href: '/portal/pagos',
    label: 'Pagos',
    description: 'Historial de pagos y saldo',
    icon: 'bi-cash-coin',
    tone: 'bg-success',
  },
];

export default function PortalHomePage() {
  const { account } = useAuth();
  const [summary, setSummary] = useState<ClienteSummary | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    const controller = new AbortController();

    const load = async () => {
      try {
        const result = await getSummary(controller.signal);
        if (!cancelled) {
          setSummary(result);
          setError(null);
        }
      } catch (err) {
        if (!cancelled && (err as { name?: string }).name !== 'AbortError') {
          setError(err instanceof Error ? err.message : 'No se pudo cargar el resumen.');
        }
      }
    };

    void load();
    return () => {
      cancelled = true;
      controller.abort();
    };
  }, []);

  const firstLoad = summary === null && error === null;

  return (
    <>
      <div className="mb-4">
        <h1 className="h4 mb-1">Hola, {account?.nombre ?? '...'}</h1>
        <p className="text-muted mb-0 small">
          Este es el resumen de tu cuenta: saldo, pedidos y presupuestos pendientes (los totales
          los calcula el backend).
        </p>
      </div>

      {error ? (
        <AppAlert variant="danger" title="Error al cargar">
          {error}
        </AppAlert>
      ) : null}

      {firstLoad ? (
        <LoadingSpinner label="Cargando resumen..." />
      ) : summary ? (
        <>
          <div className="row g-3 mb-4">
            <div className="col-12 col-md-6 col-xl-3">
              <StatCard
                title="Saldo pendiente"
                value={formatMoney(summary.saldo)}
                icon="bi-wallet2"
                tone={summary.saldo > 0 ? 'bg-danger' : 'bg-success'}
                footer="Diferencia entre pedidos y pagos"
              />
            </div>
            <div className="col-12 col-md-6 col-xl-3">
              <StatCard
                title="Total pagado"
                value={formatMoney(summary.totalPagado)}
                icon="bi-cash-stack"
                tone="bg-success"
                footer={`${summary.pedidosCount} pedido${summary.pedidosCount === 1 ? '' : 's'}`}
              />
            </div>
            <div className="col-12 col-md-6 col-xl-3">
              <StatCard
                title="Total de pedidos"
                value={formatMoney(summary.totalPedidos)}
                icon="bi-box-seam"
                tone="bg-primary"
                footer="Excluye pedidos cancelados"
              />
            </div>
            <div className="col-12 col-md-6 col-xl-3">
              <StatCard
                title="Presupuestos por responder"
                value={summary.presupuestosPendientes}
                icon="bi-file-earmark-text"
                tone="bg-warning"
                footer="Presupuestos enviados a tu email"
              />
            </div>
          </div>

          <PanelCard title="Accesos rápidos">
            <div className="p-3">
              <div className="row g-3">
                {QUICK_LINKS.map((link) => (
                  <div className="col-12 col-md-4" key={link.href}>
                    <Link
                      href={link.href}
                      className="card h-100 text-decoration-none shadow-sm border"
                    >
                      <div className="card-body d-flex align-items-start gap-3">
                        <span
                          className={`badge ${link.tone} p-2 rounded-circle`}
                          aria-hidden="true"
                        >
                          <i className={link.icon} />
                        </span>
                        <span>
                          <span className="fw-semibold d-block text-body">{link.label}</span>
                          <span className="small text-muted">{link.description}</span>
                        </span>
                      </div>
                    </Link>
                  </div>
                ))}
              </div>
            </div>
          </PanelCard>
        </>
      ) : null}
    </>
  );
}
