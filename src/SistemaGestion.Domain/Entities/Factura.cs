using SistemaGestion.Domain.Enums;

namespace SistemaGestion.Domain.Entities;

/// <summary>
/// Factura interna emitida contra un pedido (functional-spec §20,
/// BR-130/BR-131). La primera versión es simulada: el documento se marca
/// como tal y el número es interno (SIM-######); una futura integración
/// con ARCA se resuelve detrás de <c>IBillingService</c> sin reescribir
/// este modelo. El total y el nombre del cliente se congelan al emitir
/// para preservar el histórico comercial (BR-141, functional-spec §20).
/// </summary>
public class Factura : EntityBase
{
    /// <summary>Largo máximo del número interno del documento.</summary>
    public const int MaxNumeroLength = 30;

    /// <summary>Largo máximo del nombre de cliente congelado.</summary>
    public const int MaxClienteNombreLength = 300;

    /// <summary>Largo máximo de las notas de la factura.</summary>
    public const int MaxNotasLength = 500;

    public int PedidoId { get; private set; }

    /// <summary>Cliente congelado al emitir (snapshot histórico).</summary>
    public int ClienteId { get; private set; }

    /// <summary>Nombre del cliente congelado al emitir (BR-141).</summary>
    public string ClienteNombre { get; private set; } = string.Empty;

    /// <summary>
    /// Número interno del documento; vacío hasta que se emite
    /// (se asigna con <see cref="AsignarDocumento"/>).
    /// </summary>
    public string Numero { get; private set; } = string.Empty;

    /// <summary>Momento de emisión en UTC (AGENTS.md §17).</summary>
    public DateTime FechaEmision { get; private set; }

    /// <summary>
    /// Total congelado del pedido al emitir; dinero con exactitud de
    /// 2 decimales (AGENTS.md §16).
    /// </summary>
    public decimal Total { get; private set; }

    /// <summary>
    /// <c>true</c> mientras la emisión sea simulada: el documento NO es
    /// un comprobante fiscal autorizado (BR-130).
    /// </summary>
    public bool EsSimulada { get; private set; } = true;

    public EstadoFactura Estado { get; private set; } = EstadoFactura.Emitida;

    public string? Notas { get; private set; }

    /// <summary>Usuario interno que emitió la factura (auditoría §18).</summary>
    public int? UsuarioId { get; private set; }

    public Pedido? Pedido { get; private set; }

    public Cliente? Cliente { get; private set; }

    public Usuario? Usuario { get; private set; }

    private Factura()
    {
    }

    /// <summary>
    /// Crea una factura pendiente de número contra un pedido, congelando
    /// los datos comerciales del momento (functional-spec §20). Valida
    /// money/UTC/límites; la numeración se asigna después con
    /// <see cref="AsignarDocumento"/> una vez que la fila tiene Id.
    /// </summary>
    public static Factura Crear(
        int pedidoId,
        int clienteId,
        string clienteNombre,
        decimal total,
        DateTime fechaEmisionUtc,
        int? usuarioId,
        string? notas)
    {
        if (pedidoId <= 0)
        {
            throw new ArgumentException("El pedido es obligatorio.", nameof(pedidoId));
        }

        if (clienteId <= 0)
        {
            throw new ArgumentException("El cliente es obligatorio.", nameof(clienteId));
        }

        var nombreNormalizado = string.IsNullOrWhiteSpace(clienteNombre)
            ? null
            : clienteNombre.Trim();
        if (nombreNormalizado is null)
        {
            throw new ArgumentException(
                "El nombre del cliente es obligatorio.", nameof(clienteNombre));
        }

        if (nombreNormalizado.Length > MaxClienteNombreLength)
        {
            throw new ArgumentException(
                $"El nombre del cliente no puede superar {MaxClienteNombreLength} caracteres.",
                nameof(clienteNombre));
        }

        if (total <= 0)
        {
            throw new ArgumentException(
                "El total de la factura debe ser mayor a cero.", nameof(total));
        }

        if (decimal.Round(total, 2, MidpointRounding.AwayFromZero) != total)
        {
            throw new ArgumentException(
                "El total de la factura admite como máximo 2 decimales (AGENTS.md §16).",
                nameof(total));
        }

        if (fechaEmisionUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "La fecha de emisión debe estar en UTC (AGENTS.md §17).",
                nameof(fechaEmisionUtc));
        }

        if (usuarioId is <= 0)
        {
            throw new ArgumentException(
                "El usuario que emite la factura no es válido.", nameof(usuarioId));
        }

        var notasNormalizadas = string.IsNullOrWhiteSpace(notas) ? null : notas.Trim();
        if (notasNormalizadas?.Length > MaxNotasLength)
        {
            throw new ArgumentException(
                $"Las notas no pueden superar {MaxNotasLength} caracteres.",
                nameof(notas));
        }

        return new Factura
        {
            PedidoId = pedidoId,
            ClienteId = clienteId,
            ClienteNombre = nombreNormalizado,
            Total = total,
            FechaEmision = fechaEmisionUtc,
            UsuarioId = usuarioId,
            Notas = notasNormalizadas,
            Estado = EstadoFactura.Emitida,
            EsSimulada = true,
        };
    }

    /// <summary>
    /// Asigna el documento emitido por <c>IBillingService</c> (número y
    /// condición de simulado, BR-130). Sólo puede hacerse una vez: el
    /// número identifica el documento histórico de forma permanente.
    /// </summary>
    public void AsignarDocumento(string numero, bool esSimulada)
    {
        if (Numero.Length > 0)
        {
            throw new InvalidOperationException(
                "La factura ya tiene número de documento asignado.");
        }

        var numeroNormalizado = string.IsNullOrWhiteSpace(numero)
            ? null
            : numero.Trim();
        if (numeroNormalizado is null)
        {
            throw new ArgumentException(
                "El número de documento es obligatorio.", nameof(numero));
        }

        if (numeroNormalizado.Length > MaxNumeroLength)
        {
            throw new ArgumentException(
                $"El número de documento no puede superar {MaxNumeroLength} caracteres.",
                nameof(numero));
        }

        Numero = numeroNormalizado;
        EsSimulada = esSimulada;
    }
}
