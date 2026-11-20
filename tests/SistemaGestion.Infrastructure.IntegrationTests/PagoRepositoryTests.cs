using Microsoft.EntityFrameworkCore;
using Npgsql;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Infrastructure.Persistence;
using SistemaGestion.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SistemaGestion.Infrastructure.IntegrationTests;

/// <summary>
/// Tests de integración reales contra PostgreSQL en una BD aislada
/// (sistemagestion_test — AGENTS.md §42). Cubren la persistencia de
/// pagos: campos y suma de saldo (BR-083), historial ordenado, filtros del
/// listado, transacción serializable y las FK RESTRICT que protegen el
/// historial financiero (BR-082). Comparte colección con el resto de los
/// repositorios para que no paralelicen sobre la misma BD aislada.
/// </summary>
[Collection("postgres-test-db")]
public sealed class PagoRepositoryTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=sistemagestion_test;Username=postgres;Password=1234";

    public PagoRepositoryTests()
    {
        using var context = NewContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task AddAsync_PersistsAllFields_AndGetTotalPagadoSums_Br083()
    {
        using var seedContext = NewContext();
        var cliente = await SeedClienteAsync(seedContext, "pago-add@test.com");
        var usuario = await SeedUsuarioAsync(seedContext);
        var pedidoId = await SeedPedidoAsync(seedContext, cliente.Id, total: 500m);
        var otroPedidoId = await SeedPedidoAsync(seedContext, cliente.Id, total: 100m);

        var fecha = new DateTime(2026, 10, 3, 15, 30, 0, DateTimeKind.Utc);
        var pago = Pago.Crear(
            pedidoId, 100.55m, fecha, MetodoPago.Transferencia,
            referencia: "TRX-1", notas: "Señia", usuarioId: usuario.Id);

        using (var writeContext = NewContext())
        {
            var repository = new PagoRepository(writeContext);
            await repository.AddAsync(pago);
            Assert.True(pago.Id > 0);
        }

        using var verifyContext = NewContext();
        var guardado = await verifyContext.Pagos.AsNoTracking()
            .SingleAsync(p => p.Id == pago.Id);
        Assert.Equal(pedidoId, guardado.PedidoId);
        Assert.Equal(100.55m, guardado.Monto);
        Assert.Equal(fecha, guardado.FechaPago);
        Assert.Equal(MetodoPago.Transferencia, guardado.Metodo);
        Assert.Equal("TRX-1", guardado.Referencia);
        Assert.Equal("Señia", guardado.Notas);
        Assert.Equal(usuario.Id, guardado.UsuarioId);

        var repository2 = new PagoRepository(verifyContext);
        Assert.Equal(100.55m, await repository2.GetTotalPagadoAsync(pedidoId));
        Assert.Equal(0m, await repository2.GetTotalPagadoAsync(otroPedidoId));
    }

    [Fact]
    public async Task GetByPedidoAsync_ReturnsNewestFirst_WithUsuarioAttached()
    {
        using var seedContext = NewContext();
        var cliente = await SeedClienteAsync(seedContext, "pago-hist@test.com");
        var usuario = await SeedUsuarioAsync(seedContext);
        var pedidoId = await SeedPedidoAsync(seedContext, cliente.Id, total: 300m);

        using (var setupContext = NewContext())
        {
            var repository = new PagoRepository(setupContext);
            foreach (var (dia, monto) in new[] { (1, 100m), (2, 50m), (3, 25m) })
            {
                var fecha = new DateTime(2026, 10, dia, 10, 0, 0, DateTimeKind.Utc);
                await repository.AddAsync(Pago.Crear(
                    pedidoId, monto, fecha, MetodoPago.Efectivo,
                    referencia: null, notas: null, usuarioId: usuario.Id));
            }
        }

        using var readContext = NewContext();
        var readRepository = new PagoRepository(readContext);
        var pagos = await readRepository.GetByPedidoAsync(pedidoId);

        Assert.Equal(3, pagos.Count);
        Assert.Equal(25m, pagos[0].Monto);
        Assert.Equal(50m, pagos[1].Monto);
        Assert.Equal(100m, pagos[2].Monto);
        Assert.All(pagos, p => Assert.NotNull(p.Usuario));
        Assert.Equal("Caja", pagos[0].Usuario!.Nombre);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersPedidoClienteFechasBusqueda_YPagina()
    {
        using var seedContext = NewContext();
        var ana = await SeedClienteAsync(seedContext, "pago-ana@test.com", "Ana", "García");
        var luis = await SeedClienteAsync(seedContext, "pago-luis@test.com", "Luis", "Sosa");
        var pedidoAna = await SeedPedidoAsync(seedContext, ana.Id, total: 1000m);
        var pedidoAna2 = await SeedPedidoAsync(seedContext, ana.Id, total: 1000m);
        var pedidoLuis = await SeedPedidoAsync(seedContext, luis.Id, total: 1000m);

        using (var setupContext = NewContext())
        {
            var repository = new PagoRepository(setupContext);

            await repository.AddAsync(Pago.Crear(
                pedidoAna, 100m,
                new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc),
                MetodoPago.Efectivo, "TRX-UNO", null, null));
            await repository.AddAsync(Pago.Crear(
                pedidoAna2, 200m,
                new DateTime(2026, 10, 3, 10, 0, 0, DateTimeKind.Utc),
                MetodoPago.Tarjeta, "TRX-DOS", "con notas", null));
            await repository.AddAsync(Pago.Crear(
                pedidoLuis, 300m,
                new DateTime(2026, 10, 4, 10, 0, 0, DateTimeKind.Utc),
                MetodoPago.Transferencia, "OTRO-REF", null, null));
        }

        using var readContext = NewContext();
        var repository2 = new PagoRepository(readContext);

        var porPedido = await repository2.GetPagedAsync(
            1, 20, pedidoAna, null, null, null, null);
        var unico = Assert.Single(porPedido.Items);
        Assert.Equal(pedidoAna, unico.PedidoId);
        Assert.Equal("TRX-UNO", unico.Referencia);

        var porCliente = await repository2.GetPagedAsync(
            1, 20, null, luis.Id, null, null, null);
        Assert.Single(porCliente.Items);
        Assert.Equal(pedidoLuis, porCliente.Items[0].PedidoId);

        // dateFrom/dateTo inclusive sobre FechaPago.
        var porRango = await repository2.GetPagedAsync(
            1, 20, null, null,
            dateFrom: new DateTime(2026, 10, 3), dateTo: new DateTime(2026, 10, 3), search: null);
        Assert.Single(porRango.Items);
        Assert.Equal("TRX-DOS", porRango.Items[0].Referencia);

        var porBusqueda = await repository2.GetPagedAsync(
            1, 20, null, null, null, null, search: "trx-dos");
        Assert.Single(porBusqueda.Items);

        // Paginación: 3 pagos, pageSize 2 → 2 + 1.
        var pagina1 = await repository2.GetPagedAsync(
            1, 2, null, null, null, null, null);
        Assert.Equal(3, pagina1.TotalItems);
        Assert.Equal(2, pagina1.Items.Count);
        var pagina2 = await repository2.GetPagedAsync(
            2, 2, null, null, null, null, null);
        Assert.Single(pagina2.Items);

        // Los ítems del listado global traen Pedido+Cliente y Usuario.
        Assert.NotNull(pagina1.Items[0].Pedido);
        Assert.NotNull(pagina1.Items[0].Pedido!.Cliente);
    }

    [Fact]
    public async Task ExecuteSerializableAsync_CommitsOnSuccess_RollsBackOnFailure()
    {
        using var seedContext = NewContext();
        var cliente = await SeedClienteAsync(seedContext, "pago-tx@test.com");
        var pedidoId = await SeedPedidoAsync(seedContext, cliente.Id, total: 100m);

        using (var writeContext = NewContext())
        {
            var repository = new PagoRepository(writeContext);
            await repository.ExecuteSerializableAsync(async ct =>
            {
                await repository.AddAsync(Pago.Crear(
                    pedidoId, 10m,
                    new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc),
                    MetodoPago.Efectivo, null, null, null), ct);
            });

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.ExecuteSerializableAsync(async ct =>
                {
                    await repository.AddAsync(Pago.Crear(
                        pedidoId, 20m,
                        new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc),
                        MetodoPago.Efectivo, null, null, null), ct);
                    throw new InvalidOperationException("falla deliberada");
                }));
        }

        using var verifyContext = NewContext();
        var repository2 = new PagoRepository(verifyContext);
        Assert.Equal(10m, await repository2.GetTotalPagadoAsync(pedidoId));
    }

    [Fact]
    public async Task AddAsync_PedidoForeignKey_IsEnforcedByPostgresql()
    {
        using var context = NewContext();

        var pago = Pago.Crear(
            pedidoId: 999999, monto: 10m,
            fechaUtc: new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc),
            metodo: MetodoPago.Efectivo, referencia: null, notas: null, usuarioId: null);

        var repository = new PagoRepository(context);
        var ex = await Assert.ThrowsAnyAsync<Exception>(
            () => repository.AddAsync(pago));

        var pg = TryPostgresError(ex);
        Assert.NotNull(pg);
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, pg!.SqlState);
    }

    [Fact]
    public async Task DeleteOrder_WithPayments_IsRestrictedByPostgresql_Br082()
    {
        using var seedContext = NewContext();
        var cliente = await SeedClienteAsync(seedContext, "pago-protect@test.com");
        var pedidoId = await SeedPedidoAsync(seedContext, cliente.Id, total: 100m);

        using (var setupContext = NewContext())
        {
            var repository = new PagoRepository(setupContext);
            await repository.AddAsync(Pago.Crear(
                pedidoId, 10m,
                new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc),
                MetodoPago.Efectivo, null, null, null));
        }

        using var deleteContext = NewContext();
        var pedido = await deleteContext.Pedidos.SingleAsync(p => p.Id == pedidoId);
        deleteContext.Pedidos.Remove(pedido);

        var ex = await Assert.ThrowsAnyAsync<Exception>(
            () => deleteContext.SaveChangesAsync());

        var pg = TryPostgresError(ex);
        Assert.NotNull(pg);

        // ON DELETE RESTRICT de PostgreSQL reporta restrict_violation (23001).
        Assert.Equal("23001", pg!.SqlState);

        // El pedido y el pago siguen intactos: no hay borrado silencioso.
        using var verifyContext = NewContext();
        Assert.True(await verifyContext.Pedidos.AnyAsync(p => p.Id == pedidoId));
        Assert.Equal(1, await verifyContext.Pagos.CountAsync(p => p.PedidoId == pedidoId));
    }

    private static SistemaGestionDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<SistemaGestionDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new SistemaGestionDbContext(options);
    }

    /// <summary>
    /// EF Core puede envolver el error de PostgreSQL en DbUpdateException;
    /// se busca el PostgresException en cualquier nivel.
    /// </summary>
    private static PostgresException? TryPostgresError(Exception ex)
    {
        for (var actual = ex; actual is not null; actual = actual.InnerException)
        {
            if (actual is PostgresException postgres)
            {
                return postgres;
            }
        }

        return null;
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

    private static async Task<int> SeedPedidoAsync(
        SistemaGestionDbContext context,
        int clienteId,
        decimal total)
    {
        var presupuesto = Presupuesto.Crear(clienteId, null, null, null, DateTime.UtcNow);
        await context.Presupuestos.AddAsync(presupuesto);
        await context.SaveChangesAsync();

        var pedido = Pedido.Crear(clienteId, presupuesto.Id, total, DateTime.UtcNow);
        await context.Pedidos.AddAsync(pedido);
        await context.SaveChangesAsync();
        return pedido.Id;
    }

    private static async Task<Usuario> SeedUsuarioAsync(SistemaGestionDbContext context)
    {
        var usuario = Usuario.Crear(
            "caja@test.com", "Caja", "Pagos", rolId: 2, firebaseUid: "uid-pago-caja");
        await context.Usuarios.AddAsync(usuario);
        await context.SaveChangesAsync();
        return usuario;
    }
}
