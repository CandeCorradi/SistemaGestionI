using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Abstractions;

/// <summary>
/// Persistencia de notificaciones (BR-090/BR-091). Las notificaciones son
/// datos propios de cada usuario interno.
/// </summary>
public interface INotificacionRepository
{
    /// <summary>
    /// Listado paginado de las notificaciones de un usuario, más recientes
    /// primero, con filtro opcional de sólo no leídas.
    /// </summary>
    Task<PagedResult<Notificacion>> GetPagedByUsuarioAsync(
        int usuarioId,
        int page,
        int pageSize,
        bool soloNoLeidas,
        CancellationToken cancellationToken = default);

    /// <summary>Notificación por id (tracking para marcar como leída).</summary>
    Task<Notificacion?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Cantidad de notificaciones no leídas del usuario.</summary>
    Task<int> CountUnreadAsync(int usuarioId, CancellationToken cancellationToken = default);

    /// <summary>Registra un lote de notificaciones nuevas.</summary>
    Task AddRangeAsync(
        IReadOnlyCollection<Notificacion> notificaciones,
        CancellationToken cancellationToken = default);

    /// <summary>Persiste cambios pendientes (p. ej. marcar como leída).</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca como leídas todas las notificaciones no leídas del usuario y
    /// devuelve la cantidad afectada.
    /// </summary>
    Task<int> MarkAllReadAsync(int usuarioId, CancellationToken cancellationToken = default);
}
