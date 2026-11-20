import { expect, test } from '@playwright/test';
import { ADMIN, gotoShowroom, login, logout, uniqueSuffix } from './helpers';

test.describe('Trabajos realizados: administración y vidriera', () => {
  test('un trabajo publicado aparece en la vidriera y ocultarlo lo remueve', async ({
    page,
  }) => {
    // Dos ciclos de login; con la latencia intermitente del Identity
    // Toolkit se amplía el budget del test.
    test.setTimeout(480_000);
    const suffix = uniqueSuffix();
    const nombre = `Obra E2E ${suffix}`;

    // --- Admin: crear trabajo ya publicado ---
    await login(page, ADMIN, '/dashboard');
    await page.goto('/trabajos');
    await page.getByRole('button', { name: 'Nuevo trabajo' }).click();
    const dialog = page.getByRole('dialog');
    // Se acota al textbox porque el checkbox "Publicar el nombre..."
    // también coincide con "Nombre".
    await dialog.getByRole('textbox', { name: 'Nombre' }).fill(nombre);
    await dialog.getByRole('textbox', { name: 'Tipo de mueble' }).fill('Mesa');
    await dialog.getByLabel('Publicado en la vidriera').check();
    await dialog.getByRole('button', { name: 'Crear trabajo' }).click();
    await expect(dialog).toBeHidden({ timeout: 30_000 });

    const fila = page.locator('tr', { hasText: nombre }).first();
    await expect(fila.getByText('Publicado')).toBeVisible();

    // --- Vidriera pública: la obra es visible ---
    await logout(page);
    await gotoShowroom(page);
    const card = page.getByRole('link', { name: new RegExp(nombre) }).first();
    await expect(card).toBeVisible();
    await card.click();
    await expect(page.getByRole('heading', { name: nombre, level: 1 })).toBeVisible();

    // --- Admin: ocultar de la vidriera y limpiar ---
    // La vidriera consume el endpoint público (solo Publicado=true),
    // por lo que verificar la ausencia con la sesión admin ahorra un
    // ciclo completo de logout/login sin perder cobertura.
    await login(page, ADMIN, '/dashboard');
    await page.goto('/trabajos');
    await page
      .locator('tr', { hasText: nombre })
      .first()
      .getByTitle('Ocultar de la vidriera')
      .click();
    await expect(page.locator('tr', { hasText: nombre }).first().getByText('Borrador')).toBeVisible();

    // Ya no aparece públicamente.
    await gotoShowroom(page);
    await expect(page.getByRole('link', { name: new RegExp(nombre) })).toHaveCount(0);

    // --- Limpieza: eliminar el trabajo de prueba ---
    await page.goto('/trabajos');
    await page
      .locator('tr', { hasText: nombre })
      .first()
      .getByTitle('Eliminar')
      .click();
    const confirm = page.getByRole('dialog');
    await expect(confirm.getByText(/no se puede deshacer/i)).toBeVisible();
    await confirm.getByRole('button', { name: 'Eliminar', exact: true }).click();
    await expect(page.locator('tr', { hasText: nombre })).toHaveCount(0);
  });
});
