'use client';

/**
 * Registro de movimientos de inventario (api-spec §14, functional-spec §11).
 * El permiso depende del tipo: STOCK_ENTRY para Ingreso/Devolución y
 * STOCK_ADJUST para el resto (la API también lo valida — BR-003). La
 * cantidad es mayor que cero salvo en Ajuste, que admite signo; la API
 * rechaza operaciones con resultado negativo (BR-052) y conflicto
 * concurrente (BR-053).
 */

import { useEffect, useState, type FormEvent } from 'react';
import { useAuth } from '@/features/auth/auth-provider';
import { ApiError } from '@/services/api-client';
import { createMovement } from '@/services/inventario';
import { listProductos } from '@/services/productos';
import {
  TIPO_MOVIMIENTO_EFECTO,
  TIPO_MOVIMIENTO_STOCK_LABELS,
  type ProductoList,
  type StockList,
  type TipoMovimientoStock,
} from '@/types/api';
import { Modal } from '@/components/ui/modal';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { AppInput, AppSelect, AppTextarea } from '@/components/ui/form-fields';

interface MovimientoFormModalProps {
  open: boolean;
  /** Producto preseleccionado desde la fila; null = elegir del catálogo. */
  producto: StockList | null;
  onClose: () => void;
  onSaved: () => void;
}

interface FormState {
  productId: string;
  type: string;
  quantity: string;
  reason: string;
  reference: string;
}

export function MovimientoFormModal({
  open,
  producto,
  onClose,
  onSaved,
}: MovimientoFormModalProps) {
  const { can } = useAuth();

  // Tipos permitidos según permisos efectivos (UX; la API revalida).
  const tiposPermitidos: TipoMovimientoStock[] = [];
  if (can('STOCK_ENTRY')) {
    tiposPermitidos.push(0, 3); // Ingreso, Devolución
  }
  if (can('STOCK_ADJUST')) {
    tiposPermitidos.push(1, 2, 4, 5); // Egreso, Ajuste, Reserva, Liberación
  }

  const [values, setValues] = useState<FormState>(() => ({
    productId: producto ? String(producto.id) : '',
    type: String(tiposPermitidos[0] ?? 0),
    quantity: '1',
    reason: '',
    reference: '',
  }));
  const [errors, setErrors] = useState<Partial<Record<keyof FormState, string>>>({});
  const [apiError, setApiError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const [catalogo, setCatalogo] = useState<ProductoList[]>([]);
  const [catalogoError, setCatalogoError] = useState<string | null>(null);

  // Catálogo de productos activos sólo cuando hay que elegir uno.
  useEffect(() => {
    if (producto) {
      return;
    }
    const controller = new AbortController();
    listProductos({ active: true, pageSize: 100 }, controller.signal)
      .then((result) => setCatalogo(result.items))
      .catch((err: unknown) => {
        if (!controller.signal.aborted) {
          setCatalogoError(
            err instanceof Error ? err.message : 'No se pudo cargar el catálogo de productos.'
          );
        }
      });
    return () => controller.abort();
  }, [producto]);

  const tipoActual = Number(values.type) as TipoMovimientoStock;
  const efecto = TIPO_MOVIMIENTO_EFECTO[tipoActual];

  const stockActual = producto
    ? producto.stockActual
    : catalogo.find((p) => String(p.id) === values.productId)?.stockActual;

  function set<K extends keyof FormState>(key: K, value: FormState[K]) {
    setValues((current) => ({ ...current, [key]: value }));
    if (errors[key]) {
      setErrors((current) => ({ ...current, [key]: undefined }));
    }
  }

  function validate(): boolean {
    const next: Partial<Record<keyof FormState, string>> = {};

    if (!values.productId) {
      next.productId = 'Elegí un producto.';
    }

    if (!Number.isInteger(Number(values.type))) {
      next.type = 'Elegí un tipo de movimiento.';
    }

    const quantity = Number(values.quantity);
    if (values.quantity.trim() === '' || Number.isNaN(quantity) || !Number.isInteger(quantity)) {
      next.quantity = 'Ingresá una cantidad entera.';
    } else if (efecto === 0) {
      if (quantity === 0) {
        next.quantity = 'El ajuste no puede ser cero.';
      }
    } else if (quantity <= 0) {
      next.quantity = 'La cantidad debe ser mayor que cero.';
    }

    setErrors(next);
    return Object.keys(next).length === 0;
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    if (tiposPermitidos.length === 0 || !validate()) {
      return;
    }

    setSubmitting(true);
    setApiError(null);
    try {
      await createMovement({
        productId: Number(values.productId),
        type: Number(values.type) as TipoMovimientoStock,
        quantity: Number(values.quantity),
        reason: values.reason.trim() || null,
        reference: values.reference.trim() || null,
      });
      onSaved();
    } catch (err) {
      if (err instanceof ApiError) {
        setApiError(err.message);
      } else {
        setApiError('No se pudo registrar el movimiento.');
      }
    } finally {
      setSubmitting(false);
    }
  }

  if (tiposPermitidos.length === 0) {
    return (
      <Modal open={open} title="Registrar movimiento" onClose={onClose}>
        <AppAlert variant="warning" title="Sin permiso">
          Tu usuario no tiene permiso para registrar movimientos de stock
          (<code>STOCK_ENTRY</code> o <code>STOCK_ADJUST</code>).
        </AppAlert>
      </Modal>
    );
  }

  const previewStock =
    stockActual !== undefined && values.quantity.trim() !== '' && !Number.isNaN(Number(values.quantity))
      ? stockActual + efecto * Number(values.quantity)
      : null;

  return (
    <Modal
      open={open}
      title={producto ? `Movimiento — ${producto.nombre}` : 'Registrar movimiento'}
      onClose={onClose}
      footer={
        <>
          <AppButton variant="secondary" onClick={onClose} disabled={submitting}>
            Cancelar
          </AppButton>
          <AppButton type="submit" form="movimiento-form" loading={submitting}>
            Registrar movimiento
          </AppButton>
        </>
      }
    >
      {apiError ? (
        <AppAlert variant="danger" onClose={() => setApiError(null)}>
          {apiError}
        </AppAlert>
      ) : null}

      <form id="movimiento-form" onSubmit={onSubmit} noValidate>
        <div className="row">
          <div className="col-md-5">
            {producto ? (
              <>
                <label className="form-label">Producto</label>
                <input
                  className="form-control"
                  value={`${producto.nombre} (${producto.codigoSku})`}
                  readOnly
                  disabled
                />
              </>
            ) : (
              <AppSelect
                label="Producto"
                name="productId"
                required
                value={values.productId}
                error={errors.productId}
                disabled={catalogo.length === 0}
                options={[
                  { value: '', label: catalogo.length === 0 ? 'Cargando catálogo…' : 'Elegir producto…' },
                  ...catalogo.map((p) => ({
                    value: String(p.id),
                    label: `${p.nombre} (${p.codigoSku}) — stock ${p.stockActual}`,
                  })),
                ]}
                onChange={(event) => set('productId', event.target.value)}
              />
            )}
            {catalogoError ? (
              <div className="text-danger small">{catalogoError}</div>
            ) : null}
          </div>
          <div className="col-md-3">
            <AppSelect
              label="Tipo"
              name="type"
              required
              value={values.type}
              error={errors.type}
              options={tiposPermitidos.map((value) => ({
                value: String(value),
                label: TIPO_MOVIMIENTO_STOCK_LABELS[value],
              }))}
              onChange={(event) => set('type', event.target.value)}
            />
          </div>
          <div className="col-md-2">
            <AppInput
              label="Cantidad"
              name="quantity"
              type="number"
              step="1"
              required
              value={values.quantity}
              error={errors.quantity}
              hint={efecto === 0 ? 'Admite signo (±).' : undefined}
              onChange={(event) => set('quantity', event.target.value)}
            />
          </div>
          <div className="col-md-2 d-flex align-items-end">
            <div className="mb-3 w-100">
              <span className="form-label d-block small text-muted mb-1">Resultado</span>
              {previewStock !== null ? (
                <span
                  className={`badge ${previewStock < 0 ? 'text-bg-danger' : 'text-bg-light border'}`}
                >
                  {stockActual} → {previewStock}
                </span>
              ) : (
                <span className="badge text-bg-light border">—</span>
              )}
            </div>
          </div>
        </div>

        <AppTextarea
          label="Motivo"
          name="reason"
          rows={2}
          value={values.reason}
          error={errors.reason}
          hint="Queda en el historial (BR-051)."
          onChange={(event) => set('reason', event.target.value)}
        />

        <AppInput
          label="Referencia"
          name="reference"
          value={values.reference}
          hint="Opcional: nº de pedido, remito u orden de compra."
          onChange={(event) => set('reference', event.target.value)}
        />

        <p className="small text-muted mb-0">
          El stock nunca queda negativo (BR-052) y el registro se aplica de forma atómica: si otra
          persona modifica el stock al mismo tiempo, la operación se cancela sin efectos (BR-053).
        </p>
      </form>
    </Modal>
  );
}
