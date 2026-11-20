using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Application.Abstractions;

/// <summary>
/// Contrato de persistencia de imágenes de producto: sólo referencias al
/// storage externo (BR-101).
/// </summary>
public interface IProductoImagenRepository
{
    Task<IReadOnlyList<ProductoImagen>> GetByProductoAsync(
        int productoId,
        CancellationToken cancellationToken = default);

    /// <summary>Carga por lote para listados (evita N+1, AGENTS.md §62).</summary>
    Task<IReadOnlyList<ProductoImagen>> GetByProductoIdsAsync(
        IReadOnlyCollection<int> productoIds,
        CancellationToken cancellationToken = default);

    Task<ProductoImagen?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CountByProductoAsync(int productoId, CancellationToken cancellationToken = default);

    Task<ProductoImagen> AddAsync(ProductoImagen imagen, CancellationToken cancellationToken = default);

    Task DeleteAsync(ProductoImagen imagen, CancellationToken cancellationToken = default);
}
