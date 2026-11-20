using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Application.Abstractions;

/// <summary>
/// Contrato de persistencia de permisos (BR-002). La implementación
/// (EF Core + PostgreSQL) vive en Infrastructure (AGENTS.md §25).
/// </summary>
public interface IPermisoRepository
{
    /// <summary>
    /// Catálogo completo de permisos ordenado por Id.
    /// </summary>
    Task<IReadOnlyList<Permiso>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Permisos explícitos (adicionales al rol) de un usuario, ordenados
    /// por el Id del permiso.
    /// </summary>
    Task<IReadOnlyList<string>> GetExplicitCodesAsync(
        int usuarioId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Permisos efectivos de un usuario interno (unión de permisos del rol
    /// y permisos explícitos) identificado por su Firebase UID.
    /// Devuelve <c>null</c> si no existe un usuario interno activo con ese
    /// UID: en ese caso la autorización debe denegar (AGENTS.md §8).
    /// </summary>
    Task<IReadOnlyList<string>?> GetEffectiveCodesByFirebaseUidAsync(
        string firebaseUid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reemplaza por completo el conjunto de permisos explícitos del usuario.
    /// </summary>
    Task ReplaceExplicitAsync(
        int usuarioId,
        IReadOnlyCollection<int> permisoIds,
        CancellationToken cancellationToken = default);
}
