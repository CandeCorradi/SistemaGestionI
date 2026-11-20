import { apiRequest } from './api-client';
import type {
  ClienteProfile,
  ClienteSummary,
  EstadoPedido,
  EstadoPresupuesto,
  ListParams,
  Order,
  OrderHistoryItem,
  OrderListItem,
  OrderPayments,
  PagedResult,
  PaymentItem,
  Quote,
  QuoteListItem,
  RequestQuoteInput,
  UpdateProfileInput,
} from '@/types/api';

/**
 * Portal Cliente (api-spec §9B): todos los endpoints operan sobre los
 * recursos del propio cliente (`/api/v1/my/*`); la identidad la resuelve
 * el backend desde el token (BR-010) y aplica ownership estricto (404
 * para recursos ajenos).
 */

// --- Perfil -----------------------------------------------------------

export function getProfile(signal?: AbortSignal): Promise<ClienteProfile> {
  return apiRequest<ClienteProfile>('/api/v1/my/profile', { signal });
}

/** Actualiza nombre, apellido y contacto (el email no se modifica acá). */
export function updateProfile(input: UpdateProfileInput): Promise<ClienteProfile> {
  return apiRequest<ClienteProfile>('/api/v1/my/profile', {
    method: 'PATCH',
    body: input,
  });
}

// --- Resumen ----------------------------------------------------------

export function getSummary(signal?: AbortSignal): Promise<ClienteSummary> {
  return apiRequest<ClienteSummary>('/api/v1/my/summary', { signal });
}

// --- Presupuestos -----------------------------------------------------

export interface MyQuoteListParams extends ListParams {
  status?: EstadoPresupuesto;
}

export function listMyQuotes(
  params: MyQuoteListParams = {},
  signal?: AbortSignal
): Promise<PagedResult<QuoteListItem>> {
  return apiRequest<PagedResult<QuoteListItem>>('/api/v1/my/quotes', {
    query: {
      page: params.page,
      pageSize: params.pageSize,
      search: params.search,
      status: params.status,
    },
    signal,
  });
}

export function getMyQuote(id: number, signal?: AbortSignal): Promise<Quote> {
  return apiRequest<Quote>(`/api/v1/my/quotes/${id}`, { signal });
}

/**
 * Solicita un presupuesto describiendo el trabajo: crea el presupuesto en
 * borrador con un ítem sin precio que el equipo cotiza.
 */
export function requestQuote(input: RequestQuoteInput): Promise<Quote> {
  return apiRequest<Quote>('/api/v1/my/quotes', {
    method: 'POST',
    body: { description: input.description, cantidad: input.cantidad ?? 1 },
  });
}

/**
 * Acepta el presupuesto y crea el pedido en una única transacción
 * (BR-064/BR-065); 409 si no es aceptable o si otro proceso lo aceptó.
 */
export function acceptMyQuote(id: number): Promise<Quote> {
  return apiRequest<Quote>(`/api/v1/my/quotes/${id}/accept`, { method: 'POST' });
}

// --- Pedidos ----------------------------------------------------------

export interface MyOrderListParams extends ListParams {
  status?: EstadoPedido;
}

export function listMyOrders(
  params: MyOrderListParams = {},
  signal?: AbortSignal
): Promise<PagedResult<OrderListItem>> {
  return apiRequest<PagedResult<OrderListItem>>('/api/v1/my/orders', {
    query: {
      page: params.page,
      pageSize: params.pageSize,
      search: params.search,
      status: params.status,
    },
    signal,
  });
}

export function getMyOrder(id: number, signal?: AbortSignal): Promise<Order> {
  return apiRequest<Order>(`/api/v1/my/orders/${id}`, { signal });
}

/** Historial de estados del pedido propio (seguimiento, BR-075). */
export function getMyOrderHistory(
  id: number,
  signal?: AbortSignal
): Promise<OrderHistoryItem[]> {
  return apiRequest<OrderHistoryItem[]>(`/api/v1/my/orders/${id}/history`, { signal });
}

/** Pagos de un pedido propio con total pagado y saldo (BR-083). */
export function getMyOrderPayments(
  id: number,
  signal?: AbortSignal
): Promise<OrderPayments> {
  return apiRequest<OrderPayments>(`/api/v1/my/orders/${id}/payments`, { signal });
}

// --- Pagos ------------------------------------------------------------

export function listMyPayments(
  params: ListParams = {},
  signal?: AbortSignal
): Promise<PagedResult<PaymentItem>> {
  return apiRequest<PagedResult<PaymentItem>>('/api/v1/my/payments', {
    query: { page: params.page, pageSize: params.pageSize },
    signal,
  });
}
