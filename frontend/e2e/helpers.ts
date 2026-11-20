import { expect, type Page } from '@playwright/test';

export const ADMIN = {
  email: 'demo-login-6e681e26@test.local',
  password: 'demo1234',
};

export const CLIENTE = {
  email: 'cliente-demo-d4e079@test.local',
  password: 'demo1234',
};

/** Cliente interno vinculado al usuario Cliente de prueba. */
export const CLIENTE_NOMBRE = 'Diego Fernández';

/** Sufijo único por corrida para no colisionar con datos previos. */
export function uniqueSuffix(): string {
  return Date.now().toString(36);
}

const LOGIN_ATTEMPTS = 3;

/**
 * Inicia sesión y espera la navegación tras el `router.replace` de
 * Firebase. Se reintenta ante el `auth/network-request-failed`
 * intermitente del Identity Toolkit (comparte cuota entre las
 * sesiones del dev server y las peticiones del SDK). El race contra
 * el alert de error permite fallar rápido en vez de agotar el
 * timeout de navegación en cada intento. Si un intento previo llegó
 * a autenticar la sesión con lentitud, se reutiliza sin repetir el
 * formulario.
 */
export async function login(
  page: Page,
  user: { email: string; password: string },
  expectedPath: string
): Promise<void> {
  let lastError: unknown;
  for (let attempt = 1; attempt <= LOGIN_ATTEMPTS; attempt += 1) {
    try {
      // Un intento anterior pudo completar la autenticación tarde.
      if (await page.getByRole('button', { name: 'Salir' }).isVisible().catch(() => false)) {
        await page.goto(expectedPath);
        return;
      }
      await page.goto('/login');
      await page.getByLabel('Email', { exact: false }).first().fill(user.email);
      await page.getByLabel('Contraseña', { exact: false }).first().fill(user.password);
      await page.getByRole('button', { name: 'Ingresar' }).click();

      const errorAlert = page
        .getByRole('alert')
        .filter({ hasText: 'No se pudo iniciar sesión' });
      const nav = page
        .waitForURL(`**${expectedPath}`, { timeout: 60_000 })
        .then(() => 'nav' as const);
      const failed = errorAlert
        .first()
        .waitFor({ state: 'visible', timeout: 60_000 })
        .then(() => 'error' as const);
      // Evita unhandled rejection de la promesa perdedora del race.
      nav.catch(() => undefined);
      failed.catch(() => undefined);
      const outcome = await Promise.race([nav, failed]);
      if (outcome === 'error') {
        const detail = await errorAlert.first().innerText().catch(() => '');
        throw new Error(`login rechazado: ${detail.trim()}`);
      }

      await expect(page.getByRole('button', { name: 'Salir' })).toBeVisible({
        timeout: 30_000,
      });
      return;
    } catch (error) {
      lastError = error;
      // Limpia el alert de error antes de reintentar.
      const close = page.getByRole('button', { name: 'Cerrar' });
      if (await close.count().catch(() => 0)) {
        await close.first().click().catch(() => undefined);
      }
      await page.waitForTimeout(2000 * attempt);
    }
  }
  throw lastError;
}

export async function logout(page: Page): Promise<void> {
  // `signOut` de Firebase puede tardar con la misma latencia de red
  // que el login; se reintenta una vez si la navegación no llega.
  for (let attempt = 1; attempt <= 2; attempt += 1) {
    try {
      await page.getByRole('button', { name: 'Salir' }).click();
      await page.waitForURL('**/login', { timeout: 60_000 });
      return;
    } catch (error) {
      if (attempt === 2) throw error;
      await page.waitForTimeout(2000);
    }
  }
}

/**
 * Abre la vidriera pública reintentando ante respuestas transitorias
 * de la API (p. ej. timeout de conexión a PostgreSQL bajo carga).
 * Si la respuesta no se captura a tiempo pero el listado renderiza,
 * se da por buena: el heading solo se muestra con datos cargados.
 */
export async function gotoShowroom(page: Page): Promise<void> {
  for (let attempt = 1; attempt <= 3; attempt += 1) {
    try {
      const responsePromise = page
        .waitForResponse(
          (r) => r.url().includes('/api/v1/showroom/works') && r.request().method() === 'GET',
          { timeout: 60_000 }
        )
        .catch(() => null);
      await page.goto('/showroom');
      const response = await responsePromise;
      if (response && !response.ok()) {
        throw new Error(`showroom respondió ${response.status()}`);
      }
      await expect(
        page.getByRole('heading', { name: 'Trabajos realizados', level: 1 })
      ).toBeVisible({ timeout: 30_000 });
      return;
    } catch {
      if (attempt === 3) {
        throw new Error('El showroom no cargó tras reintentar.');
      }
      await page.waitForTimeout(2000 * attempt);
    }
  }
}
