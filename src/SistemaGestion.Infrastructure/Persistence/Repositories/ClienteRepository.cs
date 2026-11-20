using Microsoft.EntityFrameworkCore;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Infrastructure.Persistence.Repositories;

/// <summary>
/// Persistencia de clientes con EF Core + PostgreSQL.
/// Lecturas con AsNoTracking (AGENTS.md §26).
/// </summary>
public sealed class ClienteRepository : IClienteRepository
{
    private readonly SistemaGestionDbContext _context;

    public ClienteRepository(SistemaGestionDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<Cliente>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        bool? activo,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Clientes.AsNoTracking();

        if (activo.HasValue)
        {
            query = query.Where(c => c.Activo == activo.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(c =>
                c.Nombre.ToLower().Contains(term)
                || c.Apellido.ToLower().Contains(term)
                || c.Email.ToLower().Contains(term)
                || (c.Documento != null && c.Documento.ToLower().Contains(term)));
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Cliente>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
        };
    }

    public Task<Cliente?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Cliente?> GetByFirebaseUidAsync(
        string firebaseUid,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(firebaseUid))
        {
            return Task.FromResult<Cliente?>(null);
        }

        return _context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.FirebaseUid == firebaseUid && c.Activo, cancellationToken);
    }

    public Task<Cliente?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();

        return _context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Email == normalized && c.Activo, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(
        string email,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();

        return _context.Clientes.AsNoTracking().AnyAsync(
            c => c.Email == normalized && (excludeId == null || c.Id != excludeId),
            cancellationToken);
    }

    public async Task<Cliente> AddAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        await _context.Clientes.AddAsync(cliente, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return cliente;
    }

    public async Task UpdateAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        _context.Clientes.Update(cliente);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
