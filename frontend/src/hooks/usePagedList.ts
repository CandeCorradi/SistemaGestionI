'use client';

/**
 * Listado paginado reutilizable: carga con paginación, búsqueda con
 * debounce, filtro de activos y recarga tras mutaciones. Centraliza los
 * estados de loading/error/empty (AGENTS.md §35) para que cada módulo solo
 * declare su tabla y sus diálogos.
 */

import { useCallback, useEffect, useRef, useState } from 'react';
import { ApiError } from '@/services/api-client';
import type { PagedResult } from '@/types/api';

export interface PagedQuery {
  page: number;
  pageSize: number;
  search: string;
  active?: boolean;
}

interface UsePagedListOptions<T> {
  fetcher: (query: PagedQuery, signal?: AbortSignal) => Promise<PagedResult<T>>;
  pageSize?: number;
  /** Deja el filtro de activos sin definir (true/false para prefiltrar). */
  initialActive?: boolean;
  /**
   * Clave del fetcher: cambiarla recarga la lista. Úsese cuando el fetcher
   * cierra sobre filtros externos (tipo, stock bajo, producto…) que no
   * forman parte de PagedQuery.
   */
  fetcherKey?: string | number;
}

interface UsePagedListResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  loading: boolean;
  /** Error de red o de la API distinto de autorización. */
  error: string | null;
  /** true cuando el backend respondió 403 (sin permiso). */
  forbidden: boolean;
  search: string;
  active: boolean | undefined;
  setSearch: (value: string) => void;
  setActive: (value: boolean | undefined) => void;
  setPage: (value: number) => void;
  reload: () => void;
}

export function usePagedList<T>({
  fetcher,
  pageSize = 20,
  initialActive,
  fetcherKey,
}: UsePagedListOptions<T>): UsePagedListResult<T> {
  const [items, setItems] = useState<T[]>([]);
  const [page, setPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [forbidden, setForbidden] = useState(false);
  const [search, setSearch] = useState('');
  const [active, setActive] = useState<boolean | undefined>(initialActive);
  const [reloadToken, setReloadToken] = useState(0);

  const [debouncedSearch, setDebouncedSearch] = useState('');
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // El fetcher se guarda en ref para que un fetcher inline no re-dispare
  // el efecto en cada render (se sincroniza en un efecto).
  const fetcherRef = useRef(fetcher);
  useEffect(() => {
    fetcherRef.current = fetcher;
  }, [fetcher]);

  // Reset de estados de carga al cambiar la query (patrón de React:
  // "adjusting state when a prop changes" durante render).
  const queryKey = `${page}|${pageSize}|${debouncedSearch}|${String(active)}|${reloadToken}|${String(fetcherKey ?? '')}`;
  const [prevQueryKey, setPrevQueryKey] = useState(queryKey);
  if (prevQueryKey !== queryKey) {
    setPrevQueryKey(queryKey);
    setLoading(true);
    setError(null);
    setForbidden(false);
  }

  useEffect(() => {
    if (debounceRef.current) {
      clearTimeout(debounceRef.current);
    }
    debounceRef.current = setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPage(1);
    }, 350);
    return () => {
      if (debounceRef.current) {
        clearTimeout(debounceRef.current);
      }
    };
  }, [search]);

  useEffect(() => {
    const controller = new AbortController();

    fetcherRef.current({ page, pageSize, search: debouncedSearch, active }, controller.signal)
      .then((result) => {
        setItems(result.items);
        setTotalItems(result.totalItems);
        setTotalPages(result.totalPages);
        // Si la página actual quedó fuera del rango tras filtrar, recorrer a la última.
        if (result.totalPages > 0 && result.page > result.totalPages) {
          setPage(result.totalPages);
        }
      })
      .catch((err: unknown) => {
        if (controller.signal.aborted) {
          return;
        }
        if (err instanceof ApiError && err.isForbidden) {
          setForbidden(true);
          setItems([]);
        } else if (err instanceof ApiError && err.isUnauthorized) {
          // La sesión la resuelve el AuthProvider; no pintar error duro.
          setItems([]);
        } else if (err instanceof Error) {
          setError(err.message);
        } else {
          setError('Error inesperado al cargar los datos.');
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      });

    return () => controller.abort();
  }, [page, pageSize, debouncedSearch, active, reloadToken, fetcherKey]);

  const reload = useCallback(() => setReloadToken((token) => token + 1), []);

  return {
    items,
    page,
    pageSize,
    totalItems,
    totalPages,
    loading,
    error,
    forbidden,
    search,
    active,
    setSearch,
    setActive,
    setPage,
    reload,
  };
}
