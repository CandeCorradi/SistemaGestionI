using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.IntegrationTests.Fakes;

/// <summary>
/// Fake en memoria de <see cref="IClienteRepository"/> para tests de API.
/// </summary>
public sealed class FakeClienteRepository : IClienteRepository
{
    private readonly List<Cliente> _clientes = new();

    public IReadOnlyList<Cliente> All => _clientes;

    public Cliente Seed(
        string email,
        string nombre = "Cliente",
        string apellido = "Test",
        string? firebaseUid = null)
    {
        var cliente = Cliente.Crear(nombre, apellido, email, firebaseUid: firebaseUid);
        cliente.Id = _clientes.Count == 0 ? 1 : _clientes.Max(c => c.Id) + 1;
        _clientes.Add(cliente);
        return cliente;
    }

    public Task<PagedResult<Cliente>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        bool? activo,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Cliente> query = _clientes;

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

        var list = query.OrderBy(c => c.Id).ToList();

        return Task.FromResult(new PagedResult<Cliente>
        {
            Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = list.Count,
        });
    }

    public Task<Cliente?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_clientes.FirstOrDefault(c => c.Id == id));

    public Task<Cliente?> GetByFirebaseUidAsync(
        string firebaseUid,
        CancellationToken cancellationToken = default)
        => Task.FromResult(string.IsNullOrWhiteSpace(firebaseUid)
            ? null
            : _clientes.FirstOrDefault(c => c.FirebaseUid == firebaseUid && c.Activo));

    public Task<Cliente?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return Task.FromResult(_clientes.FirstOrDefault(c => c.Email == normalized && c.Activo));
    }

    public Task<bool> EmailExistsAsync(
        string email,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return Task.FromResult(_clientes.Any(c =>
            c.Email == normalized && (excludeId == null || c.Id != excludeId)));
    }

    public Task<Cliente> AddAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        cliente.Id = _clientes.Count == 0 ? 1 : _clientes.Max(c => c.Id) + 1;
        _clientes.Add(cliente);
        return Task.FromResult(cliente);
    }

    public Task UpdateAsync(Cliente cliente, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
