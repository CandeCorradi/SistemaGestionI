import { apiRequest } from './api-client';
import type { ListParams, NotificacionItem, PagedResult } from '@/types/api';

/** Parámetros del listado (GET /api/v1/notifications). */
export interface NotificationListParams extends ListParams {
  /** Filtra sólo las no leídas (BR-091). */
  unreadOnly?: boolean;
}

/**
 * Listado paginado de las notificaciones del usuario autenticado
 * (api-spec §18). El backend sólo devuelve las propias (BR-090).
 */
export function listNotifications(
  params: NotificationListParams = {},
  signal?: AbortSignal
): Promise<PagedResult<NotificacionItem>> {
  return apiRequest<PagedResult<NotificacionItem>>('/api/v1/notifications', {
    query: {
      page: params.page,
      pageSize: params.pageSize,
      unreadOnly: params.unreadOnly,
    },
    signal,
  });
}

/** Conteo de no leídas para el badge (GET /api/v1/notifications/unread-count). */
export function getUnreadCount(signal?: AbortSignal): Promise<{ count: number }> {
  return apiRequest<{ count: number }>('/api/v1/notifications/unread-count', { signal });
}

/** Marca una notificación propia como leída (BR-091). */
export function markNotificationRead(id: number): Promise<void> {
  return apiRequest<void>(`/api/v1/notifications/${id}/read`, { method: 'POST' });
}

/** Marca todas las notificaciones del usuario como leídas. */
export function markAllNotificationsRead(): Promise<{ updated: number }> {
  return apiRequest<{ updated: number }>('/api/v1/notifications/read-all', {
    method: 'POST',
  });
}
