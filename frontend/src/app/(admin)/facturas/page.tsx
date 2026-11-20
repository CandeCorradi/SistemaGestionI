'use client';

/**
 * Facturas simuladas (api-spec §17A, functional-spec §20): historial
 * global paginado con filtros de pedido/cliente/fechas y búsqueda en
 * número/cliente (FACTURAS_VIEW). La emisión (FACTURAS_CREATE) sólo
 * permite pedidos Entregado/Completado con una única factura por pedido
 * (BR-130); el documento es un snapshot inmutable (BR-141) y siempre
 * simulado (BR-131: la integración fiscal futura va detrás de
 * IBillingService).
 */

import { useCallback, useState, type FormEvent } from 'react';
import { useAuth } from '@/features/auth/auth-provider';
import { OrderDetailModal } from '@/features/pedidos/order-detail-modal';
import { createFactura, listFacturas } from '@/services/facturas';
import { ApiError } from '@/services/api-client';
import { usePagedList, type PagedQuery } from '@/hooks/usePagedList';
import { formatDateTime, formatMoney } from '@/lib/format';
import { ESTADO_FACTURA_BADGE, ESTADO_FACTURA_LABELS, type FacturaItem } from '@/types/api';
import { PanelCard } from '@/components/ui/cards';
import { DataTable } from '@/components/ui/data-table';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { AppInput, AppTextarea } from '@/components/ui/form-fields';
import { EmptyState } from '@/components/ui/empty-state';
import { LoadingSpinner } from '@/components/ui/loading-spinner';
import { Modal } from '@/components/ui/modal';
import { Pagination } from '@/components/ui/pagination';

export default function FacturasPage() {
  const { can } = useAuth();

  const [orderFilter, setOrderFilter] = useState('');
  const [dateFromFilter, setDateFromFilter] = useState('');
  const [dateToFilter, setDateToFilter] = useState('');

  const fetcher = useCallback(
    (query: PagedQuery, signal?: AbortSignal) =>
      listFacturas(
        {
          page: query.page,
          pageSize: query.pageSize,
          search: query.search,
          orderId: orderFilter === '' ? undefined : Number(orderFilter),
          dateFrom: dateFromFilter || undefined,
          dateTo: dateToFilter || undefined,
        },
        signal
      ),
    [orderFilter, dateFromFilter, dateToFilter]
  );

  const list = usePagedList<FacturaItem>({
    fetcher,
    fetcherKey: `${orderFilter}|${dateFromFilter}|${dateToFilter}`,
  });

  const [detailId, setDetailId] = useState<number | null>(null);

  const [emitOpen, setEmitOpen] = useState(false);
  const [emitOrderId, setEmitOrderId] = useState('');
  const [emitNotes, setEmitNotes] = useState('');
  const [emitError, setEmitError] = useState<string | null>(null);
  const [emitFieldError, setEmitFieldError] = useState<string | null>(null);
  const [emitting, setEmitting] = useState(false);

  if (!can('FACTURAS_VIEW')) {
    return (
      <AppAlert variant="warning" title="Sin permiso">
        No tenés permiso para ver las facturas (<code>FACTURAS_VIEW</code>).
      </AppAlert>
    );
  }

  const hayFiltros =
    list.search !== '' || orderFilter !== '' || dateFromFilter !== '' || dateToFilter !== '';

  const cerrarEmision = () => {
    setEmitOpen(false);
    setEmitOrderId('');
    setEmitNotes('');
    setEmitError(null);
    setEmitFieldError(null);
  };

  const onSubmitEmision = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setEmitError(null);
    setEmitFieldError(null);

    const orderId = Number(emitOrderId);
    if (!Number.isInteger(orderId) || orderId < 1) {
      setEmitFieldError('Ingresá el número de un pedido existente.');
      return;
    }

    setEmitting(true);
    try {
      await createFactura({ orderId, notes: emitNotes.trim() || null });
      cerrarEmision();
      list.reload();
    } catch (error) {
      if (error instanceof ApiError) {
        setEmitError(
          error.isConflict
            ? `${error.message} (sólo pedidos Entregado/Completado y una factura por pedido, BR-130)`
            : error.message
        );
      } else {
        setEmitError('No se pudo emitir la factura. Probá de nuevo.');
      }
    } finally {
      setEmitting(false);
    }
  };

  return (
    <>
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-2">
        <div>
          <h1 className="h4 mb-1">Facturas</h1>
          <p className="text-muted mb-0 small">
            {list.totalItems} factura{list.totalItems === 1 ? '' : 's'} · documentos simulados
            internos (BR-130)
          </p>
        </div>
        {can('FACTURAS_CREATE') ? (
          <AppButton
            icon={<i className="bi bi-file-earmark-plus" aria-hidden="true" />}
            onClick={() => setEmitOpen(true)}
          >
            Emitir factura
          </AppButton>
        ) : null}
      </div>

      <PanelCard
        title="Historial"
        actions={
          <div className="d-flex flex-wrap gap-2 align-items-center">
            <div className="input-group input-group-sm" style={{ width: '14rem' }}>
              <span className="input-group-text bg-white">
                <i className="bi bi-search" aria-hidden="true" />
              </span>
              <input
                type="search"
                className="form-control"
                placeholder="Número o cliente..."
                value={list.search}
                onChange={(event) => list.setSearch(event.target.value)}
                aria-label="Buscar facturas por número o cliente"
              />
            </div>
            <input
              type="number"
              className="form-control form-control-sm"
              style={{ width: '7rem' }}
              placeholder="Pedido"
              min={1}
              value={orderFilter}
              onChange={(event) => setOrderFilter(event.target.value)}
              aria-label="Filtrar facturas por número de pedido"
            />
            <input
              type="date"
              className="form-control form-control-sm"
              style={{ width: '9.5rem' }}
              value={dateFromFilter}
              onChange={(event) => setDateFromFilter(event.target.value)}
              aria-label="Filtrar facturas desde fecha"
            />
            <input
              type="date"
              className="form-control form-control-sm"
              style={{ width: '9.5rem' }}
              value={dateToFilter}
              onChange={(event) => setDateToFilter(event.target.value)}
              aria-label="Filtrar facturas hasta fecha"
            />
          </div>
        }
      >
        {list.loading ? (
          <LoadingSpinner label="Cargando facturas..." />
        ) : list.forbidden ? (
          <div className="p-3">
            <AppAlert variant="warning" title="Sin permiso">
              Tu usuario no tiene permiso para consultar facturas.
            </AppAlert>
          </div>
        ) : list.error ? (
          <div className="p-3">
            <AppAlert variant="danger" title="Error al cargar">
              {list.error}
            </AppAlert>
          </div>
        ) : list.items.length === 0 ? (
          <EmptyState
            icon="bi-file-earmark-x"
            title="Sin facturas"
            description={
              hayFiltros
                ? 'Ninguna factura coincide con los filtros.'
                : 'Emití la factura de un pedido entregado o completado desde el botón "Emitir factura" (BR-130).'
            }
          />
        ) : (
          <>
            <DataTable
              columns={[
                {
                  header: 'Emisión',
                  render: (row: FacturaItem) => formatDateTime(row.issueDate),
                },
                {
                  header: 'Número',
                  render: (row: FacturaItem) => (
                    <span className="font-monospace small">{row.number}</span>
                  ),
                  className: 'text-center',
                },
                {
                  header: 'Pedido',
                  render: (row: FacturaItem) => (
                    <span className="font-monospace small">#{row.orderId}</span>
                  ),
                  className: 'text-center',
                },
                {
                  header: 'Cliente',
                  render: (row: FacturaItem) => row.customerName,
                },
                {
                  header: 'Estado',
                  render: (row: FacturaItem) => (
                    <span className={`badge ${ESTADO_FACTURA_BADGE[row.status]}`}>
                      {ESTADO_FACTURA_LABELS[row.status]}
                    </span>
                  ),
                  className: 'text-center',
                },
                {
                  header: 'Emitida por',
                  render: (row: FacturaItem) => (
                    <span className="small text-muted">{row.userName ?? '—'}</span>
                  ),
                },
                {
                  header: 'Total',
                  render: (row: FacturaItem) => (
                    <span className="font-monospace">{formatMoney(row.total)}</span>
                  ),
                  className: 'text-end',
                },
              ]}
              rows={list.items}
              rowKey={(row) => row.id}
              actions={
                can('PEDIDOS_VIEW')
                  ? (row) => (
                      <button
                        type="button"
                        className="btn btn-outline-secondary btn-sm"
                        title="Ver el pedido facturado"
                        onClick={() => setDetailId(row.orderId)}
                      >
                        <i className="bi bi-eye" aria-hidden="true" />
                      </button>
                    )
                  : undefined
              }
            />
            <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 border-top p-3">
              <span className="small text-muted">
                Página {list.page} de {Math.max(list.totalPages, 1)} · {list.totalItems} resultados
              </span>
              <Pagination
                page={list.page}
                totalPages={list.totalPages}
                onPageChange={list.setPage}
              />
            </div>
          </>
        )}
      </PanelCard>

      {emitOpen ? (
        <Modal
          open
          title="Emitir factura"
          onClose={cerrarEmision}
          footer={
            <>
              <AppButton variant="secondary" onClick={cerrarEmision} disabled={emitting}>
                Cancelar
              </AppButton>
              <AppButton
                type="submit"
                form="emit-factura-form"
                loading={emitting}
                icon={<i className="bi bi-file-earmark-check" aria-hidden="true" />}
              >
                Emitir
              </AppButton>
            </>
          }
        >
          <form id="emit-factura-form" onSubmit={onSubmitEmision} noValidate>
            {emitError ? (
              <AppAlert variant="danger" title="No se pudo emitir">
                {emitError}
              </AppAlert>
            ) : null}
            <p className="small text-muted">
              El documento es simulado (BR-130) y sólo puede emitirse una vez por pedido, en estado
              Entregado o Completado.
            </p>
            <AppInput
              label="Pedido"
              name="factura-order-id"
              type="number"
              min={1}
              required
              value={emitOrderId}
              onChange={(event) => setEmitOrderId(event.target.value)}
              error={emitFieldError}
              placeholder="Nº de pedido"
            />
            <AppTextarea
              label="Notas"
              name="factura-notes"
              rows={3}
              maxLength={500}
              value={emitNotes}
              onChange={(event) => setEmitNotes(event.target.value)}
              hint="Opcional, máximo 500 caracteres."
            />
          </form>
        </Modal>
      ) : null}

      {detailId !== null ? (
        <OrderDetailModal
          open
          orderId={detailId}
          onClose={() => setDetailId(null)}
          onChanged={() => list.reload()}
        />
      ) : null}
    </>
  );
}
