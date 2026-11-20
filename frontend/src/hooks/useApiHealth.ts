'use client';

import { useEffect, useState } from 'react';
import { checkApiHealth } from '@/services/api';

export function useApiHealth() {
  const [status, setStatus] = useState<'loading' | 'healthy' | 'degraded' | 'error'>('loading');

  useEffect(() => {
    checkApiHealth()
      .then((data) => {
        const isHealthy = data?.status?.toLowerCase().includes('healthy');
        setStatus(isHealthy ? 'healthy' : 'degraded');
      })
      .catch(() => setStatus('error'));
  }, []);

  return { status };
}