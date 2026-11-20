using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Application.Notificaciones.Dtos;

/// <summary>Fila de notificación (api-spec §18).</summary>
public sealed class NotificationDto
{
    public int Id { get; init; }

    /// <summary>Tipo de evento; serializado como número (api-spec §18).</summary>
    public int Type { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string? EntityType { get; init; }

    public int? EntityId { get; init; }

    /// <summary>Estado de lectura (BR-091).</summary>
    public bool Read { get; init; }

    /// <summary>Momento de creación en UTC (AGENTS.md §17).</summary>
    public DateTime CreatedAt { get; init; }

    public static NotificationDto From(Notificacion n) => new()
    {
        Id = n.Id,
        Type = (int)n.Tipo,
        Title = n.Titulo,
        Message = n.Mensaje,
        EntityType = n.EntidadTipo,
        EntityId = n.EntidadId,
        Read = n.Leida,
        CreatedAt = n.FechaCreacionUtc,
    };
}
