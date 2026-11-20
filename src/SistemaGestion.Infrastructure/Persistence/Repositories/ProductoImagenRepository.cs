using Microsoft.EntityFrameworkCore;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Infrastructure.Persistence.Repositories;

/// <summary>
/// Persistencia de imágenes de producto con EF Core + PostgreSQL.
/// Sólo referencias al storage externo (BR-101); lecturas con
/// AsNoTracking (AGENTS.md §26).
/// </summary>
public sealed class ProductoImagenRepository : IProductoImagenRepository
{
    private readonly SistemaGestionDbContext _context;

    public ProductoImagenRepository(SistemaGestionDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProductoImagen>> GetByProductoAsync(
        int productoId,
        CancellationToken cancellationToken = default)
        => await _context.ProductoImagenes
            .AsNoTracking()
            .Where(i => i.ProductoId == productoId)
            .OrderBy(i => i.Orden)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductoImagen>> GetByProductoIdsAsync(
        IReadOnlyCollection<int> productoIds,
        CancellationToken cancellationToken = default)
    {
        if (productoIds.Count == 0)
        {
            return [];
        }

        return await _context.ProductoImagenes
            .AsNoTracking()
            .Where(i => productoIds.Contains(i.ProductoId))
            .OrderBy(i => i.ProductoId)
            .ThenBy(i => i.Orden)
            .ToListAsync(cancellationToken);
    }

    public Task<ProductoImagen?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.ProductoImagenes
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<int> CountByProductoAsync(int productoId, CancellationToken cancellationToken = default)
        => await _context.ProductoImagenes
            .CountAsync(i => i.ProductoId == productoId, cancellationToken);

    public async Task<ProductoImagen> AddAsync(ProductoImagen imagen, CancellationToken cancellationToken = default)
    {
        await _context.ProductoImagenes.AddAsync(imagen, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return imagen;
    }

    public async Task DeleteAsync(ProductoImagen imagen, CancellationToken cancellationToken = default)
    {
        // La entidad viene de una consulta sin tracking: adjuntarla antes.
        _context.ProductoImagenes.Attach(imagen);
        _context.ProductoImagenes.Remove(imagen);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
