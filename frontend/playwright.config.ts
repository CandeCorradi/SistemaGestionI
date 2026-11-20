import { defineConfig, devices } from '@playwright/test';

/**
 * Tests E2E de navegador (roadmap Phase 23, AGENTS.md §43).
 *
 * Requiere los servidores levantados:
 *   - API    → http://localhost:7100
 *   - Next   → http://localhost:3000  (o E2E_BASE_URL)
 *
 * Usuarios de prueba (datos demo, no borrar):
 *   SuperAdmin  demo-login-6e681e26@test.local / demo1234
 *   Cliente     cliente-demo-d4e079@test.local / demo1234
 */
export default defineConfig({
  testDir: './e2e',
  timeout: 300_000,
  expect: { timeout: 15_000 },
  fullyParallel: false,
  workers: 1,
  retries: 1,
  reporter: [['list']],
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:3000',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    locale: 'es-AR',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
});
