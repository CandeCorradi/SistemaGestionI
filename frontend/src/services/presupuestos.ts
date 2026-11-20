import { apiRequest } from './api-client';
import type {
  CreateQuoteInput,
  EstadoPresupuesto,
  ListParams,
  PagedResult,
  Quote,
  QuoteListItem,
  UpdateQuoteInput,
} from '@/types/api';

/** Parámetros del listado (GET /api/v1/quotes). */
export interface QuoteListParams extends ListParams {
  customerId?: number;
  status?: EstadoPresupuesto;
  dateFrom?: string;
  dateTo?: string;
}

export function listQuotes(
  params: QuoteListParams = {},
  signal?: AbortSignal
): Promise<PagedResult<QuoteListItem>> {
  return apiRequest<PagedResult<QuoteListItem>>('/api/v1/quotes', {
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

export function getQuote(id: number, signal?: AbortSignal): Promise<Quote> {
  return apiRequest<Quote>(`/api/v1/quotes/${id}`, { signal });
}

/**
 * Crea un presupuesto en borrador (BR-060/061). Los totales los calcula el
 * backend (BR-062) y las líneas con producto congelan el precio vigente
 * (BR-063).
 */
export function createQuote(input: CreateQuoteInput): Promise<Quote> {
  return apiRequest<Quote>('/api/v1/quotes', {
    method: 'POST',
    body: input,
  });
}

/** Editar (sólo en borrador; PATCH). `items` reemplaza todas las líneas. */
export function updateQuote(id: number, input: UpdateQuoteInput): Promise<Quote> {
  return apiRequest<Quote>(`/api/v1/quotes/${id}`, {
    method: 'PATCH',
    body: input,
  });
}

/** Borrador → Enviado. */
export function sendQuote(id: number): Promise<Quote> {
  return apiRequest<Quote>(`/api/v1/quotes/${id}/send`, { method: 'POST' });
}

/** Enviado → Rechazado (motivo opcional). */
export function rejectQuote(id: number, reason?: string): Promise<Quote> {
  return apiRequest<Quote>(`/api/v1/quotes/${id}/reject`, {
    method: 'POST',
    body: { reason: reason ?? null },
  });
}

/** Borrador/Enviado → Anulado (motivo opcional). */
export function cancelQuote(id: number, reason?: string): Promise<Quote> {
  return apiRequest<Quote>(`/api/v1/quotes/${id}/cancel`, {
    method: 'POST',
    body: { reason: reason ?? null },
  });
}

/**
 * Acepta el presupuesto y crea su pedido en una única transacción
 * (BR-064/BR-065). Devuelve el presupuesto con `status = 2` y el nuevo
 * `orderId`; `409` si no es aceptable o si otro proceso lo aceptó primero.
 */
export function acceptQuote(id: number): Promise<Quote> {
  return apiRequest<Quote>(`/api/v1/quotes/${id}/accept`, { method: 'POST' });
}
