'use client';

import type { ReactNode } from 'react';

interface StatCardProps {
  title: string;
  value: ReactNode;
  icon: string;
  /** Color de Bootstrap para el icono (bg-primary, bg-success...). */
  tone?: string;
  footer?: ReactNode;
}

/** Tarjeta resumen para dashboard (roadmap §17). */
export function StatCard({ title, value, icon, tone = 'bg-primary', footer }: StatCardProps) {
  return (
    <div className="card h-100 shadow-sm">
      <div className="card-body d-flex align-items-start">
        <div className={`rounded p-3 me-3 ${tone}`}>
          <i className={`bi ${icon} fs-4 text-white`} aria-hidden="true" />
        </div>
        <div className="flex-grow-1">
          <div className="text-body-secondary small text-uppercase fw-semibold">{title}</div>
          <div className="fs-4 fw-bold">{value}</div>
          {footer ? <div className="small text-muted mt-1">{footer}</div> : null}
        </div>
      </div>
    </div>
  );
}

interface PanelCardProps {
  title: string;
  actions?: ReactNode;
  children: ReactNode;
}

/** Panel genérico con encabezado (contenedores de listados). */
export function PanelCard({ title, actions, children }: PanelCardProps) {
  return (
    <div className="card shadow-sm">
      <div className="card-header bg-white d-flex flex-wrap align-items-center justify-content-between gap-2">
        <h5 className="mb-0">{title}</h5>
        {actions ? <div className="d-flex gap-2">{actions}</div> : null}
      </div>
      <div className="card-body p-0">{children}</div>
    </div>
  );
}
