using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Presupuestos;
using SistemaGestion.Application.Presupuestos.Dtos;
using SistemaGestion.Domain.Common;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;
using Xunit;

namespace SistemaGestion.Application.UnitTests;

/// <summary>
/// Reglas de presupuestos (BR-060..BR-064, functional-spec §12): snapshot
/// comercial, totales en backend y transiciones de estado.
/// </summary>
public sealed class PresupuestosServiceTests
{
    private static (PresupuestosService Service,
        FakePresupuestoRepository Presupuestos,
        FakeClienteRepository Clientes,
        FakeProductoRepository Productos,
        FakeClock Clock)
        CreateService()
    {
        var clientes = new FakeClienteRepository();
        var productos = new FakeProductoRepository();
        var presupuestos = new FakePresupuestoRepository(clientes);
        var clock = new FakeClock();
        var notificaciones = new FakeNotificacionPublisher();

        var service = new PresupuestosService(presupuestos, clientes, productos, clock, notificaciones);
        return (service, presupuestos, clientes, productos, clock);
    }

    private static CreateQuoteDto Quote(
        int customerId = 1,
        List<CreateQuoteItemDto>? items = null,
        DateTime? expirationDate = null)
        => new(
            CustomerId: customerId,
            ExpirationDate: expirationDate,
            Notes: "Notas",
            Conditions: "50% de seña",
            Items: items ?? new List<CreateQuoteItemDto>
            {
                new(ProductId: null, Description: "Mueble a medida", Quantity: 1, UnitPrice: 100m, Discount: null),
            });

    // ------------------------------------------------------------------
    // Creación (BR-060/061/062/063)
    // ------------------------------------------------------------------

    [Fact]
    public async Task Create_WithProductLine_SnapshotsCurrentPrice_IgnoresDeclaredPrice_Br063()
    {
        var (service, _, clientes, productos, _) = CreateService();
        clientes.Seed();
        productos.Seed(precio: 1500m);

        var result = await service.CreateAsync(Quote(items: new List<CreateQuoteItemDto>
        {
            new(ProductId: 1, Description: null, Quantity: 2, UnitPrice: 999_999m, Discount: null),
        }));

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(1500m, item.UnitPrice);
        Assert.Equal("Producto", item.Description);
        Assert.Equal(3000m, result.Value.Total);
    }

    [Fact]
    public async Task Create_WithoutProductLine_UsesDeclaredPrice()
    {
        var (service, _, clientes, _, _) = CreateService();
        clientes.Seed();

        var result = await service.CreateAsync(Quote());

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Null(item.ProductId);
        Assert.Equal(100m, item.UnitPrice);
    }

    [Fact]
    public async Task Create_TotalsCalculatedInBackend_Br062()
    {
        var (service, _, clientes, _, _) = CreateService();
        clientes.Seed();

        var result = await service.CreateAsync(Quote(items: new List<CreateQuoteItemDto>
        {
            new(null, "Mueble", 3, 100m, 20m),
            new(null, "Melamina", 2, 50m, null),
        }));

        Assert.True(result.IsSuccess);
        Assert.Equal(400m, result.Value.Subtotal);
        Assert.Equal(20m, result.Value.Discount);
        Assert.Equal(380m, result.Value.Total);
    }

    [Fact]
    public async Task Create_WithoutItems_ReturnsValidation_Br061()
    {
        var (service, presupuestos, clientes, _, _) = CreateService();
        clientes.Seed();

        var result = await service.CreateAsync(Quote(items: new List<CreateQuoteItemDto>()));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
        Assert.Contains("BR-061", result.Error.Description);
        Assert.Empty(presupuestos.All);
    }

    [Fact]
    public async Task Create_UnknownCustomer_ReturnsNotFound()
    {
        var (service, _, _, _, _) = CreateService();

        var result = await service.CreateAsync(Quote(customerId: 99));

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task Create_InactiveCustomer_ReturnsValidation()
    {
        var (service, _, clientes, _, _) = CreateService();
        var cliente = clientes.Seed();
        cliente.Desactivar();

        var result = await service.CreateAsync(Quote(customerId: cliente.Id));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task Create_UnknownProduct_ReturnsNotFound()
    {
        var (service, _, clientes, _, _) = CreateService();
        clientes.Seed();

        var result = await service.CreateAsync(Quote(items: new List<CreateQuoteItemDto>
        {
            new(ProductId: 42, Description: null, Quantity: 1, UnitPrice: null, Discount: null),
        }));

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task Create_LineWithoutQuantity_ReturnsValidation()
    {
        var (service, _, clientes, _, _) = CreateService();
        clientes.Seed();

        var result = await service.CreateAsync(Quote(items: new List<CreateQuoteItemDto>
        {
            new(null, "Mueble", null, 100m, null),
        }));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
        Assert.Contains("cantidad", result.Error.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_FreeLineWithoutPrice_ReturnsValidation()
    {
        var (service, _, clientes, _, _) = CreateService();
        clientes.Seed();

        var result = await service.CreateAsync(Quote(items: new List<CreateQuoteItemDto>
        {
            new(null, "Mueble a medida", 1, null, null),
        }));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
        Assert.Contains("precio", result.Error.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_PastExpirationDate_ReturnsValidation()
    {
        var (service, _, clientes, _, _) = CreateService();
        clientes.Seed();

        var result = await service.CreateAsync(Quote(
            expirationDate: new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
        Assert.Contains("validez", result.Error.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_SetsBorradorAndStoresNotes()
    {
        var (service, _, clientes, _, _) = CreateService();
        clientes.Seed();

        var result = await service.CreateAsync(Quote());

        Assert.True(result.IsSuccess);
        Assert.Equal(EstadoPresupuesto.Borrador, result.Value.Status);
        Assert.Equal("Notas", result.Value.Notes);
        Assert.Equal("50% de seña", result.Value.Conditions);
        Assert.Equal(1, result.Value.CustomerId);
        Assert.False(result.Value.Expired);
    }

    // ------------------------------------------------------------------
    // Edición (sólo borrador)
    // ------------------------------------------------------------------

    [Fact]
    public async Task Update_InBorrador_ReplacesLines_AndRecalculates()
    {
        var (service, _, clientes, _, _) = CreateService();
        clientes.Seed();
        var creado = await service.CreateAsync(Quote());

        var result = await service.UpdateAsync(creado.Value.Id, new UpdateQuoteDto(
            CustomerId: 1,
            ExpirationDate: null,
            Notes: "Actualizado",
            Conditions: null,
            Items: new List<CreateQuoteItemDto>
            {
                new(null, "Nueva línea", 5, 40m, 10m),
            }));

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal("Nueva línea", item.Description);
        Assert.Equal(200m, result.Value.Subtotal);
        Assert.Equal(10m, result.Value.Discount);
        Assert.Equal(190m, result.Value.Total);
        Assert.Equal("Actualizado", result.Value.Notes);
    }

    [Fact]
    public async Task Update_Enviado_ReturnsConflict()
    {
        var (service, _, clientes, _, _) = CreateService();
        clientes.Seed();
        var creado = await service.CreateAsync(Quote());
        await service.SendAsync(creado.Value.Id);

        var result = await service.UpdateAsync(creado.Value.Id, new UpdateQuoteDto(
            CustomerId: 1, ExpirationDate: null, Notes: null, Conditions: null, Items: null));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
    }

    [Fact]
    public async Task Update_UnknownQuote_ReturnsNotFound()
    {
        var (service, _, _, _, _) = CreateService();

        var result = await service.UpdateAsync(99, new UpdateQuoteDto(
            null, null, null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task Update_WithoutItems_KeepsCurrentLines()
    {
        var (service, _, clientes, _, _) = CreateService();
        clientes.Seed();
        var creado = await service.CreateAsync(Quote());

        var result = await service.UpdateAsync(creado.Value.Id, new UpdateQuoteDto(
            CustomerId: 1,
            ExpirationDate: null,
            Notes: "Sin cambios de líneas",
            Conditions: null,
            Items: null));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(100m, result.Value.Total);
    }

    // ------------------------------------------------------------------
    // Transiciones (BR-061/064)
    // ------------------------------------------------------------------

    [Fact]
    public async Task Send_FromBorrador_Ok()
    {
        var (service, _, clientes, _, _) = CreateService();
        clientes.Seed();
        var creado = await service.CreateAsync(Quote());

        var result = await service.SendAsync(creado.Value.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(EstadoPresupuesto.Enviado, result.Value.Status);
        Assert.NotNull(result.Value.SendDate);
    }

    [Fact]
    public async Task Send_Twice_SecondReturnsConflict()
    {
        var (service, _, clientes, _, _) = CreateService();
        clientes.Seed();
        var creado = await service.CreateAsync(Quote());
        await service.SendAsync(creado.Value.Id);

        var result = await service.SendAsync(creado.Value.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
    }

    [Fact]
    public async Task Reject_FromEnviado_Ok_StoresReason()
    {
        var (service, _, clientes, _, _) = CreateService();
        clientes.Seed();
        var creado = await service.CreateAsync(Quote());
        await service.SendAsync(creado.Value.Id);

        var result = await service.RejectAsync(creado.Value.Id, "Presupuesto caro");

        Assert.True(result.IsSuccess);
        Assert.Equal(EstadoPresupuesto.Rechazado, result.Value.Status);
        Assert.Equal("Presupuesto caro", result.Value.StateReason);
    }

    [Fact]
    public async Task Reject_FromBorrador_ReturnsConflict()
    {
        var (service, _, clientes, _, _) = CreateService();
        clientes.Seed();
        var creado = await service.CreateAsync(Quote());

        var result = await service.RejectAsync(creado.Value.Id, null);

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
    }

    [Fact]
    public async Task Cancel_FromBorrador_Ok()
    {
        var (service, _, clientes, _, _) = CreateService();
        clientes.Seed();
        var creado = await service.CreateAsync(Quote());

        var result = await service.CancelAsync(creado.Value.Id, "Se anuló");

        Assert.True(result.IsSuccess);
        Assert.Equal(EstadoPresupuesto.Anulado, result.Value.Status);
        Assert.Equal("Se anuló", result.Value.StateReason);
    }

    [Fact]
    public async Task Cancel_Aceptado_ReturnsConflict()
    {
        var (service, presupuestos, clientes, _, _) = CreateService();
        clientes.Seed();
        var creado = await service.CreateAsync(Quote());

        var entidad = presupuestos.All.Single();
        entidad.Enviar(DateTime.UtcNow);
        entidad.Aceptar(DateTime.UtcNow);

        var result = await service.CancelAsync(creado.Value.Id, null);

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
    }

    // ------------------------------------------------------------------
    // Aceptación atómica → Pedido (BR-064/BR-065/BR-070/BR-071)
    // ------------------------------------------------------------------

    [Fact]
    public async Task Accept_FromEnviado_CreatesSingleOrder_SnapshotTotal_Br065()
    {
        var (service, presupuestos, clientes, _, _) = CreateService();
        clientes.Seed();
        var creado = await service.CreateAsync(Quote());
        await service.SendAsync(creado.Value.Id);

        var result = await service.AcceptAsync(creado.Value.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(EstadoPresupuesto.Aceptado, result.Value.Status);
        Assert.NotNull(result.Value.OrderId);

        var pedido = Assert.Single(presupuestos.Pedidos);
        Assert.Equal(result.Value.OrderId, pedido.Id);
        Assert.Equal(creado.Value.CustomerId, pedido.ClienteId);
        Assert.Equal(creado.Value.Id, pedido.PresupuestoId);
        Assert.Equal(creado.Value.Total, pedido.Total);
        Assert.Equal(EstadoPedido.Pendiente, pedido.Estado);
        Assert.Equal(creado.Value.Total, result.Value.Total);
    }

    [Fact]
    public async Task Accept_FromBorrador_ReturnsConflict_Br064()
    {
        var (service, presupuestos, clientes, _, _) = CreateService();
        clientes.Seed();
        var creado = await service.CreateAsync(Quote());

        var result = await service.AcceptAsync(creado.Value.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
        Assert.Empty(presupuestos.Pedidos);
    }

    [Fact]
    public async Task Accept_Twice_SecondReturnsConflict_NoSecondOrder()
    {
        var (service, presupuestos, clientes, _, _) = CreateService();
        clientes.Seed();
        var creado = await service.CreateAsync(Quote());
        await service.SendAsync(creado.Value.Id);
        await service.AcceptAsync(creado.Value.Id);

        var result = await service.AcceptAsync(creado.Value.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
        Assert.Single(presupuestos.Pedidos);
    }

    [Fact]
    public async Task Accept_LostConcurrentRace_ReturnsConflict_NoOrderCreated()
    {
        var (service, presupuestos, clientes, _, _) = CreateService();
        clientes.Seed();
        var creado = await service.CreateAsync(Quote());
        await service.SendAsync(creado.Value.Id);
        presupuestos.FailNextAccept = true;

        var result = await service.AcceptAsync(creado.Value.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
        Assert.Contains("BR-065", result.Error.Description);
        Assert.Empty(presupuestos.Pedidos);
    }

    [Fact]
    public async Task Accept_UnknownQuote_ReturnsNotFound()
    {
        var (service, _, _, _, _) = CreateService();

        var result = await service.AcceptAsync(99);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetById_AfterAccept_ReturnsOrderId()
    {
        var (service, _, clientes, _, _) = CreateService();
        clientes.Seed();
        var creado = await service.CreateAsync(Quote());
        await service.SendAsync(creado.Value.Id);
        await service.AcceptAsync(creado.Value.Id);

        var result = await service.GetByIdAsync(creado.Value.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.OrderId);
        Assert.Equal(EstadoPresupuesto.Aceptado, result.Value.Status);
    }

    // ------------------------------------------------------------------
    // Consultas
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetList_FiltersByStatus()
    {
        var (service, _, clientes, _, _) = CreateService();
        clientes.Seed();
        var primero = await service.CreateAsync(Quote());
        var segundo = await service.CreateAsync(Quote());
        await service.SendAsync(segundo.Value.Id);

        var result = await service.GetListAsync(
            page: 1, pageSize: 20, search: null, customerId: null,
            status: EstadoPresupuesto.Enviado, dateFrom: null, dateTo: null);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(segundo.Value.Id, item.Id);
        Assert.Equal(1, result.Value.TotalItems);
        Assert.NotEqual(primero.Value.Id, item.Id);
    }

    [Fact]
    public async Task GetList_FiltersByCustomer()
    {
        var (service, _, clientes, _, _) = CreateService();
        clientes.Seed();
        clientes.Seed(nombre: "Otro", apellido: "Cliente");
        await service.CreateAsync(Quote(customerId: 1));
        await service.CreateAsync(Quote(customerId: 2));

        var result = await service.GetListAsync(
            page: 1, pageSize: 20, search: null, customerId: 2,
            status: null, dateFrom: null, dateTo: null);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(2, item.CustomerId);
        Assert.Equal("Otro Cliente", item.CustomerName);
    }

    [Fact]
    public async Task GetList_InvalidDateRange_ReturnsValidation()
    {
        var (service, _, _, _, _) = CreateService();

        var result = await service.GetListAsync(
            page: 1, pageSize: 20, search: null, customerId: null,
            status: null,
            dateFrom: new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc),
            dateTo: new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetById_WithItems_ReturnsDetail()
    {
        var (service, _, clientes, _, _) = CreateService();
        clientes.Seed();
        var creado = await service.CreateAsync(Quote());

        var result = await service.GetByIdAsync(creado.Value.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(creado.Value.Id, result.Value.Id);
        Assert.Single(result.Value.Items);
        Assert.Equal("Cliente Demo", result.Value.CustomerName);
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNotFound()
    {
        var (service, _, _, _, _) = CreateService();

        var result = await service.GetByIdAsync(404);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetById_ExpiredQuote_ReportsExpiredFlag_Br064()
    {
        var (service, _, clientes, _, clock) = CreateService();
        clientes.Seed();
        var creado = await service.CreateAsync(Quote(
            expirationDate: new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc)));
        clock.Advance(TimeSpan.FromDays(6));

        var result = await service.GetByIdAsync(creado.Value.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Expired);
    }

    // ------------------------------------------------------------------
    // Notificaciones de negocio (BR-092)
    // ------------------------------------------------------------------

    [Fact]
    public async Task Send_PublishesQuoteSentNotificationForCustomer_Br092()
    {
        var clientes = new FakeClienteRepository();
        var productos = new FakeProductoRepository();
        var presupuestos = new FakePresupuestoRepository(clientes);
        var clock = new FakeClock();
        var notificaciones = new FakeNotificacionPublisher();
        var service = new PresupuestosService(
            presupuestos, clientes, productos, clock, notificaciones);

        clientes.Seed();
        var creado = await service.CreateAsync(Quote());

        await service.SendAsync(creado.Value.Id);

        var draft = Assert.Single(notificaciones.Publicados);
        Assert.Equal(TipoNotificacion.PresupuestoEnviado, draft.Tipo);
        Assert.Equal(creado.Value.CustomerId, draft.ClienteId);
        Assert.False(draft.ParaEquipo);
        Assert.Equal("Presupuesto", draft.EntidadTipo);
        Assert.Equal(creado.Value.Id, draft.EntidadId);
    }

    [Fact]
    public async Task Accept_PublishesCombinedNotificationForTeam_Br092()
    {
        var clientes = new FakeClienteRepository();
        var productos = new FakeProductoRepository();
        var presupuestos = new FakePresupuestoRepository(clientes);
        var clock = new FakeClock();
        var notificaciones = new FakeNotificacionPublisher();
        var service = new PresupuestosService(
            presupuestos, clientes, productos, clock, notificaciones);

        clientes.Seed();
        var creado = await service.CreateAsync(Quote());
        await service.SendAsync(creado.Value.Id);

        var result = await service.AcceptAsync(creado.Value.Id);

        Assert.True(result.IsSuccess);
        var draft = Assert.Single(
            notificaciones.Publicados,
            d => d.Tipo == TipoNotificacion.PresupuestoAceptado);
        Assert.True(draft.ParaEquipo);
        Assert.Equal("Pedido", draft.EntidadTipo);
        Assert.Equal(result.Value.OrderId, draft.EntidadId);
    }

    [Fact]
    public async Task Send_WhenFailing_DoesNotPublishNotification()
    {
        var clientes = new FakeClienteRepository();
        var productos = new FakeProductoRepository();
        var presupuestos = new FakePresupuestoRepository(clientes);
        var clock = new FakeClock();
        var notificaciones = new FakeNotificacionPublisher();
        var service = new PresupuestosService(
            presupuestos, clientes, productos, clock, notificaciones);

        clientes.Seed();
        var creado = await service.CreateAsync(Quote());

        var result = await service.SendAsync(999);

        Assert.True(result.IsFailure);
        Assert.Empty(notificaciones.Publicados);
    }

    // ------------------------------------------------------------------
    // Fakes
    // ------------------------------------------------------------------

    private sealed class FakeClock : ITimeProvider
    {
        private DateTime _utcNow = new(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc);

        public DateTime UtcNow => _utcNow;

        public void Advance(TimeSpan delta) => _utcNow += delta;
    }

    private sealed class FakeClienteRepository : IClienteRepository
    {
        private readonly List<Cliente> _clientes = new();

        public Cliente Seed(
            string nombre = "Cliente",
            string apellido = "Demo",
            bool activo = true)
        {
            var cliente = Cliente.Crear(nombre, apellido, $"cliente{_clientes.Count + 1}@test.com");
            cliente.Id = _clientes.Count == 0 ? 1 : _clientes.Max(c => c.Id) + 1;
            if (!activo)
            {
                cliente.Desactivar();
            }

            _clientes.Add(cliente);
            return cliente;
        }

        public Cliente? FindById(int id) => _clientes.FirstOrDefault(c => c.Id == id);

        public Task<PagedResult<Cliente>> GetPagedAsync(
            int page, int pageSize, string? search, bool? activo,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<Cliente>());

        public Task<Cliente?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(FindById(id));

        public Task<Cliente?> GetByFirebaseUidAsync(
            string firebaseUid,
            CancellationToken cancellationToken = default)
            => Task.FromResult(string.IsNullOrWhiteSpace(firebaseUid)
                ? null
                : _clientes.FirstOrDefault(c => c.FirebaseUid == firebaseUid && c.Activo));

        public Task<Cliente?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return Task.FromResult(_clientes.FirstOrDefault(c => c.Email == normalized && c.Activo));
        }

        public Task<bool> EmailExistsAsync(string email, int? excludeId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<Cliente> AddAsync(Cliente cliente, CancellationToken cancellationToken = default)
        {
            cliente.Id = _clientes.Count == 0 ? 1 : _clientes.Max(c => c.Id) + 1;
            _clientes.Add(cliente);
            return Task.FromResult(cliente);
        }

        public Task UpdateAsync(Cliente cliente, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeProductoRepository : IProductoRepository
    {
        private readonly List<Producto> _productos = new();

        public Producto Seed(string nombre = "Producto", decimal precio = 100m)
        {
            var producto = Producto.Crear(
                nombre,
                codigoSku: $"SKU-{_productos.Count + 1:D4}",
                tipo: TipoProducto.Mueble,
                precio: precio);
            producto.Id = _productos.Count == 0 ? 1 : _productos.Max(p => p.Id) + 1;
            _productos.Add(producto);
            return producto;
        }

        public Task<PagedResult<Producto>> GetPagedAsync(
            int page, int pageSize, string? search, TipoProducto? tipo,
            string? categoria, bool? activo, bool? showroom, bool? lowStock,
            bool? isCustom, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<Producto>());

        public Task<Producto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(_productos.FirstOrDefault(p => p.Id == id));

        public Task<bool> SkuExistsAsync(string codigoSku, int? excludeId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<Producto> AddAsync(Producto producto, CancellationToken cancellationToken = default)
        {
            producto.Id = _productos.Count == 0 ? 1 : _productos.Max(p => p.Id) + 1;
            _productos.Add(producto);
            return Task.FromResult(producto);
        }

        public Task UpdateAsync(Producto producto, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakePresupuestoRepository : IPresupuestoRepository
    {
        private readonly List<Presupuesto> _presupuestos = new();

        private readonly FakeClienteRepository _clientes;

        public FakePresupuestoRepository(FakeClienteRepository clientes)
        {
            _clientes = clientes;
        }

        public List<Presupuesto> All => _presupuestos;

        private void AttachCliente(Presupuesto p)
        {
            var cliente = _clientes.FindById(p.ClienteId);
            if (cliente is not null)
            {
                // Pobla la navegación para que CustomerName funcione en el fake.
                typeof(Presupuesto)
                    .GetProperty(nameof(Presupuesto.Cliente))!
                    .SetValue(p, cliente);
            }
        }

        public Task<PagedResult<Presupuesto>> GetPagedAsync(
            int page, int pageSize, string? search, int? clienteId,
            EstadoPresupuesto? estado, DateTime? dateFrom, DateTime? dateTo,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Presupuesto> query = _presupuestos;

            if (clienteId.HasValue)
            {
                query = query.Where(p => p.ClienteId == clienteId.Value);
            }

            if (estado.HasValue)
            {
                query = query.Where(p => p.Estado == estado.Value);
            }

            if (dateFrom.HasValue)
            {
                var desde = DateTime.SpecifyKind(dateFrom.Value.Date, DateTimeKind.Utc);
                query = query.Where(p => p.FechaCreacion >= desde);
            }

            if (dateTo.HasValue)
            {
                var hasta = DateTime.SpecifyKind(dateTo.Value.Date, DateTimeKind.Utc).AddDays(1);
                query = query.Where(p => p.FechaCreacion < hasta);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                query = query.Where(p =>
                {
                    var c = _clientes.FindById(p.ClienteId);
                    return c is not null
                        && (c.Nombre.ToLower().Contains(term)
                            || c.Apellido.ToLower().Contains(term)
                            || c.Email.ToLower().Contains(term));
                });
            }

            var list = query.OrderByDescending(p => p.Id).ToList();
            foreach (var p in list)
            {
                AttachCliente(p);
            }

            return Task.FromResult(new PagedResult<Presupuesto>
            {
                Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = list.Count,
            });
        }

        public Task<Presupuesto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var presupuesto = _presupuestos.FirstOrDefault(p => p.Id == id);
            if (presupuesto is not null)
            {
                AttachCliente(presupuesto);
            }

            return Task.FromResult(presupuesto);
        }

        public Task<Presupuesto?> GetByIdReadOnlyAsync(int id, CancellationToken cancellationToken = default)
            => GetByIdAsync(id, cancellationToken);

        public Task<Presupuesto> AddAsync(Presupuesto presupuesto, CancellationToken cancellationToken = default)
        {
            presupuesto.Id = _presupuestos.Count == 0 ? 1 : _presupuestos.Max(p => p.Id) + 1;
            foreach (var detalle in presupuesto.Detalles)
            {
                detalle.Id = presupuesto.Detalles.ToList().IndexOf(detalle) + 1;
            }

            _presupuestos.Add(presupuesto);
            return Task.FromResult(presupuesto);
        }

        public Task UpdateAsync(Presupuesto presupuesto, CancellationToken cancellationToken = default)
        {
            // El objeto ya está en la lista y fue mutado en memoria
            // (equivalente al SaveChanges de EF con tracking).
            var index = _presupuestos.IndexOf(presupuesto);
            if (index < 0)
            {
                _presupuestos.Add(presupuesto);
            }

            return Task.CompletedTask;
        }

        /// <summary>Simula perder la carrera del CAS (BR-065).</summary>
        public bool FailNextAccept { get; set; }

        public List<Pedido> Pedidos { get; } = new();

        public Task<Pedido?> TryAcceptAndCreateOrderAsync(
            Presupuesto presupuesto,
            DateTime fechaUtc,
            CancellationToken cancellationToken = default)
        {
            // El CAS real mira la fila en BD (que aún dice Enviado); aquí la
            // carrera se simula con FailNextAccept o con el índice único.
            if (FailNextAccept)
            {
                FailNextAccept = false;
                return Task.FromResult<Pedido?>(null);
            }

            if (Pedidos.Any(p => p.PresupuestoId == presupuesto.Id))
            {
                return Task.FromResult<Pedido?>(null);
            }

            var pedido = Pedido.Crear(
                presupuesto.ClienteId,
                presupuesto.Id,
                presupuesto.Total,
                fechaUtc);
            pedido.Id = Pedidos.Count == 0 ? 1 : Pedidos.Max(p => p.Id) + 1;

            Pedidos.Add(pedido);
            return Task.FromResult<Pedido?>(pedido);
        }

        public Task<int?> GetOrderIdAsync(int presupuestoId, CancellationToken cancellationToken = default)
        {
            var pedido = Pedidos.FirstOrDefault(p => p.PresupuestoId == presupuestoId);
            return Task.FromResult<int?>(pedido?.Id);
        }
    }
}
