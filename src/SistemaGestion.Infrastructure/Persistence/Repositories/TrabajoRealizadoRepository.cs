using Microsoft.EntityFrameworkCore;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Infrastructure.Persistence.Repositories;

/// <summary>
/// Persistencia de trabajos realizados con EF Core + PostgreSQL.
/// Lecturas con AsNoTracking (AGENTS.md §26); la vidriera pública filtra
/// por <c>Publicado</c> (BR-190) y ordena destacados primero.
/// </summary>
public sealed class TrabajoRealizadoRepository : ITrabajoRealizadoRepository
{
    private readonly SistemaGestionDbContext _context;

    public TrabajoRealizadoRepository(SistemaGestionDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<TrabajoRealizado>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        bool? publicado,
        bool? destacado,
        string? categoria,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TrabajosRealizados.AsNoTracking();

        if (publicado.HasValue)
        {
            query = query.Where(t => t.Publicado == publicado.Value);
        }

        if (destacado.HasValue)
        {
            query = query.Where(t => t.Destacado == destacado.Value);
        }

        if (!string.IsNullOrWhiteSpace(categoria))
        {
            var cat = categoria.ToLower();
            query = query.Where(t => t.Categoria != null && t.Categoria.ToLower() == cat);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(t =>
                t.Nombre.ToLower().Contains(term)
                || (t.Descripcion != null && t.Descripcion.ToLower().Contains(term))
                || (t.TipoMueble != null && t.TipoMueble.ToLower().Contains(term))
                || (t.ClienteNombre != null && t.ClienteNombre.ToLower().Contains(term)));
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.Destacado)
            .ThenByDescending(t => t.Fecha)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TrabajoRealizado>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
        };
    }

    public async Task<PagedResult<TrabajoRealizado>> GetShowroomPagedAsync(
        int page,
        int pageSize,
        string? search,
        string? categoria,
        string? tipoMueble,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TrabajosRealizados
            .AsNoTracking()
            .Where(t => t.Publicado);

        if (!string.IsNullOrWhiteSpace(categoria))
        {
            var cat = categoria.ToLower();
            query = query.Where(t => t.Categoria != null && t.Categoria.ToLower() == cat);
        }

        if (!string.IsNullOrWhiteSpace(tipoMueble))
        {
            var tipo = tipoMueble.ToLower();
            query = query.Where(t => t.TipoMueble != null && t.TipoMueble.ToLower() == tipo);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(t =>
                t.Nombre.ToLower().Contains(term)
                || (t.Descripcion != null && t.Descripcion.ToLower().Contains(term))
                || (t.Categoria != null && t.Categoria.ToLower().Contains(term)));
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.Destacado)
            .ThenByDescending(t => t.Fecha)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TrabajoRealizado>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
        };
    }

    public Task<TrabajoRealizado?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.TrabajosRealizados
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<TrabajoRealizado?> GetPublishedByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.TrabajosRealizados
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.Publicado, cancellationToken);

    public async Task<TrabajoRealizado> AddAsync(TrabajoRealizado trabajo, CancellationToken cancellationToken = default)
    {
        await _context.TrabajosRealizados.AddAsync(trabajo, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return trabajo;
    }

    public async Task UpdateAsync(TrabajoRealizado trabajo, CancellationToken cancellationToken = default)
    {
        _context.TrabajosRealizados.Attach(trabajo);
        _context.Entry(trabajo).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TrabajoRealizado trabajo, CancellationToken cancellationToken = default)
    {
        _context.TrabajosRealizados.Attach(trabajo);
        _context.TrabajosRealizados.Remove(trabajo);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
