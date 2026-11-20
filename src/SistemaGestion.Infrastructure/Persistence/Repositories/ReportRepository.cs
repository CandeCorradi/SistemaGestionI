using Microsoft.EntityFrameworkCore;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;

namespace SistemaGestion.Infrastructure.Persistence.Repositories;

/// <summary>
/// Consultas de solo lectura para reportes (functional-spec §21): todas
/// con AsNoTracking y acotadas por rango de fechas; los totales salen de
/// los snapshots históricos (BR-141). El rango es inclusivo por día (mismo
/// criterio que los listados paginados).
/// </summary>
public sealed class ReportRepository : IReportRepository
{
    private readonly SistemaGestionDbContext _context;

    public ReportRepository(SistemaGestionDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Pedido>> GetPedidosAsync(
        DateTime? desde,
        DateTime? hasta,
        EstadoPedido? estado,
        int? clienteId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Pedidos
            .AsNoTracking()
            .Include(p => p.Cliente)
            .AsQueryable();

        if (desde.HasValue)
        {
            var valorDesde = DateTime.SpecifyKind(desde.Value.Date, DateTimeKind.Utc);
            query = query.Where(p => p.FechaCreacion >= valorDesde);
        }

        if (hasta.HasValue)
        {
            var valorHasta = DateTime.SpecifyKind(hasta.Value.Date, DateTimeKind.Utc).AddDays(1);
            query = query.Where(p => p.FechaCreacion < valorHasta);
        }

        if (estado.HasValue)
        {
            query = query.Where(p => p.Estado == estado.Value);
        }

        if (clienteId.HasValue)
        {
            query = query.Where(p => p.ClienteId == clienteId.Value);
        }

        return await query
            .OrderBy(p => p.FechaCreacion)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Pago>> GetPagosAsync(
        DateTime? desde,
        DateTime? hasta,
        int? clienteId,
        MetodoPago? metodo,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Pagos
            .AsNoTracking()
            .Include(p => p.Pedido)
                .ThenInclude(p => p!.Cliente)
            .Include(p => p.Usuario)
            .AsQueryable();

        if (desde.HasValue)
        {
            var valorDesde = DateTime.SpecifyKind(desde.Value.Date, DateTimeKind.Utc);
            query = query.Where(p => p.FechaPago >= valorDesde);
        }

        if (hasta.HasValue)
        {
            var valorHasta = DateTime.SpecifyKind(hasta.Value.Date, DateTimeKind.Utc).AddDays(1);
            query = query.Where(p => p.FechaPago < valorHasta);
        }

        if (clienteId.HasValue)
        {
            query = query.Where(p => p.Pedido!.ClienteId == clienteId.Value);
        }

        if (metodo.HasValue)
        {
            query = query.Where(p => p.Metodo == metodo.Value);
        }

        return await query
            .OrderBy(p => p.FechaPago)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Presupuesto>> GetPresupuestosAsync(
        DateTime? desde,
        DateTime? hasta,
        EstadoPresupuesto? estado,
        int? clienteId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Presupuestos
            .AsNoTracking()
            .Include(p => p.Cliente)
            .AsQueryable();

        if (desde.HasValue)
        {
            var valorDesde = DateTime.SpecifyKind(desde.Value.Date, DateTimeKind.Utc);
            query = query.Where(p => p.FechaCreacion >= valorDesde);
        }

        if (hasta.HasValue)
        {
            var valorHasta = DateTime.SpecifyKind(hasta.Value.Date, DateTimeKind.Utc).AddDays(1);
            query = query.Where(p => p.FechaCreacion < valorHasta);
        }

        if (estado.HasValue)
        {
            query = query.Where(p => p.Estado == estado.Value);
        }

        if (clienteId.HasValue)
        {
            query = query.Where(p => p.ClienteId == clienteId.Value);
        }

        return await query
            .OrderBy(p => p.FechaCreacion)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Factura>> GetFacturasAsync(
        DateTime? desde,
        DateTime? hasta,
        int? clienteId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Facturas
            .AsNoTracking()
            .AsQueryable();

        if (desde.HasValue)
        {
            var valorDesde = DateTime.SpecifyKind(desde.Value.Date, DateTimeKind.Utc);
            query = query.Where(f => f.FechaEmision >= valorDesde);
        }

        if (hasta.HasValue)
        {
            var valorHasta = DateTime.SpecifyKind(hasta.Value.Date, DateTimeKind.Utc).AddDays(1);
            query = query.Where(f => f.FechaEmision < valorHasta);
        }

        if (clienteId.HasValue)
        {
            query = query.Where(f => f.ClienteId == clienteId.Value);
        }

        return await query
            .OrderBy(f => f.FechaEmision)
            .ThenBy(f => f.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Producto>> GetProductosAsync(
        TipoProducto? tipo,
        bool? activo,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Productos
            .AsNoTracking()
            .AsQueryable();

        if (tipo.HasValue)
        {
            query = query.Where(p => p.Tipo == tipo.Value);
        }

        if (activo.HasValue)
        {
            query = query.Where(p => p.Activo == activo.Value);
        }

        return await query
            .OrderBy(p => p.CodigoSku)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, decimal>> GetSumasPagadasAsync(
        IReadOnlyCollection<int> pedidoIds,
        CancellationToken cancellationToken = default)
    {
        if (pedidoIds.Count == 0)
        {
            return new Dictionary<int, decimal>();
        }

        var sumas = await _context.Pagos
            .AsNoTracking()
            .Where(p => pedidoIds.Contains(p.PedidoId))
            .GroupBy(p => p.PedidoId)
            .Select(g => new { PedidoId = g.Key, Total = g.Sum(p => p.Monto) })
            .ToListAsync(cancellationToken);

        return sumas.ToDictionary(s => s.PedidoId, s => s.Total);
    }
}
