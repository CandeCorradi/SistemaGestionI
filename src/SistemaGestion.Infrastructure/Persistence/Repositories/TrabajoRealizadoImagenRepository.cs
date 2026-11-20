using Microsoft.EntityFrameworkCore;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Infrastructure.Persistence.Repositories;

/// <summary>
/// Persistencia de imágenes de trabajos realizados con EF Core +
/// PostgreSQL. Sólo referencias al storage externo (BR-101); lecturas
/// con AsNoTracking (AGENTS.md §26).
/// </summary>
public sealed class TrabajoRealizadoImagenRepository : ITrabajoRealizadoImagenRepository
{
    private readonly SistemaGestionDbContext _context;

    public TrabajoRealizadoImagenRepository(SistemaGestionDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TrabajoRealizadoImagen>> GetByTrabajoAsync(
        int trabajoRealizadoId,
        CancellationToken cancellationToken = default)
        => await _context.TrabajoRealizadoImagenes
            .AsNoTracking()
            .Where(i => i.TrabajoRealizadoId == trabajoRealizadoId)
            .OrderBy(i => i.Orden)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TrabajoRealizadoImagen>> GetByTrabajoIdsAsync(
        IReadOnlyCollection<int> trabajoIds,
        CancellationToken cancellationToken = default)
    {
        if (trabajoIds.Count == 0)
        {
            return [];
        }

        return await _context.TrabajoRealizadoImagenes
            .AsNoTracking()
            .Where(i => trabajoIds.Contains(i.TrabajoRealizadoId))
            .OrderBy(i => i.TrabajoRealizadoId)
            .ThenBy(i => i.Orden)
            .ToListAsync(cancellationToken);
    }

    public Task<TrabajoRealizadoImagen?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.TrabajoRealizadoImagenes
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<int> CountByTrabajoAsync(
        int trabajoRealizadoId,
        CancellationToken cancellationToken = default)
        => await _context.TrabajoRealizadoImagenes
            .CountAsync(i => i.TrabajoRealizadoId == trabajoRealizadoId, cancellationToken);

    public async Task<TrabajoRealizadoImagen> AddAsync(
        TrabajoRealizadoImagen imagen,
        CancellationToken cancellationToken = default)
    {
        await _context.TrabajoRealizadoImagenes.AddAsync(imagen, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return imagen;
    }

    public async Task DeleteAsync(TrabajoRealizadoImagen imagen, CancellationToken cancellationToken = default)
    {
        // La entidad viene de una consulta sin tracking: adjuntarla antes.
        _context.TrabajoRealizadoImagenes.Attach(imagen);
        _context.TrabajoRealizadoImagenes.Remove(imagen);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByTrabajoAsync(int trabajoRealizadoId, CancellationToken cancellationToken = default)
    {
        var imagenes = await _context.TrabajoRealizadoImagenes
            .Where(i => i.TrabajoRealizadoId == trabajoRealizadoId)
            .ToListAsync(cancellationToken);

        if (imagenes.Count == 0)
        {
            return;
        }

        _context.TrabajoRealizadoImagenes.RemoveRange(imagenes);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
