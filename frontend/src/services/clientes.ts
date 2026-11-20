import { apiRequest } from './api-client';
import type {
  Cliente,
  ClienteList,
  CreateClienteInput,
  ListParams,
  PagedResult,
  UpdateClienteInput,
} from '@/types/api';

export interface ClienteListParams extends ListParams {
  /** Filtra por estado: true = activos, false = inactivos. */
  active?: boolean;
}

export function listClientes(
  params: ClienteListParams = {},
  signal?: AbortSignal
): Promise<PagedResult<ClienteList>> {
  return apiRequest<PagedResult<ClienteList>>('/api/v1/customers', {
    query: {
      page: params.page,
      pageSize: params.pageSize,
      search: params.search,
      active: params.active,
    },
    signal,
  });
}

export function getCliente(id: number, signal?: AbortSignal): Promise<Cliente> {
  return apiRequest<Cliente>(`/api/v1/customers/${id}`, { signal });
}

export function createCliente(input: CreateClienteInput): Promise<Cliente> {
  return apiRequest<Cliente>('/api/v1/customers', { method: 'POST', body: input });
}

export function updateCliente(id: number, input: UpdateClienteInput): Promise<Cliente> {
  return apiRequest<Cliente>(`/api/v1/customers/${id}`, { method: 'PATCH', body: input });
}

/** Desactivación lógica (BR-011). */
export function deactivateCliente(id: number): Promise<Cliente> {
  return apiRequest<Cliente>(`/api/v1/customers/${id}`, { method: 'DELETE' });
}
