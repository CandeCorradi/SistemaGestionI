/**
 * Contratos públicos de la API (AGENTS.md §28). Estos tipos deben
 * mantenerse sincronizados con los DTOs del backend
 * (docs/api-specification.md).
 */

/** Respuesta paginada de listados (api-spec §5). */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

/** ProblemDetails devuelto por la API en errores (api-spec §7). */
export interface ProblemDetails {
  title: string;
  detail?: string;
  status?: number;
  instance?: string;
}

/** Identidad autenticada (GET /api/v1/auth/me). */
export interface AuthUser {
  uid: string;
  email: string | null;
  emailVerified: boolean;
}

/** Permisos efectivos (GET /api/v1/auth/permissions). */
export interface PermisosActuales {
  codigos: string[];
}

/** Perfil del usuario interno (GET /api/v1/auth/profile). */
export interface AuthProfile {
  email: string;
  nombre: string;
  apellido: string;
  rol: string;
  activo: boolean;
}

/** Solicitud de autorregistro (POST /api/v1/auth/register). */
export interface RegisterInput {
  email: string;
  password: string;
  nombre: string;
  apellido: string;
}

/** Resultado del autorregistro (POST /api/v1/auth/register). */
export interface RegisterResult {
  id: number;
  email: string;
  nombre: string;
  apellido: string;
  rol: string;
}

// --- Clientes (api-spec §10) ---

export interface ClienteList {
  id: number;
  nombre: string;
  apellido: string;
  email: string;
  telefono?: string | null;
  documento?: string | null;
  activo: boolean;
}

export interface Cliente extends ClienteList {
  direccion?: string | null;
  notas?: string | null;
  firebaseUid?: string | null;
  fechaCreacion: string;
}

export interface CreateClienteInput {
  nombre: string;
  apellido: string;
  email: string;
  telefono?: string | null;
  direccion?: string | null;
  documento?: string | null;
}

export interface UpdateClienteInput {
  nombre?: string | null;
  apellido?: string | null;
  email?: string | null;
  telefono?: string | null;
  direccion?: string | null;
  documento?: string | null;
  notas?: string | null;
  activo?: boolean | null;
}

// --- Proveedores (api-spec §11) ---

export interface ProveedorList {
  id: number;
  nombre: string;
  contacto?: string | null;
  email?: string | null;
  telefono?: string | null;
  activo: boolean;
}

export interface Proveedor extends ProveedorList {
  direccion?: string | null;
  notas?: string | null;
  fechaCreacion: string;
}

export interface CreateProveedorInput {
  nombre: string;
  contacto?: string | null;
  email?: string | null;
  telefono?: string | null;
  direccion?: string | null;
}

export interface UpdateProveedorInput {
  nombre?: string | null;
  contacto?: string | null;
  email?: string | null;
  telefono?: string | null;
  direccion?: string | null;
  notas?: string | null;
  activo?: boolean | null;
}

// --- Productos (api-spec §12) ---

/** Tipo comercial de producto (TipoProducto del backend). */
export type TipoProducto = 0 | 1 | 2 | 3 | 4;

export const TIPO_PRODUCTO_LABELS: Record<TipoProducto, string> = {
  0: 'Mueble',
  1: 'Melamina',
  2: 'Accesorio',
  3: 'Insumo',
  4: 'Otro',
};

export interface ProductoList {
  id: number;
  nombre: string;
  codigoSku: string;
  tipo: TipoProducto;
  categoria?: string | null;
  precio: number;
  stockActual: number;
  stockMinimo: number;
  showroom: boolean;
  esPersonalizado: boolean;
  activo: boolean;
}

export interface Producto extends ProductoList {
  descripcion?: string | null;
  costo?: number | null;
  material?: string | null;
  acabado?: string | null;
  color?: string | null;
  dimensiones?: string | null;
  proveedorId?: number | null;
  fechaCreacion: string;
}

export interface CreateProductoInput {
  nombre: string;
  codigoSku: string;
  tipo: TipoProducto;
  precio: number;
  descripcion?: string | null;
  categoria?: string | null;
  costo?: number | null;
  material?: string | null;
  acabado?: string | null;
  color?: string | null;
  dimensiones?: string | null;
  stockActual?: number;
  stockMinimo?: number;
  showroom?: boolean;
  esPersonalizado?: boolean;
  proveedorId?: number | null;
}

export interface UpdateProductoInput {
  nombre?: string | null;
  codigoSku?: string | null;
  tipo?: TipoProducto | null;
  precio?: number | null;
  descripcion?: string | null;
  categoria?: string | null;
  costo?: number | null;
  material?: string | null;
  acabado?: string | null;
  color?: string | null;
  dimensiones?: string | null;
  stockMinimo?: number | null;
  showroom?: boolean | null;
  esPersonalizado?: boolean | null;
  proveedorId?: number | null;
  activo?: boolean | null;
}

/** Parámetros comunes de listado paginado. */
export interface ListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  active?: boolean;
}

/* ------------------------------------------------------------------ */
/* Catálogo público e imágenes (api-spec §12A/§13, functional-spec §9)  */
/* ------------------------------------------------------------------ */

/** Imagen de producto referenciada en el storage externo (BR-101). */
export interface ProductoImagen {
  id: number;
  url: string;
  orden: number;
}

/** Fila pública del catálogo: sin costo, stock numérico ni proveedor. */
export interface CatalogoItem {
  id: number;
  nombre: string;
  tipo: TipoProducto;
  categoria?: string | null;
  precio: number;
  esPersonalizado: boolean;
  showroom: boolean;
  disponible: boolean;
  imagenUrl: string | null;
}

/** Detalle público del catálogo con galería de imágenes. */
export interface CatalogoProducto {
  id: number;
  nombre: string;
  descripcion?: string | null;
  tipo: TipoProducto;
  categoria?: string | null;
  precio: number;
  material?: string | null;
  acabado?: string | null;
  color?: string | null;
  dimensiones?: string | null;
  esPersonalizado: boolean;
  showroom: boolean;
  disponible: boolean;
  imagenes: ProductoImagen[];
}

/* ------------------------------------------------------------------ */
/* Inventario (api-spec §14, functional-spec §11, BR-050..BR-053)      */
/* ------------------------------------------------------------------ */

/** Tipos de movimiento de stock: 0 Ingreso, 1 Egreso, 2 Ajuste, 3 Devolución, 4 Reserva, 5 Liberación. */
export type TipoMovimientoStock = 0 | 1 | 2 | 3 | 4 | 5;

export const TIPO_MOVIMIENTO_STOCK_LABELS: Record<TipoMovimientoStock, string> = {
  0: 'Ingreso',
  1: 'Egreso',
  2: 'Ajuste',
  3: 'Devolución',
  4: 'Reserva',
  5: 'Liberación',
};

/** Efecto del tipo sobre el stock: 1 suma, -1 resta, 0 admite signo (Ajuste). */
export const TIPO_MOVIMIENTO_EFECTO: Record<TipoMovimientoStock, 1 | -1 | 0> = {
  0: 1,
  1: -1,
  2: 0,
  3: 1,
  4: -1,
  5: 1,
};

/** Fila del stock actual (GET /api/v1/inventory). */
export interface StockList {
  id: number;
  nombre: string;
  codigoSku: string;
  tipo: TipoProducto;
  stockActual: number;
  stockMinimo: number;
  esPersonalizado: boolean;
  abajoDeMinimo: boolean;
}

/** Movimiento del historial (BR-051). */
export interface MovimientoStock {
  id: number;
  productoId: number;
  productoNombre: string;
  tipo: TipoMovimientoStock;
  cantidad: number;
  stockAnterior: number;
  stockPosterior: number;
  motivo?: string | null;
  referencia?: string | null;
  usuarioId?: number | null;
  usuarioNombre?: string | null;
  fecha: string;
}

export interface CreateMovimientoStockInput {
  productId: number;
  type: TipoMovimientoStock;
  quantity: number;
  reason?: string | null;
  reference?: string | null;
}

/**
 * Estados del presupuesto (api-spec §15). Serializados como número en la
 * API; el estado Expirado se calcula en backend por fecha de validez.
 */
export type EstadoPresupuesto = 0 | 1 | 2 | 3 | 4 | 5;

export const ESTADO_PRESUPUESTO_LABELS: Record<EstadoPresupuesto, string> = {
  0: 'Borrador',
  1: 'Enviado',
  2: 'Aceptado',
  3: 'Rechazado',
  4: 'Anulado',
  5: 'Expirado',
};

export const ESTADO_PRESUPUESTO_BADGE: Record<EstadoPresupuesto, string> = {
  0: 'text-bg-secondary',
  1: 'text-bg-primary',
  2: 'text-bg-success',
  3: 'text-bg-danger',
  4: 'text-bg-dark',
  5: 'text-bg-warning',
};

/** Fila del listado de presupuestos (GET /api/v1/quotes). */
export interface QuoteListItem {
  id: number;
  customerId: number;
  customerName: string;
  creationDate: string;
  expirationDate: string | null;
  status: EstadoPresupuesto;
  total: number;
  itemsCount: number;
  expired: boolean;
}

/** Línea congelada del presupuesto (snapshot comercial, BR-063). */
export interface QuoteItem {
  id: number;
  productId: number | null;
  description: string;
  quantity: number;
  unitPrice: number;
  discount: number;
  subtotal: number;
}

/** Detalle del presupuesto con sus líneas. */
export interface Quote extends QuoteListItem {
  sendDate: string | null;
  subtotal: number;
  discount: number;
  notes?: string | null;
  conditions?: string | null;
  stateReason?: string | null;
  /** Pedido creado al aceptar (BR-065); null si aún no se acepta. */
  orderId: number | null;
  items: QuoteItem[];
}

export interface CreateQuoteItemInput {
  productId?: number | null;
  description?: string | null;
  quantity?: number | null;
  unitPrice?: number | null;
  discount?: number | null;
}

export interface CreateQuoteInput {
  customerId: number;
  expirationDate?: string | null;
  notes?: string | null;
  conditions?: string | null;
  items: CreateQuoteItemInput[];
}

export interface UpdateQuoteInput {
  customerId?: number | null;
  expirationDate?: string | null;
  notes?: string | null;
  conditions?: string | null;
  items?: CreateQuoteItemInput[] | null;
}

/**
 * Estados del pedido (api-spec §16). Serializados como número en la API;
 * las transiciones las valida el backend (BR-072).
 */
export type EstadoPedido = 0 | 1 | 2 | 3 | 4 | 5 | 6;

export const ESTADO_PEDIDO_LABELS: Record<EstadoPedido, string> = {
  0: 'Pendiente',
  1: 'Confirmado',
  2: 'En producción',
  3: 'Listo',
  4: 'Entregado',
  5: 'Cancelado',
  6: 'Completado',
};

export const ESTADO_PEDIDO_BADGE: Record<EstadoPedido, string> = {
  0: 'text-bg-secondary',
  1: 'text-bg-primary',
  2: 'text-bg-warning',
  3: 'text-bg-info',
  4: 'text-bg-success',
  5: 'text-bg-danger',
  6: 'text-bg-dark',
};

/** Fila del listado de pedidos (GET /api/v1/orders). */
export interface OrderListItem {
  id: number;
  customerId: number;
  customerName: string;
  /** Presupuesto de origen cuando existe (BR-070). */
  quoteId: number | null;
  creationDate: string;
  status: EstadoPedido;
  total: number;
  /** Entrega estimada para planificación (BR-076). */
  estimatedDeliveryDate: string | null;
}

/** Detalle del pedido con las líneas congeladas del presupuesto (BR-071). */
export interface Order extends OrderListItem {
  items: QuoteItem[];
  /** Marcas de seguimiento de producción/entrega (BR-076). */
  productionStartDate: string | null;
  productionEndDate: string | null;
  deliveryDate: string | null;
  /** Notas operativas del pedido (máx. 2000, BR-076). */
  notes: string | null;
}

/** Fila del historial inmutable de estados (BR-075). */
export interface OrderHistoryItem {
  id: number;
  /** Null cuando el registro corresponde a la creación. */
  previousStatus: EstadoPedido | null;
  newStatus: EstadoPedido;
  date: string;
  /** Null cuando el cambio lo hizo el sistema. */
  userName: string | null;
  comment: string | null;
}

export interface ChangeOrderStatusInput {
  status: EstadoPedido;
  comment?: string | null;
}

/**
 * Edición de la información de seguimiento editable (PATCH
 * /api/v1/orders/{id}, BR-076). Null limpia el valor.
 */
export interface UpdateOrderInput {
  notes?: string | null;
  /** Fecha date-only (YYYY-MM-DD) o ISO; null la limpia. */
  estimatedDeliveryDate?: string | null;
}

/* ------------------------------------------------------------------ */
/* Pagos (api-spec §17)                                                */
/* ------------------------------------------------------------------ */

/** Métodos de pago aceptados (api-spec §17). Serializado como número. */
export type MetodoPago = 0 | 1 | 2 | 3 | 4;

export const METODO_PAGO_LABELS: Record<MetodoPago, string> = {
  0: 'Efectivo',
  1: 'Transferencia',
  2: 'Tarjeta',
  3: 'Cheque',
  4: 'Otro',
};

/** Fila de pago (GET /api/v1/payments, GET /orders/{id}/payments). */
export interface PaymentItem {
  id: number;
  orderId: number;
  /** Cliente del pedido; null si el backend no lo cargó. */
  customerId: number | null;
  customerName: string | null;
  /** Monto en moneda local; siempre mayor a cero (BR-080). */
  amount: number;
  /** Momento del registro en UTC. */
  date: string;
  paymentMethod: MetodoPago;
  reference: string | null;
  notes: string | null;
  /** Usuario interno que registró el pago (auditoría §18). */
  userName: string | null;
}

/**
 * Pagos de un pedido con el resumen financiero (BR-083): el saldo lo
 * calcula siempre el backend (Total − pagos registrados).
 */
export interface OrderPayments {
  items: PaymentItem[];
  totalPaid: number;
  /** Saldo pendiente; nunca negativo (BR-083). */
  balance: number;
}

/** Registro de pago (POST /api/v1/orders/{orderId}/payments, BR-080). */
export interface RegisterPaymentInput {
  amount: number;
  paymentMethod: MetodoPago;
  reference?: string | null;
  notes?: string | null;
}

/* ------------------------------------------------------------------ */
/* Facturas simuladas (api-spec §17A, functional-spec §20, BR-130)     */
/* ------------------------------------------------------------------ */

/** Estado de la factura; serializado como número (api-spec §17A). */
export type EstadoFactura = 0 | 1;

export const ESTADO_FACTURA_LABELS: Record<EstadoFactura, string> = {
  0: 'Emitida',
  1: 'Anulada',
};

export const ESTADO_FACTURA_BADGE: Record<EstadoFactura, string> = {
  0: 'text-bg-success',
  1: 'text-bg-secondary',
};

/**
 * Fila de factura (GET /api/v1/facturas): snapshot inmutable del pedido
 * al emitir (BR-141); documento interno siempre simulado (BR-130).
 */
export interface FacturaItem {
  id: number;
  orderId: number;
  customerId: number;
  /** Nombre del cliente congelado al emitir (BR-141). */
  customerName: string;
  /** Documento interno SIM-######. */
  number: string;
  /** Momento de emisión en UTC. */
  issueDate: string;
  /** Total congelado del pedido al emitir. */
  total: number;
  status: EstadoFactura;
  isSimulated: boolean;
  notes: string | null;
  /** Usuario interno que emitió la factura (auditoría §18). */
  userName: string | null;
}

/**
 * Emisión de factura (POST /api/v1/facturas): el backend resuelve número,
 * snapshot y condición de simulado; una factura por pedido, sólo pedidos
 * Entregado/Completado (BR-130).
 */
export interface CreateFacturaInput {
  orderId: number;
  notes?: string | null;
}

/* ------------------------------------------------------------------ */
/* Notificaciones (api-spec §18, BR-090..BR-092)                       */
/* ------------------------------------------------------------------ */

/** Tipos de evento de notificación; serializado como número. */
export type TipoNotificacion = 0 | 1 | 2 | 3 | 4 | 5;

export const TIPO_NOTIFICACION_LABELS: Record<TipoNotificacion, string> = {
  0: 'Presupuesto enviado',
  1: 'Presupuesto aceptado',
  2: 'Pedido actualizado',
  3: 'Pago registrado',
  4: 'Pedido listo',
  5: 'Solicitud de presupuesto',
};

/** Fila de notificación propia (GET /api/v1/notifications). */
export interface NotificacionItem {
  id: number;
  type: TipoNotificacion;
  title: string;
  message: string;
  entityType: string | null;
  entityId: number | null;
  /** Estado de lectura (BR-091). */
  read: boolean;
  /** Momento de creación en UTC. */
  createdAt: string;
}

/* ------------------------------------------------------------------ */
/* Reportes (api-spec §20, functional-spec §21, BR-140/BR-141)         */
/* ------------------------------------------------------------------ */

/** Clave canónica del reporte (segmento de ruta). */
export type ReportKey = 'orders' | 'payments' | 'quotes' | 'sales' | 'inventory';

export const REPORT_KEY_LABELS: Record<ReportKey, string> = {
  orders: 'Pedidos',
  payments: 'Pagos',
  quotes: 'Presupuestos',
  sales: 'Ventas (facturas)',
  inventory: 'Inventario',
};

/** Formato de exportación de reportes. */
export type ReportFormat = 'xlsx' | 'csv' | 'pdf';

export const REPORT_FORMAT_LABELS: Record<ReportFormat, string> = {
  xlsx: 'Excel (XLSX)',
  csv: 'CSV',
  pdf: 'PDF',
};

/** Columna del documento de reporte. */
export interface ReportColumn {
  key: string;
  header: string;
  align: 'left' | 'right' | 'center';
}

/** Valor de una celda del reporte. */
export type ReportCellValue = string | number | boolean | null;

/** Fila del reporte: pares clave→valor. */
export interface ReportRow {
  cells: Record<string, ReportCellValue>;
}

/** Elemento del bloque de resumen (totales). */
export interface ReportSummaryItem {
  label: string;
  value: ReportCellValue;
}

/**
 * Documento de reporte: el preview JSON y las exportaciones consumen la
 * misma estructura (el backend es la única autoridad de totales).
 */
export interface ReportDocument {
  key: ReportKey;
  title: string;
  /** Instante UTC de generación del documento. */
  generatedAtUtc: string;
  appliedFilters: Array<{ key: string; value: string }>;
  columns: ReportColumn[];
  rows: ReportRow[];
  summary: ReportSummaryItem[];
}

/** Filtros comunes de reportes (query string). */
export interface ReportQueryParams {
  /** Fecha ISO (yyyy-MM-dd) inclusive. */
  from?: string;
  to?: string;
  customerId?: number;
  /** Estado de pedido (orders) o presupuesto (quotes). */
  status?: number;
  /** Método de pago (payments). */
  method?: number;
  /** Tipo de producto (inventory). */
  type?: number;
  /** Activo/inactivo (inventory). */
  active?: boolean;
}

// ---------------------------------------------------------------------------
// Usuarios, roles y permisos (api-spec §19, BR-001/BR-002)
// ---------------------------------------------------------------------------

/** Rol fijo del sistema (BR-001: exactamente estos cinco). */
export interface Rol {
  id: number;
  codigo: string;
  nombre: string;
}

export const ROL_LABELS: Record<number, string> = {
  1: 'SuperAdmin',
  2: 'Owner',
  3: 'Admin',
  4: 'Empleado',
  5: 'Cliente',
};

/** Fila de listado de usuarios (sin FirebaseUid: dato sensible). */
export interface UsuarioList {
  id: number;
  email: string;
  nombre: string;
  apellido: string;
  activo: boolean;
  rolId: number;
}

/** Usuario interno completo (detalle). */
export interface Usuario {
  id: number;
  email: string;
  nombre: string;
  apellido: string;
  activo: boolean;
  rolId: number;
  firebaseUid: string | null;
  fechaCreacion: string;
}

export interface CreateUsuarioInput {
  email: string;
  nombre: string;
  apellido: string;
  rolId: number;
  firebaseUid?: string;
}

export interface UpdateUsuarioInput {
  nombre?: string;
  apellido?: string;
  email?: string;
  activo?: boolean;
  rolId?: number;
}

/** Permiso del catálogo (BR-002). */
export interface PermisoCatalogo {
  id: number;
  codigo: string;
  nombre: string;
}

/** Permisos explícitos de un usuario (adicionales a los del rol). */
export interface PermisosUsuario {
  usuarioId: number;
  codigos: string[];
}

export interface UpdatePermisosUsuarioInput {
  codigos: string[];
}

// ---------------------------------------------------------------------------
// Dashboard (api-spec §19)
// ---------------------------------------------------------------------------

/** Bloque de pedidos: requiere PEDIDOS_VIEW (null si no aplica). */
export interface DashboardOrdersBlock {
  pendientes: number;
  confirmados: number;
  enProduccion: number;
  listos: number;
  entregadosMes: number;
  canceladosMes: number;
}

/** Bloque de presupuestos: requiere PRESUPUESTOS_VIEW. */
export interface DashboardQuotesBlock {
  borradores: number;
  enviados: number;
  aceptadosMes: number;
  rechazadosMes: number;
}

/** Bloque de ventas (facturas del mes): requiere FACTURAS_VIEW o REPORTES_VIEW. */
export interface DashboardSalesBlock {
  facturadoMes: number;
  facturasMes: number;
}

/** Bloque de pagos: requiere PAGOS_VIEW. */
export interface DashboardPaymentsBlock {
  cobradoMes: number;
  pagosMes: number;
}

/** Bloque de inventario: requiere STOCK_VIEW. */
export interface DashboardInventoryBlock {
  productosActivos: number;
  stockBajo: number;
  enShowroom: number;
}

/** Notificaciones no leídas del propio usuario (siempre presente). */
export interface DashboardNotificationsBlock {
  unreadCount: number;
}

/** Respuesta de GET /api/v1/dashboard. Los bloques nulos = sin permiso. */
export interface DashboardData {
  orders: DashboardOrdersBlock | null;
  quotes: DashboardQuotesBlock | null;
  sales: DashboardSalesBlock | null;
  payments: DashboardPaymentsBlock | null;
  inventory: DashboardInventoryBlock | null;
  notifications: DashboardNotificationsBlock;
  generatedAtUtc: string;
}

/* ------------------------------------------------------------------ */
/* Portal Cliente (api-spec §9B, functional-spec §23, BR-010)           */
/* ------------------------------------------------------------------ */

/** Perfil del cliente vinculado (GET /api/v1/my/profile). */
export interface ClienteProfile {
  id: number;
  email: string;
  nombre: string;
  apellido: string;
  telefono: string | null;
  direccion: string | null;
  documento: string | null;
  fechaCreacion: string;
}

/**
 * Actualización del perfil desde el portal (PATCH /api/v1/my/profile).
 * El email no se modifica desde acá: es la identidad de la cuenta.
 */
export interface UpdateProfileInput {
  nombre?: string | null;
  apellido?: string | null;
  telefono?: string | null;
  direccion?: string | null;
  documento?: string | null;
}

/**
 * Solicitud de presupuesto desde el portal (POST /api/v1/my/quotes): el
 * cliente describe el trabajo; la carpintería lo cotiza (ítem sin precio).
 */
export interface RequestQuoteInput {
  description: string;
  cantidad?: number;
}

/** Resumen del portal (GET /api/v1/my/summary): el saldo lo calcula el backend. */
export interface ClienteSummary {
  totalPedidos: number;
  totalPagado: number;
  saldo: number;
  pedidosCount: number;
  presupuestosPendientes: number;
}

/* ------------------------------------------------------------------ */
/* Trabajos realizados / vidriera (api-spec §12B/§12C, AGENTS.md §23)   */
/* ------------------------------------------------------------------ */

export interface TrabajoImagen {
  id: number;
  url: string;
  orden: number;
}

export interface TrabajoList {
  id: number;
  nombre: string;
  tipoMueble: string | null;
  clienteNombre: string | null;
  categoria: string | null;
  fecha: string | null;
  destacado: boolean;
  publicado: boolean;
  fechaCreacion: string;
}

export interface TrabajoRealizado extends TrabajoList {
  publicarNombreCliente: boolean;
  descripcion: string | null;
  medidas: string | null;
  melamina: string | null;
  accesorios: string | null;
  tags: string[];
  fechaActualizacion: string | null;
  imagenes: TrabajoImagen[];
}

export interface CreateTrabajoInput {
  nombre: string;
  tipoMueble?: string | null;
  clienteNombre?: string | null;
  publicarNombreCliente?: boolean;
  fecha?: string | null;
  descripcion?: string | null;
  medidas?: string | null;
  melamina?: string | null;
  accesorios?: string | null;
  categoria?: string | null;
  tags?: string[];
  destacado?: boolean;
  publicado?: boolean;
}

export interface UpdateTrabajoInput {
  nombre?: string;
  tipoMueble?: string | null;
  clienteNombre?: string | null;
  publicarNombreCliente?: boolean;
  fecha?: string | null;
  limpiarFecha?: boolean;
  descripcion?: string | null;
  medidas?: string | null;
  melamina?: string | null;
  accesorios?: string | null;
  categoria?: string | null;
  tags?: string[];
  destacado?: boolean;
  publicado?: boolean;
}

/** Fila de la vidriera pública: oculta flags internos y el nombre del cliente no consentido. */
export interface ShowroomItem {
  id: number;
  nombre: string;
  tipoMueble: string | null;
  clienteNombre: string | null;
  fecha: string | null;
  categoria: string | null;
  tags: string[];
  destacado: boolean;
  imagenUrl: string | null;
}

export interface ShowroomTrabajo {
  id: number;
  nombre: string;
  tipoMueble: string | null;
  clienteNombre: string | null;
  fecha: string | null;
  descripcion: string | null;
  medidas: string | null;
  melamina: string | null;
  accesorios: string | null;
  categoria: string | null;
  tags: string[];
  destacado: boolean;
  imagenes: TrabajoImagen[];
}