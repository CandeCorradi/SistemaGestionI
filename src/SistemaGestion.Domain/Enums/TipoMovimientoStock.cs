namespace SistemaGestion.Domain.Enums;

/// <summary>
/// Tipos de movimiento de inventario (AGENTS.md §15, business-rules.md
/// BR-051). El tipo define la dirección del efecto sobre el stock; la
/// cantidad es siempre mayor que cero, salvo <see cref="Ajuste"/> que
/// admite signo (negativo = corrección a la baja).
/// </summary>
public enum TipoMovimientoStock
{
    /// <summary>Entrada de mercadería (compra, recepción). Efecto: +.</summary>
    Ingreso = 0,

    /// <summary>Salida de mercadería (consumo, pérdida). Efecto: −.</summary>
    Egreso = 1,

    /// <summary>Corrección manual del stock (±, según el signo de la cantidad).</summary>
    Ajuste = 2,

    /// <summary>Devolución recibida. Efecto: +.</summary>
    Devolucion = 3,

    /// <summary>Reserva de stock para un pedido. Efecto: −.</summary>
    Reserva = 4,

    /// <summary>Liberación de una reserva previa. Efecto: +.</summary>
    Liberacion = 5,
}
