using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Abstractions;

/// <summary>
/// Contrato de persistencia de proveedores. La implementación (EF Core +
/// PostgreSQL) vive en Infrastructure (AGENTS.md §25).
/// </summary>
public interface IProveedorRepository
{
    Task<PagedResult<Proveedor>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        bool? activo,
        CancellationToken cancellationToken = default);

    Task<Proveedor?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        string email,
        int? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<Proveedor> AddAsync(Proveedor proveedor, CancellationToken cancellationToken = default);

    Task UpdateAsync(Proveedor proveedor, CancellationToken cancellationToken = default);
}
