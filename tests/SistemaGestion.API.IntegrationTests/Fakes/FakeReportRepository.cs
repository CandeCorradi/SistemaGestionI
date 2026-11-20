using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;

namespace SistemaGestion.API.IntegrationTests.Fakes;

/// <summary>
/// Fake en memoria de <see cref="IReportRepository"/> para tests de API:
/// devuelve colecciones seteables sin filtrar (el filtrado real es de
/// PostgreSQL; acá se prueba el contrato HTTP).
/// </summary>
public sealed class FakeReportRepository : IReportRepository
{
    public List<Pedido> Pedidos { get; } = new();

    public List<Pago> Pagos { get; } = new();

    public List<Presupuesto> Presupuestos { get; } = new();

    public List<Factura> Facturas { get; } = new();

    public List<Producto> Productos { get; } = new();

    public Dictionary<int, decimal> SumasPagadas { get; } = new();

    public Task<IReadOnlyList<Pedido>> GetPedidosAsync(
        DateTime? desde, DateTime? hasta, EstadoPedido? estado, int? clienteId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Pedido>>(Pedidos);

    public Task<IReadOnlyList<Pago>> GetPagosAsync(
        DateTime? desde, DateTime? hasta, int? clienteId, MetodoPago? metodo,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Pago>>(Pagos);

    public Task<IReadOnlyList<Presupuesto>> GetPresupuestosAsync(
        DateTime? desde, DateTime? hasta, EstadoPresupuesto? estado, int? clienteId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Presupuesto>>(Presupuestos);

    public Task<IReadOnlyList<Factura>> GetFacturasAsync(
        DateTime? desde, DateTime? hasta, int? clienteId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Factura>>(Facturas);

    public Task<IReadOnlyList<Producto>> GetProductosAsync(
        TipoProducto? tipo, bool? activo,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Producto>>(Productos);

    public Task<IReadOnlyDictionary<int, decimal>> GetSumasPagadasAsync(
        IReadOnlyCollection<int> pedidoIds,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyDictionary<int, decimal>>(SumasPagadas);
}
