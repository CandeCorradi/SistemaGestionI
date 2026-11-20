import { apiRequest } from './api-client';
import type {
  AuthProfile,
  AuthUser,
  PermisosActuales,
  RegisterInput,
  RegisterResult,
} from '@/types/api';

/** Identidad autenticada del token actual. */
export function getMe(signal?: AbortSignal): Promise<AuthUser> {
  return apiRequest<AuthUser>('/api/v1/auth/me', { signal });
}

/** Permisos efectivos (rol ∪ explícitos) para ocultar UI (BR-003). */
export function getMyPermissions(signal?: AbortSignal): Promise<PermisosActuales> {
  return apiRequest<PermisosActuales>('/api/v1/auth/permissions', { signal });
}

/** Perfil del usuario interno vinculado al token (roadmap §18). */
export function getMyProfile(signal?: AbortSignal): Promise<AuthProfile> {
  return apiRequest<AuthProfile>('/api/v1/auth/profile', { signal });
}

/** Autorregistro de cuenta con rol Cliente (api-spec §3). Público. */
export function register(input: RegisterInput): Promise<RegisterResult> {
  return apiRequest<RegisterResult>('/api/v1/auth/register', {
    method: 'POST',
    body: input,
  });
}

/**
 * Aprovisiona la cuenta interna de una identidad externa ya autenticada
 * (Google). El backend toma el UID y el email del token verificado y crea o
 * vincula el usuario interno con rol Cliente. Idempotente.
 */
export function provisionGoogleAccount(): Promise<AuthProfile> {
  return apiRequest<AuthProfile>('/api/v1/auth/google', { method: 'POST' });
}
