'use client';

/**
 * Detalle de un presupuesto (api-spec §15) con las líneas congeladas y las
 * acciones de transición: Enviar (BR-061), Aceptar + pedido atómico
 * (BR-064/BR-065), Rechazar y Anular — cada una validada en backend contra
 * su permiso (BR-003) y estado (BR-064).
 */

import { useEffect, useState } from 'react';
import { useAuth } from '@/features/auth/auth-provider';
import { ApiError } from '@/services/api-client';
import {
  acceptQuote,
  cancelQuote,
  getQuote,
  rejectQuote,
  sendQuote,
} from '@/services/presupuestos';
import { formatDate, formatDateTime, formatMoney } from '@/lib/format';
import { ESTADO_PRESUPUESTO_BADGE, ESTADO_PRESUPUESTO_LABELS, type Quote } from '@/types/api';
import { Modal } from '@/components/ui/modal';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { AppTextarea } from '@/components/ui/form-fields';
import { LoadingSpinner } from '@/components/ui/loading-spinner';

interface PresupuestoDetailModalProps {
  open: boolean;
  quoteId: number;
  onClose: () => void;
  /** Se dispara después de una transición exitosa (para recargar la lista). */
  onChanged: () => void;
}

export function PresupuestoDetailModal({ open, quoteId, onClose, onChanged }: PresupuestoDetailModalProps) {
  const { can } = useAuth();

  const [quote, setQuote] = useState<Quote | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [reason, setReason] = useState('');
  const [actionError, setActionError] = useState<string | null>(null);
  const [acting, setActing] = useState(false);

  // Carga inicial: `loading` arranca en true, así que el efecto sólo
  // dispara la petición y actualiza el estado en los callbacks.
  useEffect(() => {
    const controller = new AbortController();
    getQuote(quoteId, controller.signal)
      .then((result) => setQuote(result))
      .catch((err: unknown) => {
        if (!controller.signal.aborted) {
          setLoadError(err instanceof Error ? err.message : 'No se pudo cargar el presupuesto.');
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      });
    return () => controller.abort();
  }, [quoteId]);

  async function run(action: () => Promise<Quote>) {
    setActing(true);
    setActionError(null);
    try {
      const updated = await action();
      setQuote(updated);
      setReason('');
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

  const puedeEditar = can('PRESUPUESTOS_UPDATE');
  const puedeAnular = can('PRESUPUESTOS_ANULATE');
  const puedeAceptarPermiso = can('PRESUPUESTOS_ACCEPT');

  const puedeEnviar = quote?.status === 0 && puedeEditar;
  const puedeAceptar = quote?.status === 1 && puedeAceptarPermiso && !quote?.expired;
  const puedeRechazar = quote?.status === 1 && puedeEditar;
  const puedeAnularAccion =
    puedeAnular && (quote?.status === 0 || quote?.status === 1);

  return (
    <Modal
      open={open}
      title={quote ? `Presupuesto #${quote.id}` : 'Presupuesto'}
      onClose={onClose}
      size="modal-lg"
      footer={
        <>
          <AppButton variant="secondary" onClick={onClose} disabled={acting}>
            Cerrar
          </AppButton>
          {puedeEnviar ? (
            <AppButton
              loading={acting}
              icon={<i className="bi bi-send me-1" aria-hidden="true" />}
              onClick={() => run(() => sendQuote(quoteId))}
            >
              Enviar al cliente
            </AppButton>
          ) : null}
          {puedeAceptar ? (
            <AppButton
              variant="success"
              loading={acting}
              icon={<i className="bi bi-check-circle me-1" aria-hidden="true" />}
              onClick={() => run(() => acceptQuote(quoteId))}
            >
              Aceptar y crear pedido
            </AppButton>
          ) : null}
          {puedeRechazar ? (
            <AppButton
              variant="outline-danger"
              loading={acting}
              icon={<i className="bi bi-x-circle me-1" aria-hidden="true" />}
              onClick={() => run(() => rejectQuote(quoteId, reason.trim() || undefined))}
            >
              Rechazar
            </AppButton>
          ) : null}
          {puedeAnularAccion ? (
            <AppButton
              variant="outline-secondary"
              loading={acting}
              icon={<i className="bi bi-slash-circle me-1" aria-hidden="true" />}
              onClick={() => run(() => cancelQuote(quoteId, reason.trim() || undefined))}
            >
              Anular
            </AppButton>
          ) : null}
        </>
      }
    >
      {loading ? (
        <LoadingSpinner label="Cargando presupuesto..." />
      ) : loadError ? (
        <AppAlert variant="danger" title="Error al cargar">
          {loadError}
        </AppAlert>
      ) : quote ? (
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
                <dd className="col-7">{quote.customerName}</dd>
                <dt className="col-5 text-muted fw-normal">Creado</dt>
                <dd className="col-7">{formatDateTime(quote.creationDate)}</dd>
                <dt className="col-5 text-muted fw-normal">Válido hasta</dt>
                <dd className="col-7">
                  {quote.expirationDate ? formatDate(quote.expirationDate) : 'Sin vencimiento'}
                  {quote.expired ? (
                    <span className="badge text-bg-warning ms-2">Vencido</span>
                  ) : null}
                </dd>
                <dt className="col-5 text-muted fw-normal">Enviado</dt>
                <dd className="col-7">{quote.sendDate ? formatDateTime(quote.sendDate) : '—'}</dd>
              </dl>
            </div>
            <div className="col-sm-6">
              <dl className="row mb-0">
                <dt className="col-5 text-muted fw-normal">Estado</dt>
                <dd className="col-7">
                  <span className={`badge ${ESTADO_PRESUPUESTO_BADGE[quote.status]}`}>
                    {ESTADO_PRESUPUESTO_LABELS[quote.status]}
                  </span>
                  {quote.orderId !== null ? (
                    <span className="badge text-bg-light border ms-2">
                      <i className="bi bi-cart-check me-1" aria-hidden="true" />
                      Pedido #{quote.orderId}
                    </span>
                  ) : null}
                </dd>
                {quote.stateReason ? (
                  <>
                    <dt className="col-5 text-muted fw-normal">Motivo</dt>
                    <dd className="col-7">{quote.stateReason}</dd>
                  </>
                ) : null}
                <dt className="col-5 text-muted fw-normal">Notas</dt>
                <dd className="col-7">{quote.notes || '—'}</dd>
                <dt className="col-5 text-muted fw-normal">Condiciones</dt>
                <dd className="col-7">{quote.conditions || '—'}</dd>
              </dl>
            </div>
          </div>

          <div className="table-responsive">
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
                {quote.items.map((item) => (
                  <tr key={item.id}>
                    <td>
                      {item.description}
                      {item.productId !== null ? (
                        <span className="badge text-bg-light border ms-2 small">Catálogo</span>
                      ) : (
                        <span className="badge text-bg-light border ms-2 small">A medida</span>
                      )}
                    </td>
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
                  <td colSpan={4} className="text-end text-muted">
                    Subtotal
                  </td>
                  <td className="text-end font-monospace">{formatMoney(quote.subtotal)}</td>
                </tr>
                <tr>
                  <td colSpan={4} className="text-end text-muted">
                    Descuentos
                  </td>
                  <td className="text-end font-monospace">−{formatMoney(quote.discount)}</td>
                </tr>
                <tr className="fw-semibold">
                  <td colSpan={4} className="text-end">
                    Total
                  </td>
                  <td className="text-end font-monospace">{formatMoney(quote.total)}</td>
                </tr>
              </tfoot>
            </table>
          </div>

          {puedeRechazar || puedeAnularAccion ? (
            <AppTextarea
              label="Motivo (opcional)"
              name="reason"
              rows={2}
              value={reason}
              hint="Se registra en el presupuesto al rechazar o anular."
              onChange={(event) => setReason(event.target.value)}
            />
          ) : null}

          {quote.expired && quote.status <= 1 ? (
            <p className="small text-muted mb-0">
              Este presupuesto está vencido: no puede aceptarse (BR-064).
            </p>
          ) : null}
        </>
      ) : null}
    </Modal>
  );
}
