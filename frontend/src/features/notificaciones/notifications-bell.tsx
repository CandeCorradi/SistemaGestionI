'use client';

/**
 * Campana de notificaciones del header (api-spec §18, BR-091/BR-092):
 * badge con el conteo de no leídas, dropdown con las más recientes,
 * marcar como leída y enlace a /notificaciones. El conteo se refresca al
 * montar y cada 60 s (UX solamente; la autorización es del backend).
 */

import { useCallback, useEffect, useRef, useState } from 'react';
import Link from 'next/link';
import {
  getUnreadCount,
  listNotifications,
  markAllNotificationsRead,
  markNotificationRead,
} from '@/services/notificaciones';
import { formatDateTime } from '@/lib/format';
import type { NotificacionItem } from '@/types/api';

const REFRESH_MS = 60_000;

export function NotificationsBell() {
  const [count, setCount] = useState(0);
  const [open, setOpen] = useState(false);
  const [items, setItems] = useState<NotificacionItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const rootRef = useRef<HTMLDivElement>(null);

  const refreshCount = useCallback(async () => {
    try {
      const result = await getUnreadCount();
      setCount(result.count);
    } catch {
      // El badge es UX: un fallo de red no debe romper el header.
    }
  }, []);

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      try {
        const result = await getUnreadCount();
        if (!cancelled) {
          setCount(result.count);
        }
      } catch {
        // El badge es UX: un fallo de red no debe romper el header.
      }
    };

    void load();
    const interval = setInterval(() => void load(), REFRESH_MS);
    return () => {
      cancelled = true;
      clearInterval(interval);
    };
  }, []);

  useEffect(() => {
    if (!open) {
      return;
    }
    const onPointerDown = (event: PointerEvent) => {
      if (rootRef.current && !rootRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener('pointerdown', onPointerDown);
    return () => document.removeEventListener('pointerdown', onPointerDown);
  }, [open]);

  const toggle = async () => {
    const next = !open;
    setOpen(next);
    if (!next) {
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const page = await listNotifications({ pageSize: 5 });
      setItems(page.items);
    } catch {
      setError('No se pudieron cargar las notificaciones.');
    } finally {
      setLoading(false);
    }
  };

  const onMarkRead = async (item: NotificacionItem) => {
    if (item.read) {
      return;
    }
    try {
      await markNotificationRead(item.id);
      setItems((prev) => prev.map((i) => (i.id === item.id ? { ...i, read: true } : i)));
      void refreshCount();
    } catch {
      setError('No se pudo marcar la notificación como leída.');
    }
  };

  const onMarkAll = async () => {
    try {
      await markAllNotificationsRead();
      setItems((prev) => prev.map((i) => ({ ...i, read: true })));
      void refreshCount();
    } catch {
      setError('No se pudieron marcar todas como leídas.');
    }
  };

  return (
    <div ref={rootRef} className="position-relative">
      <button
        type="button"
        className="btn btn-outline-secondary btn-sm position-relative"
        onClick={() => void toggle()}
        aria-label={
          count > 0 ? `Notificaciones, ${count} sin leer` : 'Notificaciones'
        }
        aria-expanded={open}
      >
        <i className="bi bi-bell" aria-hidden="true" />
        {count > 0 ? (
          <span
            className="position-absolute top-0 start-100 translate-middle badge rounded-pill text-bg-danger"
            style={{ fontSize: '0.65rem' }}
          >
            {count > 99 ? '99+' : count}
          </span>
        ) : null}
      </button>

      {open ? (
        <div
          className="dropdown-menu dropdown-menu-end show shadow"
          style={{ right: 0, left: 'auto', minWidth: '20rem' }}
          role="menu"
        >
          <div className="d-flex justify-content-between align-items-center px-3 py-2 border-bottom">
            <span className="fw-semibold small">Notificaciones</span>
            <button
              type="button"
              className="btn btn-link btn-sm p-0"
              onClick={() => void onMarkAll()}
            >
              Marcar todas
            </button>
          </div>

          {error ? (
            <div className="px-3 py-2 small text-danger" role="alert">
              {error}
            </div>
          ) : null}

          {loading ? (
            <div className="px-3 py-3 small text-muted">Cargando...</div>
          ) : items.length === 0 ? (
            <div className="px-3 py-3 small text-muted">Sin notificaciones.</div>
          ) : (
            <div className="list-group list-group-flush" style={{ maxHeight: '18rem', overflowY: 'auto' }}>
              {items.map((item) => (
                <button
                  key={item.id}
                  type="button"
                  className={`list-group-item list-group-item-action py-2 ${item.read ? '' : 'fw-semibold'}`.trim()}
                  onClick={() => void onMarkRead(item)}
                  title={item.read ? undefined : 'Marcar como leída'}
                >
                  <div className="d-flex justify-content-between gap-2">
                    <span className="small">{item.title}</span>
                    {!item.read ? (
                      <span className="badge text-bg-danger align-self-start">Nueva</span>
                    ) : null}
                  </div>
                  <div className="small text-muted text-truncate">{item.message}</div>
                  <div className="small text-muted">{formatDateTime(item.createdAt)}</div>
                </button>
              ))}
            </div>
          )}

          <div className="border-top px-3 py-2">
            <Link href="/notificaciones" className="small" onClick={() => setOpen(false)}>
              Ver todas
            </Link>
          </div>
        </div>
      ) : null}
    </div>
  );
}
