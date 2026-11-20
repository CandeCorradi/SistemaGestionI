import { expect, test } from '@playwright/test';
import { gotoShowroom } from './helpers';

/**
 * Carrito de la vidriera (functional-spec §9A, AGENTS.md §23).
 *
 * El carrito vive en `localStorage`, así que estos casos no dependen de que
 * haya obras publicadas ni de una sesión: siembran la selección directamente
 * y comprueban la intención de compra anónima (login + carrito conservado).
 */
test.describe('Carrito de la vidriera', () => {
  const CART_KEY = 'sg-cart-v1';
  const item = {
    id: 1,
    nombre: 'Placard a medida',
    categoria: 'Dormitorio',
    tipoMueble: 'Placard',
    imagenUrl: null,
  };

  test('el checkout anónimo pide sesión y conserva la selección', async ({ page }) => {
    await gotoShowroom(page);

    await page.evaluate(
      ([key, value]) => window.localStorage.setItem(key, value),
      [CART_KEY, JSON.stringify([item])] as const
    );
    await page.reload();

    const cartButton = page.getByRole('button', { name: 'Abrir carrito (1)' });
    await expect(cartButton).toBeVisible();
    await cartButton.click();

    const drawer = page.getByRole('dialog', { name: /Mi selección/ });
    await expect(drawer).toBeVisible();
    await expect(drawer.getByText(item.nombre)).toBeVisible();

    await drawer.getByRole('button', { name: 'Ingresar para comprar' }).click();

    await expect(page).toHaveURL(/\/login\?redirect=%2Fshowroom%3Fcart%3D1/);
    await expect(page.getByRole('button', { name: 'Continuar con Google' })).toBeVisible();

    // El carrito sobrevive la navegación al login.
    const stored = await page.evaluate((key) => window.localStorage.getItem(key), CART_KEY);
    expect(stored).toContain(item.nombre);
  });

  test('?cart=1 reabre el panel y limpia el parámetro', async ({ page }) => {
    await gotoShowroom(page);

    await page.evaluate(
      ([key, value]) => window.localStorage.setItem(key, value),
      [CART_KEY, JSON.stringify([item])] as const
    );

    await page.goto('/showroom?cart=1');

    const drawer = page.getByRole('dialog', { name: /Mi selección/ });
    await expect(drawer).toBeVisible();
    await expect(page).toHaveURL(/\/showroom$/);

    // Quitar la obra deja el panel vacío y el checkout deshabilitado.
    await drawer.getByRole('button', { name: 'Quitar' }).click();
    await expect(drawer.getByText('Todavía no agregaste trabajos.')).toBeVisible();
    await expect(drawer.getByRole('button', { name: /comprar/ })).toBeDisabled();
  });
});
