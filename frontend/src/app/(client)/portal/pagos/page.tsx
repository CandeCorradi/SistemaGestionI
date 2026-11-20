'use client';

/**
 * Pagos del Portal Cliente (functional-spec §23): historial propio de
 * pagos con método/referencia y resumen de saldo calculado por el
 * backend (BR-083).
 */

import { useCallback, useEffect, useState } from 'react';
import Link from 'next/link';
import { getSummary, listMyPayments } from '@/services/portal';
import { usePagedList, type PagedQuery } from '@/hooks/usePagedList';
import { formatDate, formatMoney } from '@/lib/format';
import {
  METODO_PAGO_LABELS,
  type ClienteSummary,
  type PaymentItem,
} from '@/types/api';
import { PanelCard, StatCard } from '@/components/ui/cards';
import { DataTable } from '@/components/ui/data-table';
import { AppAlert } from '@/components/ui/app-alert';
import { EmptyState } from '@/components/ui/empty-state';
import { LoadingSpinner } from '@/components/ui/loading-spinner';
import { Pagination } from '@/components/ui/pagination';

export default function PortalPaymentsPage() {
  const [summary, setSummary] = useState<ClienteSummary | null>(null);

  const fetcher = useCallback(
    (query: PagedQuery, signal?: AbortSignal) =>
      listMyPayments({ page: query.page, pageSize: query.pageSize }, signal),
    []
  );

  const list = usePagedList<PaymentItem>({ fetcher });

  useEffect(() => {
    let cancelled = false;
    const controller = new AbortController();

    const load = async () => {
      try {
        const result = await getSummary(controller.signal);
        if (!cancelled) {
          setSummary(result);
        }
      } catch {
        if (!cancelled) {
          setSummary(null);
        }
      }
    };

    void load();
    return () => {
      cancelled = true;
      controller.abort();
    };
  }, []);

  return (
    <>
      <div className="mb-4">
        <h1 className="h4 mb-1">Mis pagos</h1>
        <p className="text-muted mb-0 small">
          Historial de pagos registrados y saldo de tu cuenta
        </p>
      </div>

      <div className="row g-3 mb-3">
        <div className="col-12 col-md-4">
          <StatCard
            title="Total pagado"
            value={formatMoney(summary?.totalPagado ?? 0)}
            icon="bi-cash-stack"
            tone="bg-success"
            footer={`${summary?.pedidosCount ?? 0} pedido${(summary?.pedidosCount ?? 0) === 1 ? '' : 's'}`}
          />
        </div>
        <div className="col-12 col-md-4">
          <StatCard
            title="Total de pedidos"
            value={formatMoney(summary?.totalPedidos ?? 0)}
            icon="bi-box-seam"
            tone="bg-primary"
            footer="Excluye pedidos cancelados"
          />
        </div>
        <div className="col-12 col-md-4">
          <StatCard
            title="Saldo pendiente"
            value={formatMoney(summary?.saldo ?? 0)}
            icon="bi-wallet2"
            tone={(summary?.saldo ?? 0) > 0 ? 'bg-danger' : 'bg-success'}
            footer="Calculado por el backend"
          />
        </div>
      </div>

      <PanelCard title="Historial de pagos">
        {list.loading ? (
          <LoadingSpinner label="Cargando pagos..." />
        ) : list.error ? (
          <div className="p-3">
            <AppAlert variant="danger" title="Error al cargar">
              {list.error}
            </AppAlert>
          </div>
        ) : list.items.length === 0 ? (
          <EmptyState
            icon="bi-cash-coin"
            title="Sin pagos registrados"
            description="Cuando registres un pago con la carpintería va a aparecer acá."
          />
        ) : (
          <>
            <DataTable
              columns={[
                {
                  header: 'Fecha',
                  render: (row: PaymentItem) => formatDate(row.date),
                },
                {
                  header: 'Pedido',
                  render: (row: PaymentItem) => (
                    <Link
                      href={`/portal/pedidos/${row.orderId}`}
                      className="font-monospace small text-decoration-none"
                    >
                      #{row.orderId}
                    </Link>
                  ),
                  className: 'text-center',
                },
                {
                  header: 'Método',
                  render: (row: PaymentItem) => METODO_PAGO_LABELS[row.paymentMethod],
                },
                {
                  header: 'Referencia',
                  render: (row: PaymentItem) => row.reference ?? '—',
                },
                {
                  header: 'Monto',
                  render: (row: PaymentItem) => (
                    <span className="font-monospace">{formatMoney(row.amount)}</span>
                  ),
                  className: 'text-end',
                },
              ]}
              rows={list.items}
              rowKey={(row) => row.id}
            />
            <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 border-top p-3">
              <span className="small text-muted">
                Página {list.page} de {Math.max(list.totalPages, 1)} · {list.totalItems} resultados
              </span>
              <Pagination page={list.page} totalPages={list.totalPages} onPageChange={list.setPage} />
            </div>
          </>
        )}
      </PanelCard>
    </>
  );
}
