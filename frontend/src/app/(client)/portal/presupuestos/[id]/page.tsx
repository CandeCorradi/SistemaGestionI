'use client';

/**
 * Detalle de presupuesto del Portal Cliente (functional-spec §23): snapshot
 * congelado de líneas y totales (BR-063), aceptación con creación atómica
 * del pedido (BR-064/BR-065) y enlace al pedido creado.
 */

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useParams } from 'next/navigation';
import { ApiError } from '@/services/api-client';
import { acceptMyQuote, getMyQuote } from '@/services/portal';
import { formatDate, formatMoney } from '@/lib/format';
import {
  ESTADO_PRESUPUESTO_BADGE,
  ESTADO_PRESUPUESTO_LABELS,
  type Quote,
} from '@/types/api';
import { PanelCard } from '@/components/ui/cards';
import { DataTable } from '@/components/ui/data-table';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { LoadingSpinner } from '@/components/ui/loading-spinner';

export default function PortalQuoteDetailPage() {
  const params = useParams<{ id: string }>();
  const id = Number(params.id);

  const [quote, setQuote] = useState<Quote | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [accepting, setAccepting] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [accepted, setAccepted] = useState<number | null>(null);

  const validId = Number.isInteger(id) && id > 0;

  useEffect(() => {
    if (!validId) {
      return;
    }
    let cancelled = false;
    const controller = new AbortController();

    const load = async () => {
      try {
        const result = await getMyQuote(id, controller.signal);
        if (!cancelled) {
          setQuote(result);
          setError(null);
        }
      } catch (err) {
        if (!cancelled && (err as { name?: string }).name !== 'AbortError') {
          setError(
            err instanceof ApiError && err.isNotFound
              ? 'Este presupuesto no existe o no te pertenece.'
              : err instanceof Error
                ? err.message
                : 'No se pudo cargar el presupuesto.'
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

  async function onAccept() {
    setAccepting(true);
    setActionError(null);
    try {
      const updated = await acceptMyQuote(id);
      setQuote(updated);
      setAccepted(updated.orderId ?? updated.id);
      setConfirmOpen(false);
    } catch (err) {
      setActionError(
        err instanceof ApiError && err.isConflict
          ? err.message
          : err instanceof Error
            ? err.message
            : 'No se pudo aceptar el presupuesto.'
      );
      setConfirmOpen(false);
    } finally {
      setAccepting(false);
    }
  }

  if (!validId) {
    return (
      <>
        <AppAlert variant="danger" title="Presupuesto no válido">
          El presupuesto indicado no existe.
        </AppAlert>
        <Link href="/portal/presupuestos" className="btn btn-outline-secondary">
          Volver a mis presupuestos
        </Link>
      </>
    );
  }

  if (error && !quote) {
    return (
      <>
        <AppAlert variant="danger" title="No se pudo cargar">
          {error}
        </AppAlert>
        <Link href="/portal/presupuestos" className="btn btn-outline-secondary">
          Volver a mis presupuestos
        </Link>
      </>
    );
  }

  if (!quote) {
    return <LoadingSpinner label="Cargando presupuesto..." />;
  }

  return (
    <>
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-2">
        <div>
          <Link href="/portal/presupuestos" className="small text-decoration-none">
            <i className="bi bi-arrow-left me-1" aria-hidden="true" />
            Mis presupuestos
          </Link>
          <h1 className="h4 mb-1">Presupuesto #{quote.id}</h1>
          <p className="text-muted mb-0 small">
            Solicitado el {formatDate(quote.creationDate)}
            {quote.sendDate ? ` · enviado el ${formatDate(quote.sendDate)}` : ''}
          </p>
        </div>
        <span className={`badge ${ESTADO_PRESUPUESTO_BADGE[quote.status]} fs-6`}>
          {ESTADO_PRESUPUESTO_LABELS[quote.status]}
        </span>
      </div>

      {actionError ? (
        <AppAlert variant="danger" title="No se pudo aceptar" onClose={() => setActionError(null)}>
          {actionError}
        </AppAlert>
      ) : null}

      {accepted !== null ? (
        <AppAlert variant="success" title="Presupuesto aceptado">
          Aceptaste el presupuesto y se creó tu pedido.{' '}
          <Link href={`/portal/pedidos/${quote.orderId ?? accepted}`} className="alert-link">
            Ver pedido
          </Link>
        </AppAlert>
      ) : null}

      <div className="row g-3">
        <div className="col-12 col-lg-8">
          <PanelCard title="Detalle del trabajo">
            <DataTable
              columns={[
                {
                  header: 'Descripción',
                  render: (row: Quote['items'][number]) => row.description,
                },
                {
                  header: 'Cant.',
                  render: (row: Quote['items'][number]) => row.quantity,
                  className: 'text-center',
                },
                {
                  header: 'Precio',
                  render: (row: Quote['items'][number]) => (
                    <span className="font-monospace">{formatMoney(row.unitPrice)}</span>
                  ),
                  className: 'text-end',
                },
                {
                  header: 'Subtotal',
                  render: (row: Quote['items'][number]) => (
                    <span className="font-monospace">{formatMoney(row.subtotal)}</span>
                  ),
                  className: 'text-end',
                },
              ]}
              rows={quote.items}
              rowKey={(row) => row.id}
            />
            <div className="border-top pt-3 mt-3 text-end">
              <div className="small">Subtotal: {formatMoney(quote.subtotal)}</div>
              <div className="small">Descuento: {formatMoney(quote.discount)}</div>
              <div className="fs-5 fw-semibold">Total: {formatMoney(quote.total)}</div>
            </div>
            {quote.status === 0 ? (
              <p className="small text-muted mb-0 mt-2">
                Presupuesto en preparación: el equipo lo está cotizando.
              </p>
            ) : null}
          </PanelCard>
        </div>

        <div className="col-12 col-lg-4">
          <PanelCard title="Información">
            <dl className="small mb-0">
              <dt>Validez</dt>
              <dd>{quote.expirationDate ? formatDate(quote.expirationDate) : 'Sin vencimiento'}</dd>
              {quote.notes ? (
                <>
                  <dt>Notas</dt>
                  <dd>{quote.notes}</dd>
                </>
              ) : null}
              {quote.conditions ? (
                <>
                  <dt>Condiciones</dt>
                  <dd>{quote.conditions}</dd>
                </>
              ) : null}
            </dl>

            {quote.status === 1 && quote.orderId === null ? (
              <AppButton
                className="w-100 mt-3"
                icon={<i className="bi bi-check2-circle me-1" aria-hidden="true" />}
                onClick={() => setConfirmOpen(true)}
              >
                Aceptar presupuesto
              </AppButton>
            ) : null}

            {quote.orderId !== null ? (
              <Link
                href={`/portal/pedidos/${quote.orderId}`}
                className="btn btn-outline-primary w-100 mt-3"
              >
                Ver pedido #{quote.orderId}
              </Link>
            ) : null}

            {quote.status === 1 ? (
              <p className="small text-muted mb-0 mt-2">
                Al aceptar se crea tu pedido automáticamente (BR-064).
              </p>
            ) : null}
          </PanelCard>
        </div>
      </div>

      <ConfirmDialog
        open={confirmOpen}
        title="Aceptar presupuesto"
        message={
          <>
            ¿Aceptás el presupuesto #{quote.id} por{' '}
            <strong>{formatMoney(quote.total)}</strong>? Se creará tu pedido y no se puede deshacer.
          </>
        }
        confirmLabel="Aceptar"
        loading={accepting}
        onConfirm={() => void onAccept()}
        onCancel={() => setConfirmOpen(false)}
      />
    </>
  );
}
