using Microsoft.EntityFrameworkCore;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Infrastructure.Persistence.Repositories;

/// <summary>
/// Lectura de roles fijos con EF Core + PostgreSQL (BR-001: solo consulta).
/// </summary>
public sealed class RolRepository : IRolRepository
{
    private readonly SistemaGestionDbContext _context;

    public RolRepository(SistemaGestionDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Rol>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        List<Rol> roles = await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);

        return roles;
    }

    public Task<Rol?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
}
