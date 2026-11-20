'use client';

/**
 * Detalle de un pedido (api-spec §16) con las líneas congeladas del
 * presupuesto aceptado (BR-071), las acciones de transición del ciclo de
 * vida (BR-072/BR-073) — cada una validada en backend contra su permiso
 * (BR-003: PEDIDOS_UPDATE, o PEDIDOS_CANCEL para anular) —, el seguimiento
 * de producción/entrega con edición de notas y entrega estimada (BR-076,
 * PEDIDOS_UPDATE), los pagos con saldo calculado por backend (BR-083,
 * PAGOS_VIEW/PAGOS_CREATE) y el historial inmutable de estados (BR-075).
 */

import { useEffect, useState } from 'react';
import { useAuth } from '@/features/auth/auth-provider';
import { ApiError } from '@/services/api-client';
import { changeOrderStatus, getOrder, getOrderHistory, updateOrder } from '@/services/pedidos';
import { getOrderPayments, registerPayment } from '@/services/pagos';
import { formatDate, formatDateTime, formatMoney } from '@/lib/format';
import {
  ESTADO_PEDIDO_BADGE,
  ESTADO_PEDIDO_LABELS,
  METODO_PAGO_LABELS,
  type EstadoPedido,
  type MetodoPago,
  type Order,
  type OrderHistoryItem,
  type OrderPayments,
} from '@/types/api';
import { Modal } from '@/components/ui/modal';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { AppInput, AppSelect, AppTextarea } from '@/components/ui/form-fields';
import { LoadingSpinner } from '@/components/ui/loading-spinner';

interface OrderDetailModalProps {
  open: boolean;
  orderId: number;
  onClose: () => void;
  /** Se dispara después de una transición exitosa (para recargar la lista). */
  onChanged: () => void;
}

/** Transición principal de cada estado (BR-073). */
const SIGUIENTE: Partial<
  Record<EstadoPedido, { status: EstadoPedido; label: string; icon: string }>
> = {
  0: { status: 1, label: 'Confirmar', icon: 'bi-check-circle' },
  1: { status: 2, label: 'Iniciar producción', icon: 'bi-gear' },
  2: { status: 3, label: 'Marcar listo', icon: 'bi-box-seam' },
  3: { status: 4, label: 'Entregar', icon: 'bi-truck' },
  4: { status: 6, label: 'Completar', icon: 'bi-check-all' },
};

/** La anulación sólo se permite desde estos estados (BR-073). */
const CANCELABLES: EstadoPedido[] = [0, 1, 2, 3];

/** Estados finales inmutables (BR-074): no admiten edición (BR-076). */
const FINALES: EstadoPedido[] = [5, 6];

/** 'YYYY-MM-DD' para el input date, desde un ISO opcional. */
const soloFecha = (iso: string | null | undefined): string => (iso ? iso.slice(0, 10) : '');

/** Opciones del select de método de pago (api-spec §17). */
const METODO_PAGO_OPTIONS = Object.entries(METODO_PAGO_LABELS).map(([value, label]) => ({
  value,
  label,
}));

export function OrderDetailModal({ open, orderId, onClose, onChanged }: OrderDetailModalProps) {
  const { can } = useAuth();

  const [order, setOrder] = useState<Order | null>(null);
  const [history, setHistory] = useState<OrderHistoryItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [comment, setComment] = useState('');
  const [actionError, setActionError] = useState<string | null>(null);
  const [acting, setActing] = useState(false);

  const [editNotes, setEditNotes] = useState('');
  const [editEstimate, setEditEstimate] = useState('');
  const [editError, setEditError] = useState<string | null>(null);

  const [payments, setPayments] = useState<OrderPayments | null>(null);
  const [payAmount, setPayAmount] = useState('');
  const [payMethod, setPayMethod] = useState<MetodoPago>(0);
  const [payReference, setPayReference] = useState('');
  const [payNotes, setPayNotes] = useState('');
  const [payError, setPayError] = useState<string | null>(null);
  const [paying, setPaying] = useState(false);

  // Carga inicial: `loading` arranca en true, así que el efecto sólo
  // dispara las peticiones y actualiza el estado en los callbacks.
  useEffect(() => {
    const controller = new AbortController();
    Promise.all([
      getOrder(orderId, controller.signal),
      getOrderHistory(orderId, controller.signal),
    ])
      .then(([pedido, historial]) => {
        setOrder(pedido);
        setHistory(historial);
        setEditNotes(pedido.notes ?? '');
        setEditEstimate(soloFecha(pedido.estimatedDeliveryDate));
      })
      .catch((err: unknown) => {
        if (!controller.signal.aborted) {
          setLoadError(err instanceof Error ? err.message : 'No se pudo cargar el pedido.');
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      });
    return () => controller.abort();
  }, [orderId]);

  // Pagos (api-spec §17): sólo si hay PAGOS_VIEW, para no romper la carga
  // del modal a usuarios sin permiso (el backend responde 403). La
  // sección se renderiza sólo con verPagos && payments, así que un permiso
  // revocado en caliente simplemente oculta el bloque.
  const verPagos = can('PAGOS_VIEW');

  useEffect(() => {
    if (!verPagos) {
      return;
    }

    const controller = new AbortController();
    getOrderPayments(orderId, controller.signal)
      .then((data) => setPayments(data))
      .catch(() => {
        if (!controller.signal.aborted) {
          setPayments(null);
        }
      });
    return () => controller.abort();
  }, [orderId, verPagos]);

  async function run(target: EstadoPedido) {
    setActing(true);
    setActionError(null);
    try {
      const updated = await changeOrderStatus(orderId, {
        status: target,
        comment: comment.trim() || null,
      });
      setOrder(updated);
      setComment('');
      const historial = await getOrderHistory(orderId);
      setHistory(historial);
      onChanged();
    } catch (err) {
      if (err instanceof ApiError) {
        setActionError(err.message);
      } else {
        setActionError('No se pudo ejecutar la acción.');
      }
    } finally {
      setActing(false);
    }
  }

  /** Guarda la edición de seguimiento (PATCH, BR-076); backend valida todo. */
  async function saveTracking(event: React.FormEvent) {
    event.preventDefault();
    setActing(true);
    setEditError(null);
    try {
      const updated = await updateOrder(orderId, {
        notes: editNotes.trim() || null,
        estimatedDeliveryDate: editEstimate || null,
      });
      setOrder(updated);
      setEditNotes(updated.notes ?? '');
      setEditEstimate(soloFecha(updated.estimatedDeliveryDate));
      onChanged();
    } catch (err) {
      if (err instanceof ApiError) {
        setEditError(err.message);
      } else {
        setEditError('No se pudieron guardar los cambios.');
      }
    } finally {
      setActing(false);
    }
  }

  /**
   * Registra un pago (POST, BR-080/BR-081): el backend valida el saldo y
   * devuelve 409 si excede el pendiente; este formulario sólo mejora la UX.
   */
  async function submitPayment(event: React.FormEvent) {
    event.preventDefault();
    const amount = Number(payAmount);
    if (!Number.isFinite(amount) || amount <= 0) {
      setPayError('El monto debe ser mayor a cero (BR-080).');
      return;
    }

    setPaying(true);
    setPayError(null);
    try {
      await registerPayment(orderId, {
        amount,
        paymentMethod: payMethod,
        reference: payReference.trim() || null,
        notes: payNotes.trim() || null,
      });
      setPayments(await getOrderPayments(orderId));
      setPayAmount('');
      setPayReference('');
      setPayNotes('');
      onChanged();
    } catch (err) {
      if (err instanceof ApiError) {
        setPayError(err.message);
      } else {
        setPayError('No se pudo registrar el pago.');
      }
    } finally {
      setPaying(false);
    }
  }

  const estado = order?.status;
  const puedeActualizar = can('PEDIDOS_UPDATE');
  const puedeAnular = can('PEDIDOS_CANCEL');
  const puedeRegistrarPago = can('PAGOS_CREATE');
  const transicion = estado !== undefined ? SIGUIENTE[estado] : undefined;
  const puedeAvanzar = transicion !== undefined && puedeActualizar;
  const puedeAnularAccion =
    estado !== undefined && CANCELABLES.includes(estado) && puedeAnular;
  const esFinal = estado !== undefined && FINALES.includes(estado);
  const puedeEditarSeguimiento = estado !== undefined && !esFinal && puedeActualizar;
  const admitePagos =
    estado !== undefined && estado !== 5 && (payments?.balance ?? 0) > 0;

  return (
    <Modal
      open={open}
      title={order ? `Pedido #${order.id}` : 'Pedido'}
      onClose={onClose}
      size="modal-lg"
      footer={
        <>
          <AppButton variant="secondary" onClick={onClose} disabled={acting}>
            Cerrar
          </AppButton>
          {puedeAvanzar && transicion ? (
            <AppButton
              loading={acting}
              icon={<i className={`bi ${transicion.icon} me-1`} aria-hidden="true" />}
              onClick={() => run(transicion.status)}
            >
              {transicion.label}
            </AppButton>
          ) : null}
          {puedeAnularAccion ? (
            <AppButton
              variant="outline-danger"
              loading={acting}
              icon={<i className="bi bi-slash-circle me-1" aria-hidden="true" />}
              onClick={() => run(5)}
            >
              Anular pedido
            </AppButton>
          ) : null}
        </>
      }
    >
      {loading ? (
        <LoadingSpinner label="Cargando pedido..." />
      ) : loadError ? (
        <AppAlert variant="danger" title="Error al cargar">
          {loadError}
        </AppAlert>
      ) : order ? (
        <>
          {actionError ? (
            <AppAlert variant="danger" onClose={() => setActionError(null)}>
              {actionError}
            </AppAlert>
          ) : null}

          <div className="row small mb-3">
            <div className="col-sm-6">
              <dl className="row mb-0">
                <dt className="col-5 text-muted fw-normal">Cliente</dt>
                <dd className="col-7">{order.customerName}</dd>
                <dt className="col-5 text-muted fw-normal">Creado</dt>
                <dd className="col-7">{formatDateTime(order.creationDate)}</dd>
                <dt className="col-5 text-muted fw-normal">Presupuesto</dt>
                <dd className="col-7">
                  {order.quoteId !== null ? (
                    <span className="badge text-bg-light border">Presupuesto #{order.quoteId}</span>
                  ) : (
                    '—'
                  )}
                </dd>
              </dl>
            </div>
            <div className="col-sm-6">
              <dl className="row mb-0">
                <dt className="col-5 text-muted fw-normal">Estado</dt>
                <dd className="col-7">
                  <span className={`badge ${ESTADO_PEDIDO_BADGE[order.status]}`}>
                    {ESTADO_PEDIDO_LABELS[order.status]}
                  </span>
                </dd>
                <dt className="col-5 text-muted fw-normal">Total</dt>
                <dd className="col-7 font-monospace">{formatMoney(order.total)}</dd>
              </dl>
            </div>
          </div>

          <div className="table-responsive mb-3">
            <table className="table table-sm align-middle">
              <thead>
                <tr className="small text-muted">
                  <th>Descripción</th>
                  <th className="text-center">Cant.</th>
                  <th className="text-end">Precio</th>
                  <th className="text-end">Descuento</th>
                  <th className="text-end">Subtotal</th>
                </tr>
              </thead>
              <tbody>
                {order.items.map((item) => (
                  <tr key={item.id}>
                    <td>{item.description}</td>
                    <td className="text-center">{item.quantity}</td>
                    <td className="text-end font-monospace">{formatMoney(item.unitPrice)}</td>
                    <td className="text-end font-monospace">
                      {item.discount > 0 ? `−${formatMoney(item.discount)}` : '—'}
                    </td>
                    <td className="text-end font-monospace">{formatMoney(item.subtotal)}</td>
                  </tr>
                ))}
              </tbody>
              <tfoot className="small">
                <tr>
                  <td colSpan={4} className="text-end fw-semibold">
                    Total del pedido
                  </td>
                  <td className="text-end font-monospace fw-semibold">
                    {formatMoney(order.total)}
                  </td>
                </tr>
              </tfoot>
            </table>
          </div>

          <h2 className="h6 mt-3 mb-2">Seguimiento</h2>
          <div className="row small mb-3">
            <div className="col-sm-6">
              <dl className="row mb-0">
                <dt className="col-6 text-muted fw-normal">Inicio de producción</dt>
                <dd className="col-6">
                  {order.productionStartDate ? formatDateTime(order.productionStartDate) : '—'}
                </dd>
                <dt className="col-6 text-muted fw-normal">Fin de producción</dt>
                <dd className="col-6">
                  {order.productionEndDate ? formatDateTime(order.productionEndDate) : '—'}
                </dd>
              </dl>
            </div>
            <div className="col-sm-6">
              <dl className="row mb-0">
                <dt className="col-6 text-muted fw-normal">Entrega estimada</dt>
                <dd className="col-6">
                  {order.estimatedDeliveryDate ? formatDate(order.estimatedDeliveryDate) : '—'}
                </dd>
                <dt className="col-6 text-muted fw-normal">Entrega real</dt>
                <dd className="col-6">
                  {order.deliveryDate ? formatDateTime(order.deliveryDate) : '—'}
                </dd>
              </dl>
            </div>
          </div>

          {puedeEditarSeguimiento ? (
            <form onSubmit={saveTracking} className="mb-3">
              <div className="row">
                <div className="col-sm-4">
                  <AppInput
                    label="Entrega estimada"
                    name="estimatedDeliveryDate"
                    type="date"
                    value={editEstimate}
                    onChange={(event) => setEditEstimate(event.target.value)}
                  />
                </div>
                <div className="col-sm-8">
                  <AppTextarea
                    label="Notas"
                    name="notes"
                    rows={2}
                    maxLength={2000}
                    value={editNotes}
                    hint="Hasta 2000 caracteres; dejar vacío para limpiar (BR-076)."
                    onChange={(event) => setEditNotes(event.target.value)}
                  />
                </div>
              </div>
              {editError ? (
                <AppAlert variant="danger" onClose={() => setEditError(null)}>
                  {editError}
                </AppAlert>
              ) : null}
              <AppButton
                type="submit"
                variant="outline-secondary"
                loading={acting}
                icon={<i className="bi bi-save me-1" aria-hidden="true" />}
              >
                Guardar seguimiento
              </AppButton>
            </form>
          ) : order.notes ? (
            <p className="small mb-3">
              <span className="text-muted">Notas:</span> {order.notes}
            </p>
          ) : null}

          {verPagos && payments ? (
            <>
              <h2 className="h6 mt-4 mb-2">Pagos</h2>
              <div className="d-flex flex-wrap gap-4 small mb-3">
                <div>
                  <span className="text-muted">Total</span>
                  <div className="font-monospace">{formatMoney(order.total)}</div>
                </div>
                <div>
                  <span className="text-muted">Pagado</span>
                  <div className="font-monospace">{formatMoney(payments.totalPaid)}</div>
                </div>
                <div>
                  <span className="text-muted">Saldo pendiente</span>
                  <div
                    className={`font-monospace fw-semibold ${
                      payments.balance > 0 ? 'text-danger' : 'text-success'
                    }`}
                  >
                    {formatMoney(payments.balance)}
                  </div>
                </div>
              </div>

              {puedeRegistrarPago && admitePagos ? (
                <form onSubmit={submitPayment} className="mb-3">
                  <div className="row">
                    <div className="col-sm-3">
                      <AppInput
                        label="Monto"
                        name="payAmount"
                        type="number"
                        min="0.01"
                        step="0.01"
                        required
                        value={payAmount}
                        onChange={(event) => setPayAmount(event.target.value)}
                      />
                    </div>
                    <div className="col-sm-3">
                      <AppSelect
                        label="Método"
                        name="payMethod"
                        options={METODO_PAGO_OPTIONS}
                        value={String(payMethod)}
                        onChange={(event) =>
                          setPayMethod(Number(event.target.value) as MetodoPago)
                        }
                      />
                    </div>
                    <div className="col-sm-3">
                      <AppInput
                        label="Referencia"
                        name="payReference"
                        maxLength={100}
                        value={payReference}
                        placeholder="TRX-12345"
                        onChange={(event) => setPayReference(event.target.value)}
                      />
                    </div>
                    <div className="col-sm-3">
                      <AppInput
                        label="Notas"
                        name="payNotes"
                        maxLength={500}
                        value={payNotes}
                        onChange={(event) => setPayNotes(event.target.value)}
                      />
                    </div>
                  </div>
                  {payError ? (
                    <AppAlert variant="danger" onClose={() => setPayError(null)}>
                      {payError}
                    </AppAlert>
                  ) : null}
                  <AppButton
                    type="submit"
                    loading={paying}
                    icon={<i className="bi bi-cash-coin me-1" aria-hidden="true" />}
                  >
                    Registrar pago
                  </AppButton>
                </form>
              ) : estado === 5 ? (
                <p className="small text-muted mb-3">
                  Los pedidos cancelados no admiten nuevos pagos.
                </p>
              ) : payments.balance <= 0 && payments.items.length > 0 ? (
                <p className="small text-success mb-3">
                  <i className="bi bi-check-circle me-1" aria-hidden="true" />
                  Pedido saldado.
                </p>
              ) : null}

              {payments.items.length === 0 ? (
                <p className="small text-muted mb-0">Sin pagos registrados.</p>
              ) : (
                <div className="table-responsive mb-3">
                  <table className="table table-sm align-middle">
                    <thead>
                      <tr className="small text-muted">
                        <th>Fecha</th>
                        <th>Método</th>
                        <th>Referencia</th>
                        <th>Registrado por</th>
                        <th className="text-end">Monto</th>
                      </tr>
                    </thead>
                    <tbody>
                      {payments.items.map((pago) => (
                        <tr key={pago.id}>
                          <td>{formatDateTime(pago.date)}</td>
                          <td>
                            <span className="badge text-bg-light border">
                              {METODO_PAGO_LABELS[pago.paymentMethod]}
                            </span>
                          </td>
                          <td>{pago.reference ?? '—'}</td>
                          <td>{pago.userName ?? '—'}</td>
                          <td className="text-end font-monospace">
                            {formatMoney(pago.amount)}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </>
          ) : null}

          {puedeAvanzar || puedeAnularAccion ? (
            <AppTextarea
              label="Comentario (opcional)"
              name="comment"
              rows={2}
              value={comment}
              hint="Se registra en el historial de estados junto con tu usuario (BR-075)."
              onChange={(event) => setComment(event.target.value)}
            />
          ) : null}

          <h2 className="h6 mt-4 mb-2">Historial de estados</h2>
          {history.length === 0 ? (
            <p className="small text-muted mb-0">Sin movimientos registrados.</p>
          ) : (
            <ul className="list-group list-group-flush small">
              {history.map((fila) => (
                <li key={fila.id} className="list-group-item px-0 py-2">
                  <div className="d-flex flex-wrap align-items-center gap-2">
                    <span className="text-muted">{formatDateTime(fila.date)}</span>
                    {fila.previousStatus !== null ? (
                      <>
                        <span className={`badge ${ESTADO_PEDIDO_BADGE[fila.previousStatus]}`}>
                          {ESTADO_PEDIDO_LABELS[fila.previousStatus]}
                        </span>
                        <i className="bi bi-arrow-right" aria-hidden="true" />
                      </>
                    ) : (
                      <i className="bi bi-plus-circle" aria-hidden="true" />
                    )}
                    <span className={`badge ${ESTADO_PEDIDO_BADGE[fila.newStatus]}`}>
                      {ESTADO_PEDIDO_LABELS[fila.newStatus]}
                    </span>
                    <span className="ms-auto text-muted">
                      {fila.userName ?? 'Sistema'}
                    </span>
                  </div>
                  {fila.comment ? <div className="text-muted mt-1">{fila.comment}</div> : null}
                </li>
              ))}
            </ul>
          )}
        </>
      ) : null}
    </Modal>
  );
}
