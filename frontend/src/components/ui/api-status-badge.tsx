'use client';

import { useApiHealth } from '@/hooks/useApiHealth';

export function ApiStatusBadge() {
  const { status } = useApiHealth();

  const badgeMap = {
    loading: { bg: 'bg-secondary', text: 'Verificando API...' },
    healthy: { bg: 'bg-success', text: 'API Healthy' },
    degraded: { bg: 'bg-warning text-dark', text: 'API Degraded' },
    error: { bg: 'bg-danger', text: 'API Offline' },
  };

  const current = badgeMap[status];

  return (
    <span className={`badge ${current.bg} p-2 fs-6`}>
      <i className="bi bi-activity me-1"></i> {current.text}
    </span>
  );
}