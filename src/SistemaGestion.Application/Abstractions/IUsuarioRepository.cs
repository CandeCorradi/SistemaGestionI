using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Abstractions;

/// <summary>
/// Contrato de persistencia de usuarios. La implementación (EF Core +
/// PostgreSQL) vive en Infrastructure (AGENTS.md §25).
/// </summary>
public interface IUsuarioRepository
{
    Task<PagedResult<Usuario>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default);

    Task<Usuario?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Usuario interno por email normalizado (minúsculas), sin filtrar por
    /// estado. Lo usa el aprovisionamiento de cuentas externas (Google) para
    /// vincular el Firebase UID a una cuenta existente sin duplicarla.
    /// </summary>
    Task<Usuario?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Usuario interno activo vinculado a un Firebase UID (perfil y
    /// autorización). <c>null</c> si no existe o está desactivado.
    /// </summary>
    Task<Usuario?> GetByFirebaseUidAsync(
        string firebaseUid,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        string email,
        int? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<Usuario> AddAsync(Usuario usuario, CancellationToken cancellationToken = default);

    Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ids de los usuarios activos con rol de equipo (SuperAdmin/Owner/
    /// Admin) para eventos de negocio dirigidos al staff (BR-092).
    /// </summary>
    Task<IReadOnlyList<int>> GetActiveStaffUserIdsAsync(
        CancellationToken cancellationToken = default);
}
