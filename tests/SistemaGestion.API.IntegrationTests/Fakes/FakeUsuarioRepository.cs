using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.IntegrationTests.Fakes;

/// <summary>
/// Fake en memoria de <see cref="IUsuarioRepository"/> para tests de API.
/// Sólo soporta la búsqueda por Firebase UID que usa inventario.
/// </summary>
public sealed class FakeUsuarioRepository : IUsuarioRepository
{
    private readonly List<Usuario> _usuarios = new();

    public Usuario Seed(string firebaseUid, string nombre, string apellido)
    {
        var usuario = Usuario.Crear(
            $"{firebaseUid}@test.com",
            nombre,
            apellido,
            rolId: 2,
            firebaseUid: firebaseUid);
        usuario.Id = _usuarios.Count == 0 ? 1 : _usuarios.Max(u => u.Id) + 1;
        _usuarios.Add(usuario);
        return usuario;
    }

    public Task<Usuario?> GetByFirebaseUidAsync(string firebaseUid, CancellationToken cancellationToken = default)
        => Task.FromResult(_usuarios.FirstOrDefault(u => u.FirebaseUid == firebaseUid));

    public Usuario? FindById(int id) => _usuarios.FirstOrDefault(u => u.Id == id);

    public Task<PagedResult<Usuario>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new PagedResult<Usuario>
        {
            Items = _usuarios,
            Page = page,
            PageSize = pageSize,
            TotalItems = _usuarios.Count,
        });

    public Task<Usuario?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_usuarios.FirstOrDefault(u => u.Id == id));

    public Task<Usuario?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return Task.FromResult(_usuarios.FirstOrDefault(u => u.Email == normalized));
    }

    public Task<bool> EmailExistsAsync(
        string email,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_usuarios.Any(u =>
            u.Email == email && (excludeId == null || u.Id != excludeId)));

    public Task<Usuario> AddAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        usuario.Id = _usuarios.Count == 0 ? 1 : _usuarios.Max(u => u.Id) + 1;
        _usuarios.Add(usuario);
        return Task.FromResult(usuario);
    }

    public Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<int>> GetActiveStaffUserIdsAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<int>>(Array.Empty<int>());
}
