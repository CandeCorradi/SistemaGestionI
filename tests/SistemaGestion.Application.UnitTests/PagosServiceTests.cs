using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Pagos;
using SistemaGestion.Application.Pagos.Dtos;
using SistemaGestion.Domain.Common;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;
using Xunit;

namespace SistemaGestion.Application.UnitTests;

/// <summary>
/// Reglas de pagos (BR-080..BR-083, functional-spec §17): registro con
/// validación de saldo, historial inmutable y saldo calculado por backend.
/// </summary>
public sealed class PagosServiceTests
{
    private const string Uid = "uid-caja-1";

    private static (PagosService Service,
        FakePedidoRepository Pedidos,
        FakePagoRepository Pagos,
        FakeUsuarioRepository Usuarios,
        FakeClock Clock)
        CreateService()
    {
        var pedidos = new FakePedidoRepository();
        var pagos = new FakePagoRepository();
        var usuarios = new FakeUsuarioRepository();
        usuarios.Seed(Uid, "Ana", "García");
        var clock = new FakeClock();
        var notificaciones = new FakeNotificacionPublisher();

        var service = new PagosService(pagos, pedidos, usuarios, clock, notificaciones);
        return (service, pedidos, pagos, usuarios, clock);
    }

    // ------------------------------------------------------------------
    // Registro (BR-080/BR-081)
    // ------------------------------------------------------------------

    [Fact]
    public async Task Register_Valid_PersistsPaymentWithUserAndClock_Br080()
    {
        var (service, pedidos, pagos, _, clock) = CreateService();
        var pedido = pedidos.Seed(pedidos.SeedCliente("ok@test.com"), 100m);

        var result = await service.RegisterAsync(
            Uid, pedido.Id,
            new RegisterPaymentDto(30m, MetodoPago.Transferencia, "  TRX-1  ", " Señia "));

        Assert.True(result.IsSuccess);
        Assert.Equal(30m, result.Value.Amount);
        Assert.Equal(pedido.Id, result.Value.OrderId);
        Assert.Equal(MetodoPago.Transferencia, result.Value.PaymentMethod);
        Assert.Equal("TRX-1", result.Value.Reference);
        Assert.Equal("Señia", result.Value.Notes);
        Assert.Equal("Ana García", result.Value.UserName);
        Assert.Equal(DateTimeKind.Utc, result.Value.Date.Kind);

        var guardado = Assert.Single(pagos.All);
        Assert.Equal(30m, guardado.Monto);
        Assert.NotNull(guardado.UsuarioId);
        Assert.Equal(clock.UtcNow, guardado.FechaPago);
    }

    [Fact]
    public async Task Register_UnknownOrder_ReturnsNotFound()
    {
        var (service, _, _, _, _) = CreateService();

        var result = await service.RegisterAsync(
            Uid, 99, new RegisterPaymentDto(10m, MetodoPago.Efectivo, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task Register_InvalidOrderId_ReturnsValidation()
    {
        var (service, _, _, _, _) = CreateService();

        var result = await service.RegisterAsync(
            Uid, 0, new RegisterPaymentDto(10m, MetodoPago.Efectivo, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task Register_UnknownUser_ReturnsNotFound()
    {
        var (service, pedidos, pagos, _, _) = CreateService();
        var pedido = pedidos.Seed(pedidos.SeedCliente("nouser@test.com"), 100m);
        var previos = pagos.All.Count;

        var result = await service.RegisterAsync(
            "uid-inexistente", pedido.Id,
            new RegisterPaymentDto(10m, MetodoPago.Efectivo, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
        Assert.Equal(previos, pagos.All.Count);
    }

    [Fact]
    public async Task Register_ZeroAmount_ReturnsValidation_Br080()
    {
        var (service, pedidos, pagos, _, _) = CreateService();
        var pedido = pedidos.Seed(pedidos.SeedCliente("zero@test.com"), 100m);

        var result = await service.RegisterAsync(
            Uid, pedido.Id, new RegisterPaymentDto(0m, MetodoPago.Efectivo, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
        Assert.Empty(pagos.All);
    }

    [Fact]
    public async Task Register_ThreeDecimalsAmount_ReturnsValidation()
    {
        var (service, pedidos, pagos, _, _) = CreateService();
        var pedido = pedidos.Seed(pedidos.SeedCliente("dec@test.com"), 100m);

        var result = await service.RegisterAsync(
            Uid, pedido.Id, new RegisterPaymentDto(10.123m, MetodoPago.Efectivo, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
        Assert.Empty(pagos.All);
    }

    [Fact]
    public async Task Register_UndefinedMethod_ReturnsValidation()
    {
        var (service, pedidos, _, _, _) = CreateService();
        var pedido = pedidos.Seed(pedidos.SeedCliente("method@test.com"), 100m);

        var result = await service.RegisterAsync(
            Uid, pedido.Id, new RegisterPaymentDto(10m, (MetodoPago)99, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task Register_Overpay_ReturnsConflict_NothingPersisted_Br081()
    {
        var (service, pedidos, pagos, _, _) = CreateService();
        var pedido = pedidos.Seed(pedidos.SeedCliente("over@test.com"), 100m);

        var primero = await service.RegisterAsync(
            Uid, pedido.Id, new RegisterPaymentDto(60m, MetodoPago.Efectivo, null, null));
        Assert.True(primero.IsSuccess);

        var segundo = await service.RegisterAsync(
            Uid, pedido.Id, new RegisterPaymentDto(50m, MetodoPago.Efectivo, null, null));

        Assert.True(segundo.IsFailure);
        Assert.Equal("Conflict.Error", segundo.Error.Code);
        Assert.Contains("BR-081", segundo.Error.Description);
        Assert.Single(pagos.All);
    }

    [Fact]
    public async Task Register_ExactBalance_Ok_ThenNextRejected_Br083()
    {
        var (service, pedidos, _, _, _) = CreateService();
        var pedido = pedidos.Seed(pedidos.SeedCliente("exact@test.com"), 100m);

        var exacto = await service.RegisterAsync(
            Uid, pedido.Id, new RegisterPaymentDto(100m, MetodoPago.Tarjeta, null, null));
        Assert.True(exacto.IsSuccess);

        var excedente = await service.RegisterAsync(
            Uid, pedido.Id, new RegisterPaymentDto(1m, MetodoPago.Efectivo, null, null));

        Assert.True(excedente.IsFailure);
        Assert.Equal("Conflict.Error", excedente.Error.Code);
    }

    [Fact]
    public async Task Register_CancelledOrder_ReturnsConflict()
    {
        var (service, pedidos, pagos, _, _) = CreateService();
        var pedido = pedidos.Seed(
            pedidos.SeedCliente("cancelada@test.com"), 100m, EstadoPedido.Cancelado);

        var result = await service.RegisterAsync(
            Uid, pedido.Id, new RegisterPaymentDto(10m, MetodoPago.Efectivo, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
        Assert.Empty(pagos.All);
    }

    [Fact]
    public async Task Register_MultiplePayments_Accumulate_Br083()
    {
        var (service, pedidos, pagos, _, _) = CreateService();
        var pedido = pedidos.Seed(pedidos.SeedCliente("multi@test.com"), 300m);

        foreach (var monto in new[] { 100m, 100m, 100m })
        {
            var paso = await service.RegisterAsync(
                Uid, pedido.Id, new RegisterPaymentDto(monto, MetodoPago.Efectivo, null, null));
            Assert.True(paso.IsSuccess);
        }

        Assert.Equal(3, pagos.All.Count);
        Assert.Equal(300m, pagos.All.Sum(p => p.Monto));
    }

    // ------------------------------------------------------------------
    // Saldo por pedido (BR-083)
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetOrderPayments_ReturnsItemsTotalAndBalance_Br083()
    {
        var (service, pedidos, pagos, _, _) = CreateService();
        var pedido = pedidos.Seed(pedidos.SeedCliente("saldo@test.com"), 300m);
        await service.RegisterAsync(
            Uid, pedido.Id, new RegisterPaymentDto(100m, MetodoPago.Efectivo, null, null));
        await service.RegisterAsync(
            Uid, pedido.Id, new RegisterPaymentDto(50m, MetodoPago.Efectivo, null, null));

        var result = await service.GetOrderPaymentsAsync(pedido.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(150m, result.Value.TotalPaid);
        Assert.Equal(150m, result.Value.Balance);
        Assert.Equal("Cliente Test", result.Value.Items[0].CustomerName);
    }

    [Fact]
    public async Task GetOrderPayments_NoPayments_ZeroPaid_FullBalance()
    {
        var (service, pedidos, _, _, _) = CreateService();
        var pedido = pedidos.Seed(pedidos.SeedCliente("sinpagos@test.com"), 250m);

        var result = await service.GetOrderPaymentsAsync(pedido.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0m, result.Value.TotalPaid);
        Assert.Equal(250m, result.Value.Balance);
    }

    [Fact]
    public async Task GetOrderPayments_Unknown_ReturnsNotFound()
    {
        var (service, _, _, _, _) = CreateService();

        var result = await service.GetOrderPaymentsAsync(99);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetOrderPayments_InvalidId_ReturnsValidation()
    {
        var (service, _, _, _, _) = CreateService();

        var result = await service.GetOrderPaymentsAsync(0);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    // ------------------------------------------------------------------
    // Listado global (AGENTS.md §30)
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetList_FilterByOrderId_ReturnsOnlyMatch()
    {
        var (service, pedidos, _, _, _) = CreateService();
        var pedidoA = pedidos.Seed(pedidos.SeedCliente("a@test.com"), 100m);
        var pedidoB = pedidos.Seed(pedidos.SeedCliente("b@test.com"), 100m);
        await service.RegisterAsync(
            Uid, pedidoA.Id, new RegisterPaymentDto(10m, MetodoPago.Efectivo, "A-1", null));
        await service.RegisterAsync(
            Uid, pedidoB.Id, new RegisterPaymentDto(20m, MetodoPago.Efectivo, "B-1", null));

        var result = await service.GetListAsync(
            page: 1, pageSize: 20, orderId: pedidoA.Id, customerId: null,
            dateFrom: null, dateTo: null, search: null);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(pedidoA.Id, item.OrderId);
        Assert.Equal("A-1", item.Reference);
    }

    [Fact]
    public async Task GetList_SearchOnReference_ReturnsMatch()
    {
        var (service, pedidos, _, _, _) = CreateService();
        var pedido = pedidos.Seed(pedidos.SeedCliente("search@test.com"), 100m);
        await service.RegisterAsync(
            Uid, pedido.Id, new RegisterPaymentDto(10m, MetodoPago.Efectivo, "TRX-BUSCAR", null));
        await service.RegisterAsync(
            Uid, pedido.Id, new RegisterPaymentDto(10m, MetodoPago.Efectivo, "OTRA", null));

        var result = await service.GetListAsync(
            1, 20, null, null, null, null, "trx-buscar");

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal("TRX-BUSCAR", item.Reference);
    }

    [Fact]
    public async Task GetList_InvalidDateRange_ReturnsValidation()
    {
        var (service, _, _, _, _) = CreateService();

        var result = await service.GetListAsync(
            1, 20, null, null,
            dateFrom: new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc),
            dateTo: new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            search: null);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetList_InvalidOrderId_ReturnsValidation()
    {
        var (service, _, _, _, _) = CreateService();

        var result = await service.GetListAsync(1, 20, orderId: 0, null, null, null, null);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetList_PageSizeIsClampedToMax()
    {
        var (service, pedidos, _, _, _) = CreateService();
        var pedido = pedidos.Seed(pedidos.SeedCliente("clamp@test.com"), 100m);
        await service.RegisterAsync(
            Uid, pedido.Id, new RegisterPaymentDto(10m, MetodoPago.Efectivo, null, null));

        var result = await service.GetListAsync(
            1, pageSize: 5000, null, null, null, null, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(PagosService.MaxPageSize, result.Value.PageSize);
    }

    // ------------------------------------------------------------------
    // Notificaciones de negocio (BR-092)
    // ------------------------------------------------------------------

    [Fact]
    public async Task Register_PublishesPaymentNotificationForTeam_Br092()
    {
        var pedidos = new FakePedidoRepository();
        var pagos = new FakePagoRepository();
        var usuarios = new FakeUsuarioRepository();
        usuarios.Seed(Uid, "Ana", "García");
        var clock = new FakeClock();
        var notificaciones = new FakeNotificacionPublisher();
        var service = new PagosService(pagos, pedidos, usuarios, clock, notificaciones);

        var pedido = pedidos.Seed(pedidos.SeedCliente("pago-notif@test.com"), 100m);

        var result = await service.RegisterAsync(
            Uid,
            pedido.Id,
            new RegisterPaymentDto(40m, MetodoPago.Efectivo, null, null));

        Assert.True(result.IsSuccess);
        var draft = Assert.Single(notificaciones.Publicados);
        Assert.Equal(TipoNotificacion.PagoRegistrado, draft.Tipo);
        Assert.True(draft.ParaEquipo);
        Assert.Equal("Pedido", draft.EntidadTipo);
        Assert.Equal(pedido.Id, draft.EntidadId);
    }

    [Fact]
    public async Task Register_WhenFailing_DoesNotPublishNotification()
    {
        var pedidos = new FakePedidoRepository();
        var pagos = new FakePagoRepository();
        var usuarios = new FakeUsuarioRepository();
        usuarios.Seed(Uid, "Ana", "García");
        var clock = new FakeClock();
        var notificaciones = new FakeNotificacionPublisher();
        var service = new PagosService(pagos, pedidos, usuarios, clock, notificaciones);

        var pedido = pedidos.Seed(pedidos.SeedCliente("pago-fail@test.com"), 100m);

        var result = await service.RegisterAsync(
            Uid,
            pedido.Id,
            new RegisterPaymentDto(500m, MetodoPago.Efectivo, null, null));

        Assert.True(result.IsFailure);
        Assert.Empty(notificaciones.Publicados);
    }

    // ------------------------------------------------------------------
    // Fakes
    // ------------------------------------------------------------------

    private sealed class FakeClock : ITimeProvider
    {
        private DateTime _utcNow = new(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);

        public DateTime UtcNow => _utcNow;

        public void Advance(TimeSpan delta) => _utcNow += delta;
    }

    private sealed class FakeUsuarioRepository : IUsuarioRepository
    {
        public Dictionary<string, Usuario> PorUid { get; } = new();

        public Usuario Seed(string firebaseUid, string nombre, string apellido)
        {
            var usuario = Usuario.Crear(
                $"{firebaseUid}@test.com", nombre, apellido, rolId: 1, firebaseUid);
            usuario.Id = PorUid.Count + 1;
            PorUid[firebaseUid] = usuario;
            return usuario;
        }

        public Task<Usuario?> GetByFirebaseUidAsync(string firebaseUid, CancellationToken cancellationToken = default)
            => Task.FromResult(PorUid.GetValueOrDefault(firebaseUid));

        public Task<PagedResult<Usuario>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<Usuario>());

        public Task<Usuario?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<Usuario?>(null);

        public Task<Usuario?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Usuario?>(null);

        public Task<bool> EmailExistsAsync(string email, int? excludeId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<Usuario> AddAsync(Usuario usuario, CancellationToken cancellationToken = default)
            => Task.FromResult(usuario);

        public Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<int>> GetActiveStaffUserIdsAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<int>>(Array.Empty<int>());
    }

    private sealed class FakePedidoRepository : IPedidoRepository
    {
        private readonly List<Pedido> _pedidos = new();

        private int _nextId = 1;

        private int _nextClienteId = 1;

        public Cliente SeedCliente(string email)
        {
            var cliente = Cliente.Crear("Cliente", "Test", email);
            cliente.Id = _nextClienteId++;
            return cliente;
        }

        public Pedido Seed(
            Cliente cliente,
            decimal total,
            EstadoPedido estadoInicial = EstadoPedido.Pendiente)
        {
            var pedido = Pedido.Crear(cliente.Id, 1, total, DateTime.UtcNow);
            pedido.Id = _nextId++;

            typeof(Pedido).GetProperty(nameof(Pedido.Cliente))!
                .SetValue(pedido, cliente);

            if (estadoInicial == EstadoPedido.Cancelado)
            {
                // Cancelar está permitido directamente desde Pendiente.
                pedido.Cancelar();
            }
            else if (estadoInicial != EstadoPedido.Pendiente)
            {
                var camino = new[]
                {
                    EstadoPedido.Confirmado, EstadoPedido.EnProduccion,
                    EstadoPedido.Listo, EstadoPedido.Entregado,
                    EstadoPedido.Completado,
                };
                foreach (var paso in camino)
                {
                    pedido.TransicionarA(paso);
                    if (pedido.Estado == estadoInicial)
                    {
                        break;
                    }
                }

                if (pedido.Estado != estadoInicial)
                {
                    throw new InvalidOperationException(
                        $"El Seed no pudo llevar el pedido a {estadoInicial}.");
                }
            }

            _pedidos.Add(pedido);
            return pedido;
        }

        public Task<PagedResult<Pedido>> GetPagedAsync(
            int page, int pageSize, string? search, int? clienteId,
            EstadoPedido? estado, DateTime? dateFrom, DateTime? dateTo,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<Pedido>());

        public Task<Pedido?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(_pedidos.FirstOrDefault(p => p.Id == id));

        public Task<Pedido?> GetByIdReadOnlyAsync(int id, CancellationToken cancellationToken = default)
            => GetByIdAsync(id, cancellationToken);

        public Task<IReadOnlyList<PedidoHistorial>> GetHistorialAsync(
            int pedidoId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PedidoHistorial>>(Array.Empty<PedidoHistorial>());

        public Task UpdateWithHistoryAsync(
            Pedido pedido, PedidoHistorial historial, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateAsync(Pedido pedido, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakePagoRepository : IPagoRepository
    {
        private readonly List<Pago> _pagos = new();

        private int _nextId = 1;

        public List<Pago> All => _pagos;

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
                .ThenByDescending(p => p.Id)
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

            if (dateFrom.HasValue)
            {
                var desde = DateTime.SpecifyKind(dateFrom.Value.Date, DateTimeKind.Utc);
                query = query.Where(p => p.FechaPago >= desde);
            }

            if (dateTo.HasValue)
            {
                var hasta = DateTime.SpecifyKind(dateTo.Value.Date, DateTimeKind.Utc).AddDays(1);
                query = query.Where(p => p.FechaPago < hasta);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                query = query.Where(p =>
                    (p.Referencia != null && p.Referencia.ToLower().Contains(term))
                    || (p.Notas != null && p.Notas.ToLower().Contains(term)));
            }

            var list = query
                .OrderByDescending(p => p.FechaPago)
                .ThenByDescending(p => p.Id)
                .ToList();

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
            => Task.FromResult(new ClientePagosResumen(0m, 0m));
    }
}
