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
/// facturas: snapshot histórico (BR-141), numeración simulada atómica
/// (BR-151), unicidad de una factura por pedido, filtros del listado y
/// las FK RESTRICT que protegen el documento histórico (functional-spec
/// §20). Comparte colección con el resto de los repositorios para que no
/// paralelicen sobre la misma BD aislada.
/// </summary>
[Collection("postgres-test-db")]
public sealed class FacturaRepositoryTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=sistemagestion_test;Username=postgres;Password=1234";

    public FacturaRepositoryTests()
    {
        using var context = NewContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task AddAsync_ThenAssignNumber_PersistsSnapshotAndDocument()
    {
        using var seedContext = NewContext();
        var cliente = await SeedClienteAsync(seedContext, "factura-add@test.com");
        var usuario = await SeedUsuarioAsync(seedContext);
        var pedidoId = await SeedPedidoAsync(seedContext, cliente.Id, total: 500m);

        var fecha = new DateTime(2026, 10, 4, 15, 30, 0, DateTimeKind.Utc);
        var factura = Factura.Crear(
            pedidoId, cliente.Id, "Cliente Test", 500m, fecha,
            usuario.Id, notas: " Entrega ");

        using (var writeContext = NewContext())
        {
            var repository = new FacturaRepository(writeContext);
            await repository.ExecuteInTransactionAsync(async ct =>
            {
                await repository.AddAsync(factura, ct);
                factura.AsignarDocumento($"SIM-{factura.Id:D6}", esSimulada: true);
                await repository.SaveChangesAsync(ct);
            });
            Assert.True(factura.Id > 0);
        }

        using var verifyContext = NewContext();
        var guardada = await verifyContext.Facturas.AsNoTracking()
            .SingleAsync(f => f.Id == factura.Id);
        Assert.Equal(pedidoId, guardada.PedidoId);
        Assert.Equal(cliente.Id, guardada.ClienteId);
        Assert.Equal("Cliente Test", guardada.ClienteNombre);
        Assert.Equal("SIM-" + factura.Id.ToString("D6"), guardada.Numero);
        Assert.Equal(fecha, guardada.FechaEmision);
        Assert.Equal(500m, guardada.Total);
        Assert.True(guardada.EsSimulada);
        Assert.Equal(EstadoFactura.Emitida, guardada.Estado);
        Assert.Equal("Entrega", guardada.Notas);
        Assert.Equal(usuario.Id, guardada.UsuarioId);

        // Detalle con Pedido+Cliente y Usuario resueltos (BR-141).
        using var readContext = NewContext();
        var readRepository = new FacturaRepository(readContext);
        var detalle = await readRepository.GetByIdReadOnlyAsync(factura.Id);
        Assert.NotNull(detalle);
        Assert.NotNull(detalle!.Pedido);
        Assert.NotNull(detalle.Pedido!.Cliente);
        Assert.NotNull(detalle.Usuario);
        Assert.Equal("Caja", detalle.Usuario!.Nombre);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenOperationFails_RollsBackEverything()
    {
        using var seedContext = NewContext();
        var cliente = await SeedClienteAsync(seedContext, "factura-rollback@test.com");
        var pedidoId = await SeedPedidoAsync(seedContext, cliente.Id, total: 100m);

        using (var writeContext = NewContext())
        {
            var repository = new FacturaRepository(writeContext);
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.ExecuteInTransactionAsync(async ct =>
                {
                    var factura = Factura.Crear(
                        pedidoId, cliente.Id, "Cliente Test", 100m,
                        DateTime.UtcNow, usuarioId: null, notas: null);
                    await repository.AddAsync(factura, ct);
                    throw new InvalidOperationException("falla deliberada");
                }));
        }

        using var verifyContext = NewContext();
        Assert.False(await verifyContext.Facturas.AnyAsync(f => f.PedidoId == pedidoId));
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_DuplicateOrder_ThrowsInvalidOperation()
    {
        using var seedContext = NewContext();
        var cliente = await SeedClienteAsync(seedContext, "factura-dup@test.com");
        var pedidoId = await SeedPedidoAsync(seedContext, cliente.Id, total: 100m);

        using var writeContext = NewContext();
        var repository = new FacturaRepository(writeContext);

        await repository.ExecuteInTransactionAsync(async ct =>
        {
            var primera = Factura.Crear(
                pedidoId, cliente.Id, "Cliente Test", 100m,
                DateTime.UtcNow, usuarioId: null, notas: null);
            await repository.AddAsync(primera, ct);
            primera.AsignarDocumento($"SIM-{primera.Id:D6}", esSimulada: true);
            await repository.SaveChangesAsync(ct);
        });

        // Segunda emisión del mismo pedido: el índice único de PedidoId
        // dispara la carrera concurrente y se traduce en 409.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.ExecuteInTransactionAsync(async ct =>
            {
                var duplicada = Factura.Crear(
                    pedidoId, cliente.Id, "Cliente Test", 100m,
                    DateTime.UtcNow, usuarioId: null, notas: null);
                await repository.AddAsync(duplicada, ct);
            }));

        Assert.Contains("Ya existe una factura", ex.Message, StringComparison.OrdinalIgnoreCase);

        using var verifyContext = NewContext();
        Assert.Equal(1, await verifyContext.Facturas.CountAsync(f => f.PedidoId == pedidoId));
    }

    [Fact]
    public async Task GetPagedAsync_FiltersPedidoClienteFechasBusqueda_YPagina()
    {
        using var seedContext = NewContext();
        var ana = await SeedClienteAsync(seedContext, "factura-ana@test.com", "Ana", "García");
        var luis = await SeedClienteAsync(seedContext, "factura-luis@test.com", "Luis", "Sosa");
        var pedidoAna = await SeedPedidoAsync(seedContext, ana.Id, total: 1000m);
        var pedidoLuis = await SeedPedidoAsync(seedContext, luis.Id, total: 2000m);

        using (var setupContext = NewContext())
        {
            var repository = new FacturaRepository(setupContext);

            await repository.ExecuteInTransactionAsync(async ct =>
            {
                var una = Factura.Crear(
                    pedidoAna, ana.Id, "Ana García", 1000m,
                    new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc),
                    usuarioId: null, notas: null);
                await repository.AddAsync(una, ct);
                una.AsignarDocumento($"SIM-{una.Id:D6}", esSimulada: true);
                await repository.SaveChangesAsync(ct);

                var otra = Factura.Crear(
                    pedidoLuis, luis.Id, "Luis Sosa", 2000m,
                    new DateTime(2026, 10, 3, 10, 0, 0, DateTimeKind.Utc),
                    usuarioId: null, notas: null);
                await repository.AddAsync(otra, ct);
                otra.AsignarDocumento($"SIM-{otra.Id:D6}", esSimulada: true);
                await repository.SaveChangesAsync(ct);
            });
        }

        using var readContext = NewContext();
        var repository2 = new FacturaRepository(readContext);

        var porPedido = await repository2.GetPagedAsync(
            1, 20, pedidoAna, null, null, null, null);
        var unica = Assert.Single(porPedido.Items);
        Assert.Equal(pedidoAna, unica.PedidoId);

        var porCliente = await repository2.GetPagedAsync(
            1, 20, null, luis.Id, null, null, null);
        Assert.Single(porCliente.Items);
        Assert.Equal("Luis Sosa", porCliente.Items[0].ClienteNombre);

        // dateFrom/dateTo inclusive sobre FechaEmision.
        var porRango = await repository2.GetPagedAsync(
            1, 20, null, null,
            dateFrom: new DateTime(2026, 10, 3), dateTo: new DateTime(2026, 10, 3),
            search: null);
        var delDia = Assert.Single(porRango.Items);
        Assert.Equal(pedidoLuis, delDia.PedidoId);

        // Búsqueda por número de documento (case-insensitive).
        var porNumero = await repository2.GetPagedAsync(
            1, 20, null, null, null, null, search: $"sim-{delDia.Id:D6}");
        Assert.Single(porNumero.Items);

        // Búsqueda por nombre de cliente congelado (BR-141).
        var porNombre = await repository2.GetPagedAsync(
            1, 20, null, null, null, null, search: "ana garcía");
        Assert.Single(porNombre.Items);

        // Paginación: 2 facturas, pageSize 1 → 1 + 1.
        var pagina1 = await repository2.GetPagedAsync(
            1, 1, null, null, null, null, null);
        Assert.Equal(2, pagina1.TotalItems);
        Assert.Single(pagina1.Items);
        var pagina2 = await repository2.GetPagedAsync(
            2, 1, null, null, null, null, null);
        Assert.Single(pagina2.Items);

        // Los ítems del listado traen Pedido+Cliente.
        Assert.NotNull(pagina1.Items[0].Pedido);
        Assert.NotNull(pagina1.Items[0].Pedido!.Cliente);
    }

    [Fact]
    public async Task AddAsync_PedidoForeignKey_IsEnforcedByPostgresql()
    {
        using var context = NewContext();

        var factura = Factura.Crear(
            pedidoId: 999999, clienteId: 999999, clienteNombre: "Cliente Test",
            total: 10m,
            fechaEmisionUtc: new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc),
            usuarioId: null, notas: null);

        var repository = new FacturaRepository(context);
        var ex = await Assert.ThrowsAnyAsync<Exception>(
            () => repository.AddAsync(factura));

        var pg = TryPostgresError(ex);
        Assert.NotNull(pg);
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, pg!.SqlState);
    }

    [Fact]
    public async Task DeleteOrder_WithInvoice_IsRestrictedByPostgresql()
    {
        using var seedContext = NewContext();
        var cliente = await SeedClienteAsync(seedContext, "factura-protect@test.com");
        var pedidoId = await SeedPedidoAsync(seedContext, cliente.Id, total: 100m);

        using (var setupContext = NewContext())
        {
            var repository = new FacturaRepository(setupContext);
            await repository.ExecuteInTransactionAsync(async ct =>
            {
                var factura = Factura.Crear(
                    pedidoId, cliente.Id, "Cliente Test", 100m,
                    DateTime.UtcNow, usuarioId: null, notas: null);
                await repository.AddAsync(factura, ct);
                factura.AsignarDocumento($"SIM-{factura.Id:D6}", esSimulada: true);
                await repository.SaveChangesAsync(ct);
            });
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

        // El pedido y la factura siguen intactos: no hay borrado silencioso.
        using var verifyContext = NewContext();
        Assert.True(await verifyContext.Pedidos.AnyAsync(p => p.Id == pedidoId));
        Assert.Equal(1, await verifyContext.Facturas.CountAsync(f => f.PedidoId == pedidoId));
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
            "caja@test.com", "Caja", "Pagos", rolId: 2, firebaseUid: "uid-factura-caja");
        await context.Usuarios.AddAsync(usuario);
        await context.SaveChangesAsync();
        return usuario;
    }
}
