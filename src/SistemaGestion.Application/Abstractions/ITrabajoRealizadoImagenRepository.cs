using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Application.Abstractions;

/// <summary>
/// Contrato de persistencia de imágenes de trabajos realizados: sólo
/// referencias al storage externo (BR-101).
/// </summary>
public interface ITrabajoRealizadoImagenRepository
{
    Task<IReadOnlyList<TrabajoRealizadoImagen>> GetByTrabajoAsync(
        int trabajoRealizadoId,
        CancellationToken cancellationToken = default);

    /// <summary>Carga por lote para listados (evita N+1, AGENTS.md §62).</summary>
    Task<IReadOnlyList<TrabajoRealizadoImagen>> GetByTrabajoIdsAsync(
        IReadOnlyCollection<int> trabajoIds,
        CancellationToken cancellationToken = default);

    Task<TrabajoRealizadoImagen?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CountByTrabajoAsync(int trabajoRealizadoId, CancellationToken cancellationToken = default);

    Task<TrabajoRealizadoImagen> AddAsync(
        TrabajoRealizadoImagen imagen,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(TrabajoRealizadoImagen imagen, CancellationToken cancellationToken = default);

    Task DeleteByTrabajoAsync(int trabajoRealizadoId, CancellationToken cancellationToken = default);
}
