using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Enums;

namespace SistemaGestion.API.IntegrationTests.Fakes;

/// <summary>
/// DashboardRepository falso con conteos seteables para tests de API.
/// </summary>
public sealed class FakeDashboardRepository : IDashboardRepository
{
    public Dictionary<EstadoPedido, int> PedidosPorEstado { get; } = new();

    public Dictionary<EstadoPresupuesto, int> PresupuestosPorEstado { get; } = new();

    public int PedidosEntregadosMes { get; set; }

    public int PedidosCanceladosMes { get; set; }

    public int PresupuestosAceptadosMes { get; set; }

    public int PresupuestosRechazadosMes { get; set; }

    public decimal PagosMesMonto { get; set; }

    public int PagosMesCantidad { get; set; }

    public decimal FacturasMesMonto { get; set; }

    public int FacturasMesCantidad { get; set; }

    public DashboardInventarioCounts Inventario { get; set; } = new(0, 0, 0);

    public Task<IReadOnlyDictionary<EstadoPedido, int>> CountPedidosPorEstadoAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyDictionary<EstadoPedido, int>>(PedidosPorEstado);

    public Task<int> CountPedidosEntregadosAsync(
        DateTime desdeUtc, DateTime hastaUtc, CancellationToken cancellationToken = default)
        => Task.FromResult(PedidosEntregadosMes);

    public Task<int> CountPedidosCanceladosAsync(
        DateTime desdeUtc, DateTime hastaUtc, CancellationToken cancellationToken = default)
        => Task.FromResult(PedidosCanceladosMes);

    public Task<IReadOnlyDictionary<EstadoPresupuesto, int>> CountPresupuestosPorEstadoAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyDictionary<EstadoPresupuesto, int>>(PresupuestosPorEstado);

    public Task<int> CountPresupuestosEnRangoAsync(
        EstadoPresupuesto estado, DateTime desdeUtc, DateTime hastaUtc,
        CancellationToken cancellationToken = default)
        => Task.FromResult(estado switch
        {
            EstadoPresupuesto.Aceptado => PresupuestosAceptadosMes,
            EstadoPresupuesto.Rechazado => PresupuestosRechazadosMes,
            _ => 0,
        });

    public Task<decimal> SumPagosAsync(
        DateTime desdeUtc, DateTime hastaUtc, CancellationToken cancellationToken = default)
        => Task.FromResult(PagosMesMonto);

    public Task<int> CountPagosAsync(
        DateTime desdeUtc, DateTime hastaUtc, CancellationToken cancellationToken = default)
        => Task.FromResult(PagosMesCantidad);

    public Task<decimal> SumFacturasAsync(
        DateTime desdeUtc, DateTime hastaUtc, CancellationToken cancellationToken = default)
        => Task.FromResult(FacturasMesMonto);

    public Task<int> CountFacturasAsync(
        DateTime desdeUtc, DateTime hastaUtc, CancellationToken cancellationToken = default)
        => Task.FromResult(FacturasMesCantidad);

    public Task<DashboardInventarioCounts> CountInventarioAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult(Inventario);
}
