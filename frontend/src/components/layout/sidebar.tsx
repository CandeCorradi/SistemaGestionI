'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';

interface NavItem {
  href: string;
  label: string;
  icon: string;
  /** Permiso requerido para ver el ítem (UX; el backend valida siempre). */
  permission?: string;
}

/**
 * Navegación del panel. Los ítems se ocultan sin permiso (AGENTS.md §8:
 * esto solo mejora la UX, la autorización real es del backend).
 */
const NAV_ITEMS: NavItem[] = [
  { href: '/dashboard', label: 'Dashboard', icon: 'bi-speedometer2' },
  { href: '/clientes', label: 'Clientes', icon: 'bi-people', permission: 'CLIENTES_VIEW' },
  {
    href: '/proveedores',
    label: 'Proveedores',
    icon: 'bi-truck',
    permission: 'PROVEEDORES_VIEW',
  },
  { href: '/productos', label: 'Productos', icon: 'bi-box-seam', permission: 'PRODUCTOS_VIEW' },
  { href: '/stock', label: 'Stock', icon: 'bi-boxes', permission: 'STOCK_VIEW' },
  {
    href: '/presupuestos',
    label: 'Presupuestos',
    icon: 'bi-file-earmark-text',
    permission: 'PRESUPUESTOS_VIEW',
  },
  { href: '/pedidos', label: 'Pedidos', icon: 'bi-cart-check', permission: 'PEDIDOS_VIEW' },
  { href: '/pagos', label: 'Pagos', icon: 'bi-cash-coin', permission: 'PAGOS_VIEW' },
  { href: '/facturas', label: 'Facturas', icon: 'bi-file-earmark-medical', permission: 'FACTURAS_VIEW' },
  {
    href: '/trabajos',
    label: 'Trabajos realizados',
    icon: 'bi-hammer',
    permission: 'TRABAJOS_VIEW',
  },
  { href: '/reportes', label: 'Reportes', icon: 'bi-bar-chart-line', permission: 'REPORTES_VIEW' },
  { href: '/usuarios', label: 'Usuarios', icon: 'bi-person-gear', permission: 'USUARIOS_VIEW' },
  { href: '/roles', label: 'Roles', icon: 'bi-shield-lock', permission: 'ROLES_VIEW' },
  { href: '/notificaciones', label: 'Notificaciones', icon: 'bi-bell' },
  { href: '/perfil', label: 'Mi perfil', icon: 'bi-person-badge' },
];

interface SidebarProps {
  can: (permission: string) => boolean;
  open: boolean;
  onClose: () => void;
}

export function Sidebar({ can, open, onClose }: SidebarProps) {
  const pathname = usePathname();
  const items = NAV_ITEMS.filter((item) => !item.permission || can(item.permission));

  return (
    <>
      {open ? (
        <div
          className="position-fixed w-100 h-100 d-md-none"
          style={{ zIndex: 1030, backgroundColor: 'rgba(0,0,0,0.4)' }}
          onClick={onClose}
          aria-hidden="true"
        />
      ) : null}
      <aside
        className={`admin-sidebar d-flex flex-column p-3 ${open ? 'open' : ''}`.trim()}
        aria-label="Navegación principal"
      >
        <Link href="/dashboard" className="d-flex align-items-center mb-3 text-white fw-semibold">
          <i className="bi bi-hammer me-2" aria-hidden="true" />
          <span>Sistema de Gestión</span>
        </Link>
        <nav className="nav flex-column gap-1">
          {items.map((item) => {
            const active = pathname === item.href || pathname.startsWith(`${item.href}/`);
            return (
              <Link
                key={item.href}
                href={item.href}
                className={`nav-link ${active ? 'active' : ''}`.trim()}
                onClick={onClose}
              >
                <i className={`bi ${item.icon} me-2`} aria-hidden="true" />
                {item.label}
              </Link>
            );
          })}
        </nav>
        <div className="mt-auto pt-3 small text-secondary">
          Carpintería · v0.1
        </div>
      </aside>
    </>
  );
}
