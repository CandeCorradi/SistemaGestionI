import { expect, test } from '@playwright/test';
import { ADMIN, CLIENTE, gotoShowroom, login, logout } from './helpers';

test.describe('Smoke: accesos y vidriera pública', () => {
  test('la vidriera pública carga sin sesión y muestra el listado', async ({ page }) => {
    await gotoShowroom(page);
    await expect(page.getByRole('navigation').getByRole('link', { name: 'Catálogo' })).toBeVisible();
    await expect(page.getByRole('navigation').getByRole('link', { name: 'Trabajos' })).toBeVisible();
  });

  test('el SuperAdmin ingresa y llega al dashboard', async ({ page }) => {
    await login(page, ADMIN, '/dashboard');
    // El sidebar (aside) y el dashboard muestran enlaces homónimos,
    // por eso se acota a la navegación principal.
    const nav = page.getByRole('complementary', { name: 'Navegación principal' });
    await expect(nav.getByRole('link', { name: 'Clientes', exact: true })).toBeVisible();
    await expect(
      nav.getByRole('link', { name: 'Trabajos realizados', exact: true })
    ).toBeVisible();
  });

  test('el cliente ingresa y llega al portal', async ({ page }) => {
    await login(page, CLIENTE, '/portal');
    // El portal tiene cards con los mismos nombres que el navbar,
    // por eso se acota al navbar.
    const nav = page.getByRole('navigation');
    await expect(nav.getByRole('link', { name: 'Presupuestos', exact: true })).toBeVisible();
    await expect(nav.getByRole('link', { name: 'Pedidos', exact: true })).toBeVisible();
    await logout(page);
  });
});
