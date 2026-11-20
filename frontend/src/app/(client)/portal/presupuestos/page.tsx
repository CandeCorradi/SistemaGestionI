'use client';

/**
 * Presupuestos del Portal Cliente (functional-spec §23): listado propio
 * paginado con filtro de estado, solicitud de presupuesto describiendo el
 * trabajo (ítem sin precio que cotiza el equipo) y acceso al detalle para
 * aceptar (BR-064/BR-065).
 */

import { useCallback, useState } from 'react';
import Link from 'next/link';
import { ApiError } from '@/services/api-client';
import { listMyQuotes, requestQuote } from '@/services/portal';
import { usePagedList, type PagedQuery } from '@/hooks/usePagedList';
import { formatDate, formatMoney } from '@/lib/format';
import {
  ESTADO_PRESUPUESTO_BADGE,
  ESTADO_PRESUPUESTO_LABELS,
  type EstadoPresupuesto,
  type QuoteListItem,
} from '@/types/api';
import { PanelCard } from '@/components/ui/cards';
import { DataTable } from '@/components/ui/data-table';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { AppInput, AppTextarea } from '@/components/ui/form-fields';
import { EmptyState } from '@/components/ui/empty-state';
import { LoadingSpinner } from '@/components/ui/loading-spinner';
import { Modal } from '@/components/ui/modal';
import { Pagination } from '@/components/ui/pagination';

const ESTADOS: EstadoPresupuesto[] = [0, 1, 2, 3, 4, 5];

export default function PortalQuotesPage() {
  const [statusFilter, setStatusFilter] = useState<EstadoPresupuesto | ''>('');
  const [requestOpen, setRequestOpen] = useState(false);
  const [description, setDescription] = useState('');
  const [cantidad, setCantidad] = useState('1');
  const [fieldError, setFieldError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [requestOk, setRequestOk] = useState<string | null>(null);

  const fetcher = useCallback(
    (query: PagedQuery, signal?: AbortSignal) =>
      listMyQuotes(
        {
          page: query.page,
          pageSize: query.pageSize,
          search: query.search,
          status: statusFilter === '' ? undefined : statusFilter,
        },
        signal
      ),
    [statusFilter]
  );

  const list = usePagedList<QuoteListItem>({
    fetcher,
    fetcherKey: String(statusFilter),
  });

  function openRequest() {
    setDescription('');
    setCantidad('1');
    setFieldError(null);
    setFormError(null);
    setRequestOk(null);
    setRequestOpen(true);
  }

  async function submitRequest() {
    setFieldError(null);
    setFormError(null);

    if (!description.trim()) {
      setFieldError('Contanos qué necesitás cotizar.');
      return;
    }

    const quantity = Number(cantidad);
    if (!Number.isInteger(quantity) || quantity <= 0) {
      setFieldError('La cantidad debe ser un número mayor que cero.');
      return;
    }

    setSubmitting(true);
    try {
      const quote = await requestQuote({ description: description.trim(), cantidad: quantity });
      setRequestOpen(false);
      setRequestOk(
        `Solicitud #${quote.id} enviada. El equipo la cotiza y te la deja disponible acá.`
      );
      list.reload();
    } catch (err) {
      setFormError(
        err instanceof ApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : 'No se pudo enviar la solicitud.'
      );
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <>
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-2">
        <div>
          <h1 className="h4 mb-1">Mis presupuestos</h1>
          <p className="text-muted mb-0 small">
            {list.totalItems} presupuesto{list.totalItems === 1 ? '' : 's'} · los totales los
            calcula el backend
          </p>
        </div>
        <AppButton
          onClick={openRequest}
          icon={<i className="bi bi-file-earmark-plus me-1" aria-hidden="true" />}
        >
          Solicitar presupuesto
        </AppButton>
      </div>

      {requestOk ? (
        <AppAlert
          variant="success"
          title="Solicitud enviada"
          onClose={() => setRequestOk(null)}
        >
          {requestOk}
        </AppAlert>
      ) : null}

      <PanelCard
        title="Listado"
        actions={
          <div className="d-flex flex-wrap gap-2 align-items-center">
            <div className="input-group input-group-sm" style={{ width: '15rem' }}>
              <span className="input-group-text bg-white">
                <i className="bi bi-search" aria-hidden="true" />
              </span>
              <input
                type="search"
                className="form-control"
                placeholder="Buscar..."
                value={list.search}
                onChange={(event) => list.setSearch(event.target.value)}
                aria-label="Buscar presupuestos"
              />
            </div>
            <select
              className="form-select form-select-sm"
              style={{ width: '10rem' }}
              value={statusFilter === '' ? '' : String(statusFilter)}
              aria-label="Filtrar por estado"
              onChange={(event) =>
                setStatusFilter(
                  event.target.value === '' ? '' : (Number(event.target.value) as EstadoPresupuesto)
                )
              }
            >
              <option value="">Todos</option>
              {ESTADOS.map((value) => (
                <option key={value} value={value}>
                  {ESTADO_PRESUPUESTO_LABELS[value]}
                </option>
              ))}
            </select>
          </div>
        }
      >
        {list.loading ? (
          <LoadingSpinner label="Cargando presupuestos..." />
        ) : list.error ? (
          <div className="p-3">
            <AppAlert variant="danger" title="Error al cargar">
              {list.error}
            </AppAlert>
          </div>
        ) : list.items.length === 0 ? (
          <EmptyState
            icon="bi-file-earmark-text"
            title="Sin presupuestos"
            description={
              list.search || statusFilter !== ''
                ? 'Ningún presupuesto coincide con los filtros.'
                : 'Solicitá tu primer presupuesto describiendo el trabajo que necesitás.'
            }
            action={
              !list.search && statusFilter === '' ? (
                <AppButton onClick={openRequest}>Solicitar presupuesto</AppButton>
              ) : null
            }
          />
        ) : (
          <>
            <DataTable
              columns={[
                {
                  header: '#',
                  render: (row: QuoteListItem) => (
                    <span className="font-monospace small">#{row.id}</span>
                  ),
                  className: 'text-center',
                },
                {
                  header: 'Solicitado',
                  render: (row: QuoteListItem) => formatDate(row.creationDate),
                },
                {
                  header: 'Estado',
                  render: (row: QuoteListItem) => (
                    <span className={`badge ${ESTADO_PRESUPUESTO_BADGE[row.status]}`}>
                      {ESTADO_PRESUPUESTO_LABELS[row.status]}
                    </span>
                  ),
                  className: 'text-center',
                },
                {
                  header: 'Ítems',
                  render: (row: QuoteListItem) => row.itemsCount,
                  className: 'text-center',
                },
                {
                  header: 'Total',
                  render: (row: QuoteListItem) => (
                    <span className="font-monospace">{formatMoney(row.total)}</span>
                  ),
                  className: 'text-end',
                },
              ]}
              rows={list.items}
              rowKey={(row) => row.id}
              actions={(row) => (
                <Link
                  href={`/portal/presupuestos/${row.id}`}
                  className="btn btn-outline-secondary btn-sm"
                  title="Ver detalle"
                >
                  <i className="bi bi-eye" aria-hidden="true" />
                </Link>
              )}
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

      <Modal
        open={requestOpen}
        title="Solicitar presupuesto"
        onClose={() => setRequestOpen(false)}
        footer={
          <>
            <AppButton
              variant="secondary"
              onClick={() => setRequestOpen(false)}
              disabled={submitting}
            >
              Cancelar
            </AppButton>
            <AppButton onClick={() => void submitRequest()} loading={submitting}>
              Enviar solicitud
            </AppButton>
          </>
        }
      >
        {formError ? <AppAlert variant="danger">{formError}</AppAlert> : null}
        <p className="small text-muted">
          Contanos qué necesitás: el equipo cotiza el trabajo y te lo deja disponible acá. No hace
          falta que cargues precios.
        </p>
        <form
          onSubmit={(event) => {
            event.preventDefault();
            void submitRequest();
          }}
        >
          <AppTextarea
            label="¿Qué necesitás cotizar?"
            name="description"
            rows={4}
            required
            placeholder="Ej.: ropero a medida 2x2,10 m, melamina blanca con cajones"
            value={description}
            error={fieldError && !description.trim() ? fieldError : null}
            onChange={(event) => setDescription(event.target.value)}
          />
          <AppInput
            label="Cantidad"
            name="cantidad"
            type="number"
            min={1}
            required
            value={cantidad}
            error={fieldError && description.trim() ? fieldError : null}
            onChange={(event) => setCantidad(event.target.value)}
          />
          <button type="submit" className="d-none" aria-hidden="true" />
        </form>
      </Modal>
    </>
  );
}
