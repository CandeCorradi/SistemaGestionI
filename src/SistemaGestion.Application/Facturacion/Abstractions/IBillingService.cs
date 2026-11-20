namespace SistemaGestion.Application.Facturacion.Abstractions;

/// <summary>
/// Aislamiento de la emisión documental (BR-131, roadmap Phase 14). La
/// primera implementación es simulada (BR-130); una futura integración
/// con ARCA se agrega como otra implementación sin reescribir el modelo
/// de dominio ni los casos de uso de facturación.
/// </summary>
public interface IBillingService
{
    /// <summary>
    /// Indica si los documentos emitidos son simulados y no representan
    /// un comprobante fiscal autorizado (BR-130).
    /// </summary>
    bool IsSimulated { get; }

    /// <summary>
    /// Genera el número interno del documento para una factura recién
    /// persistida (ya tiene Id asignado).
    /// </summary>
    string GenerateNumber(int invoiceId);
}
