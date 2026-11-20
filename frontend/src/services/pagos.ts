import { apiRequest } from './api-client';
import type {
  ListParams,
  OrderPayments,
  PagedResult,
  PaymentItem,
  RegisterPaymentInput,
} from '@/types/api';

/** Parámetros del listado global (GET /api/v1/payments). */
export interface PaymentListParams extends ListParams {
  orderId?: number;
  customerId?: number;
  dateFrom?: string;
  dateTo?: string;
}

/**
 * Listado paginado global de pagos con filtros de pedido, cliente y
 * fechas (api-spec §17). Requiere PAGOS_VIEW.
 */
export function listPayments(
  params: PaymentListParams = {},
  signal?: AbortSignal
): Promise<PagedResult<PaymentItem>> {
  return apiRequest<PagedResult<PaymentItem>>('/api/v1/payments', {
    query: {
      page: params.page,
      pageSize: params.pageSize,
      search: params.search,
      orderId: params.orderId,
      customerId: params.customerId,
      dateFrom: params.dateFrom,
      dateTo: params.dateTo,
    },
    signal,
  });
}

/**
 * Pagos de un pedido con total pagado y saldo pendiente calculados por el
 * backend (GET /api/v1/orders/{id}/payments, BR-083). Requiere
 * PAGOS_VIEW.
 */
export function getOrderPayments(
  orderId: number,
  signal?: AbortSignal
): Promise<OrderPayments> {
  return apiRequest<OrderPayments>(`/api/v1/orders/${orderId}/payments`, { signal });
}

/**
 * Registra un pago contra un pedido (POST /api/v1/orders/{id}/payments).
 * El backend valida monto positivo (BR-080) y saldo (BR-081 → 409);
 * requiere PAGOS_CREATE. El historial es inmutable (BR-082).
 */
export function registerPayment(
  orderId: number,
  input: RegisterPaymentInput
): Promise<PaymentItem> {
  return apiRequest<PaymentItem>(`/api/v1/orders/${orderId}/payments`, {
    method: 'POST',
    body: input,
  });
}
