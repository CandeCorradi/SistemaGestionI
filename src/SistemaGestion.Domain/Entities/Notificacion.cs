using SistemaGestion.Domain.Enums;

namespace SistemaGestion.Domain.Entities;

/// <summary>
/// Notificación de negocio dirigida a un usuario interno (BR-090:
/// pertenece a un usuario; BR-091: estado Leída/No leída). Generada por
/// eventos de negocio de forma best-effort: nunca forma parte de la
/// transacción crítica de su evento de origen.
/// </summary>
public class Notificacion : EntityBase
{
    public const int MaxTituloLength = 120;

    public const int MaxMensajeLength = 500;

    public const int MaxEntidadTipoLength = 40;

    public int UsuarioId { get; private set; }

    public TipoNotificacion Tipo { get; private set; }

    public string Titulo { get; private set; } = string.Empty;

    public string Mensaje { get; private set; } = string.Empty;

    /// <summary>Tipo de entidad referenciada (p. ej. "Pedido").</summary>
    public string? EntidadTipo { get; private set; }

    /// <summary>Id de la entidad referenciada cuando existe.</summary>
    public int? EntidadId { get; private set; }

    /// <summary>Estado de lectura (BR-091).</summary>
    public bool Leida { get; private set; }

    public DateTime FechaCreacionUtc { get; private set; }

    public Usuario? Usuario { get; private set; }

    private Notificacion()
    {
    }

    public static Notificacion Crear(
        int usuarioId,
        TipoNotificacion tipo,
        string titulo,
        string mensaje,
        string? entidadTipo = null,
        int? entidadId = null,
        DateTime? fechaCreacionUtc = null)
    {
        if (usuarioId <= 0)
        {
            throw new ArgumentException("El usuario es obligatorio (BR-090).", nameof(usuarioId));
        }

        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentException(
                $"El tipo de notificación '{(int)tipo}' no es válido.", nameof(tipo));
        }

        titulo = (titulo ?? string.Empty).Trim();
        if (titulo.Length == 0)
        {
            throw new ArgumentException("El título es obligatorio.", nameof(titulo));
        }

        if (titulo.Length > MaxTituloLength)
        {
            throw new ArgumentException(
                $"El título no puede superar {MaxTituloLength} caracteres.", nameof(titulo));
        }

        mensaje = (mensaje ?? string.Empty).Trim();
        if (mensaje.Length == 0)
        {
            throw new ArgumentException("El mensaje es obligatorio.", nameof(mensaje));
        }

        if (mensaje.Length > MaxMensajeLength)
        {
            throw new ArgumentException(
                $"El mensaje no puede superar {MaxMensajeLength} caracteres.", nameof(mensaje));
        }

        var tipoEntidad = string.IsNullOrWhiteSpace(entidadTipo) ? null : entidadTipo.Trim();
        if (tipoEntidad?.Length > MaxEntidadTipoLength)
        {
            throw new ArgumentException(
                $"El tipo de entidad no puede superar {MaxEntidadTipoLength} caracteres.",
                nameof(entidadTipo));
        }

        if (entidadId is <= 0)
        {
            throw new ArgumentException("La entidad referenciada no es válida.", nameof(entidadId));
        }

        var fecha = fechaCreacionUtc ?? DateTime.UtcNow;
        if (fecha.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "La fecha de creación debe estar en UTC (AGENTS.md §17).",
                nameof(fechaCreacionUtc));
        }

        return new Notificacion
        {
            UsuarioId = usuarioId,
            Tipo = tipo,
            Titulo = titulo,
            Mensaje = mensaje,
            EntidadTipo = tipoEntidad,
            EntidadId = entidadId,
            Leida = false,
            FechaCreacionUtc = fecha,
        };
    }

    /// <summary>Marca la notificación como leída (BR-091). Idempotente.</summary>
    public void MarcarLeida()
    {
        Leida = true;
    }
}
