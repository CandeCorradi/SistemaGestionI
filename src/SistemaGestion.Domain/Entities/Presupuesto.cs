using SistemaGestion.Domain.Enums;

namespace SistemaGestion.Domain.Entities;

/// <summary>
/// Presupuesto comercial (functional-spec §12, BR-060..BR-065).
/// Conserva el snapshot comercial: totales calculados en backend
/// (BR-062), items congelados al emitir (BR-063) y transiciones de
/// estado validadas (BR-064). La aceptación atómica con creación de
/// pedido es BR-065 (paso 19).
/// </summary>
public class Presupuesto : EntityBase
{
    public int ClienteId { get; private set; }

    public DateTime FechaCreacion { get; private set; } = DateTime.UtcNow;

    /// <summary>Fecha (UTC, medianoche) hasta la cual el presupuesto es válido. Null = sin vencimiento.</summary>
    public DateTime? FechaValidez { get; private set; }

    /// <summary>Instante UTC en que se envió al cliente.</summary>
    public DateTime? FechaEnvio { get; private set; }

    public EstadoPresupuesto Estado { get; private set; } = EstadoPresupuesto.Borrador;

    /// <summary>Suma bruta de las líneas antes de descuentos (BR-062).</summary>
    public decimal Subtotal { get; private set; }

    /// <summary>Suma de los descuentos de las líneas (BR-062).</summary>
    public decimal Descuento { get; private set; }

    /// <summary>Total = Subtotal − Descuento; calculado exclusivamente por backend (BR-062).</summary>
    public decimal Total { get; private set; }

    public string? Notas { get; private set; }

    public string? Condiciones { get; private set; }

    /// <summary>Motivo registrado al rechazar o anular.</summary>
    public string? EstadoMotivo { get; private set; }

    public DateTime? FechaModificacion { get; private set; }

    public Cliente? Cliente { get; private set; }

    public ICollection<PresupuestoDetalle> Detalles { get; private set; } = new List<PresupuestoDetalle>();

    private Presupuesto()
    {
    }

    /// <summary>Crea un presupuesto en estado Borrador (BR-060: un cliente exacto).</summary>
    public static Presupuesto Crear(
        int clienteId,
        DateTime? fechaValidezUtc,
        string? notas,
        string? condiciones,
        DateTime fechaCreacionUtc)
    {
        if (clienteId <= 0)
        {
            throw new ArgumentException("El cliente es obligatorio (BR-060).", nameof(clienteId));
        }

        if (fechaCreacionUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("La fecha de creación debe estar en UTC (AGENTS.md §17).", nameof(fechaCreacionUtc));
        }

        return new Presupuesto
        {
            ClienteId = clienteId,
            FechaCreacion = fechaCreacionUtc,
            FechaValidez = NormalizarFechaValidez(fechaValidezUtc),
            Notas = NormalizarTexto(notas, 2000),
            Condiciones = NormalizarTexto(condiciones, 1000),
            Estado = EstadoPresupuesto.Borrador,
        };
    }

    /// <summary>
    /// Reemplaza los datos de cabecera. Sólo permitido en Borrador
    /// (regla de servicio; aquí se validan los valores).
    /// </summary>
    public void Actualizar(
        int clienteId,
        DateTime? fechaValidezUtc,
        string? notas,
        string? condiciones,
        DateTime fechaModificacionUtc)
    {
        if (clienteId <= 0)
        {
            throw new ArgumentException("El cliente es obligatorio (BR-060).", nameof(clienteId));
        }

        ClienteId = clienteId;
        FechaValidez = NormalizarFechaValidez(fechaValidezUtc);
        Notas = NormalizarTexto(notas, 2000);
        Condiciones = NormalizarTexto(condiciones, 1000);
        FechaModificacion = fechaModificacionUtc;
    }

    /// <summary>Quita todas las líneas (la edición reemplaza el snapshot).</summary>
    public void LimpiarDetalles()
    {
        Detalles.Clear();
    }

    /// <summary>Agrega una línea ya validada y recalcula los totales (BR-062).</summary>
    public void AgregarDetalle(PresupuestoDetalle detalle)
    {
        Detalles.Add(detalle);
        RecalcularTotales();
    }

    /// <summary>
    /// Recalcula Subtotal, Descuento y Total a partir de las líneas.
    /// Sólo el backend determina los totales (BR-062).
    /// </summary>
    public void RecalcularTotales()
    {
        decimal subtotalBruto = 0;
        decimal descuentoTotal = 0;

        foreach (var detalle in Detalles)
        {
            subtotalBruto += detalle.Cantidad * detalle.PrecioUnitario;
            descuentoTotal += detalle.Descuento;
        }

        Subtotal = subtotalBruto;
        Descuento = descuentoTotal;
        Total = subtotalBruto - descuentoTotal;
    }

    /// <summary>BR-061: al menos una línea para cualquier presupuesto guardado.</summary>
    public void ValidarAlMenosUnDetalle()
    {
        if (Detalles.Count == 0)
        {
            throw new ArgumentException("El presupuesto debe tener al menos un ítem (BR-061).");
        }
    }

    /// <summary>Borrador → Enviado. Exige ítems (BR-061).</summary>
    public void Enviar(DateTime fechaEnvioUtc)
    {
        if (Estado != EstadoPresupuesto.Borrador)
        {
            throw new InvalidOperationException(
                $"Sólo los presupuestos en borrador pueden enviarse; estado actual: {Estado}.");
        }

        ValidarAlMenosUnDetalle();

        Estado = EstadoPresupuesto.Enviado;
        FechaEnvio = fechaEnvioUtc;
    }

    /// <summary>Enviado → Rechazado (BR-064: no era válido aceptarlo; rechazar sí).</summary>
    public void Rechazar(string? motivo)
    {
        if (Estado != EstadoPresupuesto.Enviado)
        {
            throw new InvalidOperationException(
                $"Sólo los presupuestos enviados pueden rechazarse; estado actual: {Estado}.");
        }

        Estado = EstadoPresupuesto.Rechazado;
        EstadoMotivo = NormalizarTexto(motivo, 500);
    }

    /// <summary>Borrador/Enviado → Anulado.</summary>
    public void Anular(string? motivo)
    {
        if (Estado != EstadoPresupuesto.Borrador && Estado != EstadoPresupuesto.Enviado)
        {
            throw new InvalidOperationException(
                $"Sólo pueden anularse presupuestos en borrador o enviados; estado actual: {Estado}.");
        }

        Estado = EstadoPresupuesto.Anulado;
        EstadoMotivo = NormalizarTexto(motivo, 500);
    }

    /// <summary>
    /// Enviado → Aceptado (BR-064): sólo si sigue vigente (no expirado,
    /// no rechazado/anulado/aceptado). La creación atómica del pedido es
    /// responsabilidad del servicio (BR-065).
    /// </summary>
    public void Aceptar(DateTime fechaHoyUtc)
    {
        if (Estado != EstadoPresupuesto.Enviado)
        {
            throw new InvalidOperationException(
                $"Sólo los presupuestos enviados pueden aceptarse; estado actual: {Estado}.");
        }

        if (EstaExpirado(fechaHoyUtc))
        {
            throw new InvalidOperationException(
                "El presupuesto está vencido y no puede aceptarse (BR-064).");
        }

        Estado = EstadoPresupuesto.Aceptado;
    }

    /// <summary>¿Venció la fecha de validez respecto al día indicado (UTC)?</summary>
    public bool EstaExpirado(DateTime fechaHoyUtc)
        => FechaValidez.HasValue && FechaValidez.Value.Date < fechaHoyUtc.Date
            && (Estado == EstadoPresupuesto.Borrador || Estado == EstadoPresupuesto.Enviado);

    private static DateTime? NormalizarFechaValidez(DateTime? fecha)
    {
        if (!fecha.HasValue)
        {
            return null;
        }

        if (fecha.Value.Kind == DateTimeKind.Local)
        {
            fecha = fecha.Value.ToUniversalTime();
        }

        // Se guarda como fecha (medianoche UTC): sólo importa el día.
        return DateTime.SpecifyKind(fecha.Value.Date, DateTimeKind.Utc);
    }

    private static string? NormalizarTexto(string? valor, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var normalizado = valor.Trim();
        return normalizado.Length > maxLength
            ? normalizado[..maxLength]
            : normalizado;
    }
}
