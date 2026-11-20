import { apiRequest } from './api-client';
import type {
  ChangeOrderStatusInput,
  EstadoPedido,
  ListParams,
  Order,
  OrderHistoryItem,
  OrderListItem,
  PagedResult,
  UpdateOrderInput,
} from '@/types/api';

/** Parámetros del listado (GET /api/v1/orders). */
export interface OrderListParams extends ListParams {
  customerId?: number;
  status?: EstadoPedido;
  dateFrom?: string;
  dateTo?: string;
}

export function listOrders(
  params: OrderListParams = {},
  signal?: AbortSignal
): Promise<PagedResult<OrderListItem>> {
  return apiRequest<PagedResult<OrderListItem>>('/api/v1/orders', {
    query: {
      page: params.page,
      pageSize: params.pageSize,
      search: params.search,
      customerId: params.customerId,
      status: params.status,
      dateFrom: params.dateFrom,
      dateTo: params.dateTo,
    },
    signal,
  });
}

/** Detalle con las líneas del snapshot del presupuesto aceptado (BR-071). */
export function getOrder(id: number, signal?: AbortSignal): Promise<Order> {
  return apiRequest<Order>(`/api/v1/orders/${id}`, { signal });
}

/** Historial inmutable, más reciente primero (BR-075). */
export function getOrderHistory(
  id: number,
  signal?: AbortSignal
): Promise<OrderHistoryItem[]> {
  return apiRequest<OrderHistoryItem[]>(`/api/v1/orders/${id}/history`, { signal });
}

/**
 * Cambia el estado (POST /api/v1/orders/{id}/status). El backend valida la
 * transición (BR-072) y registra el historial (BR-075); requiere
 * PEDIDOS_UPDATE, o PEDIDOS_CANCEL para anular (BR-003).
 */
export function changeOrderStatus(id: number, input: ChangeOrderStatusInput): Promise<Order> {
  return apiRequest<Order>(`/api/v1/orders/${id}/status`, {
    method: 'POST',
    body: input,
  });
}

/**
 * Edita la información de seguimiento editable (PATCH /api/v1/orders/{id},
 * BR-076): notas y entrega estimada. Requiere PEDIDOS_UPDATE; los estados
 * finales devuelven 409 (BR-074).
 */
export function updateOrder(id: number, input: UpdateOrderInput): Promise<Order> {
  return apiRequest<Order>(`/api/v1/orders/${id}`, {
    method: 'PATCH',
    body: input,
  });
}
