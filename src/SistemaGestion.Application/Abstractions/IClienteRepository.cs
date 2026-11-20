using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Abstractions;

/// <summary>
/// Contrato de persistencia de clientes. La implementación (EF Core +
/// PostgreSQL) vive en Infrastructure (AGENTS.md §25).
/// </summary>
public interface IClienteRepository
{
    Task<PagedResult<Cliente>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        bool? activo,
        CancellationToken cancellationToken = default);

    Task<Cliente?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cliente activo vinculado a una cuenta del portal (BR-010). null si
    /// no existe o está desactivado.
    /// </summary>
    Task<Cliente?> GetByFirebaseUidAsync(
        string firebaseUid,
        CancellationToken cancellationToken = default);

    /// <summary>Cliente activo con el email normalizado (para vincular cuentas, BR-010).</summary>
    Task<Cliente?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        string email,
        int? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<Cliente> AddAsync(Cliente cliente, CancellationToken cancellationToken = default);

    Task UpdateAsync(Cliente cliente, CancellationToken cancellationToken = default);
}
