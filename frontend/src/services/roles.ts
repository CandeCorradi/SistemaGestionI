import { apiRequest } from './api-client';
import type { Rol } from '@/types/api';

/** Roles fijos del sistema (BR-001; GET /api/v1/roles). */
export function listRoles(signal?: AbortSignal): Promise<Rol[]> {
  return apiRequest<Rol[]>('/api/v1/roles', { signal });
}
