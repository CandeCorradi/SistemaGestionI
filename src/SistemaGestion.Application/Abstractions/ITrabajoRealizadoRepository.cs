using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Abstractions;

/// <summary>
/// Contrato de persistencia de trabajos realizados / vidriera
/// (AGENTS.md §23). La implementación (EF Core + PostgreSQL) vive en
/// Infrastructure.
/// </summary>
public interface ITrabajoRealizadoRepository
{
    /// <summary>Listado administrativo paginado con filtros.</summary>
    Task<PagedResult<TrabajoRealizado>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        bool? publicado,
        bool? destacado,
        string? categoria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Vidriera pública paginada: sólo trabajos publicados. Orden:
    /// destacados primero, luego fecha descendente y id descendente.
    /// </summary>
    Task<PagedResult<TrabajoRealizado>> GetShowroomPagedAsync(
        int page,
        int pageSize,
        string? search,
        string? categoria,
        string? tipoMueble,
        CancellationToken cancellationToken = default);

    Task<TrabajoRealizado?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Detalle público por id; devuelve null si no está publicado.</summary>
    Task<TrabajoRealizado?> GetPublishedByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<TrabajoRealizado> AddAsync(TrabajoRealizado trabajo, CancellationToken cancellationToken = default);

    Task UpdateAsync(TrabajoRealizado trabajo, CancellationToken cancellationToken = default);

    Task DeleteAsync(TrabajoRealizado trabajo, CancellationToken cancellationToken = default);
}
