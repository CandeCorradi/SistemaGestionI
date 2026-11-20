using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.IntegrationTests.Fakes;

/// <summary>
/// Fake en memoria de <see cref="IInventarioRepository"/> para tests de API.
/// Comparte la fuente de productos con <see cref="FakeProductoRepository"/>
/// para que el stock leído y verificado sea el mismo objeto. El CAS se
/// replica fielmente: sólo aplica si el stock sigue igual al leído.
/// Al servir el historial asocia producto y usuario como haría el Include
/// de EF Core.
/// </summary>
public sealed class FakeInventarioRepository : IInventarioRepository
{
    private readonly FakeProductoRepository _productos;

    private readonly FakeUsuarioRepository _usuarios;

    private readonly List<MovimientoStock> _movimientos = new();

    public FakeInventarioRepository(
        FakeProductoRepository productos,
        FakeUsuarioRepository usuarios)
    {
        _productos = productos;
        _usuarios = usuarios;
    }

    public IReadOnlyList<MovimientoStock> All => _movimientos;

    public Task<Producto?> GetStockAsync(int productoId, CancellationToken cancellationToken = default)
        => Task.FromResult(_productos.FindById(productoId));

    public Task<PagedResult<MovimientoStock>> GetMovementsPagedAsync(
        int page,
        int pageSize,
        int? productoId,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<MovimientoStock> query = _movimientos;

        if (productoId.HasValue)
        {
            query = query.Where(m => m.ProductoId == productoId.Value);
        }

        var list = query
            .OrderByDescending(m => m.Fecha)
            .ThenByDescending(m => m.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        foreach (var movimiento in list)
        {
            movimiento.AsociarReferencias(
                _productos.FindById(movimiento.ProductoId),
                movimiento.UsuarioId.HasValue
                    ? _usuarios.FindById(movimiento.UsuarioId.Value)
                    : null);
        }

        return Task.FromResult(new PagedResult<MovimientoStock>
        {
            Items = list,
            Page = page,
            PageSize = pageSize,
            TotalItems = query.Count(),
        });
    }

    public Task<bool> TryApplyMovementAsync(
        MovimientoStock movimiento,
        int stockEsperado,
        CancellationToken cancellationToken = default)
    {
        var producto = _productos.FindById(movimiento.ProductoId);
        if (producto is null || producto.StockActual != stockEsperado || !producto.Activo)
        {
            return Task.FromResult(false);
        }

        producto.FijarStock(movimiento.StockPosterior);
        movimiento.Id = _movimientos.Count == 0 ? 1 : _movimientos.Max(m => m.Id) + 1;
        _movimientos.Add(movimiento);
        return Task.FromResult(true);
    }
}
