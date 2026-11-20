import { apiRequest } from './api-client';
import type { PermisoCatalogo } from '@/types/api';

/** Catálogo completo de permisos (BR-002; GET /api/v1/permissions). */
export function listPermisosCatalogo(signal?: AbortSignal): Promise<PermisoCatalogo[]> {
  return apiRequest<PermisoCatalogo[]>('/api/v1/permissions', { signal });
}
