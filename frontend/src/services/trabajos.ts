import { apiRequest, apiUpload } from './api-client';
import type {
  CreateTrabajoInput,
  ListParams,
  PagedResult,
  TrabajoImagen,
  TrabajoList,
  TrabajoRealizado,
  UpdateTrabajoInput,
} from '@/types/api';

/**
 * Gestión administrativa de trabajos realizados (api-spec §12C,
 * AGENTS.md §23): CRUD con permisos TRABAJOS_* y galería de imágenes.
 */

export interface TrabajoListParams extends ListParams {
  published?: boolean;
  featured?: boolean;
  category?: string;
}

export function listTrabajos(
  params: TrabajoListParams = {},
  signal?: AbortSignal
): Promise<PagedResult<TrabajoList>> {
  return apiRequest<PagedResult<TrabajoList>>('/api/v1/trabajos-realizados', {
    query: {
      page: params.page,
      pageSize: params.pageSize,
      search: params.search,
      published: params.published,
      featured: params.featured,
      category: params.category,
    },
    signal,
  });
}

export function getTrabajo(id: number, signal?: AbortSignal): Promise<TrabajoRealizado> {
  return apiRequest<TrabajoRealizado>(`/api/v1/trabajos-realizados/${id}`, { signal });
}

export function createTrabajo(input: CreateTrabajoInput): Promise<TrabajoRealizado> {
  return apiRequest<TrabajoRealizado>('/api/v1/trabajos-realizados', {
    method: 'POST',
    body: input,
  });
}

export function updateTrabajo(id: number, input: UpdateTrabajoInput): Promise<TrabajoRealizado> {
  return apiRequest<TrabajoRealizado>(`/api/v1/trabajos-realizados/${id}`, {
    method: 'PATCH',
    body: input,
  });
}

/** Borrado físico (la vidriera no es historial crítico, BR-161). */
export function deleteTrabajo(id: number): Promise<void> {
  return apiRequest<void>(`/api/v1/trabajos-realizados/${id}`, { method: 'DELETE' });
}

/* ------------------------------------------------------------------ */
/* Imágenes (api-spec §13, BR-100/BR-101)                              */
/* ------------------------------------------------------------------ */

export function listTrabajoImagenes(
  id: number,
  signal?: AbortSignal
): Promise<TrabajoImagen[]> {
  return apiRequest<TrabajoImagen[]>(`/api/v1/trabajos-realizados/${id}/images`, { signal });
}

/** Sube una imagen (≤ 5 MB, JPG/PNG/WEBP/GIF) al storage. */
export function subirTrabajoImagen(
  id: number,
  file: File,
  signal?: AbortSignal
): Promise<TrabajoImagen> {
  return apiUpload<TrabajoImagen>(`/api/v1/trabajos-realizados/${id}/images`, file, signal);
}

export function eliminarTrabajoImagen(id: number, imageId: number): Promise<void> {
  return apiRequest<void>(`/api/v1/trabajos-realizados/${id}/images/${imageId}`, {
    method: 'DELETE',
  });
}
