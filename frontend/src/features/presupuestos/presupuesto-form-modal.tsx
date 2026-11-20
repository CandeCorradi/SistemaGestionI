'use client';

/**
 * Creación/edición de presupuestos (api-spec §15, functional-spec §12).
 * Las líneas con producto congelan el precio vigente (BR-063); los totales
 * se muestran como preview pero los calcula el backend (BR-062). Sólo se
 * edita en borrador (la API devuelve 409 en otro estado).
 */

import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { ApiError } from '@/services/api-client';
import { createQuote, updateQuote } from '@/services/presupuestos';
import { listClientes } from '@/services/clientes';
import { listProductos } from '@/services/productos';
import { formatMoney } from '@/lib/format';
import type {
  ClienteList,
  CreateQuoteItemInput,
  ProductoList,
  Quote,
} from '@/types/api';
import { Modal } from '@/components/ui/modal';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { AppInput, AppSelect, AppTextarea } from '@/components/ui/form-fields';

interface PresupuestoFormModalProps {
  open: boolean;
  /** Presupuesto a editar; null = crear nuevo (sólo borrador). */
  quote: Quote | null;
  onClose: () => void;
  onSaved: () => void;
}

interface LineState {
  key: string;
  productId: string;
  description: string;
  quantity: string;
  unitPrice: string;
  discount: string;
}

let lineKey = 0;

function emptyLine(): LineState {
  lineKey += 1;
  return { key: `line-${lineKey}`, productId: '', description: '', quantity: '1', unitPrice: '', discount: '' };
}

function lineFromQuote(item: Quote['items'][number]): LineState {
  lineKey += 1;
  return {
    key: `line-${lineKey}`,
    productId: item.productId !== null ? String(item.productId) : '',
    description: item.description,
    quantity: String(item.quantity),
    unitPrice: String(item.unitPrice),
    discount: item.discount > 0 ? String(item.discount) : '',
  };
}

export function PresupuestoFormModal({ open, quote, onClose, onSaved }: PresupuestoFormModalProps) {
  const [customerId, setCustomerId] = useState(quote ? String(quote.customerId) : '');
  const [expirationDate, setExpirationDate] = useState('');
  const [notes, setNotes] = useState(quote?.notes ?? '');
  const [conditions, setConditions] = useState(quote?.conditions ?? '');
  const [lines, setLines] = useState<LineState[]>(() =>
    quote ? quote.items.map(lineFromQuote) : [emptyLine()]
  );

  const [customerError, setCustomerError] = useState<string | undefined>(undefined);
  const [linesError, setLinesError] = useState<string | undefined>(undefined);
  const [lineErrors, setLineErrors] = useState<Record<string, Partial<Record<keyof LineState, string>>>>({});
  const [apiError, setApiError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const [clientes, setClientes] = useState<ClienteList[]>([]);
  const [productos, setProductos] = useState<ProductoList[]>([]);
  const [catalogError, setCatalogError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    Promise.all([
      listClientes({ active: true, pageSize: 500 }, controller.signal),
      listProductos({ active: true, pageSize: 100 }, controller.signal),
    ])
      .then(([clientesResult, productosResult]) => {
        setClientes(clientesResult.items);
        setProductos(productosResult.items);
      })
      .catch((err: unknown) => {
        if (!controller.signal.aborted) {
          setCatalogError(
            err instanceof Error ? err.message : 'No se pudieron cargar clientes y catálogo.'
          );
        }
      });
    return () => controller.abort();
  }, []);

  const totals = useMemo(() => {
    let subtotal = 0;
    let discount = 0;
    for (const line of lines) {
      const quantity = Number(line.quantity);
      const price = Number(line.unitPrice);
      const lineDiscount = Number(line.discount || 0);
      if (!Number.isNaN(quantity) && !Number.isNaN(price) && quantity > 0 && price >= 0) {
        subtotal += quantity * price;
        discount += Math.max(lineDiscount, 0);
      }
    }
    return { subtotal, discount, total: subtotal - discount };
  }, [lines]);

  function updateLine(key: string, patch: Partial<LineState>) {
    setLines((current) =>
      current.map((line) => {
        if (line.key !== key) {
          return line;
        }
        const next = { ...line, ...patch };
        if (patch.productId !== undefined) {
          const producto = productos.find((p) => String(p.id) === patch.productId);
          if (producto) {
            // Preview del snapshot: el backend congela nombre y precio (BR-063).
            next.description = producto.nombre;
            next.unitPrice = String(producto.precio);
          }
        }
        return next;
      })
    );
    setLineErrors((current) => {
      const rest = { ...current };
      delete rest[key];
      return rest;
    });
    setLinesError(undefined);
  }

  function addLine() {
    setLines((current) => [...current, emptyLine()]);
  }

  function removeLine(key: string) {
    setLines((current) => (current.length > 1 ? current.filter((line) => line.key !== key) : current));
  }

  function validate(): boolean {
    let valid = true;

    if (!customerId) {
      setCustomerError('Elegí un cliente (BR-060).');
      valid = false;
    } else {
      setCustomerError(undefined);
    }

    if (lines.length === 0) {
      setLinesError('El presupuesto debe tener al menos un ítem (BR-061).');
      valid = false;
    } else {
      setLinesError(undefined);
    }

    const nextErrors: Record<string, Partial<Record<keyof LineState, string>>> = {};
    for (const line of lines) {
      const errors: Partial<Record<keyof LineState, string>> = {};
      const quantity = Number(line.quantity);
      const price = Number(line.unitPrice);
      const discount = Number(line.discount || 0);

      if (line.quantity.trim() === '' || Number.isNaN(quantity) || !Number.isInteger(quantity) || quantity <= 0) {
        errors.quantity = 'Cantidad entera mayor que cero.';
      }
      if (line.productId === '') {
        if (!line.description.trim()) {
          errors.description = 'Descripción obligatoria.';
        }
        if (line.unitPrice.trim() === '' || Number.isNaN(price) || price < 0) {
          errors.unitPrice = 'Precio obligatorio.';
        }
      } else if (line.unitPrice.trim() === '' || Number.isNaN(price) || price < 0) {
        errors.unitPrice = 'Precio inválido.';
      }
      if (line.discount.trim() !== '' && (Number.isNaN(discount) || discount < 0)) {
        errors.discount = 'Descuento inválido.';
      } else if (
        !errors.quantity &&
        !errors.unitPrice &&
        discount > quantity * price
      ) {
        errors.discount = 'Supera el importe de la línea.';
      }

      if (Object.keys(errors).length > 0) {
        nextErrors[line.key] = errors;
        valid = false;
      }
    }
    setLineErrors(nextErrors);

    return valid;
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    if (!validate()) {
      return;
    }

    const items: CreateQuoteItemInput[] = lines.map((line) => ({
      productId: line.productId ? Number(line.productId) : null,
      description: line.productId ? null : line.description.trim(),
      quantity: Number(line.quantity),
      unitPrice: line.productId ? null : Number(line.unitPrice),
      discount: line.discount ? Number(line.discount) : null,
    }));

    setSubmitting(true);
    setApiError(null);
    try {
      if (quote) {
        await updateQuote(quote.id, {
          customerId: Number(customerId),
          expirationDate: expirationDate || null,
          notes: notes.trim() || null,
          conditions: conditions.trim() || null,
          items,
        });
      } else {
        await createQuote({
          customerId: Number(customerId),
          expirationDate: expirationDate || null,
          notes: notes.trim() || null,
          conditions: conditions.trim() || null,
          items,
        });
      }
      onSaved();
    } catch (err) {
      if (err instanceof ApiError) {
        setApiError(err.message);
      } else {
        setApiError('No se pudo guardar el presupuesto.');
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Modal
      open={open}
      title={quote ? `Editar presupuesto #${quote.id}` : 'Nuevo presupuesto'}
      onClose={onClose}
      size="modal-lg"
      footer={
        <>
          <AppButton variant="secondary" onClick={onClose} disabled={submitting}>
            Cancelar
          </AppButton>
          <AppButton type="submit" form="presupuesto-form" loading={submitting}>
            {quote ? 'Guardar cambios' : 'Crear presupuesto'}
          </AppButton>
        </>
      }
    >
      {apiError ? (
        <AppAlert variant="danger" onClose={() => setApiError(null)}>
          {apiError}
        </AppAlert>
      ) : null}
      {catalogError ? (
        <AppAlert variant="warning" title="Catálogo incompleto">
          {catalogError}
        </AppAlert>
      ) : null}

      <form id="presupuesto-form" onSubmit={onSubmit} noValidate>
        <div className="row">
          <div className="col-md-6">
            <AppSelect
              label="Cliente"
              name="customerId"
              required
              value={customerId}
              error={customerError}
              disabled={clientes.length === 0}
              options={[
                { value: '', label: clientes.length === 0 ? 'Cargando clientes…' : 'Elegir cliente…' },
                ...clientes.map((c) => ({
                  value: String(c.id),
                  label: `${c.nombre} ${c.apellido} — ${c.email}`,
                })),
              ]}
              onChange={(event) => {
                setCustomerId(event.target.value);
                setCustomerError(undefined);
              }}
            />
          </div>
          <div className="col-md-3">
            <AppInput
              label="Válido hasta"
              name="expirationDate"
              type="date"
              value={expirationDate}
              hint="Opcional. No puede ser una fecha pasada."
              onChange={(event) => setExpirationDate(event.target.value)}
            />
          </div>
        </div>

        <div className="d-flex justify-content-between align-items-center mt-2 mb-2">
          <span className="form-label mb-0 fw-semibold">Ítems</span>
          <AppButton
            type="button"
            variant="outline-secondary"
            className="btn-sm"
            onClick={addLine}
            icon={<i className="bi bi-plus-lg me-1" aria-hidden="true" />}
          >
            Agregar ítem
          </AppButton>
        </div>

        {linesError ? <div className="text-danger small mb-2">{linesError}</div> : null}

        <div className="table-responsive">
          <table className="table table-sm align-middle">
            <thead>
              <tr className="small text-muted">
                <th style={{ minWidth: '12rem' }}>Producto / descripción</th>
                <th style={{ minWidth: '11rem' }}>Descripción</th>
                <th style={{ width: '5.5rem' }}>Cant.</th>
                <th style={{ width: '7rem' }}>Precio</th>
                <th style={{ width: '7rem' }}>Desc.</th>
                <th style={{ width: '7rem' }} className="text-end">
                  Subtotal
                </th>
                <th style={{ width: '2.5rem' }} aria-label="Acciones" />
              </tr>
            </thead>
            <tbody>
              {lines.map((line) => {
                const errors = lineErrors[line.key] ?? {};
                const quantity = Number(line.quantity);
                const price = Number(line.unitPrice);
                const rowSubtotal =
                  !Number.isNaN(quantity) && !Number.isNaN(price) && quantity > 0
                    ? quantity * price - Number(line.discount || 0)
                    : null;

                return (
                  <tr key={line.key}>
                    <td>
                      <select
                        className={`form-select form-select-sm ${errors.productId ? 'is-invalid' : ''}`}
                        value={line.productId}
                        aria-label="Producto de la línea"
                        onChange={(event) => updateLine(line.key, { productId: event.target.value })}
                      >
                        <option value="">— Línea libre (a medida) —</option>
                        {productos.map((p) => (
                          <option key={p.id} value={String(p.id)}>
                            {p.nombre} — {formatMoney(p.precio)}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td>
                      <input
                        className={`form-control form-control-sm ${errors.description ? 'is-invalid' : ''}`}
                        value={line.description}
                        placeholder={line.productId ? 'Tomado del producto' : 'Descripción'}
                        disabled={Boolean(line.productId)}
                        aria-label="Descripción de la línea"
                        onChange={(event) => updateLine(line.key, { description: event.target.value })}
                      />
                      {errors.description ? (
                        <div className="invalid-feedback d-block">{errors.description}</div>
                      ) : null}
                    </td>
                    <td>
                      <input
                        type="number"
                        min={1}
                        step={1}
                        className={`form-control form-control-sm ${errors.quantity ? 'is-invalid' : ''}`}
                        value={line.quantity}
                        aria-label="Cantidad"
                        onChange={(event) => updateLine(line.key, { quantity: event.target.value })}
                      />
                      {errors.quantity ? (
                        <div className="invalid-feedback d-block">{errors.quantity}</div>
                      ) : null}
                    </td>
                    <td>
                      <input
                        type="number"
                        min={0}
                        step="0.01"
                        className={`form-control form-control-sm ${errors.unitPrice ? 'is-invalid' : ''}`}
                        value={line.unitPrice}
                        disabled={Boolean(line.productId)}
                        placeholder={line.productId ? '—' : '0,00'}
                        aria-label="Precio unitario"
                        onChange={(event) => updateLine(line.key, { unitPrice: event.target.value })}
                      />
                      {errors.unitPrice ? (
                        <div className="invalid-feedback d-block">{errors.unitPrice}</div>
                      ) : null}
                    </td>
                    <td>
                      <input
                        type="number"
                        min={0}
                        step="0.01"
                        className={`form-control form-control-sm ${errors.discount ? 'is-invalid' : ''}`}
                        value={line.discount}
                        placeholder="0"
                        aria-label="Descuento"
                        onChange={(event) => updateLine(line.key, { discount: event.target.value })}
                      />
                      {errors.discount ? (
                        <div className="invalid-feedback d-block">{errors.discount}</div>
                      ) : null}
                    </td>
                    <td className="text-end font-monospace small">
                      {rowSubtotal !== null ? formatMoney(rowSubtotal) : '—'}
                    </td>
                    <td>
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-danger border-0"
                        title="Quitar ítem"
                        aria-label="Quitar ítem"
                        disabled={lines.length === 1}
                        onClick={() => removeLine(line.key)}
                      >
                        <i className="bi bi-trash" aria-hidden="true" />
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
            <tfoot>
              <tr className="small">
                <td colSpan={4} className="text-end text-muted border-0">
                  Subtotal (preview)
                </td>
                <td colSpan={2} className="text-end font-monospace border-0">
                  {formatMoney(totals.subtotal)}
                </td>
                <td />
              </tr>
              <tr className="small">
                <td colSpan={4} className="text-end text-muted border-0">
                  Descuentos
                </td>
                <td colSpan={2} className="text-end font-monospace border-0">
                  −{formatMoney(totals.discount)}
                </td>
                <td />
              </tr>
              <tr className="fw-semibold">
                <td colSpan={4} className="text-end border-0">
                  Total (preview)
                </td>
                <td colSpan={2} className="text-end font-monospace border-0">
                  {formatMoney(totals.total)}
                </td>
                <td />
              </tr>
            </tfoot>
          </table>
        </div>

        <div className="row">
          <div className="col-md-6">
            <AppTextarea
              label="Notas"
              name="notes"
              rows={2}
              value={notes}
              hint="Visibles en el presupuesto."
              onChange={(event) => setNotes(event.target.value)}
            />
          </div>
          <div className="col-md-6">
            <AppTextarea
              label="Condiciones comerciales"
              name="conditions"
              rows={2}
              value={conditions}
              hint="Ej.: 50% de seña, entrega en 15 días."
              onChange={(event) => setConditions(event.target.value)}
            />
          </div>
        </div>

        <p className="small text-muted mb-0">
          Los totales finales los calcula el backend (BR-062) y cada línea con producto congela el
          precio vigente al guardar (BR-063): cambios futuros de precio no modifican este
          presupuesto.
        </p>
      </form>
    </Modal>
  );
}
