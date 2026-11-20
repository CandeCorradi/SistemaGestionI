/** Formateadores compartidos de presentación (dinero, fechas, estado). */

/** Moneda del negocio según despliegue; configurable por env. */
const CURRENCY = process.env.NEXT_PUBLIC_CURRENCY || 'ARS';

const moneyFormatter = new Intl.NumberFormat('es-AR', {
  style: 'currency',
  currency: CURRENCY,
  minimumFractionDigits: 2,
});

const dateFormatter = new Intl.DateTimeFormat('es-AR', {
  dateStyle: 'medium',
});

const dateTimeFormatter = new Intl.DateTimeFormat('es-AR', {
  dateStyle: 'short',
  timeStyle: 'short',
});

/** Monto decimal a texto localizado. El cálculo comercial es del backend (AGENTS.md §16). */
export function formatMoney(value: number): string {
  return moneyFormatter.format(value);
}

/** ISO UTC a fecha local (AGENTS.md §17: UTC en datos, zona local en UI). */
export function formatDate(iso: string): string {
  const date = new Date(iso);
  return Number.isNaN(date.getTime()) ? iso : dateFormatter.format(date);
}

/** ISO UTC a fecha y hora local (movimientos de inventario). */
export function formatDateTime(iso: string): string {
  const date = new Date(iso);
  return Number.isNaN(date.getTime()) ? iso : dateTimeFormatter.format(date);
}

/** Nombre completo a partir de nombre+apellido. */
export function fullName(nombre: string, apellido?: string | null): string {
  return apellido ? `${nombre} ${apellido}` : nombre;
}
