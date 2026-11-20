namespace SistemaGestion.Domain.Entities;

/// <summary>
/// Imagen de un trabajo realizado en el almacenamiento externo
/// (AGENTS.md §23/§24). La base de datos guarda únicamente la
/// referencia (path en el bucket) y el orden; los binarios viven en el
/// storage y nunca en PostgreSQL (BR-101).
/// </summary>
public class TrabajoRealizadoImagen : EntityBase
{
    /// <summary>Ruta del objeto dentro del bucket (ej. trabajos/12/abc.webp).</summary>
    public string Path { get; private set; } = string.Empty;

    public int TrabajoRealizadoId { get; private set; }

    /// <summary>Orden de exhibición (0 = primera / portada).</summary>
    public int Orden { get; private set; }

    public DateTime FechaCreacion { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Crea la referencia de imagen validando la ruta (obligatoria, máx.
    /// 500 caracteres) y el orden (nunca negativo).
    /// </summary>
    public static TrabajoRealizadoImagen Crear(int trabajoRealizadoId, string path, int orden)
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

        if (trabajoRealizadoId <= 0)
        {
            throw new ArgumentException("El trabajo realizado es obligatorio.", nameof(trabajoRealizadoId));
        }

        return new TrabajoRealizadoImagen
        {
            TrabajoRealizadoId = trabajoRealizadoId,
            Path = path.Trim(),
            Orden = orden,
            FechaCreacion = DateTime.UtcNow,
        };
    }
}
