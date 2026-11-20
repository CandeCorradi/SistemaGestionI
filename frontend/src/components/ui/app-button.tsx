'use client';

import { type ButtonHTMLAttributes, type ReactNode } from 'react';

type ButtonVariant =
  | 'primary'
  | 'secondary'
  | 'success'
  | 'outline-secondary'
  | 'outline-danger'
  | 'danger'
  | 'link';

interface AppButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  loading?: boolean;
  icon?: ReactNode;
}

/** Botón reutilizable del design system (roadmap §17). */
export function AppButton({
  variant = 'primary',
  loading = false,
  icon,
  children,
  className = '',
  disabled,
  type = 'button',
  ...rest
}: AppButtonProps) {
  return (
    <button
      type={type}
      className={`btn btn-${variant} ${className}`.trim()}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      {...rest}
    >
      {loading ? (
        <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true" />
      ) : (
        icon
      )}
      {children}
    </button>
  );
}
