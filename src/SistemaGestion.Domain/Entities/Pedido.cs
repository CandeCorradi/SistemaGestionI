using SistemaGestion.Domain.Enums;

namespace SistemaGestion.Domain.Entities;

/// <summary>
/// Pedido creado al aceptar un presupuesto (BR-065/BR-070). Conserva el
/// snapshot comercial acordado (BR-071): referencia al presupuesto
/// aceptado —inmutable una vez aceptado— y total copiado al momento de la
/// aceptación. El ciclo de vida de estados es BR-072/BR-073 (paso 20).
/// </summary>
public class Pedido : EntityBase
{
    public int ClienteId { get; private set; }

    public int PresupuestoId { get; private set; }

    public DateTime FechaCreacion { get; private set; } = DateTime.UtcNow;

    /// <summary>Total congelado al aceptar el presupuesto (BR-071).</summary>
    public decimal Total { get; private set; }

    public EstadoPedido Estado { get; private set; } = EstadoPedido.Pendiente;

    /// <summary>Inicio de producción (→ EnProduccion, BR-076).</summary>
    public DateTime? FechaInicioProduccion { get; private set; }

    /// <summary>Fin de producción (→ Listo, BR-076).</summary>
    public DateTime? FechaFinProduccion { get; private set; }

    /// <summary>
    /// Entrega estimada acordada; editable mientras el pedido está activo
    /// (BR-076).
    /// </summary>
    public DateTime? FechaEntregaEstimada { get; private set; }

    /// <summary>Entrega real (→ Entregado, BR-076).</summary>
    public DateTime? FechaEntrega { get; private set; }

    /// <summary>Notas operativas; máximo <see cref="MaxNotasLength"/>.</summary>
    public string? Notas { get; private set; }

    /// <summary>Largo máximo de las notas del pedido (BR-076).</summary>
    public const int MaxNotasLength = 2000;

    public Cliente? Cliente { get; private set; }

    public Presupuesto? Presupuesto { get; private set; }

    private Pedido()
    {
    }

    /// <summary>Crea un pedido en estado Pendiente a partir de un presupuesto aceptado.</summary>
    public static Pedido Crear(
        int clienteId,
        int presupuestoId,
        decimal total,
        DateTime fechaCreacionUtc)
    {
        if (clienteId <= 0)
        {
            throw new ArgumentException("El cliente es obligatorio.", nameof(clienteId));
        }

        if (presupuestoId <= 0)
        {
            throw new ArgumentException("El presupuesto es obligatorio.", nameof(presupuestoId));
        }

        if (total < 0)
        {
            throw new ArgumentException("El total no puede ser negativo.", nameof(total));
        }

        if (fechaCreacionUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "La fecha de creación debe estar en UTC (AGENTS.md §17).",
                nameof(fechaCreacionUtc));
        }

        return new Pedido
        {
            ClienteId = clienteId,
            PresupuestoId = presupuestoId,
            Total = total,
            FechaCreacion = fechaCreacionUtc,
            Estado = EstadoPedido.Pendiente,
        };
    }

    // ------------------------------------------------------------------
    // Transiciones de estado (BR-072/BR-073). El historial de cada
    // transición lo registra la capa de aplicación (BR-075).
    // ------------------------------------------------------------------

    /// <summary>Pendiente → Confirmado.</summary>
    public void Confirmar()
    {
        TransicionarDesde(EstadoPedido.Pendiente, EstadoPedido.Confirmado,
            "Sólo los pedidos pendientes pueden confirmarse");
    }

    /// <summary>Confirmado → EnProduccion. Registra el inicio de producción.</summary>
    public void IniciarProduccion(DateTime? fechaUtc = null)
    {
        var inicio = NormalizarMomento(fechaUtc);
        TransicionarDesde(EstadoPedido.Confirmado, EstadoPedido.EnProduccion,
            "Sólo los pedidos confirmados pueden pasar a producción");
        FechaInicioProduccion = inicio;
    }

    /// <summary>EnProduccion → Listo. Registra el fin de producción.</summary>
    public void MarcarListo(DateTime? fechaUtc = null)
    {
        var fin = NormalizarMomento(fechaUtc);
        TransicionarDesde(EstadoPedido.EnProduccion, EstadoPedido.Listo,
            "Sólo los pedidos en producción pueden marcarse como listos");
        FechaFinProduccion = fin;
    }

    /// <summary>Listo → Entregado. Registra la entrega real.</summary>
    public void Entregar(DateTime? fechaUtc = null)
    {
        var entrega = NormalizarMomento(fechaUtc);
        TransicionarDesde(EstadoPedido.Listo, EstadoPedido.Entregado,
            "Sólo los pedidos listos pueden entregarse");
        FechaEntrega = entrega;
    }

    /// <summary>Entregado → Completado (BR-073: cierre del ciclo).</summary>
    public void Completar()
    {
        TransicionarDesde(EstadoPedido.Entregado, EstadoPedido.Completado,
            "Sólo los pedidos entregados pueden completarse");
    }

    /// <summary>
    /// → Cancelado, permitido sólo antes de la entrega (BR-073: la
    /// cancelación ocurre desde estados permitidos; Completado y Cancelado
    /// son estados finales inmutables, BR-074).
    /// </summary>
    public void Cancelar()
    {
        if (Estado is not (EstadoPedido.Pendiente or EstadoPedido.Confirmado
            or EstadoPedido.EnProduccion or EstadoPedido.Listo))
        {
            throw new InvalidOperationException(
                $"Sólo pueden cancelarse pedidos no entregados; estado actual: {Estado} (BR-073).");
        }

        Estado = EstadoPedido.Cancelado;
    }

    /// <summary>
    /// Aplica la transición hacia el estado destino (BR-072). Fuente única
    /// de verdad de las transiciones permitidas; los estados finales
    /// (Completado, Cancelado) no admiten cambios (BR-074). Las transiciones
    /// con marca temporal de seguimiento reciben el momento en UTC
    /// (BR-076, AGENTS.md §17).
    /// </summary>
    public void TransicionarA(EstadoPedido destino, DateTime? fechaUtc = null)
    {
        if (destino == Estado)
        {
            throw new InvalidOperationException(
                $"El pedido ya está en estado {Estado} (BR-072).");
        }

        switch (destino)
        {
            case EstadoPedido.Confirmado:
                Confirmar();
                break;
            case EstadoPedido.EnProduccion:
                IniciarProduccion(fechaUtc);
                break;
            case EstadoPedido.Listo:
                MarcarListo(fechaUtc);
                break;
            case EstadoPedido.Entregado:
                Entregar(fechaUtc);
                break;
            case EstadoPedido.Completado:
                Completar();
                break;
            case EstadoPedido.Cancelado:
                Cancelar();
                break;
            case EstadoPedido.Pendiente:
                throw new InvalidOperationException(
                    $"No se puede volver al estado Pendiente; estado actual: {Estado} (BR-072).");
            default:
                throw new InvalidOperationException(
                    $"Estado destino no válido: {destino} (BR-072).");
        }
    }

    /// <summary>
    /// Actualiza la información de seguimiento permitida del pedido: notas
    /// y entrega estimada (BR-076). Válido sólo para pedidos activos; los
    /// estados finales son inmutables (BR-074).
    /// </summary>
    public void ActualizarInformacion(string? notas, DateTime? fechaEntregaEstimada)
    {
        if (Estado is EstadoPedido.Completado or EstadoPedido.Cancelado)
        {
            throw new InvalidOperationException(
                $"No se puede editar un pedido finalizado; estado actual: {Estado} (BR-074).");
        }

        var notasNormalizadas = string.IsNullOrWhiteSpace(notas) ? null : notas.Trim();
        if (notasNormalizadas?.Length > MaxNotasLength)
        {
            throw new ArgumentException(
                $"Las notas no pueden superar {MaxNotasLength} caracteres.",
                nameof(notas));
        }

        DateTime? entregaEstimadaNormalizada = null;
        if (fechaEntregaEstimada.HasValue)
        {
            var fecha = fechaEntregaEstimada.Value;
            if (fecha.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException(
                    "La fecha de entrega estimada debe estar en UTC (AGENTS.md §17).",
                    nameof(fechaEntregaEstimada));
            }

            if (fecha.Date < FechaCreacion.Date)
            {
                throw new ArgumentException(
                    "La entrega estimada no puede ser anterior a la creación del pedido.",
                    nameof(fechaEntregaEstimada));
            }

            entregaEstimadaNormalizada = fecha;
        }

        Notas = notasNormalizadas;
        FechaEntregaEstimada = entregaEstimadaNormalizada;
    }

    /// <summary>Normaliza un momento de seguimiento a UTC (null = ahora).</summary>
    private static DateTime NormalizarMomento(DateTime? fechaUtc)
    {
        var fecha = fechaUtc ?? DateTime.UtcNow;
        if (fecha.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "El momento de seguimiento debe estar en UTC (AGENTS.md §17).",
                nameof(fechaUtc));
        }

        return fecha;
    }

    private void TransicionarDesde(EstadoPedido origen, EstadoPedido destino, string mensaje)
    {
        if (Estado != origen)
        {
            throw new InvalidOperationException($"{mensaje}; estado actual: {Estado} (BR-072).");
        }

        Estado = destino;
    }
}
