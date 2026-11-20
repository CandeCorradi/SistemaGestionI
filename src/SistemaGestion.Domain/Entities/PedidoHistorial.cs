using SistemaGestion.Domain.Enums;

namespace SistemaGestion.Domain.Entities;

/// <summary>
/// Registro inmutable del historial de estados de un pedido (BR-075,
/// functional-spec §16): estado anterior y nuevo, fecha UTC, usuario
/// responsable y comentario. La creación del pedido se registra con
/// <c>EstadoAnterior = null</c>. Los registros nunca se modifican ni
/// eliminan.
/// </summary>
public class PedidoHistorial : EntityBase
{
    public int PedidoId { get; private set; }

    /// <summary>Estado previo; null cuando el registro es la creación.</summary>
    public EstadoPedido? EstadoAnterior { get; private set; }

    public EstadoPedido EstadoNuevo { get; private set; }

    public DateTime FechaUtc { get; private set; }

    /// <summary>Usuario interno que ejecutó el cambio; null si es el sistema.</summary>
    public int? UsuarioId { get; private set; }

    public string? Comentario { get; private set; }

    public Pedido? Pedido { get; private set; }

    public Usuario? Usuario { get; private set; }

    private PedidoHistorial()
    {
    }

    public static PedidoHistorial Crear(
        int pedidoId,
        EstadoPedido? estadoAnterior,
        EstadoPedido estadoNuevo,
        DateTime fechaUtc,
        int? usuarioId = null,
        string? comentario = null)
    {
        if (pedidoId <= 0)
        {
            throw new ArgumentException("El pedido es obligatorio.", nameof(pedidoId));
        }

        if (fechaUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "La fecha del historial debe estar en UTC (AGENTS.md §17).",
                nameof(fechaUtc));
        }

        if (estadoAnterior == estadoNuevo)
        {
            throw new ArgumentException(
                "El estado anterior y el nuevo deben ser distintos.",
                nameof(estadoNuevo));
        }

        if (usuarioId is <= 0)
        {
            throw new ArgumentException("El usuario no es válido.", nameof(usuarioId));
        }

        return new PedidoHistorial
        {
            PedidoId = pedidoId,
            EstadoAnterior = estadoAnterior,
            EstadoNuevo = estadoNuevo,
            FechaUtc = fechaUtc,
            UsuarioId = usuarioId,
            Comentario = NormalizarComentario(comentario),
        };
    }

    private static string? NormalizarComentario(string? comentario)
    {
        if (string.IsNullOrWhiteSpace(comentario))
        {
            return null;
        }

        var normalizado = comentario.Trim();
        return normalizado.Length > 500 ? normalizado[..500] : normalizado;
    }
}
