namespace SistemaGestion.Domain.Enums;

/// <summary>
/// Estado de una factura interna (functional-spec §20, BR-130). Sólo
/// <see cref="Emitida"/> se genera hoy; <see cref="Anulada"/> queda
/// reservada para el mecanismo de anulación que se implementará junto con
/// un permiso FACTURAS_ANULATE específico (BR-160: preferir baja lógica
/// antes que borrado en documentos financieros).
/// </summary>
public enum EstadoFactura
{
    /// <summary>Factura emitida y vigente.</summary>
    Emitida = 0,

    /// <summary>Factura anulada (reservado; sin endpoint todavía).</summary>
    Anulada = 1,
}
