'use client';

/**
 * Render condicional según permisos (AGENTS.md §8): mejora la UX ocultando
 * funcionalidades, pero la seguridad real siempre es del backend (BR-003).
 */

import type { ReactNode } from 'react';
import { useAuth } from './auth-provider';

interface PermissionGateProps {
  permission: string;
  children: ReactNode;
  /** Contenido alternativo cuando no hay permiso (por defecto nada). */
  fallback?: ReactNode;
}

export function PermissionGate({ permission, children, fallback = null }: PermissionGateProps) {
  const { can } = useAuth();
  return <>{can(permission) ? children : fallback}</>;
}
