using Microsoft.EntityFrameworkCore;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Infrastructure.Persistence.Repositories;

/// <summary>
/// Persistencia de inventario con EF Core + PostgreSQL. La aplicación de
/// movimientos es atómica (BR-050): UPDATE condicional del stock (CAS sobre
/// el valor leído) e INSERT del movimiento dentro de una misma transacción;
/// si el CAS no coincide con el stock esperado se revierte todo (BR-053).
/// Lecturas con AsNoTracking (AGENTS.md §26).
/// </summary>
public sealed class InventarioRepository : IInventarioRepository
{
    private readonly SistemaGestionDbContext _context;

    public InventarioRepository(SistemaGestionDbContext context)
    {
        _context = context;
    }

    public Task<Producto?> GetStockAsync(int productoId, CancellationToken cancellationToken = default)
        => _context.Productos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productoId, cancellationToken);

    public async Task<PagedResult<MovimientoStock>> GetMovementsPagedAsync(
        int page,
        int pageSize,
        int? productoId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.MovimientosStock
            .AsNoTracking()
            .Include(m => m.Producto)
            .Include(m => m.Usuario)
            .AsQueryable();

        if (productoId.HasValue)
        {
            query = query.Where(m => m.ProductoId == productoId.Value);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.Fecha)
            .ThenByDescending(m => m.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<MovimientoStock>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
        };
    }

    public async Task<bool> TryApplyMovementAsync(
        MovimientoStock movimiento,
        int stockEsperado,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        // CAS: sólo actualiza si el stock sigue siendo el leído (BR-053).
        var afectadas = await _context.Productos
            .Where(p => p.Id == movimiento.ProductoId
                && p.StockActual == stockEsperado
                && p.Activo)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(p => p.StockActual, movimiento.StockPosterior),
                cancellationToken);

        if (afectadas == 0)
        {
            // Stock modificado concurrentemente: no se toca nada (BR-050).
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        _context.MovimientosStock.Add(movimiento);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
