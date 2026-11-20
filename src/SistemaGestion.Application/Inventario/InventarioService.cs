using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Inventario.Dtos;
using SistemaGestion.Domain.Common;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Inventario;

/// <summary>
/// Casos de uso de inventario (functional-spec §11, roadmap §13.3):
/// consulta del stock actual, detalle por producto, historial de
/// movimientos (BR-051) y registro de movimientos con validación de
/// reglas (BR-052 sin stock negativo) y aplicación atómica (BR-050/BR-053).
/// El stock no se modifica en ningún otro endpoint (el PATCH de productos
/// no toca StockActual).
/// </summary>
public sealed class InventarioService
{
    /// <summary>Tamaño de página máximo permitido (AGENTS.md §30).</summary>
    public const int MaxPageSize = 100;

    public const int DefaultPageSize = 20;

    private readonly IProductoRepository _productos;

    private readonly IInventarioRepository _inventario;

    private readonly IUsuarioRepository _usuarios;

    private readonly ITimeProvider _clock;

    public InventarioService(
        IProductoRepository productos,
        IInventarioRepository inventario,
        IUsuarioRepository usuarios,
        ITimeProvider clock)
    {
        _productos = productos;
        _inventario = inventario;
        _usuarios = usuarios;
        _clock = clock;
    }

    /// <summary>
    /// Lista el stock actual de productos activos con búsqueda y filtro de
    /// stock bajo. Sólo campos de inventario (performance §62).
    /// </summary>
    public async Task<Result<PagedResult<StockListDto>>> GetStockListAsync(
        int page,
        int pageSize,
        string? search,
        bool? lowStock,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var paged = await _productos.GetPagedAsync(
            page,
            pageSize,
            search,
            tipo: null,
            categoria: null,
            activo: true,
            showroom: null,
            lowStock,
            isCustom: null,
            cancellationToken);

        return new PagedResult<StockListDto>
        {
            Items = paged.Items.Select(StockListDto.From).ToList(),
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalItems = paged.TotalItems,
        };
    }

    /// <summary>Stock de un producto concreto.</summary>
    public async Task<Result<StockDetailDto>> GetProductStockAsync(
        int productoId,
        CancellationToken cancellationToken = default)
    {
        if (productoId <= 0)
        {
            return Error.Validation("El producto es obligatorio.");
        }

        var producto = await _productos.GetByIdAsync(productoId, cancellationToken);
        if (producto is null)
        {
            return Error.NotFound($"El producto {productoId} no existe.");
        }

        return StockDetailDto.From(producto);
    }

    /// <summary>Historial paginado de movimientos, opcionalmente de un producto.</summary>
    public async Task<Result<PagedResult<MovimientoStockDto>>> GetMovementsAsync(
        int page,
        int pageSize,
        int? productoId,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        if (productoId is <= 0)
        {
            return Error.Validation("El producto indicado no es válido.");
        }

        var paged = await _inventario.GetMovementsPagedAsync(page, pageSize, productoId, cancellationToken);

        return new PagedResult<MovimientoStockDto>
        {
            Items = paged.Items
                .Select(m => MovimientoStockDto.From(
                    m,
                    m.Producto?.Nombre ?? $"Producto {m.ProductoId}",
                    m.Usuario is null
                        ? null
                        : $"{m.Usuario.Nombre} {m.Usuario.Apellido}".Trim()))
                .ToList(),
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalItems = paged.TotalItems,
        };
    }

    /// <summary>
    /// Registra un movimiento en nombre del usuario interno autenticado.
    /// El stock se lee, se valida (BR-052) y se aplica con CAS atómico
    /// (BR-050): si otro proceso modificó el stock en el medio devuelve
    /// Conflict (BR-053) sin tocar nada.
    /// </summary>
    public async Task<Result<MovimientoStockDto>> RegisterMovementAsync(
        string firebaseUid,
        CreateMovimientoStockDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(firebaseUid))
        {
            return Error.NotFound("No hay usuario interno asociado al token.");
        }

        if (dto.ProductId <= 0)
        {
            return Error.Validation("El producto es obligatorio.");
        }

        var producto = await _inventario.GetStockAsync(dto.ProductId, cancellationToken);
        if (producto is null)
        {
            return Error.NotFound($"El producto {dto.ProductId} no existe.");
        }

        if (!producto.Activo)
        {
            return Error.Validation("El producto está desactivado; reactívelo antes de registrar movimientos.");
        }

        var usuario = await _usuarios.GetByFirebaseUidAsync(firebaseUid, cancellationToken);
        if (usuario is null || !usuario.Activo)
        {
            return Error.NotFound("No hay usuario interno activo asociado al token.");
        }

        MovimientoStock movimiento;
        try
        {
            movimiento = MovimientoStock.Crear(
                productoId: producto.Id,
                tipo: dto.Type,
                cantidad: dto.Quantity,
                stockAnterior: producto.StockActual,
                usuarioId: usuario.Id,
                fechaUtc: _clock.UtcNow,
                motivo: dto.Reason,
                referencia: dto.Reference);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(ex.Message);
        }

        var aplicado = await _inventario.TryApplyMovementAsync(
            movimiento, producto.StockActual, cancellationToken);

        if (!aplicado)
        {
            return Error.Conflict(
                "El stock cambió por una operación concurrente; no se modificó nada. "
                + "Verifique el stock actual e intente nuevamente (BR-053).");
        }

        return MovimientoStockDto.From(
            movimiento,
            producto.Nombre,
            $"{usuario.Nombre} {usuario.Apellido}".Trim());
    }
}
