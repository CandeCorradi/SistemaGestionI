import { apiRequest } from './api-client';
import type {
  CreateProveedorInput,
  ListParams,
  PagedResult,
  Proveedor,
  ProveedorList,
  UpdateProveedorInput,
} from '@/types/api';

export function listProveedores(
  params: ListParams = {},
  signal?: AbortSignal
): Promise<PagedResult<ProveedorList>> {
  return apiRequest<PagedResult<ProveedorList>>('/api/v1/suppliers', {
    query: {
      page: params.page,
      pageSize: params.pageSize,
      search: params.search,
      active: params.active,
    },
    signal,
  });
}

export function getProveedor(id: number, signal?: AbortSignal): Promise<Proveedor> {
  return apiRequest<Proveedor>(`/api/v1/suppliers/${id}`, { signal });
}

export function createProveedor(input: CreateProveedorInput): Promise<Proveedor> {
  return apiRequest<Proveedor>('/api/v1/suppliers', { method: 'POST', body: input });
}

export function updateProveedor(id: number, input: UpdateProveedorInput): Promise<Proveedor> {
  return apiRequest<Proveedor>(`/api/v1/suppliers/${id}`, { method: 'PATCH', body: input });
}

/** Desactivación lógica (BR-021). */
export function deactivateProveedor(id: number): Promise<Proveedor> {
  return apiRequest<Proveedor>(`/api/v1/suppliers/${id}`, { method: 'DELETE' });
}
