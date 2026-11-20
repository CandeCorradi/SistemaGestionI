using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Dashboard;
using SistemaGestion.Domain.Common;
using SistemaGestion.Domain.Enums;
using DomainPermisos = SistemaGestion.Domain.Authorization.Permisos;
using SistemaGestion.Shared.Results;
using Xunit;

namespace SistemaGestion.Application.UnitTests;

/// <summary>
/// Dashboard administrativo (api-spec Â§19, BR-140 aplicado a bloques):
/// bloques filtrados por permisos efectivos y notificaciones propias.
/// </summary>
public sealed class DashboardServiceTests
{
    private const string Uid = "uid-dashboard-1";

    [Fact]
    public async Task Get_EmptyUid_ReturnsNotFound()
    {
        var (service, _, _) = CreateService();

        var result = await service.GetAsync(string.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task Get_UnknownUid_ReturnsNotFound()
    {
        var (service, permisos, _) = CreateService();
        permisos.Efectivos[Uid] = new List<string> { DomainPermisos.PedidosView };

        var result = await service.GetAsync("uid-inexistente");

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task Get_ReturnsOnlyPermittedBlocks()
    {
        var (service, permisos, _) = CreateService();
        permisos.Efectivos[Uid] = new List<string>
        {
            DomainPermisos.PedidosView, DomainPermisos.PagosView,
        };

        var result = await service.GetAsync(Uid);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Orders);
        Assert.NotNull(result.Value.Payments);
        Assert.Null(result.Value.Quotes);
        Assert.Null(result.Value.Sales);
        Assert.Null(result.Value.Inventory);
        Assert.NotNull(result.Value.Notifications);
    }

    [Fact]
    public async Task Get_SalesBlock_VisibleWithReportPermissionEvenWithoutInvoices()
    {
        var (service, permisos, _) = CreateService();
        permisos.Efectivos[Uid] = new List<string> { DomainPermisos.ReportesView };

        var result = await service.GetAsync(Uid);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Sales);
        Assert.Null(result.Value.Orders);
    }

    [Fact]
    public async Task Get_MapsCountsAndMonthTotals()
    {
        var (service, permisos, repository) = CreateService();
        permisos.Efectivos[Uid] = new List<string>
        {
            DomainPermisos.PedidosView, DomainPermisos.PresupuestosView,
            DomainPermisos.PagosView, DomainPermisos.StockView,
        };
        repository.PedidosPorEstado[EstadoPedido.Pendiente] = 3;
        repository.PedidosPorEstado[EstadoPedido.EnProduccion] = 2;
        repository.PedidosEntregadosMes = 4;
        repository.PresupuestosPorEstado[EstadoPresupuesto.Enviado] = 5;
        repository.PresupuestosAceptadosMes = 2;
        repository.PagosMesMonto = 1500.5m;
        repository.PagosMesCantidad = 3;
        repository.Inventario = new DashboardInventarioCounts(
            ProductosActivos: 10, StockBajo: 2, EnShowroom: 4);

        var result = await service.GetAsync(Uid);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Orders!.Pendientes);
        Assert.Equal(2, result.Value.Orders.EnProduccion);
        Assert.Equal(4, result.Value.Orders.EntregadosMes);
        Assert.Equal(5, result.Value.Quotes!.Enviados);
        Assert.Equal(2, result.Value.Quotes.AceptadosMes);
        Assert.Equal(1500.5m, result.Value.Payments!.CobradoMes);
        Assert.Equal(3, result.Value.Payments.PagosMes);
        Assert.Equal(10, result.Value.Inventory!.ProductosActivos);
        Assert.Equal(2, result.Value.Inventory.StockBajo);
        Assert.Equal(7, result.Value.Notifications.UnreadCount);
    }

    [Fact]
    public async Task Get_AlwaysIncludesOwnUnreadNotifications()
    {
        var (service, permisos, _) = CreateService();
        permisos.Efectivos[Uid] = new List<string>();

        var result = await service.GetAsync(Uid);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.Notifications.UnreadCount);
        Assert.Null(result.Value.Orders);
    }

    // ------------------------------------------------------------------
    // fakes
    // ------------------------------------------------------------------

    private static (DashboardService Service, PermisoRepositoryFake Permisos, DashboardRepositoryFake Repository)
        CreateService()
    {
        var repository = new DashboardRepositoryFake();
        var permisos = new PermisoRepositoryFake();
        var usuarios = new UsuarioRepositoryFake();
        usuarios.Seed(Uid, "Dashboard", "Tester");
        var service = new DashboardService(
            repository,
            permisos,
            usuarios,
            new NotificacionRepositoryFake { Unread = 7 },
            new FakeClock());
        return (service, permisos, repository);
    }

    private sealed class FakeClock : ITimeProvider
    {
        public DateTime UtcNow { get; } = new(2026, 10, 9, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class DashboardRepositoryFake : IDashboardRepository
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

        public DashboardInventarioCounts Inventario { get; set; } =
            new(0, 0, 0);

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

    private sealed class PermisoRepositoryFake : IPermisoRepository
    {
        public Dictionary<string, List<string>> Efectivos { get; } = new();

        public Task<IReadOnlyList<Domain.Entities.Permiso>> GetAllAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Domain.Entities.Permiso>>(
                new List<Domain.Entities.Permiso>());

        public Task<IReadOnlyList<string>> GetExplicitCodesAsync(
            int usuarioId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(new List<string>());

        public Task<IReadOnlyList<string>?> GetEffectiveCodesByFirebaseUidAsync(
            string firebaseUid, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>?>(
                Efectivos.TryGetValue(firebaseUid, out var codigos)
                    ? codigos
                    : null);

        public Task ReplaceExplicitAsync(
            int usuarioId, IReadOnlyCollection<int> permisoIds,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class UsuarioRepositoryFake : IUsuarioRepository
    {
        private readonly List<Domain.Entities.Usuario> _all = new();

        private int _nextId = 1;

        public Domain.Entities.Usuario Seed(string firebaseUid, string nombre, string apellido)
        {
            var usuario = Domain.Entities.Usuario.Crear(
                $"{firebaseUid}@test.local", nombre, apellido, rolId: 2, firebaseUid: firebaseUid);
            usuario.Id = _nextId++;
            _all.Add(usuario);
            return usuario;
        }

        public Task<PagedResult<Domain.Entities.Usuario>> GetPagedAsync(
            int page, int pageSize, string? search, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Domain.Entities.Usuario?> GetByIdAsync(
            int id, CancellationToken cancellationToken = default)
            => Task.FromResult(_all.FirstOrDefault(u => u.Id == id));

        public Task<Domain.Entities.Usuario?> GetByEmailAsync(
            string email, CancellationToken cancellationToken = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return Task.FromResult(_all.FirstOrDefault(u => u.Email == normalized));
        }

        public Task<Domain.Entities.Usuario?> GetByFirebaseUidAsync(
            string firebaseUid, CancellationToken cancellationToken = default)
            => Task.FromResult(_all.FirstOrDefault(u => u.FirebaseUid == firebaseUid && u.Activo));

        public Task<bool> EmailExistsAsync(
            string email, int? excludeId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<Domain.Entities.Usuario> AddAsync(
            Domain.Entities.Usuario usuario, CancellationToken cancellationToken = default)
            => Task.FromResult(usuario);

        public Task UpdateAsync(
            Domain.Entities.Usuario usuario, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<int>> GetActiveStaffUserIdsAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<int>>(new List<int>());
    }

    private sealed class NotificacionRepositoryFake : INotificacionRepository
    {
        public int Unread { get; set; }

        public Task<PagedResult<Domain.Entities.Notificacion>> GetPagedByUsuarioAsync(
            int usuarioId, int page, int pageSize, bool soloNoLeidas,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Domain.Entities.Notificacion?> GetByIdAsync(
            int id, CancellationToken cancellationToken = default)
            => Task.FromResult<Domain.Entities.Notificacion?>(null);

        public Task<int> CountUnreadAsync(
            int usuarioId, CancellationToken cancellationToken = default)
            => Task.FromResult(Unread);

        public Task AddRangeAsync(
            IReadOnlyCollection<Domain.Entities.Notificacion> notificaciones,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<int> MarkAllReadAsync(
            int usuarioId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);
    }
}
