import { apiRequest } from './api-client';
import type {
  CreateFacturaInput,
  FacturaItem,
  ListParams,
  PagedResult,
} from '@/types/api';

/** Parámetros del listado (GET /api/v1/facturas). */
export interface FacturaListParams extends ListParams {
  orderId?: number;
  customerId?: number;
  dateFrom?: string;
  dateTo?: string;
}

/**
 * Listado paginado global de facturas con filtros de pedido, cliente,
 * fechas y búsqueda en número/cliente (api-spec §17A). Requiere
 * FACTURAS_VIEW.
 */
export function listFacturas(
  params: FacturaListParams = {},
  signal?: AbortSignal
): Promise<PagedResult<FacturaItem>> {
  return apiRequest<PagedResult<FacturaItem>>('/api/v1/facturas', {
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
 * Emite la factura interna de un pedido (POST /api/v1/facturas). El
 * backend exige pedido Entregado/Completado y una sola factura por
 * pedido (409, BR-130); requiere FACTURAS_CREATE.
 */
export function createFactura(input: CreateFacturaInput): Promise<FacturaItem> {
  return apiRequest<FacturaItem>('/api/v1/facturas', {
    method: 'POST',
    body: input,
  });
}
