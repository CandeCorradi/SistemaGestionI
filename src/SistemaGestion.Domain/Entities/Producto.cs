using SistemaGestion.Domain.Enums;

namespace SistemaGestion.Domain.Entities;

/// <summary>
/// Producto comercial de la carpintería (functional-spec §8, AGENTS.md §14).
/// La identidad de negocio es el <see cref="CodigoSku"/> único (BR-030) y
/// los productos usados en operaciones históricas se desactivan de forma
/// lógica en lugar de borrarse (BR-032). El stock actual se mantiene aquí
/// de forma transaccional y su historial se registra con movimientos
/// (AGENTS.md §15).
/// </summary>
public class Producto : EntityBase
{
    public string Nombre { get; private set; } = string.Empty;

    /// <summary>Código/referencia comercial único (BR-030).</summary>
    public string CodigoSku { get; private set; } = string.Empty;

    public string? Descripcion { get; private set; }

    public TipoProducto Tipo { get; private set; }

    public string? Categoria { get; private set; }

    /// <summary>Precio de venta actual; decimal, nunca float/double (AGENTS.md §16).</summary>
    public decimal Precio { get; private set; }

    /// <summary>Costo de adquisición/producción, opcional.</summary>
    public decimal? Costo { get; private set; }

    public string? Material { get; private set; }

    public string? Acabado { get; private set; }

    public string? Color { get; private set; }

    public string? Dimensiones { get; private set; }

    /// <summary>Stock actual en unidades; negativos prohibidos (BR-052).</summary>
    public int StockActual { get; private set; }

    /// <summary>Umbral de alerta de stock bajo; 0 = sin umbral configurado.</summary>
    public int StockMinimo { get; private set; }

    /// <summary>Visibilidad en el showroom/vidriera (functional-spec §9).</summary>
    public bool Showroom { get; private set; }

    /// <summary>
    /// false = producto estándar de catálogo; true = mueble/artículo
    /// personalizado a medida (functional-spec §9 "distinguish standard
    /// and custom furniture", roadmap §13.2). Las especificaciones de una
    /// pieza a medida hecha a pedido viven en el presupuesto (BR-041).
    /// </summary>
    public bool EsPersonalizado { get; private set; }

    /// <summary>
    /// Proveedor asociado, opcional (functional-spec §7: asociar productos
    /// a proveedores). Restricción de borrado: conserva el historial del
    /// proveedor (BR-021).
    /// </summary>
    public int? ProveedorId { get; private set; }

    public bool Activo { get; private set; } = true;

    public DateTime FechaCreacion { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Crea un producto validando los datos obligatorios y los importes.
    /// El SKU se normaliza a mayúsculas; el precio no admite negativos
    /// (AGENTS.md §16).
    /// </summary>
    public static Producto Crear(
        string nombre,
        string codigoSku,
        TipoProducto tipo,
        decimal precio,
        string? descripcion = null,
        string? categoria = null,
        decimal? costo = null,
        string? material = null,
        string? acabado = null,
        string? color = null,
        string? dimensiones = null,
        int stockActual = 0,
        int stockMinimo = 0,
        bool showroom = false,
        int? proveedorId = null,
        bool esPersonalizado = false)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));
        }

        if (nombre.Trim().Length > 200)
        {
            throw new ArgumentException("El nombre no puede superar 200 caracteres.", nameof(nombre));
        }

        if (string.IsNullOrWhiteSpace(codigoSku))
        {
            throw new ArgumentException("El código SKU es obligatorio.", nameof(codigoSku));
        }

        if (codigoSku.Trim().Length > 50)
        {
            throw new ArgumentException("El código SKU no puede superar 50 caracteres.", nameof(codigoSku));
        }

        if (precio < 0)
        {
            throw new ArgumentException("El precio no puede ser negativo.", nameof(precio));
        }

        if (costo < 0)
        {
            throw new ArgumentException("El costo no puede ser negativo.", nameof(costo));
        }

        if (stockActual < 0)
        {
            throw new ArgumentException("El stock actual no puede ser negativo.", nameof(stockActual));
        }

        if (stockMinimo < 0)
        {
            throw new ArgumentException("El stock mínimo no puede ser negativo.", nameof(stockMinimo));
        }

        return new Producto
        {
            Nombre = nombre.Trim(),
            CodigoSku = NormalizeSku(codigoSku),
            Tipo = tipo,
            Precio = precio,
            Costo = costo,
            Descripcion = NullIfBlank(descripcion),
            Categoria = NullIfBlank(categoria),
            Material = NullIfBlank(material),
            Acabado = NullIfBlank(acabado),
            Color = NullIfBlank(color),
            Dimensiones = NullIfBlank(dimensiones),
            StockActual = stockActual,
            StockMinimo = stockMinimo,
            Showroom = showroom,
            ProveedorId = proveedorId,
            EsPersonalizado = esPersonalizado,
        };
    }

    /// <summary>
    /// Actualiza el producto con los valores provistos (mezcla tipo PATCH:
    /// los valores no nulos reemplazan a los actuales). Un string vacío
    /// limpia el campo opcional. Cambiar <see cref="Precio"/> no altera
    /// presupuestos ni pedidos históricos (BR-031).
    /// </summary>
    public void Actualizar(
        string? nombre = null,
        string? codigoSku = null,
        TipoProducto? tipo = null,
        decimal? precio = null,
        string? descripcion = null,
        string? categoria = null,
        decimal? costo = null,
        string? material = null,
        string? acabado = null,
        string? color = null,
        string? dimensiones = null,
        int? stockMinimo = null,
        bool? showroom = null,
        int? proveedorId = null,
        bool? esPersonalizado = null,
        bool? activo = null)
    {
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            if (nombre.Trim().Length > 200)
            {
                throw new ArgumentException("El nombre no puede superar 200 caracteres.", nameof(nombre));
            }

            Nombre = nombre.Trim();
        }

        if (!string.IsNullOrWhiteSpace(codigoSku))
        {
            if (codigoSku.Trim().Length > 50)
            {
                throw new ArgumentException("El código SKU no puede superar 50 caracteres.", nameof(codigoSku));
            }

            CodigoSku = NormalizeSku(codigoSku);
        }

        if (tipo.HasValue)
        {
            Tipo = tipo.Value;
        }

        if (precio.HasValue)
        {
            if (precio.Value < 0)
            {
                throw new ArgumentException("El precio no puede ser negativo.", nameof(precio));
            }

            Precio = precio.Value;
        }

        if (costo.HasValue)
        {
            if (costo.Value < 0)
            {
                throw new ArgumentException("El costo no puede ser negativo.", nameof(costo));
            }

            Costo = costo.Value;
        }

        if (descripcion is not null)
        {
            Descripcion = NullIfBlank(descripcion);
        }

        if (categoria is not null)
        {
            Categoria = NullIfBlank(categoria);
        }

        if (material is not null)
        {
            Material = NullIfBlank(material);
        }

        if (acabado is not null)
        {
            Acabado = NullIfBlank(acabado);
        }

        if (color is not null)
        {
            Color = NullIfBlank(color);
        }

        if (dimensiones is not null)
        {
            Dimensiones = NullIfBlank(dimensiones);
        }

        if (stockMinimo.HasValue)
        {
            if (stockMinimo.Value < 0)
            {
                throw new ArgumentException("El stock mínimo no puede ser negativo.", nameof(stockMinimo));
            }

            StockMinimo = stockMinimo.Value;
        }

        if (showroom.HasValue)
        {
            Showroom = showroom.Value;
        }

        if (proveedorId.HasValue)
        {
            ProveedorId = proveedorId;
        }

        if (esPersonalizado.HasValue)
        {
            EsPersonalizado = esPersonalizado.Value;
        }

        if (activo.HasValue)
        {
            Activo = activo.Value;
        }
    }

    /// <summary>
    /// Desactivación lógica (BR-032): conserva el producto para el
    /// historial de presupuestos, pedidos y ventas.
    /// </summary>
    public void Desactivar() => Activo = false;

    /// <summary>
    /// Fija el stock a un valor absoluto validando el invariante de
    /// dominio (BR-052: nunca negativo). En la aplicación real el stock
    /// se modifica mediante movimientos atómicos en inventario; este
    /// método se usa donde la entidad debe reflejar el resultado aplicado.
    /// </summary>
    public void FijarStock(int stock)
    {
        if (stock < 0)
        {
            throw new ArgumentException(
                "El stock actual no puede ser negativo (BR-052).",
                nameof(stock));
        }

        StockActual = stock;
    }

    private static string NormalizeSku(string codigoSku)
        => codigoSku.Trim().ToUpperInvariant();

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
