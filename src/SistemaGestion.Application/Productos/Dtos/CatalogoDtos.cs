using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;

namespace SistemaGestion.Application.Productos.Dtos;

/// <summary>
/// Contrato público de una imagen de producto: expone la URL de lectura
/// (resuelta por el storage) y nunca el path interno.
/// </summary>
public sealed record ProductoImagenDto
{
    public int Id { get; init; }

    public string Url { get; init; } = string.Empty;

    public int Orden { get; init; }

    public static ProductoImagenDto From(ProductoImagen imagen, string url) => new()
    {
        Id = imagen.Id,
        Url = url,
        Orden = imagen.Orden,
    };
}

/// <summary>
/// Fila pública del catálogo (functional-spec §9): datos de exhibición
/// únicamente. No expone costo, stock numérico, proveedor ni estado
/// interno — sólo la disponibilidad (BR-004 aplicado a datos internos).
/// </summary>
public sealed record CatalogoItemDto
{
    public int Id { get; init; }

    public string Nombre { get; init; } = string.Empty;

    public TipoProducto Tipo { get; init; }

    public string? Categoria { get; init; }

    public decimal Precio { get; init; }

    public bool EsPersonalizado { get; init; }

    public bool Showroom { get; init; }

    /// <summary>true cuando hay stock disponible (¿se ve en vidriera?).</summary>
    public bool Disponible { get; init; }

    /// <summary>URL de la imagen de portada; null sin imágenes.</summary>
    public string? ImagenUrl { get; init; }
}

/// <summary>
/// Detalle público del catálogo (functional-spec §9): incluye la
/// descripción, materiales, medidas y la galería de imágenes. Distingue
/// producto estándar de mueble a medida (functional-spec §9).
/// </summary>
public sealed record CatalogoProductoDto
{
    public int Id { get; init; }

    public string Nombre { get; init; } = string.Empty;

    public string? Descripcion { get; init; }

    public TipoProducto Tipo { get; init; }

    public string? Categoria { get; init; }

    public decimal Precio { get; init; }

    public string? Material { get; init; }

    public string? Acabado { get; init; }

    public string? Color { get; init; }

    public string? Dimensiones { get; init; }

    public bool EsPersonalizado { get; init; }

    public bool Showroom { get; init; }

    public bool Disponible { get; init; }

    public IReadOnlyList<ProductoImagenDto> Imagenes { get; init; } = [];
}
