using Microsoft.EntityFrameworkCore;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Infrastructure.Persistence.Repositories;

/// <summary>
/// Persistencia de productos con EF Core + PostgreSQL. Lecturas con
/// AsNoTracking (AGENTS.md §26); filtros del catálogo según api-spec §12.
/// </summary>
public sealed class ProductoRepository : IProductoRepository
{
    private readonly SistemaGestionDbContext _context;

    public ProductoRepository(SistemaGestionDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<Producto>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        TipoProducto? tipo,
        string? categoria,
        bool? activo,
        bool? showroom,
        bool? lowStock,
        bool? isCustom,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Productos.AsNoTracking();

        if (activo.HasValue)
        {
            query = query.Where(p => p.Activo == activo.Value);
        }

        if (showroom.HasValue)
        {
            query = query.Where(p => p.Showroom == showroom.Value);
        }

        if (isCustom.HasValue)
        {
            query = query.Where(p => p.EsPersonalizado == isCustom.Value);
        }

        if (tipo.HasValue)
        {
            query = query.Where(p => p.Tipo == tipo.Value);
        }

        if (!string.IsNullOrWhiteSpace(categoria))
        {
            var cat = categoria.ToLower();
            query = query.Where(p => p.Categoria != null && p.Categoria.ToLower() == cat);
        }

        if (lowStock == true)
        {
            query = query.Where(p => p.StockMinimo > 0 && p.StockActual <= p.StockMinimo);
        }
        else if (lowStock == false)
        {
            query = query.Where(p => p.StockMinimo == 0 || p.StockActual > p.StockMinimo);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(p =>
                p.Nombre.ToLower().Contains(term)
                || p.CodigoSku.ToLower().Contains(term)
                || (p.Descripcion != null && p.Descripcion.ToLower().Contains(term)));
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Producto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
        };
    }

    public Task<Producto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Productos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> SkuExistsAsync(string codigoSku, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = codigoSku.Trim().ToUpperInvariant();

        return _context.Productos.AsNoTracking().AnyAsync(
            p => p.CodigoSku == normalized && (excludeId == null || p.Id != excludeId),
            cancellationToken);
    }

    public async Task<Producto> AddAsync(Producto producto, CancellationToken cancellationToken = default)
    {
        await _context.Productos.AddAsync(producto, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return producto;
    }

    public async Task UpdateAsync(Producto producto, CancellationToken cancellationToken = default)
    {
        _context.Productos.Update(producto);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
