using SistemaGestion.Domain.Enums;

namespace SistemaGestion.Application.Abstractions;

/// <summary>
/// Borrador de notificación a publicar por un evento de negocio (BR-092).
/// El destino se resuelve así: <see cref="ClienteId"/> envía al usuario
/// vinculado del cliente (via Firebase UID); <see cref="ParaEquipo"/> envía
/// a los usuarios activos con rol SuperAdmin/Owner/Admin.
/// </summary>
public sealed record NotificacionDraft(
    TipoNotificacion Tipo,
    string Titulo,
    string Mensaje,
    int? ClienteId = null,
    bool ParaEquipo = false,
    string? EntidadTipo = null,
    int? EntidadId = null);

/// <summary>
/// Publicación best-effort de notificaciones (BR-092): una falla al
/// generarlas NUNCA debe romper la operación de negocio que las origina.
/// </summary>
public interface INotificacionPublisher
{
    Task PublicarAsync(NotificacionDraft draft, CancellationToken cancellationToken = default);
}
