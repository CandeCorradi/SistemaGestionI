import { apiRequest } from './api-client';
import type { PagedResult, ShowroomItem, ShowroomTrabajo } from '@/types/api';

/**
 * Vidriera pública de trabajos realizados (api-spec §12B, AGENTS.md
 * §23): endpoints anónimos que sólo devuelven obras publicadas
 * (BR-190) y ocultan el nombre del cliente sin consentimiento (BR-191).
 */

export interface ShowroomParams {
  page?: number;
  pageSize?: number;
  search?: string;
  category?: string;
  type?: string;
}

export function listShowroom(
  params: ShowroomParams = {},
  signal?: AbortSignal
): Promise<PagedResult<ShowroomItem>> {
  return apiRequest<PagedResult<ShowroomItem>>('/api/v1/showroom/works', {
    query: {
      page: params.page,
      pageSize: params.pageSize,
      search: params.search,
      category: params.category,
      type: params.type,
    },
    signal,
  });
}

export function getShowroom(id: number, signal?: AbortSignal): Promise<ShowroomTrabajo> {
  return apiRequest<ShowroomTrabajo>(`/api/v1/showroom/works/${id}`, { signal });
}
