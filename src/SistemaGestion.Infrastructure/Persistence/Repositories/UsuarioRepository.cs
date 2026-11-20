using Microsoft.EntityFrameworkCore;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Infrastructure.Persistence.Repositories;

/// <summary>
/// Persistencia de usuarios con EF Core + PostgreSQL.
/// Lecturas con AsNoTracking (AGENTS.md §26).
/// </summary>
public sealed class UsuarioRepository : IUsuarioRepository
{
    private readonly SistemaGestionDbContext _context;

    public UsuarioRepository(SistemaGestionDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<Usuario>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Usuarios.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(u =>
                u.Nombre.ToLower().Contains(term)
                || u.Apellido.ToLower().Contains(term)
                || u.Email.ToLower().Contains(term));
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Usuario>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
        };
    }

    public Task<Usuario?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<Usuario?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();

        return _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
    }

    public Task<Usuario?> GetByFirebaseUidAsync(
        string firebaseUid,
        CancellationToken cancellationToken = default)
        => _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid && u.Activo, cancellationToken);

    public Task<bool> EmailExistsAsync(
        string email,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();

        return _context.Usuarios.AsNoTracking().AnyAsync(
            u => u.Email == normalized && (excludeId == null || u.Id != excludeId),
            cancellationToken);
    }

    public async Task<Usuario> AddAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        await _context.Usuarios.AddAsync(usuario, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return usuario;
    }

    public async Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        _context.Usuarios.Update(usuario);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<int>> GetActiveStaffUserIdsAsync(
        CancellationToken cancellationToken = default)
    {
        var staffCodes = new[]
        {
            Rol.SuperAdminCodigo,
            Rol.OwnerCodigo,
            Rol.AdminCodigo,
        };

        return await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.Activo)
            .Join(_context.Roles, u => u.RolId, r => r.Id, (u, r) => new { u.Id, r.Codigo })
            .Where(x => staffCodes.Contains(x.Codigo))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
    }
}
