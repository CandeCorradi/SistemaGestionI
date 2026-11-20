namespace SistemaGestion.Domain.Entities;

/// <summary>
/// Línea de un presupuesto. Congela el snapshot comercial (descripción,
/// cantidad, precio, descuento, subtotal) para que los cambios futuros de
/// precios no alteren presupuestos históricos (BR-063 / AGENTS.md §10).
/// </summary>
public class PresupuestoDetalle : EntityBase
{
    public int PresupuestoId { get; private set; }

    /// <summary>Referencia opcional al producto del catálogo; el precio congelado es el del snapshot.</summary>
    public int? ProductoId { get; private set; }

    public string Descripcion { get; private set; } = string.Empty;

    public int Cantidad { get; private set; }

    public decimal PrecioUnitario { get; private set; }

    public decimal Descuento { get; private set; }

    public decimal Subtotal { get; private set; }

    public Presupuesto? Presupuesto { get; private set; }

    public Producto? Producto { get; private set; }

    private PresupuestoDetalle()
    {
    }

    /// <summary>
    /// Crea una línea validada. <paramref name="precioUnitario"/> es
    /// obligatorio (el servicio toma el precio vigente del producto cuando
    /// hay <paramref name="productoId"/>; si no, el precio declarado en la
    /// línea — mueble a medida sin SKU).
    /// </summary>
    public static PresupuestoDetalle Crear(
        string descripcion,
        int cantidad,
        decimal precioUnitario,
        decimal descuento,
        int? productoId = null)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            throw new ArgumentException("La descripción de la línea es obligatoria.", nameof(descripcion));
        }

        if (cantidad <= 0)
        {
            throw new ArgumentException("La cantidad debe ser mayor que cero.", nameof(cantidad));
        }

        if (precioUnitario < 0)
        {
            throw new ArgumentException("El precio unitario no puede ser negativo.", nameof(precioUnitario));
        }

        if (descuento < 0)
        {
            throw new ArgumentException("El descuento no puede ser negativo.", nameof(descuento));
        }

        decimal bruto = cantidad * precioUnitario;
        if (descuento > bruto)
        {
            throw new ArgumentException("El descuento no puede superar el importe de la línea.", nameof(descuento));
        }

        if (productoId.HasValue && productoId.Value <= 0)
        {
            throw new ArgumentException("El identificador de producto no es válido.", nameof(productoId));
        }

        return new PresupuestoDetalle
        {
            ProductoId = productoId,
            Descripcion = descripcion.Trim(),
            Cantidad = cantidad,
            PrecioUnitario = precioUnitario,
            Descuento = descuento,
            Subtotal = bruto - descuento,
        };
    }
}
