using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Facturacion;
using SistemaGestion.Application.Facturacion.Dtos;
using SistemaGestion.Domain.Common;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;
using Xunit;

namespace SistemaGestion.Application.UnitTests;

/// <summary>
/// Facturación simulada (BR-130/BR-131/BR-141, functional-spec §20):
/// emisión sólo para pedidos entregados o completados, una factura por
/// pedido, snapshot histórico congelado, numeración aislada detrás de
/// IBillingService y listado paginado con filtros.
/// </summary>
public sealed class FacturasServiceTests
{
    private const string Uid = "uid-factura-1";

    private static (FacturasService Service,
        FakePedidoRepository Pedidos,
        FakeFacturaRepository Facturas,
        FakeUsuarioRepository Usuarios,
        FakeClock Clock)
        CreateService()
    {
        var pedidos = new FakePedidoRepository();
        var usuarios = new FakeUsuarioRepository();
        var facturas = new FakeFacturaRepository(usuarios);
        usuarios.Seed(Uid, "Ana", "García");
        var clock = new FakeClock();

        var service = new FacturasService(
            facturas, pedidos, usuarios, new SimulatedBillingService(), clock);
        return (service, pedidos, facturas, usuarios, clock);
    }

    // ------------------------------------------------------------------
    // Emisión (BR-130/BR-141)
    // ------------------------------------------------------------------

    [Fact]
    public async Task Create_DeliveredOrder_PersistsSnapshotAndSimulatedNumber_Br130()
    {
        var (service, pedidos, facturas, _, clock) = CreateService();
        var cliente = pedidos.SeedCliente("ok@test.com");
        var pedido = pedidos.Seed(cliente, 500m, EstadoPedido.Entregado);

        var result = await service.CreateAsync(
            Uid, new CreateFacturaDto(pedido.Id, Notes: "Entrega completada"));

        Assert.True(result.IsSuccess);
        Assert.Equal(pedido.Id, result.Value.OrderId);
        Assert.Equal(cliente.Id, result.Value.CustomerId);
        Assert.Equal("Cliente Test", result.Value.CustomerName);
        Assert.Equal(500m, result.Value.Total);
        Assert.Equal("SIM-000001", result.Value.Number);
        Assert.True(result.Value.IsSimulated);
        Assert.Equal(EstadoFactura.Emitida, result.Value.Status);
        Assert.Equal("Ana García", result.Value.UserName);
        Assert.Equal("Entrega completada", result.Value.Notes);
        Assert.Equal(clock.UtcNow, result.Value.IssueDate);
        Assert.Equal(DateTimeKind.Utc, result.Value.IssueDate.Kind);

        var guardada = Assert.Single(facturas.All);
        Assert.Equal("SIM-000001", guardada.Numero);
        Assert.True(guardada.EsSimulada);
        Assert.Equal(clock.UtcNow, guardada.FechaEmision);
        Assert.Equal(500m, guardada.Total);
        Assert.NotNull(guardada.UsuarioId);
    }

    [Fact]
    public async Task Create_CompletedOrder_Ok()
    {
        var (service, pedidos, _, _, _) = CreateService();
        var pedido = pedidos.Seed(
            pedidos.SeedCliente("completado@test.com"), 300m, EstadoPedido.Completado);

        var result = await service.CreateAsync(Uid, new CreateFacturaDto(pedido.Id, null));

        Assert.True(result.IsSuccess);
        Assert.Equal(EstadoFactura.Emitida, result.Value.Status);
    }

    [Fact]
    public async Task Create_Snapshot_IsIndependentOfLaterCustomerChanges_Br141()
    {
        var (service, pedidos, facturas, _, _) = CreateService();
        var cliente = pedidos.SeedCliente("snapshot@test.com");
        var pedido = pedidos.Seed(cliente, 800m, EstadoPedido.Entregado);
        var primero = await service.CreateAsync(Uid, new CreateFacturaDto(pedido.Id, null));
        Assert.True(primero.IsSuccess);

        cliente.Actualizar(nombre: "Nombre Nuevo");
        pedido.TransicionarA(EstadoPedido.Completado);

        var guardada = Assert.Single(facturas.All);
        Assert.Equal("Cliente Test", guardada.ClienteNombre);
        Assert.Equal(800m, guardada.Total);
        Assert.Equal(EstadoFactura.Emitida, guardada.Estado);
    }

    [Theory]
    [InlineData(EstadoPedido.Pendiente)]
    [InlineData(EstadoPedido.Confirmado)]
    [InlineData(EstadoPedido.EnProduccion)]
    [InlineData(EstadoPedido.Listo)]
    [InlineData(EstadoPedido.Cancelado)]
    public async Task Create_OrderNotDelivered_ReturnsConflict(
        EstadoPedido estado)
    {
        var (service, pedidos, facturas, _, _) = CreateService();
        var pedido = pedidos.Seed(pedidos.SeedCliente("estado@test.com"), 100m, estado);

        var result = await service.CreateAsync(Uid, new CreateFacturaDto(pedido.Id, null));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
        Assert.Empty(facturas.All);
    }

    [Fact]
    public async Task Create_UnknownOrder_ReturnsNotFound()
    {
        var (service, _, facturas, _, _) = CreateService();

        var result = await service.CreateAsync(Uid, new CreateFacturaDto(99, null));

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
        Assert.Empty(facturas.All);
    }

    [Fact]
    public async Task Create_InvalidOrderId_ReturnsValidation()
    {
        var (service, _, _, _, _) = CreateService();

        var result = await service.CreateAsync(Uid, new CreateFacturaDto(0, null));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task Create_EmptyUid_ReturnsNotFound()
    {
        var (service, pedidos, _, _, _) = CreateService();
        var pedido = pedidos.Seed(pedidos.SeedCliente("nouid@test.com"), 100m, EstadoPedido.Entregado);

        var result = await service.CreateAsync("", new CreateFacturaDto(pedido.Id, null));

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task Create_UnknownUser_ReturnsNotFound_NothingPersisted()
    {
        var (service, pedidos, facturas, _, _) = CreateService();
        var pedido = pedidos.Seed(pedidos.SeedCliente("nouser@test.com"), 100m, EstadoPedido.Entregado);

        var result = await service.CreateAsync(
            "uid-inexistente", new CreateFacturaDto(pedido.Id, null));

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
        Assert.Empty(facturas.All);
    }

    [Fact]
    public async Task Create_ZeroTotalOrder_ReturnsValidation()
    {
        var (service, pedidos, facturas, _, _) = CreateService();
        var pedido = pedidos.Seed(
            pedidos.SeedCliente("total-cero@test.com"), 0m, EstadoPedido.Entregado);

        var result = await service.CreateAsync(Uid, new CreateFacturaDto(pedido.Id, null));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
        Assert.Empty(facturas.All);
    }

    // ------------------------------------------------------------------
    // Una factura por pedido (carrera concurrente → 409)
    // ------------------------------------------------------------------

    [Fact]
    public async Task Create_DuplicateInvoice_ReturnsConflict_NothingPersisted()
    {
        var (service, pedidos, facturas, _, _) = CreateService();
        var pedido = pedidos.Seed(pedidos.SeedCliente("dup@test.com"), 100m, EstadoPedido.Entregado);

        var primero = await service.CreateAsync(Uid, new CreateFacturaDto(pedido.Id, null));
        Assert.True(primero.IsSuccess);

        var segundo = await service.CreateAsync(Uid, new CreateFacturaDto(pedido.Id, null));

        Assert.True(segundo.IsFailure);
        Assert.Equal("Conflict.Error", segundo.Error.Code);
        Assert.Single(facturas.All);
    }

    [Fact]
    public async Task Create_ConcurrentDuplicate_AddThrows_ReturnsConflict()
    {
        var (service, pedidos, facturas, _, _) = CreateService();
        var pedido = pedidos.Seed(
            pedidos.SeedCliente("carrera@test.com"), 100m, EstadoPedido.Entregado);
        facturas.SimularDuplicadoConcurrente = true;

        var result = await service.CreateAsync(Uid, new CreateFacturaDto(pedido.Id, null));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
        Assert.Empty(facturas.All);
    }

    // ------------------------------------------------------------------
    // Detalle
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetById_Existing_ReturnsInvoice()
    {
        var (service, pedidos, _, _, _) = CreateService();
        var pedido = pedidos.Seed(pedidos.SeedCliente("detalle@test.com"), 250m, EstadoPedido.Entregado);
        var creado = await service.CreateAsync(Uid, new CreateFacturaDto(pedido.Id, null));

        var result = await service.GetByIdAsync(creado.Value.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("SIM-000001", result.Value.Number);
        Assert.Equal("Ana García", result.Value.UserName);
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNotFound()
    {
        var (service, _, _, _, _) = CreateService();

        var result = await service.GetByIdAsync(99);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetById_InvalidId_ReturnsValidation()
    {
        var (service, _, _, _, _) = CreateService();

        var result = await service.GetByIdAsync(0);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    // ------------------------------------------------------------------
    // Listado (AGENTS.md §30)
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetList_ReturnsItemsWithFilters()
    {
        var (service, pedidos, _, _, _) = CreateService();
        var ana = pedidos.SeedCliente("ana@test.com");
        var luis = pedidos.SeedCliente("luis@test.com");
        var pedidoAna = pedidos.Seed(ana, 100m, EstadoPedido.Entregado);
        var pedidoLuis = pedidos.Seed(luis, 200m, EstadoPedido.Entregado);
        await service.CreateAsync(Uid, new CreateFacturaDto(pedidoAna.Id, null));
        await service.CreateAsync(Uid, new CreateFacturaDto(pedidoLuis.Id, null));

        var porPedido = await service.GetListAsync(
            1, 20, pedidoAna.Id, null, null, null, null);
        Assert.True(porPedido.IsSuccess);
        var unico = Assert.Single(porPedido.Value.Items);
        Assert.Equal(pedidoAna.Id, unico.OrderId);

        var porCliente = await service.GetListAsync(
            1, 20, null, luis.Id, null, null, null);
        Assert.Single(porCliente.Value.Items);

        var porBusqueda = await service.GetListAsync(
            1, 20, null, null, null, null, search: "sim-000001");
        Assert.Single(porBusqueda.Value.Items);
    }

    [Fact]
    public async Task GetList_PageSizeIsClampedToMax()
    {
        var (service, pedidos, _, _, _) = CreateService();
        var pedido = pedidos.Seed(pedidos.SeedCliente("clamp@test.com"), 100m, EstadoPedido.Entregado);
        await service.CreateAsync(Uid, new CreateFacturaDto(pedido.Id, null));

        var result = await service.GetListAsync(
            1, pageSize: 5000, null, null, null, null, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(FacturasService.MaxPageSize, result.Value.PageSize);
    }

    [Fact]
    public async Task GetList_InvalidDateRange_ReturnsValidation()
    {
        var (service, _, _, _, _) = CreateService();

        var result = await service.GetListAsync(
            1, 20, null, null,
            dateFrom: new DateTime(2026, 10, 5), dateTo: new DateTime(2026, 10, 1),
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
    public async Task GetList_InvalidCustomerId_ReturnsValidation()
    {
        var (service, _, _, _, _) = CreateService();

        var result = await service.GetListAsync(1, 20, null, customerId: 0, null, null, null);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    // ------------------------------------------------------------------
    // Fakes
    // ------------------------------------------------------------------

    private sealed class FakeClock : ITimeProvider
    {
        public DateTime UtcNow { get; } = new(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);
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

    private sealed class FakeFacturaRepository : IFacturaRepository
    {
        private readonly List<Factura> _facturas = new();

        private readonly FakeUsuarioRepository _usuarios;

        private int _nextId = 1;

        public FakeFacturaRepository(FakeUsuarioRepository? usuarios = null)
        {
            _usuarios = usuarios!;
        }

        public List<Factura> All => _facturas;

        /// <summary>
        /// Simula una emisión concurrente del mismo pedido: el índice
        /// único de PedidoId haría fallar la inserción (409).
        /// </summary>
        public bool SimularDuplicadoConcurrente { get; set; }

        public Task<bool> ExistsByPedidoAsync(
            int pedidoId, CancellationToken cancellationToken = default)
            => Task.FromResult(_facturas.Any(f => f.PedidoId == pedidoId));

        public Task AddAsync(Factura factura, CancellationToken cancellationToken = default)
        {
            if (SimularDuplicadoConcurrente
                || _facturas.Any(f => f.PedidoId == factura.PedidoId))
            {
                throw new InvalidOperationException(
                    "Ya existe una factura para el pedido indicado.");
            }

            factura.Id = _nextId++;
            _facturas.Add(factura);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
            => await operation(cancellationToken);

        public Task<Factura?> GetByIdReadOnlyAsync(
            int id, CancellationToken cancellationToken = default)
        {
            var factura = _facturas.FirstOrDefault(f => f.Id == id);
            Attach(factura);
            return Task.FromResult(factura);
        }

        public Task<PagedResult<Factura>> GetPagedAsync(
            int page, int pageSize, int? pedidoId, int? clienteId,
            DateTime? dateFrom, DateTime? dateTo, string? search,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Factura> query = _facturas;

            if (pedidoId.HasValue)
            {
                query = query.Where(f => f.PedidoId == pedidoId.Value);
            }

            if (clienteId.HasValue)
            {
                query = query.Where(f => f.ClienteId == clienteId.Value);
            }

            if (dateFrom.HasValue)
            {
                var desde = DateTime.SpecifyKind(dateFrom.Value.Date, DateTimeKind.Utc);
                query = query.Where(f => f.FechaEmision >= desde);
            }

            if (dateTo.HasValue)
            {
                var hasta = DateTime.SpecifyKind(dateTo.Value.Date, DateTimeKind.Utc).AddDays(1);
                query = query.Where(f => f.FechaEmision < hasta);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                query = query.Where(f =>
                    f.Numero.ToLower().Contains(term)
                    || f.ClienteNombre.ToLower().Contains(term));
            }

            var list = query
                .OrderByDescending(f => f.FechaEmision)
                .ThenByDescending(f => f.Id)
                .ToList();

            foreach (var fila in list)
            {
                Attach(fila);
            }

            return Task.FromResult(new PagedResult<Factura>
            {
                Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = list.Count,
            });
        }

        private void Attach(Factura? factura)
        {
            if (factura is not null
                && factura.Usuario is null
                && factura.UsuarioId is int usuarioId
                && _usuarios is not null)
            {
                var usuario = _usuarios.PorUid.Values
                    .FirstOrDefault(u => u.Id == usuarioId);
                if (usuario is not null)
                {
                    typeof(Factura).GetProperty(nameof(Factura.Usuario))!
                        .SetValue(factura, usuario);
                }
            }
        }
    }
}
