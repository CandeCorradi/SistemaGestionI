namespace SistemaGestion.Domain.Enums;

/// <summary>
/// Estados de un presupuesto (functional-spec §12, BR-064). Se omiten
/// Pending/Viewed del listado de la spec: en este sistema el presupuesto
/// nace en Borrador y pasa a Enviado cuando se entrega al cliente; la
/// visualización se registrará en el Portal Cliente si se requiere.
/// Expirado se evalúa por FechaValidez al momento de aceptar (BR-064).
/// </summary>
public enum EstadoPresupuesto
{
    /// <summary>Creado, editable internamente.</summary>
    Borrador = 0,

    /// <summary>Entregado al cliente; sólo lectura (editarlo requiere volver a borrador en una futura iteración).</summary>
    Enviado = 1,

    /// <summary>Aceptado por el cliente; da origen a un pedido (BR-065, paso 19).</summary>
    Aceptado = 2,

    /// <summary>Rechazado por el cliente.</summary>
    Rechazado = 3,

    /// <summary>Anulado internamente.</summary>
    Anulado = 4,

    /// <summary>Venció su fecha de validez sin respuesta.</summary>
    Expirado = 5,
}
