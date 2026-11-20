'use client';

/**
 * Notificaciones del Portal Cliente (functional-spec §23): el usuario
 * autenticado consulta las suyas con el mismo servicio genérico
 * (BR-090); aquí se muestran eventos de presupuestos, pedidos y pagos
 * (BR-092) con acciones de lectura.
 */

import { useCallback, useState } from 'react';
import {
  listNotifications,
  markAllNotificationsRead,
  markNotificationRead,
} from '@/services/notificaciones';
import { usePagedList, type PagedQuery } from '@/hooks/usePagedList';
import { formatDateTime } from '@/lib/format';
import { TIPO_NOTIFICACION_LABELS, type NotificacionItem } from '@/types/api';
import { PanelCard } from '@/components/ui/cards';
import { DataTable } from '@/components/ui/data-table';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { EmptyState } from '@/components/ui/empty-state';
import { LoadingSpinner } from '@/components/ui/loading-spinner';
import { Pagination } from '@/components/ui/pagination';

export default function PortalNotificacionesPage() {
  const [unreadOnly, setUnreadOnly] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  const fetcher = useCallback(
    (query: PagedQuery, signal?: AbortSignal) =>
      listNotifications(
        { page: query.page, pageSize: query.pageSize, unreadOnly },
        signal
      ),
    [unreadOnly]
  );

  const list = usePagedList<NotificacionItem>({
    fetcher,
    fetcherKey: String(unreadOnly),
  });

  const onMarkRead = async (item: NotificacionItem) => {
    if (item.read) {
      return;
    }
    setActionError(null);
    try {
      await markNotificationRead(item.id);
      list.reload();
    } catch {
      setActionError('No se pudo marcar la notificación como leída.');
    }
  };

  const onMarkAll = async () => {
    setActionError(null);
    try {
      await markAllNotificationsRead();
      list.reload();
    } catch {
      setActionError('No se pudieron marcar todas como leídas.');
    }
  };

  return (
    <>
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-2">
        <div>
          <h1 className="h4 mb-1">Notificaciones</h1>
          <p className="text-muted mb-0 small">
            {list.totalItems} notificación{list.totalItems === 1 ? '' : 'es'} · novedades de tus
            presupuestos, pedidos y pagos
          </p>
        </div>
        <AppButton
          variant="outline-secondary"
          icon={<i className="bi bi-check2-all" aria-hidden="true" />}
          onClick={() => void onMarkAll()}
        >
          Marcar todas como leídas
        </AppButton>
      </div>

      {actionError ? (
        <AppAlert variant="danger" title="Error">
          {actionError}
        </AppAlert>
      ) : null}

      <PanelCard
        title="Historial"
        actions={
          <div className="form-check form-switch m-0">
            <input
              className="form-check-input"
              type="checkbox"
              id="portal-solo-no-leidas"
              checked={unreadOnly}
              onChange={(event) => setUnreadOnly(event.target.checked)}
            />
            <label className="form-check-label small" htmlFor="portal-solo-no-leidas">
              Sólo no leídas
            </label>
          </div>
        }
      >
        {list.loading ? (
          <LoadingSpinner label="Cargando notificaciones..." />
        ) : list.forbidden ? (
          <div className="p-3">
            <AppAlert variant="warning" title="Sin acceso">
              Tu cuenta no tiene un usuario interno activo.
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
            icon="bi-bell-slash"
            title="Sin notificaciones"
            description={
              unreadOnly
                ? 'No hay notificaciones sin leer.'
                : 'Las notificaciones aparecen cuando ocurren eventos de negocio (BR-092).'
            }
          />
        ) : (
          <>
            <DataTable
              columns={[
                {
                  header: 'Fecha',
                  render: (row: NotificacionItem) => formatDateTime(row.createdAt),
                },
                {
                  header: 'Título',
                  render: (row: NotificacionItem) => (
                    <div>
                      <div className={row.read ? '' : 'fw-semibold'}>{row.title}</div>
                      <div className="small text-muted">{row.message}</div>
                    </div>
                  ),
                },
                {
                  header: 'Tipo',
                  render: (row: NotificacionItem) => (
                    <span className="badge text-bg-light border small">
                      {TIPO_NOTIFICACION_LABELS[row.type]}
                    </span>
                  ),
                  className: 'text-center',
                },
                {
                  header: 'Estado',
                  render: (row: NotificacionItem) => (
                    <span className={`badge ${row.read ? 'text-bg-light border' : 'text-bg-danger'}`}>
                      {row.read ? 'Leída' : 'Sin leer'}
                    </span>
                  ),
                  className: 'text-center',
                },
              ]}
              rows={list.items}
              rowKey={(row) => row.id}
              actions={(row) =>
                row.read ? (
                  <span className="small text-muted" aria-hidden="true">
                    —
                  </span>
                ) : (
                  <button
                    type="button"
                    className="btn btn-outline-secondary btn-sm"
                    title="Marcar como leída"
                    onClick={() => void onMarkRead(row)}
                  >
                    <i className="bi bi-check" aria-hidden="true" />
                  </button>
                )
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
    </>
  );
}
