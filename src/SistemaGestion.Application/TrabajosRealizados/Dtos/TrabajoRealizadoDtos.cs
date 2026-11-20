using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Application.TrabajosRealizados.Dtos;

/// <summary>
/// Contrato de una imagen de trabajo realizado: expone la URL de
/// lectura (resuelta por el storage) y nunca el path interno.
/// </summary>
public sealed record TrabajoRealizadoImagenDto
{
    public int Id { get; init; }

    public string Url { get; init; } = string.Empty;

    public int Orden { get; init; }

    public static TrabajoRealizadoImagenDto From(TrabajoRealizadoImagen imagen, string url) => new()
    {
        Id = imagen.Id,
        Url = url,
        Orden = imagen.Orden,
    };
}

public sealed record CreateTrabajoRealizadoDto(
    string Nombre,
    string? TipoMueble = null,
    string? ClienteNombre = null,
    bool PublicarNombreCliente = false,
    DateOnly? Fecha = null,
    string? Descripcion = null,
    string? Medidas = null,
    string? Melamina = null,
    string? Accesorios = null,
    string? Categoria = null,
    IReadOnlyList<string>? Tags = null,
    bool Destacado = false,
    bool Publicado = false);

/// <summary>
/// Edición parcial (patrón PATCH de productos): null = sin cambios;
/// string vacío = limpia el campo; booleanos y fecha sólo cambian cuando
/// se proveen.
/// </summary>
public sealed record UpdateTrabajoRealizadoDto(
    string? Nombre = null,
    string? TipoMueble = null,
    string? ClienteNombre = null,
    bool? PublicarNombreCliente = null,
    DateOnly? Fecha = null,
    bool LimpiarFecha = false,
    string? Descripcion = null,
    string? Medidas = null,
    string? Melamina = null,
    string? Accesorios = null,
    string? Categoria = null,
    IReadOnlyList<string>? Tags = null,
    bool? Destacado = null,
    bool? Publicado = null);

/// <summary>Fila administrativa del listado de trabajos.</summary>
public sealed record TrabajoRealizadoListDto
{
    public int Id { get; init; }

    public string Nombre { get; init; } = string.Empty;

    public string? TipoMueble { get; init; }

    public string? ClienteNombre { get; init; }

    public string? Categoria { get; init; }

    public DateOnly? Fecha { get; init; }

    public bool Destacado { get; init; }

    public bool Publicado { get; init; }

    public DateTime FechaCreacion { get; init; }

    public static TrabajoRealizadoListDto From(TrabajoRealizado trabajo) => new()
    {
        Id = trabajo.Id,
        Nombre = trabajo.Nombre,
        TipoMueble = trabajo.TipoMueble,
        ClienteNombre = trabajo.ClienteNombre,
        Categoria = trabajo.Categoria,
        Fecha = trabajo.Fecha,
        Destacado = trabajo.Destacado,
        Publicado = trabajo.Publicado,
        FechaCreacion = trabajo.FechaCreacion,
    };
}

/// <summary>Detalle administrativo completo con la galería de imágenes.</summary>
public sealed record TrabajoRealizadoDto
{
    public int Id { get; init; }

    public string Nombre { get; init; } = string.Empty;

    public string? TipoMueble { get; init; }

    public string? ClienteNombre { get; init; }

    public bool PublicarNombreCliente { get; init; }

    public DateOnly? Fecha { get; init; }

    public string? Descripcion { get; init; }

    public string? Medidas { get; init; }

    public string? Melamina { get; init; }

    public string? Accesorios { get; init; }

    public string? Categoria { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    public bool Destacado { get; init; }

    public bool Publicado { get; init; }

    public DateTime FechaCreacion { get; init; }

    public DateTime? FechaActualizacion { get; init; }

    public IReadOnlyList<TrabajoRealizadoImagenDto> Imagenes { get; init; } = [];

    public static TrabajoRealizadoDto From(
        TrabajoRealizado trabajo,
        IReadOnlyList<TrabajoRealizadoImagenDto> imagenes) => new()
    {
        Id = trabajo.Id,
        Nombre = trabajo.Nombre,
        TipoMueble = trabajo.TipoMueble,
        ClienteNombre = trabajo.ClienteNombre,
        PublicarNombreCliente = trabajo.PublicarNombreCliente,
        Fecha = trabajo.Fecha,
        Descripcion = trabajo.Descripcion,
        Medidas = trabajo.Medidas,
        Melamina = trabajo.Melamina,
        Accesorios = trabajo.Accesorios,
        Categoria = trabajo.Categoria,
        Tags = trabajo.Tags,
        Destacado = trabajo.Destacado,
        Publicado = trabajo.Publicado,
        FechaCreacion = trabajo.FechaCreacion,
        FechaActualizacion = trabajo.FechaActualizacion,
        Imagenes = imagenes,
    };
}

/// <summary>
/// Fila pública de la vidriera (AGENTS.md §23). No expone el estado de
/// publicación ni flags internos; el nombre del cliente sólo se incluye
/// cuando el trabajo lo permite (BR-191).
/// </summary>
public sealed record ShowroomItemDto
{
    public int Id { get; init; }

    public string Nombre { get; init; } = string.Empty;

    public string? TipoMueble { get; init; }

    /// <summary>Null cuando el trabajo no publica el nombre del cliente.</summary>
    public string? ClienteNombre { get; init; }

    public DateOnly? Fecha { get; init; }

    public string? Categoria { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    public bool Destacado { get; init; }

    public string? ImagenUrl { get; init; }

    public static ShowroomItemDto From(
        TrabajoRealizado trabajo,
        string? portadaUrl) => new()
    {
        Id = trabajo.Id,
        Nombre = trabajo.Nombre,
        TipoMueble = trabajo.TipoMueble,
        ClienteNombre = trabajo.PublicarNombreCliente ? trabajo.ClienteNombre : null,
        Fecha = trabajo.Fecha,
        Categoria = trabajo.Categoria,
        Tags = trabajo.Tags,
        Destacado = trabajo.Destacado,
        ImagenUrl = portadaUrl,
    };
}

/// <summary>Detalle público de la vidriera con la galería completa.</summary>
public sealed record ShowroomTrabajoDto
{
    public int Id { get; init; }

    public string Nombre { get; init; } = string.Empty;

    public string? TipoMueble { get; init; }

    public string? ClienteNombre { get; init; }

    public DateOnly? Fecha { get; init; }

    public string? Descripcion { get; init; }

    public string? Medidas { get; init; }

    public string? Melamina { get; init; }

    public string? Accesorios { get; init; }

    public string? Categoria { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    public bool Destacado { get; init; }

    public IReadOnlyList<TrabajoRealizadoImagenDto> Imagenes { get; init; } = [];

    public static ShowroomTrabajoDto From(
        TrabajoRealizado trabajo,
        IReadOnlyList<TrabajoRealizadoImagenDto> imagenes) => new()
    {
        Id = trabajo.Id,
        Nombre = trabajo.Nombre,
        TipoMueble = trabajo.TipoMueble,
        ClienteNombre = trabajo.PublicarNombreCliente ? trabajo.ClienteNombre : null,
        Fecha = trabajo.Fecha,
        Descripcion = trabajo.Descripcion,
        Medidas = trabajo.Medidas,
        Melamina = trabajo.Melamina,
        Accesorios = trabajo.Accesorios,
        Categoria = trabajo.Categoria,
        Tags = trabajo.Tags,
        Destacado = trabajo.Destacado,
        Imagenes = imagenes,
    };
}
