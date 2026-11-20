using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;

namespace SistemaGestion.Application.Productos.Dtos;

/// <summary>
/// Contrato de creación de productos (functional-spec §8).
/// </summary>
public sealed record CreateProductoDto(
    string Nombre,
    string CodigoSku,
    TipoProducto Tipo,
    decimal Precio,
    string? Descripcion = null,
    string? Categoria = null,
    decimal? Costo = null,
    string? Material = null,
    string? Acabado = null,
    string? Color = null,
    string? Dimensiones = null,
    int StockActual = 0,
    int StockMinimo = 0,
    bool Showroom = false,
    int? ProveedorId = null,
    bool EsPersonalizado = false);

/// <summary>
/// Actualización parcial de un producto (PATCH): los campos no nulos
/// reemplazan a los actuales. El stock actual no se modifica por aquí:
/// se gestiona con movimientos de inventario (AGENTS.md §15).
/// </summary>
public sealed record UpdateProductoDto(
    string? Nombre = null,
    string? CodigoSku = null,
    TipoProducto? Tipo = null,
    decimal? Precio = null,
    string? Descripcion = null,
    string? Categoria = null,
    decimal? Costo = null,
    string? Material = null,
    string? Acabado = null,
    string? Color = null,
    string? Dimensiones = null,
    int? StockMinimo = null,
    bool? Showroom = null,
    int? ProveedorId = null,
    bool? EsPersonalizado = null,
    bool? Activo = null);

/// <summary>
/// Contrato público de la API para productos (AGENTS.md §28).
/// </summary>
public sealed record ProductoDto
{
    public int Id { get; init; }

    public string Nombre { get; init; } = string.Empty;

    public string CodigoSku { get; init; } = string.Empty;

    public string? Descripcion { get; init; }

    public TipoProducto Tipo { get; init; }

    public string? Categoria { get; init; }

    public decimal Precio { get; init; }

    public decimal? Costo { get; init; }

    public string? Material { get; init; }

    public string? Acabado { get; init; }

    public string? Color { get; init; }

    public string? Dimensiones { get; init; }

    public int StockActual { get; init; }

    public int StockMinimo { get; init; }

    public bool Showroom { get; init; }

    public bool EsPersonalizado { get; init; }

    public int? ProveedorId { get; init; }

    public bool Activo { get; init; }

    public DateTime FechaCreacion { get; init; }

    public static ProductoDto From(Producto producto) => new()
    {
        Id = producto.Id,
        Nombre = producto.Nombre,
        CodigoSku = producto.CodigoSku,
        Descripcion = producto.Descripcion,
        Tipo = producto.Tipo,
        Categoria = producto.Categoria,
        Precio = producto.Precio,
        Costo = producto.Costo,
        Material = producto.Material,
        Acabado = producto.Acabado,
        Color = producto.Color,
        Dimensiones = producto.Dimensiones,
        StockActual = producto.StockActual,
        StockMinimo = producto.StockMinimo,
        Showroom = producto.Showroom,
        EsPersonalizado = producto.EsPersonalizado,
        ProveedorId = producto.ProveedorId,
        Activo = producto.Activo,
        FechaCreacion = producto.FechaCreacion,
    };
}

/// <summary>
/// Fila de listado de productos: omite costo (dato interno) y notas
/// extensas.
/// </summary>
public sealed record ProductoListDto
{
    public int Id { get; init; }

    public string Nombre { get; init; } = string.Empty;

    public string CodigoSku { get; init; } = string.Empty;

    public TipoProducto Tipo { get; init; }

    public string? Categoria { get; init; }

    public decimal Precio { get; init; }

    public int StockActual { get; init; }

    public int StockMinimo { get; init; }

    public bool Showroom { get; init; }

    public bool EsPersonalizado { get; init; }

    public bool Activo { get; init; }

    public static ProductoListDto From(Producto producto) => new()
    {
        Id = producto.Id,
        Nombre = producto.Nombre,
        CodigoSku = producto.CodigoSku,
        Tipo = producto.Tipo,
        Categoria = producto.Categoria,
        Precio = producto.Precio,
        StockActual = producto.StockActual,
        StockMinimo = producto.StockMinimo,
        Showroom = producto.Showroom,
        EsPersonalizado = producto.EsPersonalizado,
        Activo = producto.Activo,
    };
}
