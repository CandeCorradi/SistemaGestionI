using Microsoft.EntityFrameworkCore;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Enums;

namespace SistemaGestion.Infrastructure.Persistence.Repositories;

/// <summary>
/// Conteos agregados del dashboard (api-spec §19) con EF Core:
/// consultas de una sola pasada sin traer colecciones completas.
/// </summary>
public sealed class DashboardRepository : IDashboardRepository
{
    private readonly SistemaGestionDbContext _context;

    public DashboardRepository(SistemaGestionDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyDictionary<EstadoPedido, int>> CountPedidosPorEstadoAsync(
        CancellationToken cancellationToken = default)
    {
        var grupos = await _context.Pedidos
            .AsNoTracking()
            .GroupBy(p => p.Estado)
            .Select(g => new { Estado = g.Key, Total = g.Count() })
            .ToListAsync(cancellationToken);

        return grupos.ToDictionary(g => g.Estado, g => g.Total);
    }

    public Task<int> CountPedidosEntregadosAsync(
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken cancellationToken = default)
        => _context.Pedidos
            .AsNoTracking()
            .CountAsync(p =>
                p.Estado == EstadoPedido.Entregado
                && p.FechaEntrega >= desdeUtc
                && p.FechaEntrega < hastaUtc,
                cancellationToken);

    public Task<int> CountPedidosCanceladosAsync(
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken cancellationToken = default)
        => _context.Pedidos
            .AsNoTracking()
            .CountAsync(p =>
                p.Estado == EstadoPedido.Cancelado
                && p.FechaCreacion >= desdeUtc
                && p.FechaCreacion < hastaUtc,
                cancellationToken);

    public async Task<IReadOnlyDictionary<EstadoPresupuesto, int>> CountPresupuestosPorEstadoAsync(
        CancellationToken cancellationToken = default)
    {
        var grupos = await _context.Presupuestos
            .AsNoTracking()
            .GroupBy(p => p.Estado)
            .Select(g => new { Estado = g.Key, Total = g.Count() })
            .ToListAsync(cancellationToken);

        return grupos.ToDictionary(g => g.Estado, g => g.Total);
    }

    public Task<int> CountPresupuestosEnRangoAsync(
        EstadoPresupuesto estado,
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken cancellationToken = default)
    {
        // La fecha de referencia del estado es la última modificación del
        // presupuesto; para Aceptado/Rechazado se usa FechaModificacion.
        return _context.Presupuestos
            .AsNoTracking()
            .CountAsync(p =>
                p.Estado == estado
                && p.FechaModificacion >= desdeUtc
                && p.FechaModificacion < hastaUtc,
                cancellationToken);
    }

    public async Task<decimal> SumPagosAsync(
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken cancellationToken = default)
    {
        var total = await _context.Pagos
            .AsNoTracking()
            .Where(p => p.FechaPago >= desdeUtc && p.FechaPago < hastaUtc)
            .SumAsync(p => (decimal?)p.Monto, cancellationToken);
        return total ?? 0m;
    }

    public Task<int> CountPagosAsync(
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken cancellationToken = default)
        => _context.Pagos
            .AsNoTracking()
            .CountAsync(p => p.FechaPago >= desdeUtc && p.FechaPago < hastaUtc, cancellationToken);

    public async Task<decimal> SumFacturasAsync(
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken cancellationToken = default)
    {
        var total = await _context.Facturas
            .AsNoTracking()
            .Where(f => f.FechaEmision >= desdeUtc && f.FechaEmision < hastaUtc)
            .SumAsync(f => (decimal?)f.Total, cancellationToken);
        return total ?? 0m;
    }

    public Task<int> CountFacturasAsync(
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken cancellationToken = default)
        => _context.Facturas
            .AsNoTracking()
            .CountAsync(f =>
                f.FechaEmision >= desdeUtc && f.FechaEmision < hastaUtc,
                cancellationToken);

    public async Task<DashboardInventarioCounts> CountInventarioAsync(
        CancellationToken cancellationToken = default)
    {
        var productos = await _context.Productos
            .AsNoTracking()
            .Where(p => p.Activo)
            .Select(p => new { p.StockActual, p.StockMinimo, p.Showroom })
            .ToListAsync(cancellationToken);

        return new DashboardInventarioCounts(
            productos.Count,
            productos.Count(p => p.StockMinimo > 0 && p.StockActual <= p.StockMinimo),
            productos.Count(p => p.Showroom));
    }
}
