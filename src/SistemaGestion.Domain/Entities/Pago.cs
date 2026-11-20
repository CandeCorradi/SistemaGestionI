using SistemaGestion.Domain.Enums;

namespace SistemaGestion.Domain.Entities;

/// <summary>
/// Pago registrado contra un pedido (functional-spec §17, BR-080..BR-083).
/// El registro es inmutable: no se elimina ni se edita el monto para
/// preservar la integridad financiera (BR-082); las correcciones futuras
/// serán anulaciones (permiso PAGOS_ANULATE).
/// </summary>
public class Pago : EntityBase
{
    /// <summary>Largo máximo de la referencia externa (ej. TRX-12345).</summary>
    public const int MaxReferenciaLength = 100;

    /// <summary>Largo máximo de las notas del pago.</summary>
    public const int MaxNotasLength = 500;

    public int PedidoId { get; private set; }

    /// <summary>Monto en moneda local; siempre mayor a cero (BR-080).</summary>
    public decimal Monto { get; private set; }

    /// <summary>Momento del registro en UTC (AGENTS.md §17).</summary>
    public DateTime FechaPago { get; private set; }

    public MetodoPago Metodo { get; private set; }

    public string? Referencia { get; private set; }

    public string? Notas { get; private set; }

    /// <summary>Usuario interno que registró el pago (auditoría §18).</summary>
    public int? UsuarioId { get; private set; }

    public Pedido? Pedido { get; private set; }

    public Usuario? Usuario { get; private set; }

    private Pago()
    {
    }

    /// <summary>
    /// Registra un pago contra un pedido. Valida BR-080 (monto positivo y
    /// con exactitud monetaria) y los límites de longitud; la validación
    /// de saldo (BR-081) la hace la aplicación contra la base.
    /// </summary>
    public static Pago Crear(
        int pedidoId,
        decimal monto,
        DateTime fechaUtc,
        MetodoPago metodo,
        string? referencia,
        string? notas,
        int? usuarioId)
    {
        if (pedidoId <= 0)
        {
            throw new ArgumentException("El pedido es obligatorio.", nameof(pedidoId));
        }

        if (monto <= 0)
        {
            throw new ArgumentException(
                "El monto del pago debe ser mayor a cero (BR-080).", nameof(monto));
        }

        if (decimal.Round(monto, 2, MidpointRounding.AwayFromZero) != monto)
        {
            throw new ArgumentException(
                "El monto del pago admite como máximo 2 decimales (AGENTS.md §16).",
                nameof(monto));
        }

        if (fechaUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "La fecha del pago debe estar en UTC (AGENTS.md §17).", nameof(fechaUtc));
        }

        if (!Enum.IsDefined(metodo))
        {
            throw new ArgumentException(
                $"El método de pago '{(int)metodo}' no es válido.", nameof(metodo));
        }

        if (usuarioId is <= 0)
        {
            throw new ArgumentException(
                "El usuario que registra el pago no es válido.", nameof(usuarioId));
        }

        var referenciaNormalizada = string.IsNullOrWhiteSpace(referencia)
            ? null
            : referencia.Trim();
        if (referenciaNormalizada?.Length > MaxReferenciaLength)
        {
            throw new ArgumentException(
                $"La referencia no puede superar {MaxReferenciaLength} caracteres.",
                nameof(referencia));
        }

        var notasNormalizadas = string.IsNullOrWhiteSpace(notas) ? null : notas.Trim();
        if (notasNormalizadas?.Length > MaxNotasLength)
        {
            throw new ArgumentException(
                $"Las notas no pueden superar {MaxNotasLength} caracteres.",
                nameof(notas));
        }

        return new Pago
        {
            PedidoId = pedidoId,
            Monto = monto,
            FechaPago = fechaUtc,
            Metodo = metodo,
            Referencia = referenciaNormalizada,
            Notas = notasNormalizadas,
            UsuarioId = usuarioId,
        };
    }
}
