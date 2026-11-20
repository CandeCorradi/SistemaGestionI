import { apiRequest } from './api-client';
import type {
  CreateMovimientoStockInput,
  ListParams,
  MovimientoStock,
  PagedResult,
  StockList,
} from '@/types/api';

/** Parámetros del stock actual (GET /api/v1/inventory). */
export interface StockListParams extends ListParams {
  lowStock?: boolean;
}

export function listStock(
  params: StockListParams = {},
  signal?: AbortSignal
): Promise<PagedResult<StockList>> {
  return apiRequest<PagedResult<StockList>>('/api/v1/inventory', {
    query: {
      page: params.page,
      pageSize: params.pageSize,
      search: params.search,
      lowStock: params.lowStock,
    },
    signal,
  });
}

/** Parámetros del historial (GET /api/v1/inventory/movements). */
export interface MovimientosParams extends ListParams {
  productId?: number;
}

export function listMovements(
  params: MovimientosParams = {},
  signal?: AbortSignal
): Promise<PagedResult<MovimientoStock>> {
  return apiRequest<PagedResult<MovimientoStock>>('/api/v1/inventory/movements', {
    query: {
      page: params.page,
      pageSize: params.pageSize,
      productId: params.productId,
    },
    signal,
  });
}

/**
 * Registra un movimiento (POST). El permiso depende del tipo: STOCK_ENTRY
 * (Ingreso/Devolución) o STOCK_ADJUST (resto). 409 si el stock cambió de
 * forma concurrente (BR-053); 400 si dejaría stock negativo (BR-052).
 */
export function createMovement(input: CreateMovimientoStockInput): Promise<MovimientoStock> {
  return apiRequest<MovimientoStock>('/api/v1/inventory/movements', {
    method: 'POST',
    body: input,
  });
}
