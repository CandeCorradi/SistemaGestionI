using SistemaGestion.Domain.Enums;

namespace SistemaGestion.Application.Abstractions;

/// <summary>
/// Conteo liviano para el dashboard (api-spec §19): consultas agregadas
/// de una sola pasada, sin cargar colecciones completas (AGENTS.md §62).
/// </summary>
public interface IDashboardRepository
{
    /// <summary>Cantidad de pedidos agrupados por estado.</summary>
    Task<IReadOnlyDictionary<EstadoPedido, int>> CountPedidosPorEstadoAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Pedidos entregados dentro del rango [desdeUtc, hastaUtc).</summary>
    Task<int> CountPedidosEntregadosAsync(
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Pedidos cancelados dentro del rango [desdeUtc, hastaUtc).</summary>
    Task<int> CountPedidosCanceladosAsync(
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Cantidad de presupuestos agrupados por estado.</summary>
    Task<IReadOnlyDictionary<EstadoPresupuesto, int>> CountPresupuestosPorEstadoAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Presupuestos aceptados/rechazados dentro del rango.</summary>
    Task<int> CountPresupuestosEnRangoAsync(
        EstadoPresupuesto estado,
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Suma de montos de pagos registrados en el rango.</summary>
    Task<decimal> SumPagosAsync(
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken cancellationToken = default);

    /// <summary>cantidad de pagos registrados en el rango.</summary>
    Task<int> CountPagosAsync(
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Suma de totales de facturas emitidas en el rango (BR-141).</summary>
    Task<decimal> SumFacturasAsync(
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Cantidad de facturas emitidas en el rango.</summary>
    Task<int> CountFacturasAsync(
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Conteos de inventario: activos, con stock bajo y en showroom.</summary>
    Task<DashboardInventarioCounts> CountInventarioAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>Conteos auxiliares de inventario del dashboard.</summary>
public sealed record DashboardInventarioCounts(
    int ProductosActivos,
    int StockBajo,
    int EnShowroom);
