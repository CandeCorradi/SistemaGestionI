'use client';

/**
 * Dashboard del panel (functional-spec §22, api-spec §19): indicadores de
 * negocio filtrados por permisos (el backend sólo devuelve los bloques
 * permitidos), estado de la API, información de la sesión y accesos
 * rápidos.
 */

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useAuth } from '@/features/auth/auth-provider';
import { useApiHealth } from '@/hooks/useApiHealth';
import { getDashboard } from '@/services/dashboard';
import { formatMoney } from '@/lib/format';
import type { DashboardData } from '@/types/api';
import { StatCard } from '@/components/ui/cards';
import { AppAlert } from '@/components/ui/app-alert';
import { LoadingSpinner } from '@/components/ui/loading-spinner';

interface QuickLink {
  href: string;
  label: string;
  description: string;
  icon: string;
  tone: string;
  permission: string;
}

const QUICK_LINKS: QuickLink[] = [
  {
    href: '/clientes',
    label: 'Clientes',
    description: 'ABM y datos de contacto',
    icon: 'bi-people-fill',
    tone: 'bg-primary',
    permission: 'CLIENTES_VIEW',
  },
  {
    href: '/proveedores',
    label: 'Proveedores',
    description: 'Melaminas, accesorios e insumos',
    icon: 'bi-truck',
    tone: 'bg-success',
    permission: 'PROVEEDORES_VIEW',
  },
  {
    href: '/productos',
    label: 'Productos',
    description: 'Catálogo, precios y stock',
    icon: 'bi-box-seam',
    tone: 'bg-warning',
    permission: 'PRODUCTOS_VIEW',
  },
];

export default function DashboardPage() {
  const { profile, permissions, can } = useAuth();
  const { status } = useApiHealth();

  const [data, setData] = useState<DashboardData | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    const controller = new AbortController();

    const load = async () => {
      try {
        const result = await getDashboard(controller.signal);
        if (!cancelled) {
          setData(result);
          setError(null);
        }
      } catch (err) {
        if (!cancelled && (err as { name?: string }).name !== 'AbortError') {
          setError(err instanceof Error ? err.message : 'No se pudieron cargar los indicadores.');
        }
      }
    };

    void load();
    return () => {
      cancelled = true;
      controller.abort();
    };
  }, []);

  const statusLabel =
    status === 'healthy'
      ? 'Operativa'
      : status === 'degraded'
        ? 'Degradada'
        : status === 'error'
          ? 'Sin conexión'
          : 'Verificando...';

  const statusTone =
    status === 'healthy'
      ? 'bg-success'
      : status === 'error'
        ? 'bg-danger'
        : status === 'degraded'
          ? 'bg-warning'
          : 'bg-secondary';

  const links = QUICK_LINKS.filter((link) => can(link.permission));

  const orders = data?.orders ?? null;
  const quotes = data?.quotes ?? null;
  const sales = data?.sales ?? null;
  const payments = data?.payments ?? null;
  const inventory = data?.inventory ?? null;
  const notifications = data?.notifications ?? null;

  const firstLoad = data === null && error === null;
  const hasBusinessBlocks =
    Boolean(orders || quotes || sales || payments || inventory) || notifications !== null;

  return (
    <>
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-2">
        <div>
          <h1 className="h4 mb-1">Dashboard</h1>
          <p className="text-muted mb-0 small">
            Bienvenido{profile?.email ? `: ${profile.email}` : ''}
          </p>
        </div>
      </div>

      {firstLoad ? (
        <LoadingSpinner label="Cargando indicadores..." />
      ) : error ? (
        <AppAlert variant="danger" title="Error al cargar indicadores">
          {error}
        </AppAlert>
      ) : hasBusinessBlocks ? (
        <div className="row g-3 mb-4">
          {notifications ? (
            <div className="col-12 col-md-6 col-xl-3">
              <StatCard
                title="Notificaciones sin leer"
                value={notifications.unreadCount}
                icon="bi-bell"
                tone={notifications.unreadCount > 0 ? 'bg-danger' : 'bg-secondary'}
                footer="Tuyas: las notificaciones son privadas (BR-090)"
              />
            </div>
          ) : null}
          {orders ? (
            <>
              <div className="col-12 col-md-6 col-xl-3">
                <StatCard
                  title="Pedidos en curso"
                  value={
                    orders.pendientes + orders.confirmados + orders.enProduccion + orders.listos
                  }
                  icon="bi-cart-check"
                  tone="bg-primary"
                  footer={`${orders.pendientes} pend. · ${orders.confirmados} conf. · ${orders.enProduccion} prod. · ${orders.listos} listos`}
                />
              </div>
              <div className="col-12 col-md-6 col-xl-3">
                <StatCard
                  title="Entregados este mes"
                  value={orders.entregadosMes}
                  icon="bi-box-seam"
                  tone="bg-success"
                  footer={`${orders.canceladosMes} cancelado${orders.canceladosMes === 1 ? '' : 's'} este mes`}
                />
              </div>
            </>
          ) : null}
          {quotes ? (
            <div className="col-12 col-md-6 col-xl-3">
              <StatCard
                title="Presupuestos sin respuesta"
                value={quotes.enviados}
                icon="bi-file-earmark-text"
                tone="bg-info"
                footer={`${quotes.borradores} borrador${quotes.borradores === 1 ? '' : 'es'} · ${quotes.aceptadosMes} aceptados este mes`}
              />
            </div>
          ) : null}
          {sales ? (
            <div className="col-12 col-md-6 col-xl-3">
              <StatCard
                title="Facturado este mes"
                value={formatMoney(sales.facturadoMes)}
                icon="bi-graph-up-arrow"
                tone="bg-dark"
                footer={`${sales.facturasMes} factura${sales.facturasMes === 1 ? '' : 's'} emitida${sales.facturasMes === 1 ? '' : 's'}`}
              />
            </div>
          ) : null}
          {payments ? (
            <div className="col-12 col-md-6 col-xl-3">
              <StatCard
                title="Cobrado este mes"
                value={formatMoney(payments.cobradoMes)}
                icon="bi-cash-coin"
                tone="bg-success"
                footer={`${payments.pagosMes} pago${payments.pagosMes === 1 ? '' : 's'} registrado${payments.pagosMes === 1 ? '' : 's'}`}
              />
            </div>
          ) : null}
          {inventory ? (
            <div className="col-12 col-md-6 col-xl-3">
              <StatCard
                title="Productos con stock bajo"
                value={inventory.stockBajo}
                icon="bi-exclamation-triangle"
                tone={inventory.stockBajo > 0 ? 'bg-warning' : 'bg-secondary'}
                footer={`${inventory.productosActivos} activos · ${inventory.enShowroom} en showroom`}
              />
            </div>
          ) : null}
        </div>
      ) : null}

      <h2 className="h6 text-uppercase text-muted fw-semibold mb-3">Sistema</h2>
      <div className="row g-3 mb-4">
        <div className="col-12 col-md-6 col-xl-4">
          <StatCard
            title="Estado de la API"
            value={statusLabel}
            icon="bi-activity"
            tone={statusTone}
            footer={process.env.NEXT_PUBLIC_API_URL || 'http://localhost:7100'}
          />
        </div>
        <div className="col-12 col-md-6 col-xl-4">
          <StatCard
            title="Permisos efectivos"
            value={permissions.length}
            icon="bi-shield-check"
            tone="bg-info"
            footer="Unión de permisos del rol y permisos explícitos"
          />
        </div>
        <div className="col-12 col-xl-4">
          <StatCard
            title="Sesión"
            value={profile?.emailVerified ? 'Verificada' : 'Activa'}
            icon="bi-person-badge"
            tone="bg-secondary"
            footer={profile?.uid}
          />
        </div>
      </div>

      <h2 className="h6 text-uppercase text-muted fw-semibold mb-3">Accesos rápidos</h2>
      <div className="row g-3">
        {links.map((link) => (
          <div className="col-12 col-md-4" key={link.href}>
            <Link href={link.href} className="text-decoration-none">
              <div className="card h-100 shadow-sm border-0">
                <div className="card-body d-flex align-items-center">
                  <div className={`rounded p-3 me-3 ${link.tone}`}>
                    <i className={`bi ${link.icon} fs-4 text-white`} aria-hidden="true" />
                  </div>
                  <div>
                    <div className="fw-semibold text-dark">{link.label}</div>
                    <div className="small text-muted">{link.description}</div>
                  </div>
                  <i className="bi bi-chevron-right ms-auto text-muted" aria-hidden="true" />
                </div>
              </div>
            </Link>
          </div>
        ))}
        {links.length === 0 ? (
          <div className="col-12">
            <div className="alert alert-warning mb-0">
              Tu usuario no tiene permisos de consulta habilitados. Contactá al administrador.
            </div>
          </div>
        ) : null}
      </div>
    </>
  );
}
