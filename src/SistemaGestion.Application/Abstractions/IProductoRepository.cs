using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Abstractions;

/// <summary>
/// Contrato de persistencia de productos (functional-spec §8). La
/// implementación (EF Core + PostgreSQL) vive en Infrastructure.
/// </summary>
public interface IProductoRepository
{
    Task<PagedResult<Producto>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        TipoProducto? tipo,
        string? categoria,
        bool? activo,
        bool? showroom,
        bool? lowStock,
        bool? isCustom,
        CancellationToken cancellationToken = default);

    Task<Producto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> SkuExistsAsync(string codigoSku, int? excludeId = null, CancellationToken cancellationToken = default);

    Task<Producto> AddAsync(Producto producto, CancellationToken cancellationToken = default);

    Task UpdateAsync(Producto producto, CancellationToken cancellationToken = default);
}
