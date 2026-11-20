'use client';

/**
 * Detalle de pedido del Portal Cliente (functional-spec §23): líneas
 * congeladas (BR-071), seguimiento del historial de estados (BR-075) y
 * pagos con saldo calculado por el backend (BR-083).
 */

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useParams } from 'next/navigation';
import { ApiError } from '@/services/api-client';
import { getMyOrder, getMyOrderHistory, getMyOrderPayments } from '@/services/portal';
import { formatDate, formatDateTime, formatMoney } from '@/lib/format';
import {
  ESTADO_PEDIDO_BADGE,
  ESTADO_PEDIDO_LABELS,
  METODO_PAGO_LABELS,
  type Order,
  type OrderHistoryItem,
  type OrderPayments,
} from '@/types/api';
import { PanelCard, StatCard } from '@/components/ui/cards';
import { DataTable } from '@/components/ui/data-table';
import { AppAlert } from '@/components/ui/app-alert';
import { LoadingSpinner } from '@/components/ui/loading-spinner';

export default function PortalOrderDetailPage() {
  const params = useParams<{ id: string }>();
  const id = Number(params.id);

  const [order, setOrder] = useState<Order | null>(null);
  const [history, setHistory] = useState<OrderHistoryItem[]>([]);
  const [payments, setPayments] = useState<OrderPayments | null>(null);
  const [error, setError] = useState<string | null>(null);

  const validId = Number.isInteger(id) && id > 0;

  useEffect(() => {
    if (!validId) {
      return;
    }
    let cancelled = false;
    const controller = new AbortController();

    const load = async () => {
      try {
        const [orderResult, historyResult, paymentsResult] = await Promise.all([
          getMyOrder(id, controller.signal),
          getMyOrderHistory(id, controller.signal),
          getMyOrderPayments(id, controller.signal),
        ]);
        if (!cancelled) {
          setOrder(orderResult);
          setHistory(historyResult);
          setPayments(paymentsResult);
          setError(null);
        }
      } catch (err) {
        if (!cancelled && (err as { name?: string }).name !== 'AbortError') {
          setError(
            err instanceof ApiError && err.isNotFound
              ? 'Este pedido no existe o no te pertenece.'
              : err instanceof Error
                ? err.message
                : 'No se pudo cargar el pedido.'
          );
        }
      }
    };

    void load();
    return () => {
      cancelled = true;
      controller.abort();
    };
  }, [id, validId]);

  if (!validId) {
    return (
      <>
        <AppAlert variant="danger" title="Pedido no válido">
          El pedido indicado no existe.
        </AppAlert>
        <Link href="/portal/pedidos" className="btn btn-outline-secondary">
          Volver a mis pedidos
        </Link>
      </>
    );
  }

  if (error && !order) {
    return (
      <>
        <AppAlert variant="danger" title="No se pudo cargar">
          {error}
        </AppAlert>
        <Link href="/portal/pedidos" className="btn btn-outline-secondary">
          Volver a mis pedidos
        </Link>
      </>
    );
  }

  if (!order) {
    return <LoadingSpinner label="Cargando pedido..." />;
  }

  return (
    <>
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-2">
        <div>
          <Link href="/portal/pedidos" className="small text-decoration-none">
            <i className="bi bi-arrow-left me-1" aria-hidden="true" />
            Mis pedidos
          </Link>
          <h1 className="h4 mb-1">Pedido #{order.id}</h1>
          <p className="text-muted mb-0 small">
            Creado el {formatDate(order.creationDate)}
            {order.quoteId !== null ? (
              <>
                {' · origen: '}
                <Link href={`/portal/presupuestos/${order.quoteId}`}>
                  presupuesto #{order.quoteId}
                </Link>
              </>
            ) : null}
          </p>
        </div>
        <span className={`badge ${ESTADO_PEDIDO_BADGE[order.status]} fs-6`}>
          {ESTADO_PEDIDO_LABELS[order.status]}
        </span>
      </div>

      <div className="row g-3 mb-3">
        <div className="col-12 col-md-4">
          <StatCard
            title="Total del pedido"
            value={formatMoney(order.total)}
            icon="bi-receipt"
            tone="bg-primary"
            footer="Líneas congeladas del presupuesto"
          />
        </div>
        <div className="col-12 col-md-4">
          <StatCard
            title="Total pagado"
            value={formatMoney(payments?.totalPaid ?? 0)}
            icon="bi-cash-stack"
            tone="bg-success"
            footer={`${payments?.items.length ?? 0} pago${(payments?.items.length ?? 0) === 1 ? '' : 's'}`}
          />
        </div>
        <div className="col-12 col-md-4">
          <StatCard
            title="Saldo pendiente"
            value={formatMoney(payments?.balance ?? 0)}
            icon="bi-wallet2"
            tone={(payments?.balance ?? 0) > 0 ? 'bg-danger' : 'bg-success'}
            footer="Calculado por el backend (BR-083)"
          />
        </div>
      </div>

      <div className="row g-3">
        <div className="col-12 col-lg-7">
          <PanelCard title="Detalle">
            <DataTable
              columns={[
                {
                  header: 'Descripción',
                  render: (row: Order['items'][number]) => row.description,
                },
                {
                  header: 'Cant.',
                  render: (row: Order['items'][number]) => row.quantity,
                  className: 'text-center',
                },
                {
                  header: 'Precio',
                  render: (row: Order['items'][number]) => (
                    <span className="font-monospace">{formatMoney(row.unitPrice)}</span>
                  ),
                  className: 'text-end',
                },
                {
                  header: 'Subtotal',
                  render: (row: Order['items'][number]) => (
                    <span className="font-monospace">{formatMoney(row.subtotal)}</span>
                  ),
                  className: 'text-end',
                },
              ]}
              rows={order.items}
              rowKey={(row) => row.id}
            />
            <div className="border-top p-3 text-end fw-semibold">
              Total: {formatMoney(order.total)}
            </div>
          </PanelCard>

          <div className="d-flex flex-wrap gap-3 mt-3">
            <div className="card flex-grow-1 shadow-sm">
              <div className="card-body small">
                <span className="text-uppercase text-muted fw-semibold d-block">Seguimiento</span>
                <div className="mt-2">
                  <span className="text-muted">Entrega estimada:</span>{' '}
                  {order.estimatedDeliveryDate ? formatDate(order.estimatedDeliveryDate) : '—'}
                </div>
                <div>
                  <span className="text-muted">Producción:</span>{' '}
                  {order.productionStartDate
                    ? `${formatDate(order.productionStartDate)}${order.productionEndDate ? ` → ${formatDate(order.productionEndDate)}` : ''}`
                    : '—'}
                </div>
                <div>
                  <span className="text-muted">Entregado:</span>{' '}
                  {order.deliveryDate ? formatDate(order.deliveryDate) : '—'}
                </div>
                {order.notes ? (
                  <div className="mt-2">
                    <span className="text-muted">Notas:</span> {order.notes}
                  </div>
                ) : null}
              </div>
            </div>
          </div>
        </div>

        <div className="col-12 col-lg-5">
          <PanelCard title="Historial de estados">
            {history.length === 0 ? (
              <p className="text-muted small p-3 mb-0">Sin movimientos registrados.</p>
            ) : (
              <ul className="list-group list-group-flush">
                {history.map((entry) => (
                  <li className="list-group-item small" key={entry.id}>
                    <div className="d-flex justify-content-between gap-2">
                      <span>
                        <span className="badge text-bg-light me-1">
                          {entry.previousStatus !== null
                            ? ESTADO_PEDIDO_LABELS[entry.previousStatus]
                            : '—'}
                        </span>
                        <i className="bi bi-arrow-right mx-1 text-muted" aria-hidden="true" />
                        <span className="fw-semibold">
                          {ESTADO_PEDIDO_LABELS[entry.newStatus]}
                        </span>
                      </span>
                      <span className="text-muted text-nowrap">{formatDateTime(entry.date)}</span>
                    </div>
                    <div className="text-muted mt-1">
                      {entry.userName ? `${entry.userName} · ` : ''}
                      {entry.comment ?? 'Sin comentario'}
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </PanelCard>

          <PanelCard title="Pagos del pedido">
            {!payments || payments.items.length === 0 ? (
              <p className="text-muted small p-3 mb-0">Todavía no hay pagos registrados.</p>
            ) : (
              <ul className="list-group list-group-flush">
                {payments.items.map((payment) => (
                  <li className="list-group-item small" key={payment.id}>
                    <div className="d-flex justify-content-between gap-2">
                      <span className="fw-semibold">{formatMoney(payment.amount)}</span>
                      <span className="text-muted text-nowrap">{formatDate(payment.date)}</span>
                    </div>
                    <div className="text-muted mt-1">
                      {METODO_PAGO_LABELS[payment.paymentMethod]}
                      {payment.reference ? ` · ref. ${payment.reference}` : ''}
                    </div>
                  </li>
                ))}
              </ul>
            )}
            <div className="border-top p-3 small d-flex justify-content-between">
              <span className="text-muted">Total pagado</span>
              <span className="fw-semibold">{formatMoney(payments?.totalPaid ?? 0)}</span>
            </div>
            <div className="border-top p-3 small d-flex justify-content-between">
              <span className="text-muted">Saldo pendiente</span>
              <span
                className={`fw-semibold ${(payments?.balance ?? 0) > 0 ? 'text-danger' : 'text-success'}`}
              >
                {formatMoney(payments?.balance ?? 0)}
              </span>
            </div>
          </PanelCard>
        </div>
      </div>
    </>
  );
}
