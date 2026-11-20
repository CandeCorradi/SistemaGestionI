using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Application.Abstractions;

/// <summary>
/// Lectura de los roles fijos del sistema (BR-001: no hay creación).
/// </summary>
public interface IRolRepository
{
    Task<IReadOnlyList<Rol>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Rol?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
