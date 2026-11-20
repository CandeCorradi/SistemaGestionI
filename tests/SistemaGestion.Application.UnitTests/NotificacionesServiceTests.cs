using Microsoft.Extensions.Logging.Abstractions;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Notificaciones;
using SistemaGestion.Domain.Common;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;
using Xunit;

namespace SistemaGestion.Application.UnitTests;

/// <summary>
/// Reglas de notificaciones (BR-090..BR-092, functional-spec §18):
/// propiedad por usuario, estado de lectura y publicación best-effort de
/// eventos de negocio.
/// </summary>
public sealed class NotificacionesServiceTests
{
    private const string UidStaff = "uid-staff-1";
    private const string UidCliente = "uid-cliente-1";

    [Fact]
    public async Task GetList_ReturnsOnlyOwnNotificationsNewestFirst()
    {
        var (service, notificaciones, _, _) = CreateService();
        notificaciones.Seed(1, "Uno", DateTime.UtcNow.AddHours(-2));
        notificaciones.Seed(1, "Dos", DateTime.UtcNow);
        notificaciones.Seed(2, "Ajena", DateTime.UtcNow);

        var result = await service.GetListAsync(UidStaff, 1, 20, unreadOnly: false);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalItems);
        Assert.Equal(new[] { "Dos", "Uno" }, result.Value.Items.Select(n => n.Title));
    }

    [Fact]
    public async Task GetList_UnreadOnly_FiltersReadRows()
    {
        var (service, notificaciones, _, _) = CreateService();
        notificaciones.Seed(1, "Leida", DateTime.UtcNow.AddMinutes(-5), leida: true);
        notificaciones.Seed(1, "Nueva", DateTime.UtcNow);

        var result = await service.GetListAsync(UidStaff, 1, 20, unreadOnly: true);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal("Nueva", item.Title);
    }

    [Fact]
    public async Task CountUnread_ReturnsOwnUnreadCount()
    {
        var (service, notificaciones, _, _) = CreateService();
        notificaciones.Seed(1, "A", DateTime.UtcNow.AddMinutes(-3), leida: true);
        notificaciones.Seed(1, "B", DateTime.UtcNow.AddMinutes(-2));
        notificaciones.Seed(1, "C", DateTime.UtcNow);
        notificaciones.Seed(2, "Ajena", DateTime.UtcNow);

        var result = await service.CountUnreadAsync(UidStaff);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
    }

    [Fact]
    public async Task MarkRead_OwnNotification_Ok()
    {
        var (service, notificaciones, _, _) = CreateService();
        var id = notificaciones.Seed(1, "Propia", DateTime.UtcNow);

        var result = await service.MarkReadAsync(UidStaff, id);

        Assert.True(result.IsSuccess);
        Assert.True(notificaciones.All.Single(n => n.Id == id).Leida);
    }

    [Fact]
    public async Task MarkRead_OtherUserNotification_ReturnsNotFound()
    {
        var (service, notificaciones, _, _) = CreateService();
        var id = notificaciones.Seed(2, "Ajena", DateTime.UtcNow);

        var result = await service.MarkReadAsync(UidStaff, id);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
        Assert.False(notificaciones.All.Single().Leida);
    }

    [Fact]
    public async Task MarkAllRead_MarksOwnRowsAndReturnsCount()
    {
        var (service, notificaciones, _, _) = CreateService();
        notificaciones.Seed(1, "A", DateTime.UtcNow.AddMinutes(-2));
        notificaciones.Seed(1, "B", DateTime.UtcNow);
        notificaciones.Seed(2, "Ajena", DateTime.UtcNow);

        var result = await service.MarkAllReadAsync(UidStaff);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
        Assert.All(notificaciones.All.Where(n => n.UsuarioId == 1), n => Assert.True(n.Leida));
        Assert.False(notificaciones.All.Single(n => n.UsuarioId == 2).Leida);
    }

    [Fact]
    public async Task Publicar_ParaCliente_ResolvesUserViaFirebaseUid_Br090()
    {
        var (service, notificaciones, clientes, usuarios) = CreateService();
        var cliente = clientes.Add(Cliente.Crear(
            "Ana", "García", "ana@test.com", firebaseUid: UidCliente));
        usuarios.Seed(UidCliente, "Ana", "García", rolId: 99);

        await service.PublicarAsync(new NotificacionDraft(
            Tipo: TipoNotificacion.PresupuestoEnviado,
            Titulo: "Presupuesto enviado",
            Mensaje: "El presupuesto #1 fue enviado.",
            ClienteId: cliente.Id,
            EntidadTipo: "Presupuesto",
            EntidadId: 1));

        var fila = Assert.Single(notificaciones.All);
        Assert.Equal(usuarios.All.Single(u => u.FirebaseUid == UidCliente).Id, fila.UsuarioId);
        Assert.Equal(TipoNotificacion.PresupuestoEnviado, fila.Tipo);
    }

    [Fact]
    public async Task Publicar_ParaEquipo_NotifiesAllActiveStaffUsers()
    {
        var (service, notificaciones, _, usuarios) = CreateService();
        usuarios.StaffUserIds.AddRange(new[] { 10, 11 });
        usuarios.Seed("uid-a", "A", "A");
        usuarios.Seed("uid-b", "B", "B");

        await service.PublicarAsync(new NotificacionDraft(
            Tipo: TipoNotificacion.PagoRegistrado,
            Titulo: "Pago registrado",
            Mensaje: "Se registró un pago.",
            ParaEquipo: true));

        Assert.Equal(2, notificaciones.All.Count);
        Assert.Contains(notificaciones.All, n => n.UsuarioId == 10);
        Assert.Contains(notificaciones.All, n => n.UsuarioId == 11);
    }

    [Fact]
    public async Task Publicar_ClienteWithoutLinkedUser_SkipsSilently()
    {
        var (service, notificaciones, clientes, _) = CreateService();
        var cliente = clientes.Add(Cliente.Crear("Sin", "Vínculo", "sin@test.com"));

        await service.PublicarAsync(new NotificacionDraft(
            Tipo: TipoNotificacion.PedidoListo,
            Titulo: "Pedido listo",
            Mensaje: "El pedido #1 está listo.",
            ClienteId: cliente.Id));

        Assert.Empty(notificaciones.All);
    }

    [Fact]
    public async Task Publicar_WhenRepositoryThrows_DoesNotPropagate_Br092()
    {
        // Con el repo en modo falla: PublicarAsync jamás debe lanzar
        // (best-effort, BR-092).
        var failing = new NotificacionRepositoryFake { FailOnWrite = true };
        var usuarios = new UsuarioRepositoryFake();
        usuarios.StaffUserIds.Add(10);
        var serviceConFalla = new NotificacionesService(
            failing,
            new ClienteRepositoryFake(),
            usuarios,
            new FakeClock(),
            NullLogger<NotificacionesService>.Instance);

        var exception = await Record.ExceptionAsync(() =>
            serviceConFalla.PublicarAsync(new NotificacionDraft(
                Tipo: TipoNotificacion.PagoRegistrado,
                Titulo: "Pago registrado",
                Mensaje: "Se registró un pago.",
                ParaEquipo: true)));

        Assert.Null(exception);
        Assert.Empty(failing.All);
    }

    // ------------------------------------------------------------------
    // Fakes y helper
    // ------------------------------------------------------------------

    private static (NotificacionesService Service,
        NotificacionRepositoryFake Notificaciones,
        ClienteRepositoryFake Clientes,
        UsuarioRepositoryFake Usuarios)
        CreateService()
    {
        var notificaciones = new NotificacionRepositoryFake();
        var clientes = new ClienteRepositoryFake();
        var usuarios = new UsuarioRepositoryFake();
        usuarios.Seed(UidStaff, "Equipo", "Staff", rolId: 1);

        var service = new NotificacionesService(
            notificaciones, clientes, usuarios, new FakeClock(),
            NullLogger<NotificacionesService>.Instance);
        return (service, notificaciones, clientes, usuarios);
    }

    private sealed class FakeClock : ITimeProvider
    {
        public DateTime UtcNow { get; } = new(2026, 10, 9, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class NotificacionRepositoryFake : INotificacionRepository
    {
        public List<Notificacion> All { get; } = new();

        public bool FailOnWrite { get; set; }

        private int _nextId = 1;

        public int Seed(
            int usuarioId,
            string titulo,
            DateTime fechaUtc,
            bool leida = false)
        {
            var notificacion = Notificacion.Crear(
                usuarioId, TipoNotificacion.PresupuestoEnviado, titulo, "Mensaje de prueba.",
                fechaCreacionUtc: fechaUtc);
            notificacion.Id = _nextId++;
            if (leida)
            {
                notificacion.MarcarLeida();
            }

            All.Add(notificacion);
            return notificacion.Id;
        }

        public Task<PagedResult<Notificacion>> GetPagedByUsuarioAsync(
            int usuarioId, int page, int pageSize, bool soloNoLeidas,
            CancellationToken cancellationToken = default)
        {
            var query = All.Where(n => n.UsuarioId == usuarioId).AsEnumerable();
            if (soloNoLeidas)
            {
                query = query.Where(n => !n.Leida);
            }

            var list = query
                .OrderByDescending(n => n.FechaCreacionUtc)
                .ThenByDescending(n => n.Id)
                .ToList();

            return Task.FromResult(new PagedResult<Notificacion>
            {
                Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = list.Count,
            });
        }

        public Task<Notificacion?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(All.FirstOrDefault(n => n.Id == id));

        public Task<int> CountUnreadAsync(int usuarioId, CancellationToken cancellationToken = default)
            => Task.FromResult(All.Count(n => n.UsuarioId == usuarioId && !n.Leida));

        public Task AddRangeAsync(
            IReadOnlyCollection<Notificacion> notificaciones,
            CancellationToken cancellationToken = default)
        {
            if (FailOnWrite)
            {
                throw new InvalidOperationException("Fallo simulado de persistencia.");
            }

            foreach (var notificacion in notificaciones)
            {
                notificacion.Id = _nextId++;
                All.Add(notificacion);
            }

            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<int> MarkAllReadAsync(int usuarioId, CancellationToken cancellationToken = default)
        {
            var pendientes = All.Where(n => n.UsuarioId == usuarioId && !n.Leida).ToList();
            foreach (var notificacion in pendientes)
            {
                notificacion.MarcarLeida();
            }

            return Task.FromResult(pendientes.Count);
        }
    }

    private sealed class ClienteRepositoryFake : IClienteRepository
    {
        public List<Cliente> All { get; } = new();

        private int _nextId = 1;

        public Cliente Add(Cliente cliente)
        {
            cliente.Id = _nextId++;
            All.Add(cliente);
            return cliente;
        }

        public Task<PagedResult<Cliente>> GetPagedAsync(
            int page, int pageSize, string? search, bool? activo,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Cliente?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(All.FirstOrDefault(c => c.Id == id));

        public Task<Cliente?> GetByFirebaseUidAsync(
            string firebaseUid,
            CancellationToken cancellationToken = default)
            => Task.FromResult(string.IsNullOrWhiteSpace(firebaseUid)
                ? null
                : All.FirstOrDefault(c => c.FirebaseUid == firebaseUid && c.Activo));

        public Task<Cliente?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return Task.FromResult(All.FirstOrDefault(c => c.Email == normalized && c.Activo));
        }

        public Task<bool> EmailExistsAsync(
            string email, int? excludeId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<Cliente> AddAsync(Cliente cliente, CancellationToken cancellationToken = default)
            => Task.FromResult(Add(cliente));

        public Task UpdateAsync(Cliente cliente, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class UsuarioRepositoryFake : IUsuarioRepository
    {
        public List<Usuario> All { get; } = new();

        public List<int> StaffUserIds { get; } = new();

        private int _nextId = 1;

        public Usuario Seed(string firebaseUid, string nombre, string apellido, int rolId = 1)
        {
            var usuario = Usuario.Crear(
                $"{firebaseUid}@test.local", nombre, apellido, rolId, firebaseUid);
            usuario.Id = _nextId++;
            All.Add(usuario);
            return usuario;
        }

        public Task<PagedResult<Usuario>> GetPagedAsync(
            int page, int pageSize, string? search, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Usuario?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(All.FirstOrDefault(u => u.Id == id));

        public Task<Usuario?> GetByEmailAsync(
            string email, CancellationToken cancellationToken = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return Task.FromResult(All.FirstOrDefault(u => u.Email == normalized));
        }

        public Task<Usuario?> GetByFirebaseUidAsync(
            string firebaseUid, CancellationToken cancellationToken = default)
            => Task.FromResult(All.FirstOrDefault(u => u.FirebaseUid == firebaseUid && u.Activo));

        public Task<bool> EmailExistsAsync(
            string email, int? excludeId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<Usuario> AddAsync(Usuario usuario, CancellationToken cancellationToken = default)
            => Task.FromResult(usuario);

        public Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<int>> GetActiveStaffUserIdsAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<int>>(StaffUserIds.ToList());
    }
}
