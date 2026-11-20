'use client';

/**
 * Registra el service worker de la PWA (AGENTS.md §37).
 *
 * Sólo se registra en producción: en desarrollo el caché de assets
 * generaría confusión durante el hot reload. Las transacciones
 * comerciales siempre dependen de la API (ver public/sw.js).
 */

import { useEffect } from 'react';

export function ServiceWorkerRegister() {
  useEffect(() => {
    if (process.env.NODE_ENV !== 'production') {
      return;
    }
    if (!('serviceWorker' in navigator)) {
      return;
    }
    navigator.serviceWorker
      .register('/sw.js', { scope: '/', updateViaCache: 'none' })
      .catch(() => {
        // El registro del SW es best-effort; la app funciona sin él.
      });
  }, []);

  return null;
}
