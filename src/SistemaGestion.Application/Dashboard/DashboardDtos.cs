namespace SistemaGestion.Application.Dashboard;

/// <summary>
/// Bloque de pedidos del dashboard (api-spec §19): requiere PEDIDOS_VIEW.
/// </summary>
public sealed record DashboardOrdersBlock(
    int Pendientes,
    int Confirmados,
    int EnProduccion,
    int Listos,
    int EntregadosMes,
    int CanceladosMes);

/// <summary>
/// Bloque de presupuestos del dashboard: requiere PRESUPUESTOS_VIEW.
/// </summary>
public sealed record DashboardQuotesBlock(
    int Borradores,
    int Enviados,
    int AceptadosMes,
    int RechazadosMes);

/// <summary>
/// Bloque de ventas (facturas emitidas) del dashboard: requiere
/// FACTURAS_VIEW o REPORTES_VIEW.
/// </summary>
public sealed record DashboardSalesBlock(
    decimal FacturadoMes,
    int FacturasMes);

/// <summary>
/// Bloque de pagos del dashboard: requiere PAGOS_VIEW.
/// </summary>
public sealed record DashboardPaymentsBlock(
    decimal CobradoMes,
    int PagosMes);

/// <summary>
/// Bloque de inventario del dashboard: requiere STOCK_VIEW.
/// </summary>
public sealed record DashboardInventoryBlock(
    int ProductosActivos,
    int StockBajo,
    int EnShowroom);

/// <summary>
/// Notificaciones no leídas del propio usuario (siempre presente: son
/// datos propios del token, BR-090).
/// </summary>
public sealed record DashboardNotificationsBlock(int UnreadCount);

/// <summary>
/// Respuesta de GET /api/v1/dashboard (api-spec §19). Los bloques nulos
/// indican que el usuario no tiene el permiso correspondiente: la API
/// sólo devuelve información autorizada para el solicitante.
/// </summary>
public sealed record DashboardDto(
    DashboardOrdersBlock? Orders,
    DashboardQuotesBlock? Quotes,
    DashboardSalesBlock? Sales,
    DashboardPaymentsBlock? Payments,
    DashboardInventoryBlock? Inventory,
    DashboardNotificationsBlock Notifications,
    DateTime GeneratedAtUtc);
