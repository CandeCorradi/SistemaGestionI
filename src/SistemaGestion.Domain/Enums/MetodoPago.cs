namespace SistemaGestion.Domain.Enums;

/// <summary>
/// Métodos de pago aceptados por la carpintería (functional-spec §17,
/// api-spec §17). Catálogo cerrado: las correcciones no son edición del
/// método sino anulación (BR-082).
/// </summary>
public enum MetodoPago
{
    /// <summary>Pago en efectivo.</summary>
    Efectivo = 0,

    /// <summary>Transferencia bancaria.</summary>
    Transferencia = 1,

    /// <summary>Tarjeta de crédito/débito.</summary>
    Tarjeta = 2,

    /// <summary>Cheque.</summary>
    Cheque = 3,

    /// <summary>Otro medio (detallar en notas/referencia).</summary>
    Otro = 4,
}
