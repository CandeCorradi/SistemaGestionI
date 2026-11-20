using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;

namespace SistemaGestion.Application.Inventario.Dtos;

/// <summary>
/// Registro de un movimiento de inventario (api-spec §14).
/// <c>Quantity</c> es mayor que cero salvo para Ajuste, que admite signo.
/// </summary>
public sealed record CreateMovimientoStockDto(
    int ProductId,
    TipoMovimientoStock Type,
    int Quantity,
    string? Reason = null,
    string? Reference = null);

/// <summary>
/// Fila del stock actual (functional-spec §11: stock actual y mínimo).
/// </summary>
public sealed class StockListDto
{
    public int Id { get; init; }

    public string Nombre { get; init; } = string.Empty;

    public string CodigoSku { get; init; } = string.Empty;

    public TipoProducto Tipo { get; init; }

    public int StockActual { get; init; }

    public int StockMinimo { get; init; }

    public bool EsPersonalizado { get; init; }

    /// <summary>Stock actual por debajo del mínimo (stockMinimo &gt; 0).</summary>
    public bool AbajoDeMinimo { get; init; }

    public static StockListDto From(Producto producto) => new()
    {
        Id = producto.Id,
        Nombre = producto.Nombre,
        CodigoSku = producto.CodigoSku,
        Tipo = producto.Tipo,
        StockActual = producto.StockActual,
        StockMinimo = producto.StockMinimo,
        EsPersonalizado = producto.EsPersonalizado,
        AbajoDeMinimo = producto.StockMinimo > 0 && producto.StockActual <= producto.StockMinimo,
    };
}

/// <summary>
/// Stock de un producto concreto.
/// </summary>
public sealed class StockDetailDto
{
    public int Id { get; init; }

    public string Nombre { get; init; } = string.Empty;

    public string CodigoSku { get; init; } = string.Empty;

    public TipoProducto Tipo { get; init; }

    public bool EsPersonalizado { get; init; }

    public int StockActual { get; init; }

    public int StockMinimo { get; init; }

    public bool Activo { get; init; }

    public bool AbajoDeMinimo { get; init; }

    public static StockDetailDto From(Producto producto) => new()
    {
        Id = producto.Id,
        Nombre = producto.Nombre,
        CodigoSku = producto.CodigoSku,
        Tipo = producto.Tipo,
        EsPersonalizado = producto.EsPersonalizado,
        StockActual = producto.StockActual,
        StockMinimo = producto.StockMinimo,
        Activo = producto.Activo,
        AbajoDeMinimo = producto.StockMinimo > 0 && producto.StockActual <= producto.StockMinimo,
    };
}

/// <summary>
/// Movimiento del historial con producto y usuario denormalizados
/// (functional-spec §11: producto, tipo, cantidad, fecha, usuario,
/// referencia y motivo).
/// </summary>
public sealed class MovimientoStockDto
{
    public int Id { get; init; }

    public int ProductoId { get; init; }

    public string ProductoNombre { get; init; } = string.Empty;

    public TipoMovimientoStock Tipo { get; init; }

    public int Cantidad { get; init; }

    public int StockAnterior { get; init; }

    public int StockPosterior { get; init; }

    public string? Motivo { get; init; }

    public string? Referencia { get; init; }

    public int? UsuarioId { get; init; }

    public string? UsuarioNombre { get; init; }

    public DateTime Fecha { get; init; }

    public static MovimientoStockDto From(
        MovimientoStock movimiento,
        string productoNombre,
        string? usuarioNombre)
        => new()
        {
            Id = movimiento.Id,
            ProductoId = movimiento.ProductoId,
            ProductoNombre = productoNombre,
            Tipo = movimiento.Tipo,
            Cantidad = movimiento.Cantidad,
            StockAnterior = movimiento.StockAnterior,
            StockPosterior = movimiento.StockPosterior,
            Motivo = movimiento.Motivo,
            Referencia = movimiento.Referencia,
            UsuarioId = movimiento.UsuarioId,
            UsuarioNombre = usuarioNombre,
            Fecha = movimiento.Fecha,
        };
}
