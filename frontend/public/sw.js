/**
 * Service worker (AGENTS.md §37): estrategia de caché deliberadamente
 * conservadora.
 *
 * - Se cachean SÓLO los assets con hash de /_next/static/ (inmutables).
 * - Nunca se cachea /api/ ni responses de la API: las operaciones
 *   comerciales críticas requieren conexión con el backend.
 * - Las navegaciones van directo a red (sin HTML cacheado) para no
 *   servir datos administrativos desactualizados ni de otros usuarios.
 */

const CACHE_NAME = 'sg-static-v1';

self.addEventListener('install', (event) => {
  self.skipWaiting();
  event.waitUntil(Promise.resolve());
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches
      .keys()
      .then((keys) =>
        Promise.all(
          keys
            .filter((key) => key !== CACHE_NAME && key.startsWith('sg-static-'))
            .map((key) => caches.delete(key))
        )
      )
      .then(() => self.clients.claim())
  );
});

self.addEventListener('fetch', (event) => {
  const request = event.request;

  if (request.method !== 'GET') {
    return;
  }

  const url = new URL(request.url);

  if (url.origin !== self.location.origin) {
    return;
  }

  if (url.pathname.startsWith('/api/')) {
    return;
  }

  if (url.pathname.startsWith('/_next/static/')) {
    event.respondWith(
      caches.open(CACHE_NAME).then(async (cache) => {
        const cached = await cache.match(request);
        if (cached) {
          return cached;
        }
        const response = await fetch(request);
        if (response.ok) {
          cache.put(request, response.clone());
        }
        return response;
      })
    );
  }
});
