using SistemaGestion.Application.Facturacion.Abstractions;

namespace SistemaGestion.Application.Facturacion;

/// <summary>
/// Emisión documental simulada (BR-130): genera el número interno
/// <c>SIM-######</c> a partir del Id de la factura. El prefijo SIM y el
/// flag <see cref="IsSimulated"/> dejan explícito que el documento no es
/// un comprobante fiscal autorizado. El día que exista integración con
/// ARCA se agrega otra implementación de <see cref="IBillingService"/>
/// (BR-131) sin tocar este modelo ni los casos de uso.
/// </summary>
public sealed class SimulatedBillingService : IBillingService
{
    /// <summary>Prefijo del número interno simulado.</summary>
    public const string Prefix = "SIM";

    public bool IsSimulated => true;

    public string GenerateNumber(int invoiceId)
    {
        if (invoiceId <= 0)
        {
            throw new ArgumentException(
                "El Id de la factura es obligatorio para numerar el documento.",
                nameof(invoiceId));
        }

        return $"{Prefix}-{invoiceId:D6}";
    }
}
