/**
 * Cliente HTTP tipado para la API (AGENTS.md §35: servicios para API,
 * manejo de errores). Envía el Firebase ID Token como Bearer y convierte
 * los ProblemDetails de la API en excepciones `ApiError` con el código de
 * negocio (`title`) para que la UI pueda reaccionar (409 duplicado, 400
 * validación, 401 sesión vencida, 403 sin permiso).
 */

import type { ProblemDetails } from '@/types/api';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:7100';

export class ApiError extends Error {
  readonly status: number;
  readonly code: string;

  constructor(status: number, code: string, message: string) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.code = code;
  }

  get isValidation(): boolean {
    return this.status === 400;
  }

  get isUnauthorized(): boolean {
    return this.status === 401;
  }

  get isForbidden(): boolean {
    return this.status === 403;
  }

  get isNotFound(): boolean {
    return this.status === 404;
  }

  get isConflict(): boolean {
    return this.status === 409;
  }
}

type TokenGetter = () => Promise<string | null>;

let tokenGetter: TokenGetter | null = null;

/**
 * Registra el proveedor del ID Token (lo hace el AuthProvider de
 * Firebase). Se consulta en cada request para usar siempre un token
 * vigente.
 */
export function setAuthTokenGetter(getter: TokenGetter): void {
  tokenGetter = getter;
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PATCH' | 'PUT' | 'DELETE';
  body?: unknown;
  /** Query string a serializar (valores undefined/null se omiten). */
  query?: Record<string, string | number | boolean | undefined | null>;
  signal?: AbortSignal;
}

function buildUrl(path: string, query?: RequestOptions['query']): string {
  const url = new URL(`${API_BASE_URL}${path}`);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== '') {
        url.searchParams.set(key, String(value));
      }
    }
  }
  return url.toString();
}

async function parseError(response: Response): Promise<ApiError> {
  let code = 'Error.Unknown';
  let message = response.statusText || 'Error inesperado';

  try {
    const problem = (await response.json()) as Partial<ProblemDetails>;
    if (problem.title) {
      code = problem.title;
    }
    if (problem.detail) {
      message = problem.detail;
    }
  } catch {
    // Respuesta sin cuerpo JSON: conservar statusText.
  }

  return new ApiError(response.status, code, message);
}

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = 'GET', body, query, signal } = options;

  const headers: Record<string, string> = { Accept: 'application/json' };
  if (body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }

  const token = tokenGetter ? await tokenGetter() : null;
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(buildUrl(path, query), {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
    signal,
  });

  if (!response.ok) {
    throw await parseError(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

/** Archivo descargado con su nombre sugerido por la API. */
export interface DownloadedFile {
  blob: Blob;
  fileName: string;
}

/**
 * Sube un archivo multipart autenticado (imágenes de producto,
 * api-spec §13): el navegador arma el Content-Type con el boundary,
 * por eso no se fija cabecera manualmente.
 */
export async function apiUpload<T>(path: string, file: File, signal?: AbortSignal): Promise<T> {
  const headers: Record<string, string> = {};
  const token = tokenGetter ? await tokenGetter() : null;
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const form = new FormData();
  form.append('file', file);

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: 'POST',
    headers,
    body: form,
    signal,
  });

  if (!response.ok) {
    throw await parseError(response);
  }

  const text = await response.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

/**
 * Descarga binaria autenticada (exportaciones de reportes, api-spec §20):
 * devuelve el blob y el nombre de archivo sugerido por Content-Disposition.
 */
export async function apiDownload(
  path: string,
  query?: RequestOptions['query']
): Promise<DownloadedFile> {
  const headers: Record<string, string> = {};
  const token = tokenGetter ? await tokenGetter() : null;
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(buildUrl(path, query), { method: 'GET', headers });

  if (!response.ok) {
    throw await parseError(response);
  }

  const disposition = response.headers.get('content-disposition') ?? '';
  const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition);
  const fileName = match?.[1] ? decodeURIComponent(match[1]) : 'descarga';

  return { blob: await response.blob(), fileName };
}
