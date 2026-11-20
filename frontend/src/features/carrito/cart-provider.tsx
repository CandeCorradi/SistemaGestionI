'use client';

/**
 * Carrito de la vidriera pública (AGENTS.md §23).
 *
 * El carrito vive en el navegador (localStorage) porque el catálogo y la
 * vidriera son anónimos: cualquier visitante puede armar su selección y,
 * al finalizar, se le pide iniciar sesión para convertirla en una solicitud
 * de presupuesto (el flujo comercial del sistema: presupuesto → pedido).
 * Nunca se envían precios ni permisos desde acá: el backend es la autoridad.
 *
 * La lectura del storage usa `useSyncExternalStore` para no llamar a
 * setState dentro de un efecto y para que el servidor renderice el carrito
 * vacío sin romper la hidratación.
 */

import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  useSyncExternalStore,
  type ReactNode,
} from 'react';

const STORAGE_KEY = 'sg-cart-v1';

/** Evento interno para notificar cambios hechos en esta misma pestaña. */
const CART_EVENT = 'sg-cart-change';

export interface CartItem {
  /** Id de la obra de la vidriera. */
  id: number;
  nombre: string;
  categoria: string | null;
  tipoMueble: string | null;
  imagenUrl: string | null;
}

interface CartContextValue {
  items: CartItem[];
  count: number;
  open: boolean;
  addItem: (item: CartItem) => void;
  removeItem: (id: number) => void;
  clear: () => void;
  openCart: () => void;
  closeCart: () => void;
}

const CartContext = createContext<CartContextValue | null>(null);

function parseItems(raw: string | null): CartItem[] {
  if (!raw) {
    return [];
  }
  try {
    const parsed = JSON.parse(raw) as unknown;
    if (!Array.isArray(parsed)) {
      return [];
    }
    return parsed.filter(
      (item): item is CartItem =>
        typeof item === 'object' &&
        item !== null &&
        typeof (item as CartItem).id === 'number' &&
        typeof (item as CartItem).nombre === 'string'
    );
  } catch {
    return [];
  }
}

function subscribe(onChange: () => void): () => void {
  // `storage` cubre cambios de otras pestañas; CART_EVENT los de esta.
  window.addEventListener('storage', onChange);
  window.addEventListener(CART_EVENT, onChange);
  return () => {
    window.removeEventListener('storage', onChange);
    window.removeEventListener(CART_EVENT, onChange);
  };
}

function getSnapshot(): string | null {
  try {
    return window.localStorage.getItem(STORAGE_KEY);
  } catch {
    // Sin storage disponible (modo privado): carrito vacío.
    return null;
  }
}

/** En el servidor no hay storage: siempre vacío. */
function getServerSnapshot(): string | null {
  return null;
}

function persist(items: CartItem[]): void {
  try {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(items));
  } catch {
    // Sin storage disponible: el cambio no se persiste, pero no rompe la UI.
  }
  window.dispatchEvent(new Event(CART_EVENT));
}

export function CartProvider({ children }: { children: ReactNode }) {
  const raw = useSyncExternalStore(subscribe, getSnapshot, getServerSnapshot);
  const items = useMemo(() => parseItems(raw), [raw]);
  const [open, setOpen] = useState(false);

  const addItem = useCallback(
    (item: CartItem) => {
      const current = parseItems(getSnapshot());
      if (current.some((existing) => existing.id === item.id)) {
        return;
      }
      persist([...current, item]);
    },
    []
  );

  const removeItem = useCallback((id: number) => {
    persist(parseItems(getSnapshot()).filter((item) => item.id !== id));
  }, []);

  const clear = useCallback(() => persist([]), []);
  const openCart = useCallback(() => setOpen(true), []);
  const closeCart = useCallback(() => setOpen(false), []);

  const value = useMemo<CartContextValue>(
    () => ({
      items,
      count: items.length,
      open,
      addItem,
      removeItem,
      clear,
      openCart,
      closeCart,
    }),
    [items, open, addItem, removeItem, clear, openCart, closeCart]
  );

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}

export function useCart(): CartContextValue {
  const context = useContext(CartContext);
  if (!context) {
    throw new Error('useCart debe usarse dentro de <CartProvider>.');
  }
  return context;
}
