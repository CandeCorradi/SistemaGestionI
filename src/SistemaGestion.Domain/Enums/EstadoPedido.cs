namespace SistemaGestion.Domain.Enums;

/// <summary>
/// Ciclo de vida de un pedido (business-rules.md BR-072/BR-073,
/// functional-spec §15): Pendiente → Confirmado → EnProduccion → Listo →
/// Entregado → Completado, con Cancelado como rama permitida.
/// </summary>
public enum EstadoPedido
{
    Pendiente = 0,
    Confirmado = 1,
    EnProduccion = 2,
    Listo = 3,
    Entregado = 4,
    Cancelado = 5,

    /// <summary>Estado final: pedido entregado y cerrado (BR-073/BR-074).</summary>
    Completado = 6
}
