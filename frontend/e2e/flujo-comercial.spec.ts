import { expect, test } from '@playwright/test';
import { ADMIN, CLIENTE, login, logout, uniqueSuffix } from './helpers';

test.describe('Flujo comercial: presupuesto → pedido → pago', () => {
  test('aceptación en el portal crea el pedido; admin lo confirma y registra un pago', async ({
    page,
  }) => {
    // Tres ciclos de login (admin → cliente → admin); con la latencia
    // intermitente del Identity Toolkit se amplía el budget.
    test.setTimeout(600_000);
    const suffix = uniqueSuffix();
    const clienteEmail = `e2e-cliente-${suffix}@test.local`;
    const itemDesc = `Mesa E2E ${suffix}`;

    // --- Admin: crear cliente interno ---
    await login(page, ADMIN, '/dashboard');
    await page.goto('/clientes');
    await page.getByRole('button', { name: 'Nuevo cliente' }).click();
    const clienteDialog = page.getByRole('dialog');
    await clienteDialog.getByLabel('Nombre').fill('E2E');
    await clienteDialog.getByLabel('Apellido').fill(`Cliente ${suffix}`);
    await clienteDialog.getByLabel('Email').fill(clienteEmail);
    await clienteDialog.getByRole('button', { name: 'Crear cliente' }).click();
    await expect(clienteDialog).toBeHidden({ timeout: 30_000 });
    await expect(page.getByText(clienteEmail)).toBeVisible();

    // --- Admin: crear presupuesto Borrador y enviarlo ---
    await page.goto('/presupuestos');
    await page.getByRole('button', { name: 'Nuevo presupuesto' }).click();
    const presDialog = page.getByRole('dialog');

    // Elegir al cliente demo (el usuario Cliente del portal) en el select.
    // El option muestra el email del Cliente interno, no el de Firebase.
    // Se espera a que cargue la lista async antes de inspeccionar.
    const clienteSelect = presDialog.getByLabel('Cliente');
    await expect(
      clienteSelect.locator('option', { hasText: 'diego.fernandez@mail.test' })
    ).toHaveCount(1);
    const demoValue = await clienteSelect.evaluate((el: HTMLSelectElement) => {
      const opt = Array.from(el.options).find((o) =>
        o.textContent?.includes('diego.fernandez@mail.test')
      );
      return opt?.value ?? null;
    });
    expect(demoValue).not.toBeNull();
    await clienteSelect.selectOption(demoValue!);

    // El form arranca con una fila "línea libre" por defecto; no se
    // agrega otra para no duplicar los labels de la línea.
    await presDialog.getByLabel('Descripción de la línea').first().fill(itemDesc);
    await presDialog.getByLabel('Precio unitario').first().fill('1500');
    await presDialog.getByRole('button', { name: 'Crear presupuesto' }).click();
    await expect(presDialog).toBeHidden({ timeout: 30_000 });

    // Filtrar por el cliente demo y abrir el presupuesto más reciente
    // (el listado ordena por Id descendente).
    await page.getByLabel('Buscar presupuestos por cliente').fill('Diego');
    await page
      .locator('tr', { hasText: 'Diego' })
      .first()
      .getByTitle('Ver detalle y acciones')
      .click();
    const presDialog2 = page.getByRole('dialog');
    // El título muestra "#N" recién cuando el detalle terminó de cargar.
    const presHeading = presDialog2.getByRole('heading', { name: /Presupuesto #\d+/ });
    await expect(presHeading).toBeVisible();
    const presTitle = await presHeading.textContent();
    const presId = presTitle?.match(/#(\d+)/)?.[1];
    expect(presId, 'debería capturarse el número de presupuesto').toBeTruthy();
    await expect(presDialog2.getByText('Borrador')).toBeVisible();

    await presDialog2.getByRole('button', { name: 'Enviar al cliente' }).click();
    // "Enviado" también es el dt del campo de fecha; se acota al badge.
    await expect(presDialog2.locator('.badge', { hasText: 'Enviado' })).toBeVisible();
    await presDialog2.locator('[aria-label="Cerrar"]').click();

    // --- Cliente: acepta el presupuesto en el portal ---
    await logout(page);
    await login(page, CLIENTE, '/portal');
    await page.goto(`/portal/presupuestos/${presId}`);
    await expect(
      page.getByRole('heading', { name: `Presupuesto #${presId}`, level: 1 })
    ).toBeVisible();
    await page.getByRole('button', { name: 'Aceptar presupuesto' }).click();
    const confirm = page.getByRole('dialog');
    await expect(confirm.getByText(/Se creará tu pedido/)).toBeVisible();
    await confirm.getByRole('button', { name: 'Aceptar', exact: true }).click();
    await expect(page.getByText('Presupuesto aceptado')).toBeVisible();
    const pedidoLink = page.getByRole('link', { name: /Ver pedido/ }).first();
    const pedidoHref = await pedidoLink.getAttribute('href');
    const orderId = pedidoHref?.match(/(\d+)/)?.[1];
    expect(orderId, 'debería capturarse el número de pedido').toBeTruthy();

    // El pedido nace Pendiente en el portal.
    await page.goto(`/portal/pedidos/${orderId}`);
    await expect(page.getByText('Pendiente').first()).toBeVisible();
    await expect(page.getByText('Saldo pendiente').first()).toBeVisible();

    // --- Admin: confirma el pedido y registra un pago ---
    await logout(page);
    await login(page, ADMIN, '/dashboard');
    await page.goto('/pedidos');
    await page.getByLabel('Buscar pedidos por cliente').fill('Diego');
    await page
      .locator('tr', { hasText: `#${orderId}` })
      .first()
      .getByTitle('Ver detalle, avanzar estado e historial')
      .click();
    const orderDialog = page.getByRole('dialog');
    await expect(orderDialog.getByRole('heading', { name: `Pedido #${orderId}` })).toBeVisible();

    await orderDialog.getByRole('button', { name: 'Confirmar' }).click();
    // El badge de estado; el historial también muestra "Confirmado".
    await expect(orderDialog.locator('.badge', { hasText: 'Confirmado' }).first()).toBeVisible();

    await orderDialog.getByLabel('Monto').fill('500');
    await orderDialog.getByLabel('Método').selectOption({ label: 'Efectivo' });
    await orderDialog.getByRole('button', { name: 'Registrar pago' }).click();
    await expect(orderDialog.getByText('Saldo pendiente')).toBeVisible();
    await expect(orderDialog.getByText(/1\.000,00/)).toBeVisible();

    // El pago queda en el historial de pagos.
    await orderDialog.locator('[aria-label="Cerrar"]').click();
    await page.goto('/pagos');
    await expect(page.getByText(`#${orderId}`).first()).toBeVisible();

    // --- Limpieza: desactivar el cliente E2E creado ---
    await page.goto('/clientes');
    await page.getByLabel('Buscar clientes').fill(clienteEmail);
    await page
      .locator('tr', { hasText: clienteEmail })
      .first()
      .getByTitle('Desactivar')
      .click();
    const deactivateConfirm = page.getByRole('dialog');
    await expect(deactivateConfirm.getByText(/no se borra/i)).toBeVisible();
    await deactivateConfirm.getByRole('button', { name: 'Desactivar' }).click();
    await expect(deactivateConfirm).toBeHidden({ timeout: 15_000 });
  });
});
