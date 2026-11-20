using Microsoft.EntityFrameworkCore;
using Npgsql;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Infrastructure.Persistence.Repositories;

/// <summary>
/// Persistencia de facturas con EF Core + PostgreSQL. La emisión completa
/// (insertar la fila y asignar el número de documento) corre dentro de una
/// transacción para no dejar facturas sin numerar (BR-151); una violación
/// de unicidad del índice en PedidoId (emisión concurrente del mismo
/// pedido) se relanza como InvalidOperationException para que la
/// aplicación responda 409.
/// </summary>
public sealed class FacturaRepository : IFacturaRepository
{
    private readonly SistemaGestionDbContext _context;

    public FacturaRepository(SistemaGestionDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsByPedidoAsync(
        int pedidoId,
        CancellationToken cancellationToken = default)
        => await _context.Facturas
            .AsNoTracking()
            .AnyAsync(f => f.PedidoId == pedidoId, cancellationToken);

    public async Task AddAsync(Factura factura, CancellationToken cancellationToken = default)
    {
        await _context.Facturas.AddAsync(factura, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database
            .BeginTransactionAsync(cancellationToken);

        try
        {
            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex) when (EsViolacionUnica(ex))
        {
            // La transacción queda abortada; el dispose hace el rollback.
            throw new InvalidOperationException(
                "Ya existe una factura para el pedido indicado.", ex);
        }
    }

    public async Task<Factura?> GetByIdReadOnlyAsync(
        int id,
        CancellationToken cancellationToken = default)
        => await _context.Facturas
            .AsNoTracking()
            .Include(f => f.Pedido)
                .ThenInclude(p => p!.Cliente)
            .Include(f => f.Usuario)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<PagedResult<Factura>> GetPagedAsync(
        int page,
        int pageSize,
        int? pedidoId,
        int? clienteId,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Facturas.AsNoTracking();

        if (pedidoId.HasValue)
        {
            query = query.Where(f => f.PedidoId == pedidoId.Value);
        }

        if (clienteId.HasValue)
        {
            query = query.Where(f => f.ClienteId == clienteId.Value);
        }

        if (dateFrom.HasValue)
        {
            var desde = DateTime.SpecifyKind(dateFrom.Value.Date, DateTimeKind.Utc);
            query = query.Where(f => f.FechaEmision >= desde);
        }

        if (dateTo.HasValue)
        {
            var hasta = DateTime.SpecifyKind(dateTo.Value.Date, DateTimeKind.Utc).AddDays(1);
            query = query.Where(f => f.FechaEmision < hasta);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(f =>
                f.Numero.ToLower().Contains(term)
                || f.ClienteNombre.ToLower().Contains(term));
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(f => f.Pedido)
                .ThenInclude(p => p!.Cliente)
            .Include(f => f.Usuario)
            .OrderByDescending(f => f.FechaEmision)
            .ThenByDescending(f => f.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Factura>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
        };
    }

    private static bool EsViolacionUnica(Exception ex)
        => ex is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
            || ex is DbUpdateException { InnerException: PostgresException inner }
                && inner.SqlState == PostgresErrorCodes.UniqueViolation;
}
