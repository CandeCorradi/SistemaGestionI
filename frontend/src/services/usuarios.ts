import { apiRequest } from './api-client';
import type {
  CreateUsuarioInput,
  ListParams,
  PagedResult,
  PermisosUsuario,
  UpdatePermisosUsuarioInput,
  UpdateUsuarioInput,
  Usuario,
  UsuarioList,
} from '@/types/api';

export function listUsuarios(
  params: ListParams = {},
  signal?: AbortSignal
): Promise<PagedResult<UsuarioList>> {
  return apiRequest<PagedResult<UsuarioList>>('/api/v1/users', {
    query: {
      page: params.page,
      pageSize: params.pageSize,
      search: params.search,
    },
    signal,
  });
}

export function getUsuario(id: number, signal?: AbortSignal): Promise<Usuario> {
  return apiRequest<Usuario>(`/api/v1/users/${id}`, { signal });
}

export function createUsuario(input: CreateUsuarioInput): Promise<Usuario> {
  return apiRequest<Usuario>('/api/v1/users', { method: 'POST', body: input });
}

export function updateUsuario(id: number, input: UpdateUsuarioInput): Promise<Usuario> {
  return apiRequest<Usuario>(`/api/v1/users/${id}`, { method: 'PATCH', body: input });
}

/** Permisos explícitos del usuario (adicionales a los de su rol). */
export function getPermisosUsuario(
  id: number,
  signal?: AbortSignal
): Promise<PermisosUsuario> {
  return apiRequest<PermisosUsuario>(`/api/v1/users/${id}/permissions`, { signal });
}

/** Reemplaza el conjunto completo de permisos explícitos (PUT). */
export function updatePermisosUsuario(
  id: number,
  input: UpdatePermisosUsuarioInput
): Promise<PermisosUsuario> {
  return apiRequest<PermisosUsuario>(`/api/v1/users/${id}/permissions`, {
    method: 'PUT',
    body: input,
  });
}
