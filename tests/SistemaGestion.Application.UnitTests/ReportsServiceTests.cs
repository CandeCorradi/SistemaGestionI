using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Reports;
using SistemaGestion.Domain.Common;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using Xunit;

namespace SistemaGestion.Application.UnitTests;

/// <summary>
/// Reglas de reportes (functional-spec §21, BR-140/BR-141): construcción
/// de los 5 documentos, validación de filtros y exportación.
/// </summary>
public sealed class ReportsServiceTests
{
    private static readonly DateTime FechaBase =
        new(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Build_UnknownKey_ReturnsNotFound()
    {
        var (service, _, _) = CreateService();

        var result = await service.BuildAsync("ventas-raras", new ReportFilters());

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task Build_FromAfterTo_ReturnsValidation()
    {
        var (service, _, _) = CreateService();

        var result = await service.BuildAsync(ReportKeys.Orders, new ReportFilters
        {
            From = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc),
            To = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
        });

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task Build_Orders_MapsRowsWithBalanceAndSummary()
    {
        var (service, repository, _) = CreateService();
        var cliente = Cliente.Crear("Ana", "García", "ana@test.com");
        cliente.Id = 7;
        repository.Pedidos.Add(Pedido.Crear(cliente.Id, 1, 2000m, FechaBase));
        repository.Pedidos.Add(Pedido.Crear(cliente.Id, 2, 500m, FechaBase.AddHours(1)));
        repository.Pedidos[0].Id = 1;
        repository.Pedidos[1].Id = 2;
        repository.SumasPagadas[1] = 300m;

        var result = await service.BuildAsync(ReportKeys.Orders, new ReportFilters());

        Assert.True(result.IsSuccess);
        var document = result.Value;
        Assert.Equal(ReportKeys.Orders, document.Key);
        Assert.Equal(2, document.Rows.Count);
        Assert.Equal(300m, document.Rows[0].Cells["pagado"]);
        Assert.Equal(1700m, document.Rows[0].Cells["saldo"]);
        Assert.Equal(0m, document.Rows[1].Cells["pagado"]);
        Assert.Equal(500m, document.Rows[1].Cells["saldo"]);
        Assert.Contains(document.Summary, s => s.Label == "Total" && Equals(s.Value, 2500m));
        Assert.Contains(document.Summary, s => s.Label == "Saldo" && Equals(s.Value, 2200m));
    }

    [Fact]
    public async Task Build_Orders_InvalidStatus_ReturnsValidation()
    {
        var (service, _, _) = CreateService();

        var result = await service.BuildAsync(ReportKeys.Orders, new ReportFilters { Status = 99 });

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task Build_Payments_InvalidMethod_ReturnsValidation()
    {
        var (service, _, _) = CreateService();

        var result = await service.BuildAsync(ReportKeys.Payments, new ReportFilters { Method = -1 });

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task Build_Quotes_MapsSnapshotTotalsAndLabels()
    {
        var (service, repository, _) = CreateService();
        var cliente = Cliente.Crear("Luis", "Pérez", "luis@test.com");
        cliente.Id = 5;
        var presupuesto = Presupuesto.Crear(cliente.Id, null, null, null, FechaBase);
        presupuesto.AgregarDetalle(PresupuestoDetalle.Crear("Mueble a medida", 2, 100m, 10m));
        presupuesto.RecalcularTotales();
        presupuesto.Enviar(FechaBase.AddHours(2));
        repository.Presupuestos.Add(presupuesto);

        var result = await service.BuildAsync(ReportKeys.Quotes, new ReportFilters());

        Assert.True(result.IsSuccess);
        var row = Assert.Single(result.Value.Rows);
        Assert.Equal(200m, row.Cells["subtotal"]);
        Assert.Equal(10m, row.Cells["descuento"]);
        Assert.Equal(190m, row.Cells["total"]);
        Assert.Equal("Enviado", row.Cells["estado"]);
    }

    [Fact]
    public async Task Build_Sales_UsesInvoiceSnapshotAndCondition()
    {
        var (service, repository, _) = CreateService();
        var factura = Factura.Crear(
            pedidoId: 1,
            clienteId: 7,
            clienteNombre: "María López",
            total: 1500m,
            fechaEmisionUtc: FechaBase,
            usuarioId: 1,
            notas: null);
        factura.AsignarDocumento("SIM-000001", esSimulada: true);
        repository.Facturas.Add(factura);

        var result = await service.BuildAsync(ReportKeys.Sales, new ReportFilters());

        Assert.True(result.IsSuccess);
        var row = Assert.Single(result.Value.Rows);
        Assert.Equal("SIM-000001", row.Cells["numero"]);
        Assert.Equal("María López", row.Cells["cliente"]);
        Assert.Equal(1500m, row.Cells["total"]);
        Assert.Equal("Simulada", row.Cells["condicion"]);
        Assert.Contains(result.Value.Summary, s => s.Label == "Total facturado" && Equals(s.Value, 1500m));
    }

    [Fact]
    public async Task Build_Inventory_ValueUsesCostAndSkipsNullCost()
    {
        var (service, repository, _) = CreateService();
        var conCosto = Producto.Crear(
            "Melamina blanca", "MEL-001", TipoProducto.Melamina, precio: 50m,
            costo: 30m, stockActual: 4);
        var sinCosto = Producto.Crear(
            "Pomo", "ACC-001", TipoProducto.Accesorio, precio: 10m,
            stockActual: 12);
        repository.Productos.Add(conCosto);
        repository.Productos.Add(sinCosto);

        var result = await service.BuildAsync(ReportKeys.Inventory, new ReportFilters());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Rows.Count);
        var filaMelamina = result.Value.Rows.Single(r => Equals(r.Cells["sku"], "MEL-001"));
        Assert.Equal(120m, filaMelamina.Cells["valor"]);
        var filaPomo = result.Value.Rows.Single(r => Equals(r.Cells["sku"], "ACC-001"));
        Assert.Equal(string.Empty, filaPomo.Cells["valor"]);
        Assert.Contains(result.Value.Summary, s => s.Label == "Valor inventario (costo)" && Equals(s.Value, 120m));
        Assert.Contains(result.Value.Summary, s => s.Label == "Unidades en stock" && Equals(s.Value, 16));
    }

    [Fact]
    public async Task Build_PassesFiltersToRepository()
    {
        var (service, repository, _) = CreateService();
        var desde = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var hasta = new DateTime(2026, 9, 30, 0, 0, 0, DateTimeKind.Utc);

        await service.BuildAsync(ReportKeys.Orders, new ReportFilters
        {
            From = desde,
            To = hasta,
            CustomerId = 3,
            Status = (int)EstadoPedido.Listo,
        });

        Assert.Equal(desde, repository.UltimoDesde);
        Assert.Equal(hasta, repository.UltimoHasta);
        Assert.Equal(3, repository.UltimoClienteId);
        Assert.Equal(EstadoPedido.Listo, repository.UltimoEstado);
    }

    [Fact]
    public async Task Export_CallsExporterAndBuildsMetadata()
    {
        var (service, _, exporter) = CreateService();
        exporter.Content = new byte[] { 0xDE, 0xAD };

        var result = await service.ExportAsync(
            ReportKeys.Payments, new ReportFilters(), ReportFormat.Xlsx);

        Assert.True(result.IsSuccess);
        Assert.Equal(new byte[] { 0xDE, 0xAD }, result.Value.Content);
        Assert.Equal(ReportFormat.Xlsx, exporter.LastFormat);
        Assert.Equal("reporte-payments-20261001-100000.xlsx", result.Value.FileName);
        Assert.Contains("spreadsheetml", result.Value.ContentType);
    }

    [Fact]
    public async Task Export_UnknownKey_ReturnsNotFoundWithoutExporting()
    {
        var (service, _, exporter) = CreateService();

        var result = await service.ExportAsync(
            "otro", new ReportFilters(), ReportFormat.Pdf);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
        Assert.Null(exporter.LastFormat);
    }

    // ------------------------------------------------------------------
    // fakes
    // ------------------------------------------------------------------

    private static (ReportsService Service, ReportRepositoryFake Repository, ExporterFake Exporter)
        CreateService()
    {
        var repository = new ReportRepositoryFake();
        var exporter = new ExporterFake();
        var service = new ReportsService(repository, exporter, new FakeClock());
        return (service, repository, exporter);
    }

    private sealed class FakeClock : ITimeProvider
    {
        public DateTime UtcNow { get; } = new(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);
    }

    private sealed class ExporterFake : IReportExporter
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();

        public ReportDocument? LastDocument { get; private set; }

        public ReportFormat? LastFormat { get; private set; }

        public byte[] Export(ReportDocument document, ReportFormat format)
        {
            LastDocument = document;
            LastFormat = format;
            return Content;
        }
    }

    private sealed class ReportRepositoryFake : IReportRepository
    {
        public List<Pedido> Pedidos { get; } = new();

        public List<Pago> Pagos { get; } = new();

        public List<Presupuesto> Presupuestos { get; } = new();

        public List<Factura> Facturas { get; } = new();

        public List<Producto> Productos { get; } = new();

        public Dictionary<int, decimal> SumasPagadas { get; } = new();

        public DateTime? UltimoDesde { get; private set; }

        public DateTime? UltimoHasta { get; private set; }

        public EstadoPedido? UltimoEstado { get; private set; }

        public MetodoPago? UltimoMetodo { get; private set; }

        public int? UltimoClienteId { get; private set; }

        public Task<IReadOnlyList<Pedido>> GetPedidosAsync(
            DateTime? desde, DateTime? hasta, EstadoPedido? estado, int? clienteId,
            CancellationToken cancellationToken = default)
        {
            Registrar(desde, hasta, estado, null, clienteId);
            return Task.FromResult<IReadOnlyList<Pedido>>(Pedidos);
        }

        public Task<IReadOnlyList<Pago>> GetPagosAsync(
            DateTime? desde, DateTime? hasta, int? clienteId, MetodoPago? metodo,
            CancellationToken cancellationToken = default)
        {
            Registrar(desde, hasta, null, metodo, clienteId);
            return Task.FromResult<IReadOnlyList<Pago>>(Pagos);
        }

        public Task<IReadOnlyList<Presupuesto>> GetPresupuestosAsync(
            DateTime? desde, DateTime? hasta, EstadoPresupuesto? estado, int? clienteId,
            CancellationToken cancellationToken = default)
        {
            Registrar(desde, hasta, null, null, clienteId);
            return Task.FromResult<IReadOnlyList<Presupuesto>>(Presupuestos);
        }

        public Task<IReadOnlyList<Factura>> GetFacturasAsync(
            DateTime? desde, DateTime? hasta, int? clienteId,
            CancellationToken cancellationToken = default)
        {
            Registrar(desde, hasta, null, null, clienteId);
            return Task.FromResult<IReadOnlyList<Factura>>(Facturas);
        }

        public Task<IReadOnlyList<Producto>> GetProductosAsync(
            TipoProducto? tipo, bool? activo,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Producto>>(Productos);

        public Task<IReadOnlyDictionary<int, decimal>> GetSumasPagadasAsync(
            IReadOnlyCollection<int> pedidoIds,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyDictionary<int, decimal>>(SumasPagadas);

        private void Registrar(
            DateTime? desde, DateTime? hasta, EstadoPedido? estado,
            MetodoPago? metodo, int? clienteId)
        {
            UltimoDesde = desde;
            UltimoHasta = hasta;
            UltimoEstado = estado;
            UltimoMetodo = metodo;
            UltimoClienteId = clienteId;
        }
    }
}
