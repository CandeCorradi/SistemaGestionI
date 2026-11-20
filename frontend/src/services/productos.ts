import { apiRequest, apiUpload } from './api-client';
import type {
  CreateProductoInput,
  ListParams,
  PagedResult,
  Producto,
  ProductoImagen,
  ProductoList,
  TipoProducto,
  UpdateProductoInput,
} from '@/types/api';

export interface ProductoListParams extends ListParams {
  tipo?: TipoProducto;
  category?: string;
  showroom?: boolean;
  lowStock?: boolean;
  isCustom?: boolean;
}

export function listProductos(
  params: ProductoListParams = {},
  signal?: AbortSignal
): Promise<PagedResult<ProductoList>> {
  return apiRequest<PagedResult<ProductoList>>('/api/v1/products', {
    query: {
      page: params.page,
      pageSize: params.pageSize,
      search: params.search,
      type: params.tipo,
      category: params.category,
      active: params.active,
      showroom: params.showroom,
      lowStock: params.lowStock,
      isCustom: params.isCustom,
    },
    signal,
  });
}

export function getProducto(id: number, signal?: AbortSignal): Promise<Producto> {
  return apiRequest<Producto>(`/api/v1/products/${id}`, { signal });
}

export function createProducto(input: CreateProductoInput): Promise<Producto> {
  return apiRequest<Producto>('/api/v1/products', { method: 'POST', body: input });
}

export function updateProducto(id: number, input: UpdateProductoInput): Promise<Producto> {
  return apiRequest<Producto>(`/api/v1/products/${id}`, { method: 'PATCH', body: input });
}

/** Desactivación lógica (BR-032). */
export function deactivateProducto(id: number): Promise<Producto> {
  return apiRequest<Producto>(`/api/v1/products/${id}`, { method: 'DELETE' });
}

/* ------------------------------------------------------------------ */
/* Imágenes de producto (api-spec §13, BR-100/BR-101)                  */
/* ------------------------------------------------------------------ */

export function listImagenes(id: number, signal?: AbortSignal): Promise<ProductoImagen[]> {
  return apiRequest<ProductoImagen[]>(`/api/v1/products/${id}/images`, { signal });
}

/** Sube una imagen (≤ 5 MB, JPG/PNG/WEBP/GIF) al storage. */
export function subirImagen(id: number, file: File, signal?: AbortSignal): Promise<ProductoImagen> {
  return apiUpload<ProductoImagen>(`/api/v1/products/${id}/images`, file, signal);
}

export function eliminarImagen(id: number, imageId: number): Promise<void> {
  return apiRequest<void>(`/api/v1/products/${id}/images/${imageId}`, { method: 'DELETE' });
}
