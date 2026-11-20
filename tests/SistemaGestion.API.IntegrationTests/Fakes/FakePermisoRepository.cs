using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Authorization;
using SistemaGestion.Domain.Entities;

namespace SistemaGestion.API.IntegrationTests.Fakes;

/// <summary>
/// Fake en memoria de <see cref="IPermisoRepository"/> para tests de API.
/// Permite simular permisos efectivos por Firebase UID y permisos
/// explícitos por usuario.
/// </summary>
public sealed class FakePermisoRepository : IPermisoRepository
{
    private readonly List<Permiso> _catalogo = Permisos.Catalogo
        .Select(p => new Permiso(p.Codigo, p.Nombre) { Id = p.Id })
        .ToList();

    /// <summary>Permisos efectivos por Firebase UID (null si no existe).</summary>
    public Dictionary<string, List<string>> EfectivosPorUid { get; } = new(StringComparer.Ordinal);

    /// <summary>Permisos explícitos por Id de usuario.</summary>
    public Dictionary<int, List<string>> ExplicitosPorUsuario { get; } = new();

    /// <summary>
    /// Crea un repositorio donde <paramref name="uid"/> tiene todos los
    /// permisos del catálogo.
    /// </summary>
    public static FakePermisoRepository ConTodosLosPermisos(string uid)
    {
        var repository = new FakePermisoRepository();
        repository.EfectivosPorUid[uid] = Permisos.Todos.ToList();
        return repository;
    }

    public Task<IReadOnlyList<Permiso>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Permiso>>(_catalogo.OrderBy(p => p.Id).ToList());

    public Task<IReadOnlyList<string>> GetExplicitCodesAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> codigos = ExplicitosPorUsuario.TryGetValue(usuarioId, out var lista)
            ? lista
            : new List<string>();

        return Task.FromResult(codigos);
    }

    public Task<IReadOnlyList<string>?> GetEffectiveCodesByFirebaseUidAsync(
        string firebaseUid,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string>? codigos = EfectivosPorUid.TryGetValue(firebaseUid, out var lista)
            ? lista
            : null;

        return Task.FromResult(codigos);
    }

    public Task ReplaceExplicitAsync(
        int usuarioId,
        IReadOnlyCollection<int> permisoIds,
        CancellationToken cancellationToken = default)
    {
        ExplicitosPorUsuario[usuarioId] = permisoIds
            .OrderBy(id => id)
            .Select(id => _catalogo.First(p => p.Id == id).Codigo)
            .ToList();

        return Task.CompletedTask;
    }
}
