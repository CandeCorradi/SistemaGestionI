import { apiRequest } from './api-client';
import type { CatalogoItem, CatalogoProducto, PagedResult, TipoProducto } from '@/types/api';

/**
 * Catálogo público (api-spec §12A, functional-spec §9): endpoints
 * anónimos que no envían token de autorización.
 */

export interface CatalogoParams {
  page?: number;
  pageSize?: number;
  search?: string;
  tipo?: TipoProducto;
  category?: string;
}

export function listCatalogo(
  params: CatalogoParams = {},
  signal?: AbortSignal
): Promise<PagedResult<CatalogoItem>> {
  return apiRequest<PagedResult<CatalogoItem>>('/api/v1/catalog/products', {
    query: {
      page: params.page,
      pageSize: params.pageSize,
      search: params.search,
      type: params.tipo,
      category: params.category,
    },
    signal,
  });
}

export function getCatalogo(id: number, signal?: AbortSignal): Promise<CatalogoProducto> {
  return apiRequest<CatalogoProducto>(`/api/v1/catalog/products/${id}`, { signal });
}
