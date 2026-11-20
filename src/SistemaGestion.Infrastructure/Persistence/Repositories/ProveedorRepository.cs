using Microsoft.EntityFrameworkCore;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Infrastructure.Persistence.Repositories;

/// <summary>
/// Persistencia de proveedores con EF Core + PostgreSQL (BR-020).
/// Lecturas con AsNoTracking (AGENTS.md §26).
/// </summary>
public sealed class ProveedorRepository : IProveedorRepository
{
    private readonly SistemaGestionDbContext _context;

    public ProveedorRepository(SistemaGestionDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<Proveedor>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        bool? activo,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Proveedores.AsNoTracking();

        if (activo.HasValue)
        {
            query = query.Where(p => p.Activo == activo.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(p =>
                p.Nombre.ToLower().Contains(term)
                || (p.Contacto != null && p.Contacto.ToLower().Contains(term))
                || (p.Email != null && p.Email.ToLower().Contains(term)));
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Proveedor>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
        };
    }

    public Task<Proveedor?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Proveedores
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> EmailExistsAsync(
        string email,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();

        return _context.Proveedores.AsNoTracking().AnyAsync(
            p => p.Email == normalized && (excludeId == null || p.Id != excludeId),
            cancellationToken);
    }

    public async Task<Proveedor> AddAsync(Proveedor proveedor, CancellationToken cancellationToken = default)
    {
        await _context.Proveedores.AddAsync(proveedor, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return proveedor;
    }

    public async Task UpdateAsync(Proveedor proveedor, CancellationToken cancellationToken = default)
    {
        _context.Proveedores.Update(proveedor);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
