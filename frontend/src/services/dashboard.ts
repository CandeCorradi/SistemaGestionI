import { apiRequest } from './api-client';
import type { DashboardData } from '@/types/api';

/** Indicadores del dashboard (GET /api/v1/dashboard, api-spec §19). */
export function getDashboard(signal?: AbortSignal): Promise<DashboardData> {
  return apiRequest<DashboardData>('/api/v1/dashboard', { signal });
}
