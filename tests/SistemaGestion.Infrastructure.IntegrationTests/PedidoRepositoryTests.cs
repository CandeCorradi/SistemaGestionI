using Microsoft.EntityFrameworkCore;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Infrastructure.Persistence;
using SistemaGestion.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SistemaGestion.Infrastructure.IntegrationTests;

/// <summary>
/// Tests de integración reales contra PostgreSQL en una BD aislada
/// (sistemagestion_test — AGENTS.md §42). Cubren la persistencia de
/// pedidos: filtros/paginación del listado, detalle con snapshot
/// (BR-071), escritura atómica del cambio de estado con historial
/// (BR-075), la información de seguimiento de producción/entrega (BR-076)
/// y el historial inicial que escribe la aceptación de un
/// presupuesto. Comparte colección con <see cref="PresupuestoRepositoryTests"/>
/// para que no paralelicen sobre la misma BD aislada.
/// </summary>
[Collection("postgres-test-db")]
public sealed class PedidoRepositoryTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=sistemagestion_test;Username=postgres;Password=1234";

    public PedidoRepositoryTests()
    {
        using var context = NewContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task GetPagedAsync_FiltersEstadoClienteSearchDates_AndPaginates()
    {
        using var seedContext = NewContext();
        var ana = await SeedClienteAsync(seedContext, "ana-pedidos@test.com", "Ana", "García");
        var luis = await SeedClienteAsync(seedContext, "luis-pedidos@test.com", "Luis", "Sosa");
        var presuAna1 = await SeedPresupuestoAsync(seedContext, ana.Id);
        var presuAna2 = await SeedPresupuestoAsync(seedContext, ana.Id);
        var presuLuis = await SeedPresupuestoAsync(seedContext, luis.Id);

        var fecha1 = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);
        var fecha2 = new DateTime(2026, 10, 3, 10, 0, 0, DateTimeKind.Utc);
        var fecha3 = new DateTime(2026, 10, 4, 10, 0, 0, DateTimeKind.Utc);

        using (var setupContext = NewContext())
        {
            // Índice único IX_Pedidos_PresupuestoId: un pedido por presupuesto.
            var pedidoAnaConfirmado = Pedido.Crear(ana.Id, presuAna1, 100m, fecha1);
            pedidoAnaConfirmado.Confirmar();
            await setupContext.Pedidos.AddAsync(pedidoAnaConfirmado);

            await setupContext.Pedidos.AddAsync(
                Pedido.Crear(ana.Id, presuAna2, 200m, fecha2));
            await setupContext.Pedidos.AddAsync(
                Pedido.Crear(luis.Id, presuLuis, 300m, fecha3));

            await setupContext.SaveChangesAsync();
        }

        using var readContext = NewContext();
        var repository = new PedidoRepository(readContext);

        var confirmados = await repository.GetPagedAsync(
            1, 20, search: null, clienteId: null, estado: EstadoPedido.Confirmado,
            dateFrom: null, dateTo: null);
        Assert.Equal(1, confirmados.TotalItems);
        Assert.Equal(EstadoPedido.Confirmado, confirmados.Items[0].Estado);

        var deAna = await repository.GetPagedAsync(
            1, 20, search: null, clienteId: ana.Id, estado: null,
            dateFrom: null, dateTo: null);
        Assert.Equal(2, deAna.TotalItems);
        Assert.All(deAna.Items, p => Assert.Equal(ana.Id, p.ClienteId));

        var porBusqueda = await repository.GetPagedAsync(
            1, 20, search: "sosa", clienteId: null, estado: null,
            dateFrom: null, dateTo: null);
        Assert.Single(porBusqueda.Items);
        Assert.Equal(luis.Id, porBusqueda.Items[0].ClienteId);

        // dateFrom/dateTo inclusive sobre FechaCreacion.
        var desde3 = await repository.GetPagedAsync(
            1, 20, search: null, clienteId: null, estado: null,
            dateFrom: new DateTime(2026, 10, 3), dateTo: null);
        Assert.Equal(2, desde3.TotalItems);

        var hasta1 = await repository.GetPagedAsync(
            1, 20, search: null, clienteId: null, estado: null,
            dateFrom: null, dateTo: new DateTime(2026, 10, 1));
        Assert.Equal(1, hasta1.TotalItems);

        // Paginación: 3 pedidos, pageSize 2 → 2 + 1.
        var pagina1 = await repository.GetPagedAsync(
            1, 2, search: null, clienteId: null, estado: null,
            dateFrom: null, dateTo: null);
        Assert.Equal(3, pagina1.TotalItems);
        Assert.Equal(2, pagina1.Items.Count);

        var pagina2 = await repository.GetPagedAsync(
            2, 2, search: null, clienteId: null, estado: null,
            dateFrom: null, dateTo: null);
        Assert.Single(pagina2.Items);
    }

    [Fact]
    public async Task GetByIdAsync_LoadsClienteAndPresupuestoSnapshot_Br071()
    {
        using var seedContext = NewContext();
        var cliente = await SeedClienteAsync(seedContext, "snapshot-pedido@test.com", "Sara", "López");

        int pedidoId;
        int presupuestoId;
        using (var setupContext = NewContext())
        {
            var repository = new PresupuestoRepository(setupContext);
            var presupuesto = Presupuesto.Crear(cliente.Id, null, null, null, DateTime.UtcNow);
            presupuesto.AgregarDetalle(PresupuestoDetalle.Crear("Mueble a medida", 1, 450m, 0));
            var creado = await repository.AddAsync(presupuesto);
            creado.Enviar(DateTime.UtcNow);
            await repository.UpdateAsync(creado);

            presupuestoId = creado.Id;
            var pedido = await repository.TryAcceptAndCreateOrderAsync(creado, DateTime.UtcNow);
            Assert.NotNull(pedido);
            pedidoId = pedido.Id;
        }

        using var readContext = NewContext();
        var pedidoRepository = new PedidoRepository(readContext);

        var tracked = await pedidoRepository.GetByIdAsync(pedidoId);
        Assert.NotNull(tracked);
        Assert.Equal("Sara", tracked.Cliente!.Nombre);
        Assert.Equal(presupuestoId, tracked.Presupuesto!.Id);
        var linea = Assert.Single(tracked.Presupuesto.Detalles);
        Assert.Equal("Mueble a medida", linea.Descripcion);
        Assert.Equal(450m, linea.PrecioUnitario);
        Assert.Equal(EntityState.Unchanged, readContext.Entry(tracked).State);

        var readOnly = await pedidoRepository.GetByIdReadOnlyAsync(pedidoId);
        Assert.NotNull(readOnly);
        Assert.Single(readOnly.Presupuesto!.Detalles);
        Assert.Equal(EntityState.Detached, readContext.Entry(readOnly).State);
    }

    [Fact]
    public async Task UpdateWithHistoryAsync_PersistsEstadoAndHistoryRow_Together()
    {
        using var seedContext = NewContext();
        var cliente = await SeedClienteAsync(seedContext, "historial-escritura@test.com");
        var usuario = await SeedUsuarioAsync(seedContext);
        var presupuestoId = await SeedPresupuestoAsync(seedContext, cliente.Id);

        int pedidoId;
        using (var setupContext = NewContext())
        {
            var pedido = Pedido.Crear(cliente.Id, presupuestoId, 999m, DateTime.UtcNow);
            await setupContext.Pedidos.AddAsync(pedido);
            await setupContext.SaveChangesAsync();
            pedidoId = pedido.Id;
        }

        using (var updateContext = NewContext())
        {
            var repository = new PedidoRepository(updateContext);
            var tracked = await repository.GetByIdAsync(pedidoId);
            Assert.NotNull(tracked);

            var fechaUtc = new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);
            var anterior = tracked.Estado;
            tracked.Confirmar();

            var historial = PedidoHistorial.Crear(
                pedidoId, anterior, tracked.Estado, fechaUtc,
                usuarioId: usuario.Id, comentario: "Listo para producción");
            await repository.UpdateWithHistoryAsync(tracked, historial);
        }

        using var verifyContext = NewContext();
        var guardado = await verifyContext.Pedidos.AsNoTracking()
            .SingleAsync(p => p.Id == pedidoId);
        Assert.Equal(EstadoPedido.Confirmado, guardado.Estado);

        var fila = Assert.Single(await verifyContext.PedidoHistoriales.AsNoTracking()
            .Where(h => h.PedidoId == pedidoId)
            .ToListAsync());
        Assert.Equal(EstadoPedido.Pendiente, fila.EstadoAnterior);
        Assert.Equal(EstadoPedido.Confirmado, fila.EstadoNuevo);
        Assert.Equal(usuario.Id, fila.UsuarioId);
        Assert.Equal("Listo para producción", fila.Comentario);
        Assert.Equal(new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc), fila.FechaUtc);
    }

    [Fact]
    public async Task UpdateAsync_PersistsTrackingInfo_AndClearsIt_Br076()
    {
        using var seedContext = NewContext();
        var cliente = await SeedClienteAsync(seedContext, "seguimiento-persist@test.com");
        var presupuestoId = await SeedPresupuestoAsync(seedContext, cliente.Id);

        int pedidoId;
        using (var setupContext = NewContext())
        {
            var pedido = Pedido.Crear(cliente.Id, presupuestoId, 500m, DateTime.UtcNow);
            await setupContext.Pedidos.AddAsync(pedido);
            await setupContext.SaveChangesAsync();
            pedidoId = pedido.Id;
        }

        var estimada = DateTime.UtcNow.Date.AddDays(7);
        using (var updateContext = NewContext())
        {
            var repository = new PedidoRepository(updateContext);
            var tracked = await repository.GetByIdAsync(pedidoId);
            Assert.NotNull(tracked);

            tracked.ActualizarInformacion("Coordinar entrega con el cliente", estimada);
            await repository.UpdateAsync(tracked);
        }

        using var verifyContext = NewContext();
        var guardado = await verifyContext.Pedidos.AsNoTracking()
            .SingleAsync(p => p.Id == pedidoId);
        Assert.Equal("Coordinar entrega con el cliente", guardado.Notas);
        Assert.Equal(estimada, guardado.FechaEntregaEstimada);

        // La limpieza (null) también persiste.
        using (var clearContext = NewContext())
        {
            var repository = new PedidoRepository(clearContext);
            var tracked = await repository.GetByIdAsync(pedidoId);
            Assert.NotNull(tracked);

            tracked.ActualizarInformacion(null, null);
            await repository.UpdateAsync(tracked);
        }

        using var finalContext = NewContext();
        var limpio = await finalContext.Pedidos.AsNoTracking()
            .SingleAsync(p => p.Id == pedidoId);
        Assert.Null(limpio.Notas);
        Assert.Null(limpio.FechaEntregaEstimada);
    }

    [Fact]
    public async Task UpdateWithHistoryAsync_PersistsTrackingStamps_Br076()
    {
        using var seedContext = NewContext();
        var cliente = await SeedClienteAsync(seedContext, "marcas-persist@test.com");
        var usuario = await SeedUsuarioAsync(seedContext);
        var presupuestoId = await SeedPresupuestoAsync(seedContext, cliente.Id);

        int pedidoId;
        using (var setupContext = NewContext())
        {
            var pedido = Pedido.Crear(cliente.Id, presupuestoId, 750m, DateTime.UtcNow);
            pedido.Confirmar();
            await setupContext.Pedidos.AddAsync(pedido);
            await setupContext.SaveChangesAsync();
            pedidoId = pedido.Id;
        }

        var inicio = new DateTime(2026, 10, 5, 8, 30, 0, DateTimeKind.Utc);
        using (var updateContext = NewContext())
        {
            var repository = new PedidoRepository(updateContext);
            var tracked = await repository.GetByIdAsync(pedidoId);
            Assert.NotNull(tracked);

            var anterior = tracked.Estado;
            tracked.IniciarProduccion(inicio);

            var historial = PedidoHistorial.Crear(
                pedidoId, anterior, tracked.Estado, inicio, usuarioId: usuario.Id);
            await repository.UpdateWithHistoryAsync(tracked, historial);
        }

        using var verifyContext = NewContext();
        var guardado = await verifyContext.Pedidos.AsNoTracking()
            .SingleAsync(p => p.Id == pedidoId);
        Assert.Equal(EstadoPedido.EnProduccion, guardado.Estado);
        Assert.Equal(inicio, guardado.FechaInicioProduccion);

        var fila = Assert.Single(await verifyContext.PedidoHistoriales.AsNoTracking()
            .Where(h => h.PedidoId == pedidoId)
            .ToListAsync());
        Assert.Equal(EstadoPedido.Confirmado, fila.EstadoAnterior);
        Assert.Equal(EstadoPedido.EnProduccion, fila.EstadoNuevo);
    }

    [Fact]
    public async Task GetHistorialAsync_ReturnsNewestFirst_WithUsuario()
    {
        using var seedContext = NewContext();
        var cliente = await SeedClienteAsync(seedContext, "historial-orden@test.com");
        var usuario = await SeedUsuarioAsync(seedContext);
        var presupuestoId = await SeedPresupuestoAsync(seedContext, cliente.Id);

        int pedidoId;
        using (var setupContext = NewContext())
        {
            var pedido = Pedido.Crear(cliente.Id, presupuestoId, 50m, DateTime.UtcNow);
            await setupContext.Pedidos.AddAsync(pedido);
            // El id se asigna al guardar: PedidoHistorial exige pedidoId > 0.
            await setupContext.SaveChangesAsync();
            pedidoId = pedido.Id;

            var t1 = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);
            var t2 = new DateTime(2026, 10, 2, 9, 0, 0, DateTimeKind.Utc);
            await setupContext.PedidoHistoriales.AddRangeAsync(
                PedidoHistorial.Crear(
                    pedidoId, estadoAnterior: null, estadoNuevo: EstadoPedido.Pendiente,
                    fechaUtc: t1),
                PedidoHistorial.Crear(
                    pedidoId, EstadoPedido.Pendiente, EstadoPedido.Confirmado, t2,
                    usuarioId: usuario.Id));

            await setupContext.SaveChangesAsync();
        }

        using var readContext = NewContext();
        var repository = new PedidoRepository(readContext);

        var historial = await repository.GetHistorialAsync(pedidoId);

        Assert.Equal(2, historial.Count);
        Assert.Equal(EstadoPedido.Confirmado, historial[0].EstadoNuevo);
        Assert.NotNull(historial[0].Usuario);
        Assert.Equal("Operario", historial[0].Usuario!.Nombre);
        Assert.Equal(EstadoPedido.Pendiente, historial[1].EstadoNuevo);
        Assert.Null(historial[1].UsuarioId);

        var vacio = await repository.GetHistorialAsync(pedidoId + 1000);
        Assert.Empty(vacio);
    }

    [Fact]
    public async Task TryAccept_WritesInitialHistoryRecord_Br075()
    {
        using var seedContext = NewContext();
        var cliente = await SeedClienteAsync(seedContext, "historial-creacion@test.com");

        int pedidoId;
        int presupuestoId;
        using (var setupContext = NewContext())
        {
            var repository = new PresupuestoRepository(setupContext);
            var presupuesto = Presupuesto.Crear(cliente.Id, null, null, null, DateTime.UtcNow);
            presupuesto.AgregarDetalle(PresupuestoDetalle.Crear("Escritorio", 1, 700m, 0));
            var creado = await repository.AddAsync(presupuesto);
            creado.Enviar(DateTime.UtcNow);
            await repository.UpdateAsync(creado);

            presupuestoId = creado.Id;
            var pedido = await repository.TryAcceptAndCreateOrderAsync(creado, DateTime.UtcNow);
            Assert.NotNull(pedido);
            pedidoId = pedido.Id;
        }

        using var verifyContext = NewContext();
        var fila = Assert.Single(await verifyContext.PedidoHistoriales.AsNoTracking()
            .Where(h => h.PedidoId == pedidoId)
            .ToListAsync());

        Assert.Null(fila.EstadoAnterior);
        Assert.Equal(EstadoPedido.Pendiente, fila.EstadoNuevo);
        Assert.Null(fila.UsuarioId);
        Assert.Equal($"Creado al aceptar el presupuesto #{presupuestoId}", fila.Comentario);
    }

    private static SistemaGestionDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<SistemaGestionDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new SistemaGestionDbContext(options);
    }

    private static async Task<Cliente> SeedClienteAsync(
        SistemaGestionDbContext context,
        string email,
        string nombre = "Cliente",
        string apellido = "Test")
    {
        var cliente = Cliente.Crear(nombre, apellido, email);
        await context.Clientes.AddAsync(cliente);
        await context.SaveChangesAsync();
        return cliente;
    }

    private static async Task<int> SeedPresupuestoAsync(
        SistemaGestionDbContext context,
        int clienteId)
    {
        var presupuesto = Presupuesto.Crear(clienteId, null, null, null, DateTime.UtcNow);
        await context.Presupuestos.AddAsync(presupuesto);
        await context.SaveChangesAsync();
        return presupuesto.Id;
    }

    private static async Task<Usuario> SeedUsuarioAsync(SistemaGestionDbContext context)
    {
        var usuario = Usuario.Crear(
            "operario@test.com", "Operario", "Pedidos", rolId: 2, firebaseUid: "uid-ord-hist");
        await context.Usuarios.AddAsync(usuario);
        await context.SaveChangesAsync();
        return usuario;
    }
}
