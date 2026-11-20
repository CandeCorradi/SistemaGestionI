using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Clientes;
using SistemaGestion.Application.Pagos;
using SistemaGestion.Application.Pedidos;
using SistemaGestion.Application.PortalClientes;
using SistemaGestion.Application.Presupuestos;
using SistemaGestion.Domain.Common;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;
using Xunit;

namespace SistemaGestion.Application.UnitTests;

/// <summary>
/// Reglas del Portal Cliente (roadmap Phase 20 — Client Portal + Customer
/// Isolation, functional-spec §23): resolución del cliente por Firebase
/// UID (BR-010), ownership estricto de recursos (404 para ajenos),
/// solicitud de presupuesto con ítem sin precio y aceptación atómica
/// reutilizada (BR-064/BR-065).
/// </summary>
public sealed class PortalClientesServiceTests
{
    private const string Uid = "uid-portal-cliente";

    private const string UidOtro = "uid-portal-otro";

    private static (PortalClientesService Service,
        FakeClienteRepository Clientes,
        FakePresupuestoRepository Presupuestos,
        FakePedidoRepository Pedidos,
        FakePagoRepository Pagos,
        FakeNotificacionPublisher Notificaciones)
        CreateService()
    {
        var clientes = new FakeClienteRepository();
        var productos = new FakeProductoRepository();
        var pedidos = new FakePedidoRepository();
        var presupuestos = new FakePresupuestoRepository(clientes, pedidos);
        var pagos = new FakePagoRepository(pedidos);
        var usuarios = new FakeUsuarioRepository();
        var clock = new FakeClock();
        var notificaciones = new FakeNotificacionPublisher();

        var service = new PortalClientesService(
            clientes,
            new ClientesService(clientes),
            new PresupuestosService(presupuestos, clientes, productos, clock, notificaciones),
            new PedidosService(pedidos, usuarios, clock, notificaciones),
            new PagosService(pagos, pedidos, usuarios, clock, notificaciones),
            pagos,
            notificaciones);

        return (service, clientes, presupuestos, pedidos, pagos, notificaciones);
    }

    private static Cliente SeedCliente(
        FakeClienteRepository clientes,
        string uid,
        string email)
        => clientes.Seed(email, firebaseUid: uid);

    // ------------------------------------------------------------------
    // Perfil (BR-010)
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetProfile_WithoutLinkedClient_ReturnsNotFound()
    {
        var (service, _, _, _, _, _) = CreateService();

        var result = await service.GetProfileAsync("uid-sin-vinculo");

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetProfile_WithNullUid_ReturnsNotFound()
    {
        var (service, _, _, _, _, _) = CreateService();

        var result = await service.GetProfileAsync(null);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetProfile_LinkedClient_ReturnsProfileData()
    {
        var (service, clientes, _, _, _, _) = CreateService();
        SeedCliente(clientes, Uid, "portal@test.com");

        var result = await service.GetProfileAsync(Uid);

        Assert.True(result.IsSuccess);
        Assert.Equal("portal@test.com", result.Value.Email);
        Assert.Equal("Cliente", result.Value.Nombre);
        Assert.Equal("Demo", result.Value.Apellido);
    }

    [Fact]
    public async Task UpdateProfile_PersistsContactData()
    {
        var (service, clientes, _, _, _, _) = CreateService();
        SeedCliente(clientes, Uid, "portal@test.com");

        var result = await service.UpdateProfileAsync(
            Uid,
            new UpdateClienteProfileDto(
                Nombre: "Nombre Nuevo",
                Apellido: null,
                Telefono: "123456789",
                Direccion: "Calle Falsa 123",
                Documento: null));

        Assert.True(result.IsSuccess);
        Assert.Equal("Nombre Nuevo", result.Value.Nombre);
        Assert.Equal("Demo", result.Value.Apellido);
        Assert.Equal("123456789", result.Value.Telefono);
        Assert.Equal("Calle Falsa 123", result.Value.Direccion);
        Assert.Equal("portal@test.com", result.Value.Email);
    }

    // ------------------------------------------------------------------
    // Ownership de presupuestos (Customer Isolation)
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetQuotes_ReturnsOnlyOwnQuotes()
    {
        var (service, clientes, presupuestos, _, _, _) = CreateService();
        var propio = SeedCliente(clientes, Uid, "propio@test.com");
        var ajeno = SeedCliente(clientes, UidOtro, "ajeno@test.com");
        presupuestos.SeedPropio(propio.Id, estado: EstadoPresupuesto.Enviado);
        presupuestos.SeedPropio(ajeno.Id, estado: EstadoPresupuesto.Enviado);

        var result = await service.GetQuotesAsync(
            Uid, page: 1, pageSize: 20, search: null, status: null);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(propio.Id, item.CustomerId);
    }

    [Fact]
    public async Task GetQuote_ForeignQuote_ReturnsNotFound()
    {
        var (service, clientes, presupuestos, _, _, _) = CreateService();
        SeedCliente(clientes, Uid, "propio@test.com");
        var ajeno = SeedCliente(clientes, UidOtro, "ajeno@test.com");
        var ajenoQuote = presupuestos.SeedPropio(ajeno.Id);

        var result = await service.GetQuoteAsync(Uid, ajenoQuote.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetQuote_UnknownId_ReturnsNotFound()
    {
        var (service, clientes, _, _, _, _) = CreateService();
        SeedCliente(clientes, Uid, "propio@test.com");

        var result = await service.GetQuoteAsync(Uid, 999);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    // ------------------------------------------------------------------
    // Solicitud de presupuesto desde el portal
    // ------------------------------------------------------------------

    [Fact]
    public async Task RequestQuote_WithoutDescription_ReturnsValidation()
    {
        var (service, clientes, presupuestos, _, _, _) = CreateService();
        SeedCliente(clientes, Uid, "propio@test.com");

        var result = await service.RequestQuoteAsync(
            Uid, new RequestQuoteDto(Description: "   ", Cantidad: 1));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
        Assert.Empty(presupuestos.All);
    }

    [Fact]
    public async Task RequestQuote_WithNonPositiveQuantity_ReturnsValidation()
    {
        var (service, clientes, presupuestos, _, _, _) = CreateService();
        SeedCliente(clientes, Uid, "propio@test.com");

        var result = await service.RequestQuoteAsync(
            Uid, new RequestQuoteDto(Description: "Mueble a medida", Cantidad: 0));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
        Assert.Empty(presupuestos.All);
    }

    [Fact]
    public async Task RequestQuote_WithoutLinkedClient_ReturnsNotFound()
    {
        var (service, _, presupuestos, _, _, _) = CreateService();

        var result = await service.RequestQuoteAsync(
            "uid-sin-vinculo", new RequestQuoteDto("Mueble a medida", 1));

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
        Assert.Empty(presupuestos.All);
    }

    [Fact]
    public async Task RequestQuote_CreatesDraftWithZeroPriceAndNotifiesTeam()
    {
        var (service, clientes, presupuestos, _, _, notificaciones) = CreateService();
        var cliente = SeedCliente(clientes, Uid, "propio@test.com");

        var result = await service.RequestQuoteAsync(
            Uid, new RequestQuoteDto("Mueble a medida 2x1", 2));

        Assert.True(result.IsSuccess);
        Assert.Equal(EstadoPresupuesto.Borrador, result.Value.Status);
        Assert.Equal(cliente.Id, result.Value.CustomerId);
        var item = Assert.Single(result.Value.Items);
        Assert.Null(item.ProductId);
        Assert.Equal("Mueble a medida 2x1", item.Description);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(0m, item.UnitPrice);
        Assert.Equal(0m, result.Value.Total);

        var draft = Assert.Single(notificaciones.Publicados);
        Assert.Equal(TipoNotificacion.PresupuestoSolicitado, draft.Tipo);
        Assert.True(draft.ParaEquipo);
        Assert.Equal("Presupuesto", draft.EntidadTipo);
        Assert.Equal(result.Value.Id, draft.EntidadId);
    }

    // ------------------------------------------------------------------
    // Aceptación desde el portal (BR-064/BR-065)
    // ------------------------------------------------------------------

    [Fact]
    public async Task AcceptQuote_ForeignQuote_ReturnsNotFoundAndCreatesNoOrder()
    {
        var (service, clientes, presupuestos, pedidos, _, _) = CreateService();
        SeedCliente(clientes, Uid, "propio@test.com");
        var ajeno = SeedCliente(clientes, UidOtro, "ajeno@test.com");
        var ajenoQuote = presupuestos.SeedPropio(
            ajeno.Id, estado: EstadoPresupuesto.Enviado);

        var result = await service.AcceptQuoteAsync(Uid, ajenoQuote.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
        Assert.Empty(pedidos.All);
        Assert.Equal(EstadoPresupuesto.Enviado, ajenoQuote.Estado);
    }

    [Fact]
    public async Task AcceptQuote_OwnSentQuote_CreatesOrderAtomically()
    {
        var (service, clientes, presupuestos, pedidos, _, notificaciones) = CreateService();
        var cliente = SeedCliente(clientes, Uid, "propio@test.com");
        var quote = presupuestos.SeedPropio(
            cliente.Id, estado: EstadoPresupuesto.Enviado, total: 300m);

        var result = await service.AcceptQuoteAsync(Uid, quote.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(EstadoPresupuesto.Aceptado, quote.Estado);
        Assert.NotNull(result.Value.OrderId);
        var pedido = Assert.Single(pedidos.All);
        Assert.Equal(300m, pedido.Total);
        Assert.Contains(
            notificaciones.Publicados,
            d => d.Tipo == TipoNotificacion.PresupuestoAceptado);
    }

    [Fact]
    public async Task AcceptQuote_DraftQuote_ReturnsConflict()
    {
        var (service, clientes, presupuestos, pedidos, _, _) = CreateService();
        var cliente = SeedCliente(clientes, Uid, "propio@test.com");
        var quote = presupuestos.SeedPropio(cliente.Id);

        var result = await service.AcceptQuoteAsync(Uid, quote.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
        Assert.Empty(pedidos.All);
    }

    // ------------------------------------------------------------------
    // Pedidos y pagos (Customer Isolation)
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetOrders_ReturnsOnlyOwnOrders()
    {
        var (service, clientes, _, pedidos, _, _) = CreateService();
        var propio = SeedCliente(clientes, Uid, "propio@test.com");
        var ajeno = SeedCliente(clientes, UidOtro, "ajeno@test.com");
        pedidos.Seed(propio.Id, 100m);
        pedidos.Seed(ajeno.Id, 200m);

        var result = await service.GetOrdersAsync(
            Uid, page: 1, pageSize: 20, search: null, status: null);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(propio.Id, item.CustomerId);
        Assert.Equal(100m, item.Total);
    }

    [Fact]
    public async Task GetOrder_ForeignOrder_ReturnsNotFound()
    {
        var (service, clientes, _, pedidos, _, _) = CreateService();
        SeedCliente(clientes, Uid, "propio@test.com");
        var ajeno = SeedCliente(clientes, UidOtro, "ajeno@test.com");
        var ajenoPedido = pedidos.Seed(ajeno.Id, 200m);

        var result = await service.GetOrderAsync(Uid, ajenoPedido.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetOrderHistory_ForeignOrder_ReturnsNotFound()
    {
        var (service, clientes, _, pedidos, _, _) = CreateService();
        SeedCliente(clientes, Uid, "propio@test.com");
        var ajeno = SeedCliente(clientes, UidOtro, "ajeno@test.com");
        var ajenoPedido = pedidos.Seed(ajeno.Id, 200m);

        var result = await service.GetOrderHistoryAsync(Uid, ajenoPedido.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetOrderHistory_OwnOrder_ReturnsEntries()
    {
        var (service, clientes, _, pedidos, _, _) = CreateService();
        var propio = SeedCliente(clientes, Uid, "propio@test.com");
        var pedido = pedidos.Seed(propio.Id, 100m);

        var result = await service.GetOrderHistoryAsync(Uid, pedido.Id);

        Assert.True(result.IsSuccess);
        var fila = Assert.Single(result.Value);
        Assert.Equal(EstadoPedido.Pendiente, fila.NewStatus);
    }

    [Fact]
    public async Task GetPayments_ReturnsOnlyOwnPayments()
    {
        var (service, clientes, _, pedidos, pagos, _) = CreateService();
        var propio = SeedCliente(clientes, Uid, "propio@test.com");
        var ajeno = SeedCliente(clientes, UidOtro, "ajeno@test.com");
        var pedidoPropio = pedidos.Seed(propio.Id, 100m);
        var pedidoAjeno = pedidos.Seed(ajeno.Id, 200m);
        pagos.Seed(pedidoPropio.Id, 50m);
        pagos.Seed(pedidoAjeno.Id, 60m);

        var result = await service.GetPaymentsAsync(Uid, page: 1, pageSize: 20);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(50m, item.Amount);
        Assert.Equal(pedidoPropio.Id, item.OrderId);
    }

    // ------------------------------------------------------------------
    // Resumen / saldo
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetSummary_ComputesBalanceExcludingCancelledOrders()
    {
        var (service, clientes, _, pedidos, pagos, _) = CreateService();
        var propio = SeedCliente(clientes, Uid, "propio@test.com");
        var pedido = pedidos.Seed(propio.Id, 300m);
        pedidos.Seed(propio.Id, 150m, EstadoPedido.Cancelado);
        pagos.Seed(pedido.Id, 100m);

        var result = await service.GetSummaryAsync(Uid);

        Assert.True(result.IsSuccess);
        Assert.Equal(300m, result.Value.TotalPedidos);
        Assert.Equal(100m, result.Value.TotalPagado);
        Assert.Equal(200m, result.Value.Saldo);

        // PedidosCount es el total de pedidos del cliente (incluye cancelados).
        Assert.Equal(2, result.Value.PedidosCount);
        Assert.Equal(0, result.Value.PresupuestosPendientes);
    }

    [Fact]
    public async Task GetSummary_CountsSentQuotesAsPending()
    {
        var (service, clientes, presupuestos, pedidos, _, _) = CreateService();
        var propio = SeedCliente(clientes, Uid, "propio@test.com");
        presupuestos.SeedPropio(propio.Id, estado: EstadoPresupuesto.Enviado);
        presupuestos.SeedPropio(propio.Id, estado: EstadoPresupuesto.Enviado);
        presupuestos.SeedPropio(propio.Id, estado: EstadoPresupuesto.Borrador);
        pedidos.Seed(propio.Id, 100m);

        var result = await service.GetSummaryAsync(Uid);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.PresupuestosPendientes);
        Assert.Equal(1, result.Value.PedidosCount);
    }

    [Fact]
    public async Task GetSummary_WithoutLinkedClient_ReturnsNotFound()
    {
        var (service, _, _, _, _, _) = CreateService();

        var result = await service.GetSummaryAsync("uid-sin-vinculo");

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    // ------------------------------------------------------------------
    // Fakes
    // ------------------------------------------------------------------

    private sealed class FakeClock : ITimeProvider
    {
        private DateTime _utcNow = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

        public DateTime UtcNow => _utcNow;

        public void Advance(TimeSpan delta) => _utcNow += delta;
    }

    private sealed class FakeUsuarioRepository : IUsuarioRepository
    {
        public Task<Usuario?> GetByFirebaseUidAsync(
            string firebaseUid, CancellationToken cancellationToken = default)
            => Task.FromResult<Usuario?>(null);

        public Task<PagedResult<Usuario>> GetPagedAsync(
            int page, int pageSize, string? search, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<Usuario>());

        public Task<Usuario?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<Usuario?>(null);

        public Task<Usuario?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Usuario?>(null);

        public Task<bool> EmailExistsAsync(
            string email, int? excludeId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<Usuario> AddAsync(Usuario usuario, CancellationToken cancellationToken = default)
            => Task.FromResult(usuario);

        public Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<int>> GetActiveStaffUserIdsAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<int>>(Array.Empty<int>());
    }

    private sealed class FakeClienteRepository : IClienteRepository
    {
        private readonly List<Cliente> _clientes = new();

        public Cliente Seed(
            string email,
            string nombre = "Cliente",
            string apellido = "Demo",
            string? firebaseUid = null)
        {
            var cliente = Cliente.Crear(nombre, apellido, email, firebaseUid: firebaseUid);
            cliente.Id = _clientes.Count == 0 ? 1 : _clientes.Max(c => c.Id) + 1;
            _clientes.Add(cliente);
            return cliente;
        }

        public Task<PagedResult<Cliente>> GetPagedAsync(
            int page, int pageSize, string? search, bool? activo,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<Cliente>());

        public Task<Cliente?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(_clientes.FirstOrDefault(c => c.Id == id));

        public Task<Cliente?> GetByFirebaseUidAsync(
            string firebaseUid, CancellationToken cancellationToken = default)
            => Task.FromResult(string.IsNullOrWhiteSpace(firebaseUid)
                ? null
                : _clientes.FirstOrDefault(c => c.FirebaseUid == firebaseUid && c.Activo));

        public Task<Cliente?> GetByEmailAsync(
            string email, CancellationToken cancellationToken = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return Task.FromResult(_clientes.FirstOrDefault(
                c => c.Email == normalized && c.Activo));
        }

        public Task<bool> EmailExistsAsync(
            string email, int? excludeId = null, CancellationToken cancellationToken = default)
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
        public Task<PagedResult<Producto>> GetPagedAsync(
            int page, int pageSize, string? search, TipoProducto? tipo,
            string? categoria, bool? activo, bool? showroom, bool? lowStock,
            bool? isCustom, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<Producto>());

        public Task<Producto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<Producto?>(null);

        public Task<bool> SkuExistsAsync(
            string codigoSku, int? excludeId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<Producto> AddAsync(Producto producto, CancellationToken cancellationToken = default)
            => Task.FromResult(producto);

        public Task UpdateAsync(Producto producto, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakePresupuestoRepository : IPresupuestoRepository
    {
        private readonly List<Presupuesto> _presupuestos = new();

        private readonly FakeClienteRepository _clientes;

        private readonly FakePedidoRepository _pedidos;

        public FakePresupuestoRepository(
            FakeClienteRepository clientes,
            FakePedidoRepository pedidos)
        {
            _clientes = clientes;
            _pedidos = pedidos;
        }

        public List<Presupuesto> All => _presupuestos;

        public Presupuesto SeedPropio(
            int clienteId,
            EstadoPresupuesto estado = EstadoPresupuesto.Borrador,
            decimal total = 100m)
        {
            var presupuesto = Presupuesto.Crear(
                clienteId, fechaValidezUtc: null, notas: null,
                condiciones: null, fechaCreacionUtc: DateTime.UtcNow);
            presupuesto.AgregarDetalle(PresupuestoDetalle.Crear(
                descripcion: "Mueble", cantidad: 1,
                precioUnitario: total, descuento: 0m));
            presupuesto.Id = _presupuestos.Count == 0
                ? 1
                : _presupuestos.Max(p => p.Id) + 1;

            if (estado == EstadoPresupuesto.Enviado)
            {
                presupuesto.Enviar(DateTime.UtcNow);
            }
            else if (estado == EstadoPresupuesto.Aceptado)
            {
                presupuesto.Enviar(DateTime.UtcNow);
                presupuesto.Aceptar(DateTime.UtcNow);
            }

            _presupuestos.Add(presupuesto);
            return presupuesto;
        }

        private void AttachCliente(Presupuesto p)
        {
            var cliente = _clientes.GetByIdAsync(p.ClienteId).GetAwaiter().GetResult();
            if (cliente is not null)
            {
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

        public Task<Presupuesto?> GetByIdAsync(
            int id, CancellationToken cancellationToken = default)
        {
            var presupuesto = _presupuestos.FirstOrDefault(p => p.Id == id);
            if (presupuesto is not null)
            {
                AttachCliente(presupuesto);
            }

            return Task.FromResult(presupuesto);
        }

        public Task<Presupuesto?> GetByIdReadOnlyAsync(
            int id, CancellationToken cancellationToken = default)
            => GetByIdAsync(id, cancellationToken);

        public Task<Presupuesto> AddAsync(
            Presupuesto presupuesto, CancellationToken cancellationToken = default)
        {
            presupuesto.Id = _presupuestos.Count == 0
                ? 1
                : _presupuestos.Max(p => p.Id) + 1;
            _presupuestos.Add(presupuesto);
            return Task.FromResult(presupuesto);
        }

        public Task UpdateAsync(
            Presupuesto presupuesto, CancellationToken cancellationToken = default)
        {
            if (!_presupuestos.Contains(presupuesto))
            {
                _presupuestos.Add(presupuesto);
            }

            return Task.CompletedTask;
        }

        public Task<Pedido?> TryAcceptAndCreateOrderAsync(
            Presupuesto presupuesto,
            DateTime fechaUtc,
            CancellationToken cancellationToken = default)
        {
            if (_pedidos.All.Any(p => p.PresupuestoId == presupuesto.Id))
            {
                return Task.FromResult<Pedido?>(null);
            }

            var pedido = Pedido.Crear(
                presupuesto.ClienteId, presupuesto.Id, presupuesto.Total, fechaUtc);
            _pedidos.Add(pedido);
            return Task.FromResult<Pedido?>(pedido);
        }

        public Task<int?> GetOrderIdAsync(
            int presupuestoId, CancellationToken cancellationToken = default)
        {
            var pedido = _pedidos.All.FirstOrDefault(p => p.PresupuestoId == presupuestoId);
            return Task.FromResult<int?>(pedido?.Id);
        }
    }

    private sealed class FakePedidoRepository : IPedidoRepository
    {
        private readonly List<Pedido> _pedidos = new();

        private readonly List<PedidoHistorial> _historial = new();

        public IReadOnlyList<Pedido> All => _pedidos;

        /// <summary>Agrega un pedido creado fuera del fake (p. ej. por la aceptación).</summary>
        public Pedido Add(Pedido pedido)
        {
            pedido.Id = _pedidos.Count == 0 ? 1 : _pedidos.Max(p => p.Id) + 1;
            _pedidos.Add(pedido);
            return pedido;
        }

        public Pedido Seed(
            int clienteId,
            decimal total,
            EstadoPedido estadoInicial = EstadoPedido.Pendiente)
        {
            var pedido = Add(Pedido.Crear(clienteId, 1, total, DateTime.UtcNow));

            if (estadoInicial == EstadoPedido.Cancelado)
            {
                pedido.Cancelar();
            }

            _historial.Add(PedidoHistorial.Crear(
                pedido.Id,
                estadoAnterior: null,
                estadoNuevo: EstadoPedido.Pendiente,
                fechaUtc: DateTime.UtcNow,
                usuarioId: null,
                comentario: "Creado"));
            return pedido;
        }

        public Task<PagedResult<Pedido>> GetPagedAsync(
            int page, int pageSize, string? search, int? clienteId,
            EstadoPedido? estado, DateTime? dateFrom, DateTime? dateTo,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Pedido> query = _pedidos;

            if (clienteId.HasValue)
            {
                query = query.Where(p => p.ClienteId == clienteId.Value);
            }

            if (estado.HasValue)
            {
                query = query.Where(p => p.Estado == estado.Value);
            }

            var list = query.OrderByDescending(p => p.Id).ToList();
            return Task.FromResult(new PagedResult<Pedido>
            {
                Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = list.Count,
            });
        }

        public Task<Pedido?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(_pedidos.FirstOrDefault(p => p.Id == id));

        public Task<Pedido?> GetByIdReadOnlyAsync(
            int id, CancellationToken cancellationToken = default)
            => GetByIdAsync(id, cancellationToken);

        public Task<IReadOnlyList<PedidoHistorial>> GetHistorialAsync(
            int pedidoId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PedidoHistorial>>(
                _historial.Where(h => h.PedidoId == pedidoId).ToList());

        public Task UpdateWithHistoryAsync(
            Pedido pedido, PedidoHistorial historial, CancellationToken cancellationToken = default)
        {
            _historial.Add(historial);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Pedido pedido, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakePagoRepository : IPagoRepository
    {
        private readonly List<Pago> _pagos = new();

        private readonly FakePedidoRepository _pedidos;

        private int _nextId = 1;

        public FakePagoRepository(FakePedidoRepository pedidos)
        {
            _pedidos = pedidos;
        }

        public Pago Seed(int pedidoId, decimal monto)
        {
            var pago = Pago.Crear(
                pedidoId, monto, DateTime.UtcNow, MetodoPago.Efectivo,
                referencia: null, notas: null, usuarioId: null);
            pago.Id = _nextId++;
            _pagos.Add(pago);
            return pago;
        }

        public Task<decimal> GetTotalPagadoAsync(
            int pedidoId, CancellationToken cancellationToken = default)
            => Task.FromResult(_pagos.Where(p => p.PedidoId == pedidoId).Sum(p => p.Monto));

        public Task AddAsync(Pago pago, CancellationToken cancellationToken = default)
        {
            pago.Id = _nextId++;
            _pagos.Add(pago);
            return Task.CompletedTask;
        }

        public async Task ExecuteSerializableAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
            => await operation(cancellationToken);

        public Task<IReadOnlyList<Pago>> GetByPedidoAsync(
            int pedidoId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Pago>>(_pagos
                .Where(p => p.PedidoId == pedidoId)
                .OrderByDescending(p => p.FechaPago)
                .ToList());

        public Task<PagedResult<Pago>> GetPagedAsync(
            int page, int pageSize, int? pedidoId, int? clienteId,
            DateTime? dateFrom, DateTime? dateTo, string? search,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Pago> query = _pagos;

            if (pedidoId.HasValue)
            {
                query = query.Where(p => p.PedidoId == pedidoId.Value);
            }

            if (clienteId.HasValue)
            {
                var ids = _pedidos.All
                    .Where(p => p.ClienteId == clienteId.Value)
                    .Select(p => p.Id)
                    .ToHashSet();
                query = query.Where(p => ids.Contains(p.PedidoId));
            }

            var list = query.OrderByDescending(p => p.FechaPago).ToList();
            return Task.FromResult(new PagedResult<Pago>
            {
                Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = list.Count,
            });
        }

        public Task<ClientePagosResumen> GetResumenClienteAsync(
            int clienteId, CancellationToken cancellationToken = default)
        {
            var pedidosDelCliente = _pedidos.All
                .Where(p => p.ClienteId == clienteId)
                .ToList();

            var totalPedidos = pedidosDelCliente
                .Where(p => p.Estado != EstadoPedido.Cancelado)
                .Sum(p => p.Total);

            var ids = pedidosDelCliente.Select(p => p.Id).ToHashSet();
            var totalPagado = _pagos
                .Where(p => ids.Contains(p.PedidoId))
                .Sum(p => p.Monto);

            return Task.FromResult(new ClientePagosResumen(totalPedidos, totalPagado));
        }
    }
}
