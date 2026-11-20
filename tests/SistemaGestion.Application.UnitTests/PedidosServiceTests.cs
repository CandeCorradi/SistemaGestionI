using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Pedidos;
using SistemaGestion.Application.Pedidos.Dtos;
using SistemaGestion.Domain.Common;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;
using Xunit;

namespace SistemaGestion.Application.UnitTests;

/// <summary>
/// Reglas de pedidos (BR-070..BR-076, functional-spec §15/§16): consultas,
/// snapshot del presupuesto aceptado, transiciones validadas por dominio
/// (BR-072), historial inmutable (BR-075) y edición de seguimiento (BR-076).
/// </summary>
public sealed class PedidosServiceTests
{
    private const string Uid = "uid-empleado-1";

    private static (PedidosService Service,
        FakePedidoRepository Pedidos,
        FakeUsuarioRepository Usuarios,
        FakeClock Clock)
        CreateService()
    {
        var pedidos = new FakePedidoRepository();
        var usuarios = new FakeUsuarioRepository();
        usuarios.Seed(Uid, "Ana", "García");
        var clock = new FakeClock();
        var notificaciones = new FakeNotificacionPublisher();

        var service = new PedidosService(pedidos, usuarios, clock, notificaciones);
        return (service, pedidos, usuarios, clock);
    }

    // ------------------------------------------------------------------
    // Consultas
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetList_FiltersByStatus()
    {
        var (service, pedidos, _, _) = CreateService();
        var cliente = pedidos.SeedCliente("list@test.com", "Luis", "Sosa");
        pedidos.Seed(cliente);
        var confirmado = pedidos.Seed(cliente);
        confirmado.TransicionarA(EstadoPedido.Confirmado);

        var result = await service.GetListAsync(
            page: 1, pageSize: 20, search: null, customerId: null,
            status: EstadoPedido.Confirmado, dateFrom: null, dateTo: null);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(confirmado.Id, item.Id);
        Assert.Equal(1, result.Value.TotalItems);
        Assert.Equal("Luis Sosa", item.CustomerName);
    }

    [Fact]
    public async Task GetList_InvalidDateRange_ReturnsValidation()
    {
        var (service, _, _, _) = CreateService();

        var result = await service.GetListAsync(
            1, 20, null, null, null,
            dateFrom: new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc),
            dateTo: new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetById_Existing_ReturnsItemsFromQuoteSnapshot_Br071()
    {
        var (service, pedidos, _, _) = CreateService();
        var cliente = pedidos.SeedCliente("detail@test.com", "Ana", "Pérez");
        var pedido = pedidos.Seed(cliente, conLineas: true);

        var result = await service.GetByIdAsync(pedido.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("Ana Pérez", result.Value.CustomerName);
        Assert.Equal(pedido.PresupuestoId, result.Value.QuoteId);
        Assert.Equal(300m, result.Value.Total);
        Assert.Equal(2, result.Value.Items.Count);
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNotFound()
    {
        var (service, _, _, _) = CreateService();

        var result = await service.GetByIdAsync(99);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetHistory_Existing_ReturnsNewestFirst_Br075()
    {
        var (service, pedidos, _, _) = CreateService();
        var cliente = pedidos.SeedCliente("history@test.com");
        var pedido = pedidos.Seed(cliente);

        pedido.TransicionarA(EstadoPedido.Confirmado);
        pedidos.RegistrarCambio(pedido, EstadoPedido.Pendiente, "Primera");
        pedido.TransicionarA(EstadoPedido.EnProduccion);
        pedidos.RegistrarCambio(pedido, EstadoPedido.Confirmado, "Segunda");

        var result = await service.GetHistoryAsync(pedido.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
        Assert.Equal(EstadoPedido.EnProduccion, result.Value[0].NewStatus);
        Assert.Equal(EstadoPedido.Confirmado, result.Value[1].NewStatus);
        Assert.Null(result.Value[2].PreviousStatus);
        Assert.Equal(EstadoPedido.Pendiente, result.Value[2].NewStatus);
    }

    [Fact]
    public async Task GetHistory_Unknown_ReturnsNotFound()
    {
        var (service, _, _, _) = CreateService();

        var result = await service.GetHistoryAsync(99);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    // ------------------------------------------------------------------
    // Cambio de estado (BR-072/BR-075)
    // ------------------------------------------------------------------

    [Fact]
    public async Task ChangeStatus_PendienteToConfirmado_PersistsHistoryWithUser_Br075()
    {
        var (service, pedidos, _, clock) = CreateService();
        var cliente = pedidos.SeedCliente("status@test.com");
        var pedido = pedidos.Seed(cliente);

        var result = await service.ChangeStatusAsync(
            Uid, pedido.Id, new ChangeOrderStatusDto(EstadoPedido.Confirmado, "Señia recibida"));

        Assert.True(result.IsSuccess);
        Assert.Equal(EstadoPedido.Confirmado, result.Value.Status);
        Assert.Equal(EstadoPedido.Confirmado, pedido.Estado);

        var historial = Assert.Single(
            pedidos.Historial, h => h.PedidoId == pedido.Id && h.EstadoNuevo == EstadoPedido.Confirmado);
        Assert.Equal(EstadoPedido.Pendiente, historial.EstadoAnterior);
        Assert.Equal(EstadoPedido.Confirmado, historial.EstadoNuevo);
        Assert.Equal("Señia recibida", historial.Comentario);
        Assert.NotNull(historial.UsuarioId);
        Assert.Equal(clock.UtcNow, historial.FechaUtc);
    }

    [Fact]
    public async Task ChangeStatus_InvalidJump_ReturnsConflict_NoHistory_Br072()
    {
        var (service, pedidos, _, _) = CreateService();
        var cliente = pedidos.SeedCliente("jump@test.com");
        var pedido = pedidos.Seed(cliente);
        var cambiosPrevios = pedidos.Historial.Count;

        var result = await service.ChangeStatusAsync(
            Uid, pedido.Id, new ChangeOrderStatusDto(EstadoPedido.Listo, null));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
        Assert.Equal(EstadoPedido.Pendiente, pedido.Estado);
        Assert.Equal(cambiosPrevios, pedidos.Historial.Count);
    }

    [Fact]
    public async Task ChangeStatus_SameState_ReturnsConflict()
    {
        var (service, pedidos, _, _) = CreateService();
        var cliente = pedidos.SeedCliente("same@test.com");
        var pedido = pedidos.Seed(cliente);

        var result = await service.ChangeStatusAsync(
            Uid, pedido.Id, new ChangeOrderStatusDto(EstadoPedido.Pendiente, null));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
    }

    [Fact]
    public async Task ChangeStatus_UndefinedStateValue_ReturnsValidation()
    {
        var (service, pedidos, _, _) = CreateService();
        var cliente = pedidos.SeedCliente("undefined@test.com");
        var pedido = pedidos.Seed(cliente);

        var result = await service.ChangeStatusAsync(
            Uid, pedido.Id, new ChangeOrderStatusDto((EstadoPedido)99, null));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task ChangeStatus_UnknownUser_ReturnsNotFound()
    {
        var (service, pedidos, _, _) = CreateService();
        var cliente = pedidos.SeedCliente("nouser@test.com");
        var pedido = pedidos.Seed(cliente);

        var result = await service.ChangeStatusAsync(
            "uid-inexistente", pedido.Id,
            new ChangeOrderStatusDto(EstadoPedido.Confirmado, null));

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
        Assert.Equal(EstadoPedido.Pendiente, pedido.Estado);
    }

    [Fact]
    public async Task ChangeStatus_BlankUid_ReturnsNotFound()
    {
        var (service, pedidos, _, _) = CreateService();
        var cliente = pedidos.SeedCliente("blank@test.com");
        var pedido = pedidos.Seed(cliente);

        var result = await service.ChangeStatusAsync(
            string.Empty, pedido.Id,
            new ChangeOrderStatusDto(EstadoPedido.Confirmado, null));

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task ChangeStatus_FullLifecycle_ToCompletado_Br073()
    {
        var (service, pedidos, _, _) = CreateService();
        var cliente = pedidos.SeedCliente("lifecycle@test.com");
        var pedido = pedidos.Seed(cliente);

        foreach (var destino in new[]
        {
            EstadoPedido.Confirmado, EstadoPedido.EnProduccion,
            EstadoPedido.Listo, EstadoPedido.Entregado, EstadoPedido.Completado,
        })
        {
            var paso = await service.ChangeStatusAsync(
                Uid, pedido.Id, new ChangeOrderStatusDto(destino, null));
            Assert.True(paso.IsSuccess, $"falló al pasar a {destino}");
        }

        Assert.Equal(EstadoPedido.Completado, pedido.Estado);
        Assert.Equal(6, pedidos.Historial.Count(h => h.PedidoId == pedido.Id));
    }

    [Fact]
    public async Task ChangeStatus_CancelFromListo_Ok_KeepsCancelado_Ok()
    {
        var (service, pedidos, _, _) = CreateService();
        var cliente = pedidos.SeedCliente("cancel@test.com");
        var pedido = pedidos.Seed(cliente, estadoInicial: EstadoPedido.Listo);

        var result = await service.ChangeStatusAsync(
            Uid, pedido.Id, new ChangeOrderStatusDto(EstadoPedido.Cancelado, "Cliente anuló"));

        Assert.True(result.IsSuccess);
        Assert.Equal(EstadoPedido.Cancelado, result.Value.Status);
    }

    [Fact]
    public async Task ChangeStatus_CancelFromEntregado_ReturnsConflict_Br073()
    {
        var (service, pedidos, _, _) = CreateService();
        var cliente = pedidos.SeedCliente("cancel-entregado@test.com");
        var pedido = pedidos.Seed(cliente, estadoInicial: EstadoPedido.Entregado);

        var result = await service.ChangeStatusAsync(
            Uid, pedido.Id, new ChangeOrderStatusDto(EstadoPedido.Cancelado, null));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
        Assert.Equal(EstadoPedido.Entregado, pedido.Estado);
    }

    [Fact]
    public async Task ChangeStatus_UnknownOrder_ReturnsNotFound()
    {
        var (service, _, _, _) = CreateService();

        var result = await service.ChangeStatusAsync(
            Uid, 99, new ChangeOrderStatusDto(EstadoPedido.Confirmado, null));

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task ChangeStatus_RecordsTrackingTimestamps_Br076()
    {
        var (service, pedidos, _, clock) = CreateService();
        var cliente = pedidos.SeedCliente("stamp@test.com");
        var pedido = pedidos.Seed(cliente);

        foreach (var destino in new[]
        {
            EstadoPedido.Confirmado, EstadoPedido.EnProduccion,
            EstadoPedido.Listo, EstadoPedido.Entregado,
        })
        {
            var paso = await service.ChangeStatusAsync(
                Uid, pedido.Id, new ChangeOrderStatusDto(destino, null));
            Assert.True(paso.IsSuccess, $"falló al pasar a {destino}");
        }

        Assert.Equal(clock.UtcNow, pedido.FechaInicioProduccion);
        Assert.Equal(clock.UtcNow, pedido.FechaFinProduccion);
        Assert.Equal(clock.UtcNow, pedido.FechaEntrega);

        var detalle = await service.GetByIdAsync(pedido.Id);
        Assert.Equal(clock.UtcNow, detalle.Value.ProductionStartDate);
        Assert.Equal(clock.UtcNow, detalle.Value.ProductionEndDate);
        Assert.Equal(clock.UtcNow, detalle.Value.DeliveryDate);
    }

    // ------------------------------------------------------------------
    // Edición de seguimiento (BR-076)
    // ------------------------------------------------------------------

    [Fact]
    public async Task Update_WithValidData_PersistsNotesAndEstimate_Br076()
    {
        var (service, pedidos, _, clock) = CreateService();
        var cliente = pedidos.SeedCliente("tracking@test.com");
        var pedido = pedidos.Seed(cliente);
        var estimada = clock.UtcNow.Date.AddDays(7);

        var result = await service.UpdateAsync(
            pedido.Id, new UpdateOrderDto("  Entrega coordinada  ", estimada));

        Assert.True(result.IsSuccess);
        Assert.Equal("Entrega coordinada", result.Value.Notes);
        Assert.Equal(estimada, result.Value.EstimatedDeliveryDate);
        Assert.Equal("Entrega coordinada", pedido.Notas);
    }

    [Fact]
    public async Task Update_UnspecifiedKindDate_IsTreatedAsUtc()
    {
        var (service, pedidos, _, clock) = CreateService();
        var cliente = pedidos.SeedCliente("kind@test.com");
        var pedido = pedidos.Seed(cliente);

        // Los date-only del frontend llegan como Unspecified.
        var fecha = DateTime.SpecifyKind(
            clock.UtcNow.Date.AddDays(7), DateTimeKind.Unspecified);
        var result = await service.UpdateAsync(
            pedido.Id, new UpdateOrderDto(null, fecha));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.EstimatedDeliveryDate);
        Assert.Equal(DateTimeKind.Utc, result.Value.EstimatedDeliveryDate!.Value.Kind);
    }

    [Fact]
    public async Task Update_NullValues_ClearTrackingFields()
    {
        var (service, pedidos, _, clock) = CreateService();
        var cliente = pedidos.SeedCliente("clear@test.com");
        var pedido = pedidos.Seed(cliente);
        await service.UpdateAsync(
            pedido.Id,
            new UpdateOrderDto("nota previa", clock.UtcNow.Date.AddDays(7)));

        var result = await service.UpdateAsync(pedido.Id, new UpdateOrderDto(null, null));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Notes);
        Assert.Null(result.Value.EstimatedDeliveryDate);
    }

    [Fact]
    public async Task Update_UnknownOrder_ReturnsNotFound()
    {
        var (service, _, _, _) = CreateService();

        var result = await service.UpdateAsync(99, new UpdateOrderDto("x", null));

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task Update_InvalidId_ReturnsValidation()
    {
        var (service, _, _, _) = CreateService();

        var result = await service.UpdateAsync(0, new UpdateOrderDto("x", null));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task Update_TerminalOrder_ReturnsConflict_KeepsData_Br074()
    {
        var (service, pedidos, _, _) = CreateService();
        var cliente = pedidos.SeedCliente("terminal@test.com");
        var pedido = pedidos.Seed(cliente, estadoInicial: EstadoPedido.Completado);

        var result = await service.UpdateAsync(pedido.Id, new UpdateOrderDto("x", null));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
        Assert.Null(pedido.Notas);
    }

    [Fact]
    public async Task Update_TooLongNotes_ReturnsValidation()
    {
        var (service, pedidos, _, _) = CreateService();
        var cliente = pedidos.SeedCliente("long@test.com");
        var pedido = pedidos.Seed(cliente);

        var result = await service.UpdateAsync(
            pedido.Id, new UpdateOrderDto(new string('x', Pedido.MaxNotasLength + 1), null));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task Update_EstimateBeforeCreation_ReturnsValidation()
    {
        var (service, pedidos, _, _) = CreateService();
        var cliente = pedidos.SeedCliente("past@test.com");
        var pedido = pedidos.Seed(cliente);

        var result = await service.UpdateAsync(
            pedido.Id, new UpdateOrderDto(null, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    // ------------------------------------------------------------------
    // Notificaciones de negocio (BR-092)
    // ------------------------------------------------------------------

    [Fact]
    public async Task ChangeStatus_WalksLifecycle_PublishesListoNotification_Br092()
    {
        var pedidos = new FakePedidoRepository();
        var usuarios = new FakeUsuarioRepository();
        usuarios.Seed(Uid, "Ana", "García");
        var clock = new FakeClock();
        var notificaciones = new FakeNotificacionPublisher();
        var service = new PedidosService(pedidos, usuarios, clock, notificaciones);

        var cliente = pedidos.SeedCliente("listo@test.com", "Luis", "Sosa");
        var pedido = pedidos.Seed(cliente);

        await service.ChangeStatusAsync(
            Uid, pedido.Id, new ChangeOrderStatusDto(EstadoPedido.Confirmado, null));
        await service.ChangeStatusAsync(
            Uid, pedido.Id, new ChangeOrderStatusDto(EstadoPedido.EnProduccion, null));
        var result = await service.ChangeStatusAsync(
            Uid, pedido.Id, new ChangeOrderStatusDto(EstadoPedido.Listo, null));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, notificaciones.Publicados.Count);

        var listo = notificaciones.Publicados[^1];
        Assert.Equal(TipoNotificacion.PedidoListo, listo.Tipo);
        Assert.Equal(cliente.Id, listo.ClienteId);
        Assert.Equal("Pedido", listo.EntidadTipo);
        Assert.Equal(pedido.Id, listo.EntidadId);

        var cambio = notificaciones.Publicados[0];
        Assert.Equal(TipoNotificacion.PedidoEstadoCambiado, cambio.Tipo);
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
        /// <summary>
        /// Fecha fija alineada con <see cref="FakeClock"/>: los pedidos
        /// sembrados no dependen de la fecha real del equipo.
        /// </summary>
        private static readonly DateTime SeedDate =
            new(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);

        private readonly List<Pedido> _pedidos = new();

        private readonly List<PedidoHistorial> _historial = new();

        private int _nextId = 1;

        private int _nextClienteId = 1;

        /// <summary>
        /// Reloj monótono del historial: cada fila queda con una fecha
        /// posterior a la anterior para que el orden descendente sea
        /// determinista (el historial se ordena por fecha).
        /// </summary>
        private DateTime _historialDate = SeedDate;

        private DateTime NextHistoryDate() => _historialDate = _historialDate.AddSeconds(1);

        public List<Pedido> All => _pedidos;

        public List<PedidoHistorial> Historial => _historial;

        public Cliente SeedCliente(string email, string nombre = "Cliente", string apellido = "Test")
        {
            var cliente = Cliente.Crear(nombre, apellido, email);
            cliente.Id = _nextClienteId++;
            return cliente;
        }

        public Pedido Seed(
            Cliente cliente,
            bool conLineas = false,
            EstadoPedido estadoInicial = EstadoPedido.Pendiente)
        {
            Presupuesto? presu = null;
            if (conLineas)
            {
                presu = Presupuesto.Crear(cliente.Id, null, null, null, SeedDate);
                presu.Id = 900;
                presu.AgregarDetalle(PresupuestoDetalle.Crear("Mueble", 1, 200m, 0));
                presu.AgregarDetalle(PresupuestoDetalle.Crear("Melamina", 2, 50m, 0));
            }

            var pedido = Pedido.Crear(
                cliente.Id,
                presu?.Id ?? 1,
                presu?.Total ?? 100m,
                SeedDate);
            pedido.Id = _nextId++;

            Attach(pedido, cliente, presu);
            _pedidos.Add(pedido);

            // Historial inicial de creación (como hace la aceptación).
            _historial.Add(PedidoHistorial.Crear(
                pedido.Id, null, EstadoPedido.Pendiente, NextHistoryDate()));

            if (estadoInicial != EstadoPedido.Pendiente)
            {
                var camino = new[]
                {
                    EstadoPedido.Confirmado, EstadoPedido.EnProduccion,
                    EstadoPedido.Listo, EstadoPedido.Entregado,
                    EstadoPedido.Completado,
                };
                foreach (var paso in camino)
                {
                    var anterior = pedido.Estado;
                    pedido.TransicionarA(paso);
                    _historial.Add(PedidoHistorial.Crear(
                        pedido.Id, anterior, paso, NextHistoryDate()));
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

            return pedido;
        }

        /// <summary>Registra una fila de historial como lo haría el service.</summary>
        public void RegistrarCambio(Pedido pedido, EstadoPedido anterior, string? comentario)
            => _historial.Add(PedidoHistorial.Crear(
                pedido.Id, anterior, pedido.Estado, NextHistoryDate(), usuarioId: 1, comentario));

        private static void Attach(Pedido pedido, Cliente cliente, Presupuesto? presu)
        {
            typeof(Pedido).GetProperty(nameof(Pedido.Cliente))!
                .SetValue(pedido, cliente);
            if (presu is not null)
            {
                typeof(Pedido).GetProperty(nameof(Pedido.Presupuesto))!
                    .SetValue(pedido, presu);
            }
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
                    p.Cliente!.Nombre.ToLower().Contains(term)
                    || p.Cliente!.Apellido.ToLower().Contains(term)
                    || p.Cliente!.Email.ToLower().Contains(term));
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

        public Task<Pedido?> GetByIdReadOnlyAsync(int id, CancellationToken cancellationToken = default)
            => GetByIdAsync(id, cancellationToken);

        public Task<IReadOnlyList<PedidoHistorial>> GetHistorialAsync(
            int pedidoId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PedidoHistorial>>(
                _historial
                    .Where(h => h.PedidoId == pedidoId)
                    .OrderByDescending(h => h.FechaUtc)
                    .ThenByDescending(h => h.Id)
                    .ToList());

        public Task UpdateWithHistoryAsync(
            Pedido pedido, PedidoHistorial historial, CancellationToken cancellationToken = default)
        {
            historial.Id = _historial.Count + 1;
            _historial.Add(historial);
            return Task.CompletedTask;
        }

        /// <summary>La edición muta la referencia viva de la lista (no-op).</summary>
        public Task UpdateAsync(Pedido pedido, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
