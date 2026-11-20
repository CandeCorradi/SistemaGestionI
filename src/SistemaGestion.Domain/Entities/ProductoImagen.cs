namespace SistemaGestion.Domain.Entities;

/// <summary>
/// Imagen de un producto en el almacenamiento externo (functional-spec
/// §8/§9, AGENTS.md §24). La base de datos guarda únicamente la
/// referencia (path en el bucket) y el orden; los binarios viven en el
/// storage y nunca en PostgreSQL (BR-101).
/// </summary>
public class ProductoImagen : EntityBase
{
    /// <summary>Ruta del objeto dentro del bucket (ej. productos/12/abc.webp).</summary>
    public string Path { get; private set; } = string.Empty;

    public int ProductoId { get; private set; }

    /// <summary>Orden de exhibición (0 = primera / portada).</summary>
    public int Orden { get; private set; }

    public DateTime FechaCreacion { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Crea la referencia de imagen validando la ruta (obligatoria, máx.
    /// 500 caracteres) y el orden (nunca negativo).
    /// </summary>
    public static ProductoImagen Crear(int productoId, string path, int orden)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("La ruta de la imagen es obligatoria.", nameof(path));
        }

        if (path.Trim().Length > 500)
        {
            throw new ArgumentException("La ruta de la imagen no puede superar 500 caracteres.", nameof(path));
        }

        if (orden < 0)
        {
            throw new ArgumentException("El orden de la imagen no puede ser negativo.", nameof(orden));
        }

        if (productoId <= 0)
        {
            throw new ArgumentException("El producto es obligatorio.", nameof(productoId));
        }

        return new ProductoImagen
        {
            ProductoId = productoId,
            Path = path.Trim(),
            Orden = orden,
            FechaCreacion = DateTime.UtcNow,
        };
    }
}
