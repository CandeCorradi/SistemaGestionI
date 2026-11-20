using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.IntegrationTests.Fakes;

/// <summary>
/// Fake en memoria de <see cref="IProveedorRepository"/> para tests de API.
/// </summary>
public sealed class FakeProveedorRepository : IProveedorRepository
{
    private readonly List<Proveedor> _proveedores = new();

    public IReadOnlyList<Proveedor> All => _proveedores;

    public Proveedor Seed(string nombre = "Proveedor SA", string? email = null)
    {
        var proveedor = Proveedor.Crear(nombre, email: email);
        proveedor.Id = _proveedores.Count == 0 ? 1 : _proveedores.Max(p => p.Id) + 1;
        _proveedores.Add(proveedor);
        return proveedor;
    }

    public Task<PagedResult<Proveedor>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        bool? activo,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Proveedor> query = _proveedores;

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

        var list = query.OrderBy(p => p.Id).ToList();

        return Task.FromResult(new PagedResult<Proveedor>
        {
            Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = list.Count,
        });
    }

    public Task<Proveedor?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_proveedores.FirstOrDefault(p => p.Id == id));

    public Task<bool> EmailExistsAsync(
        string email,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return Task.FromResult(_proveedores.Any(p =>
            p.Email == normalized && (excludeId == null || p.Id != excludeId)));
    }

    public Task<Proveedor> AddAsync(Proveedor proveedor, CancellationToken cancellationToken = default)
    {
        proveedor.Id = _proveedores.Count == 0 ? 1 : _proveedores.Max(p => p.Id) + 1;
        _proveedores.Add(proveedor);
        return Task.FromResult(proveedor);
    }

    public Task UpdateAsync(Proveedor proveedor, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
