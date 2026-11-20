import { apiDownload, apiRequest } from './api-client';
import type {
  ReportDocument,
  ReportFormat,
  ReportKey,
  ReportQueryParams,
} from '@/types/api';

function buildQuery(params: ReportQueryParams) {
  return {
    from: params.from,
    to: params.to,
    customerId: params.customerId,
    status: params.status,
    method: params.method,
    type: params.type,
    active: params.active,
  };
}

/** Preview JSON del reporte (GET /api/v1/reports/{key}). */
export function getReport(
  key: ReportKey,
  params: ReportQueryParams = {},
  signal?: AbortSignal
): Promise<ReportDocument> {
  return apiRequest<ReportDocument>(`/api/v1/reports/${key}`, {
    query: buildQuery(params),
    signal,
  });
}

/** Descarga el reporte exportado (GET /api/v1/reports/{key}/export). */
export function downloadReport(
  key: ReportKey,
  params: ReportQueryParams,
  format: ReportFormat
): Promise<{ blob: Blob; fileName: string }> {
  return apiDownload(`/api/v1/reports/${key}/export`, {
    ...buildQuery(params),
    format,
  });
}

/** Dispara la descarga del blob en el navegador. */
export function saveBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = fileName;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
}
