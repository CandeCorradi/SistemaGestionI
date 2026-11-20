'use client';

import type { ReactNode } from 'react';

export type AlertVariant = 'success' | 'danger' | 'warning' | 'info' | 'secondary';

interface AppAlertProps {
  variant?: AlertVariant;
  title?: string;
  children: ReactNode;
  onClose?: () => void;
  className?: string;
}

/** Alerta de Bootstrap para mensajes de éxito/error (roadmap §17). */
export function AppAlert({
  variant = 'info',
  title,
  children,
  onClose,
  className = '',
}: AppAlertProps) {
  const iconMap: Record<AlertVariant, string> = {
    success: 'bi-check-circle-fill',
    danger: 'bi-exclamation-triangle-fill',
    warning: 'bi-exclamation-circle-fill',
    info: 'bi-info-circle-fill',
    secondary: 'bi-info-circle',
  };

  return (
    <div
      className={`alert alert-${variant} d-flex align-items-start ${className}`.trim()}
      role="alert"
    >
      <i className={`bi ${iconMap[variant]} me-2 mt-1`} aria-hidden="true" />
      <div className="flex-grow-1">
        {title ? <strong className="d-block">{title}</strong> : null}
        {children}
      </div>
      {onClose ? (
        <button
          type="button"
          className="btn-close"
          aria-label="Cerrar"
          onClick={onClose}
        />
      ) : null}
    </div>
  );
}
