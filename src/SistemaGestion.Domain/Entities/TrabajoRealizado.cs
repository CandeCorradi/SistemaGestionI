namespace SistemaGestion.Domain.Entities;

/// <summary>
/// Trabajo realizado / vidriera (AGENTS.md §23): obra terminada que se
/// exhibe en el showroom público. No es un producto a la venta: es
/// portfolio de la carpintería, con galería propia en el storage
/// (BR-100/BR-101). El nombre del cliente sólo se publica cuando el
/// trabajo lo permite (BR-191).
/// </summary>
public class TrabajoRealizado : EntityBase
{
    public const int MaxNombreLength = 200;
    public const int MaxTextoLength = 1000;
    public const int MaxEtiquetaLength = 30;
    public const int MaxEtiquetas = 10;

    public string Nombre { get; private set; } = string.Empty;

    /// <summary>Tipo de mueble exhibido (libre: "Cocina", "Roperos"...).</summary>
    public string? TipoMueble { get; private set; }

    /// <summary>Nombre del cliente (referencia interna; opcionalmente publicable).</summary>
    public string? ClienteNombre { get; private set; }

    /// <summary>
    /// Cuando es false, el nombre del cliente NO se expone en el
    /// showroom público (privacidad, AGENTS.md §23 / BR-191).
    /// </summary>
    public bool PublicarNombreCliente { get; private set; }

    /// <summary>Fecha de realización/entrega (presentación, no instante UTC).</summary>
    public DateOnly? Fecha { get; private set; }

    public string? Descripcion { get; private set; }

    public string? Medidas { get; private set; }

    public string? Melamina { get; private set; }

    public string? Accesorios { get; private set; }

    public string? Categoria { get; private set; }

    public string[] Tags { get; private set; } = [];

    public bool Destacado { get; private set; }

    /// <summary>Sólo los trabajos publicados aparecen en la vidriera (BR-190).</summary>
    public bool Publicado { get; private set; }

    public DateTime FechaCreacion { get; private set; } = DateTime.UtcNow;

    public DateTime? FechaActualizacion { get; private set; }

    /// <summary>
    /// Crea un trabajo validando el nombre y normalizando etiquetas.
    /// </summary>
    public static TrabajoRealizado Crear(
        string nombre,
        string? tipoMueble = null,
        string? clienteNombre = null,
        bool publicarNombreCliente = false,
        DateOnly? fecha = null,
        string? descripcion = null,
        string? medidas = null,
        string? melamina = null,
        string? accesorios = null,
        string? categoria = null,
        IEnumerable<string>? tags = null,
        bool destacado = false,
        bool publicado = false)
    {
        ValidarNombre(nombre);

        return new TrabajoRealizado
        {
            Nombre = nombre.Trim(),
            TipoMueble = NullIfBlank(tipoMueble),
            ClienteNombre = NullIfBlank(clienteNombre),
            PublicarNombreCliente = publicarNombreCliente,
            Fecha = fecha,
            Descripcion = Truncar(NullIfBlank(descripcion)),
            Medidas = NullIfBlank(medidas),
            Melamina = NullIfBlank(melamina),
            Accesorios = NullIfBlank(accesorios),
            Categoria = NullIfBlank(categoria),
            Tags = tags is null ? [] : NormalizarEtiquetas(tags),
            Destacado = destacado,
            Publicado = publicado,
            FechaCreacion = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Actualiza el trabajo con mezcla tipo PATCH (patrón de
    /// <see cref="Producto"/>): strings null = sin cambios, string no
    /// vacío = reemplaza, string vacío = limpia; booleanos y fecha sólo
    /// cambian cuando se proveen.
    /// </summary>
    public void Actualizar(
        string? nombre = null,
        string? tipoMueble = null,
        string? clienteNombre = null,
        bool? publicarNombreCliente = null,
        DateOnly? fecha = null,
        bool limpiarFecha = false,
        string? descripcion = null,
        string? medidas = null,
        string? melamina = null,
        string? accesorios = null,
        string? categoria = null,
        IEnumerable<string>? tags = null)
    {
        if (nombre is not null)
        {
            ValidarNombre(nombre);

            if (!string.IsNullOrWhiteSpace(nombre))
            {
                Nombre = nombre.Trim();
            }
        }

        if (tipoMueble is not null)
        {
            TipoMueble = NullIfBlank(tipoMueble);
        }

        if (clienteNombre is not null)
        {
            ClienteNombre = NullIfBlank(clienteNombre);
        }

        if (publicarNombreCliente.HasValue)
        {
            PublicarNombreCliente = publicarNombreCliente.Value;
        }

        if (limpiarFecha)
        {
            Fecha = null;
        }
        else if (fecha.HasValue)
        {
            Fecha = fecha;
        }

        if (descripcion is not null)
        {
            Descripcion = Truncar(NullIfBlank(descripcion));
        }

        if (medidas is not null)
        {
            Medidas = NullIfBlank(medidas);
        }

        if (melamina is not null)
        {
            Melamina = NullIfBlank(melamina);
        }

        if (accesorios is not null)
        {
            Accesorios = NullIfBlank(accesorios);
        }

        if (categoria is not null)
        {
            Categoria = NullIfBlank(categoria);
        }

        if (tags is not null)
        {
            Tags = NormalizarEtiquetas(tags);
        }

        FechaActualizacion = DateTime.UtcNow;
    }

    public void Publicar()
    {
        Publicado = true;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void Ocultar()
    {
        Publicado = false;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void MarcarDestacado(bool destacado)
    {
        Destacado = destacado;
        FechaActualizacion = DateTime.UtcNow;
    }

    private static void ValidarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));
        }

        if (nombre.Trim().Length > MaxNombreLength)
        {
            throw new ArgumentException(
                $"El nombre no puede superar {MaxNombreLength} caracteres.", nameof(nombre));
        }
    }

    /// <summary>
    /// Normaliza las etiquetas: recorta, quita vacías/duplicados
    /// (case-insensitive) y limita cantidad y longitud.
    /// </summary>
    public static string[] NormalizarEtiquetas(IEnumerable<string> tags)
    {
        var resultado = new List<string>();

        foreach (var tag in tags)
        {
            var limpio = (tag ?? string.Empty).Trim();
            if (limpio.Length == 0)
            {
                continue;
            }

            if (limpio.Length > MaxEtiquetaLength)
            {
                throw new ArgumentException(
                    $"Cada etiqueta no puede superar {MaxEtiquetaLength} caracteres.", nameof(tags));
            }

            if (resultado.Any(t => string.Equals(t, limpio, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            resultado.Add(limpio);

            if (resultado.Count > MaxEtiquetas)
            {
                throw new ArgumentException(
                    $"No se admiten más de {MaxEtiquetas} etiquetas.", nameof(tags));
            }
        }

        return resultado.ToArray();
    }

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Truncar(string? value)
        => value is { Length: > MaxTextoLength } ? value[..MaxTextoLength] : value;
}
