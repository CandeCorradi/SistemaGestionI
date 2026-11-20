using SistemaGestion.Application.Abstractions;

namespace SistemaGestion.Application.UnitTests;

/// <summary>
/// Fake que captura los borradores de notificación publicados por los
/// servicios de negocio (BR-092) para poder asertarlos en tests.
/// </summary>
public sealed class FakeNotificacionPublisher : INotificacionPublisher
{
    public List<NotificacionDraft> Publicados { get; } = new();

    public Task PublicarAsync(
        NotificacionDraft draft,
        CancellationToken cancellationToken = default)
    {
        Publicados.Add(draft);
        return Task.CompletedTask;
    }
}
