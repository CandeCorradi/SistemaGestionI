using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.IntegrationTests.Fakes;

/// <summary>
/// INotificacionRepository falso: sólo devuelve el conteo seteable.
/// </summary>
public sealed class FakeNotificacionRepository : INotificacionRepository
{
    public int Unread { get; set; }

    public Task<PagedResult<Notificacion>> GetPagedByUsuarioAsync(
        int usuarioId, int page, int pageSize, bool soloNoLeidas,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<Notificacion?> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
        => Task.FromResult<Notificacion?>(null);

    public Task<int> CountUnreadAsync(
        int usuarioId, CancellationToken cancellationToken = default)
        => Task.FromResult(Unread);

    public Task AddRangeAsync(
        IReadOnlyCollection<Notificacion> notificaciones,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<int> MarkAllReadAsync(
        int usuarioId, CancellationToken cancellationToken = default)
        => Task.FromResult(0);
}
