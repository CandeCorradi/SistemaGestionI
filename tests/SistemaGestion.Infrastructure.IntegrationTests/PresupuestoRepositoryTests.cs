using Microsoft.EntityFrameworkCore;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Infrastructure.Persistence;
using SistemaGestion.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SistemaGestion.Infrastructure.IntegrationTests;

/// <summary>
/// Tests de integración reales contra PostgreSQL en una BD aislada
/// (sistemagestion_test — AGENTS.md §42: nunca la BD de desarrollo).
/// Cubren la persistencia del snapshot comercial (BR-063), los totales en
/// backend (BR-062) y el reemplazo de líneas en la edición (las líneas
/// quitadas deben eliminarse, no quedar huérfanas). Comparte colección con
/// <see cref="InventarioRepositoryTests"/> para que no paralelicen sobre
/// la misma BD aislada.
/// </summary>
[Collection("postgres-test-db")]
public sealed class PresupuestoRepositoryTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=sistemagestion_test;Username=postgres;Password=1234";

    public PresupuestoRepositoryTests()
    {
        using var context = NewContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task AddAsync_WithDetails_PersistsSnapshotLines()
    {
        using var seedContext = NewContext();
        var cliente = await SeedClienteAsync(seedContext, "snap@test.com");
        var producto = await SeedProductoAsync(seedContext);

        using var context = NewContext();
        var repository = new PresupuestoRepository(context);

        var presupuesto = Presupuesto.Crear(
            cliente.Id,
            fechaValidezUtc: new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            notas: "Snapshot",
            condiciones: "50% de seña",
            fechaCreacionUtc: DateTime.UtcNow);

        // Línea con producto: precio congelado (BR-063).
        presupuesto.AgregarDetalle(PresupuestoDetalle.Crear(
            producto.Nombre, cantidad: 2, precioUnitario: producto.Precio,
            descuento: 50m, productoId: producto.Id));
        // Línea libre: mueble a medida sin SKU.
        presupuesto.AgregarDetalle(PresupuestoDetalle.Crear(
            "Mueble a medida", cantidad: 1, precioUnitario: 800m, descuento: 0m));

        var creado = await repository.AddAsync(presupuesto);

        Assert.True(creado.Id > 0);

        using var verifyContext = NewContext();
        var guardado = await verifyContext.Presupuestos.AsNoTracking()
            .Include(p => p.Detalles)
            .SingleAsync(p => p.Id == creado.Id);

        Assert.Equal(EstadoPresupuesto.Borrador, guardado.Estado);
        Assert.Equal(1100m, guardado.Subtotal);
        Assert.Equal(50m, guardado.Descuento);
        Assert.Equal(1050m, guardado.Total);
        Assert.Equal(2, guardado.Detalles.Count);

        var conProducto = guardado.Detalles.Single(d => d.ProductoId.HasValue);
        Assert.Equal(producto.Id, conProducto.ProductoId);
        Assert.Equal(150m, conProducto.PrecioUnitario);
        Assert.Equal(250m, conProducto.Subtotal);

        var libre = guardado.Detalles.Single(d => !d.ProductoId.HasValue);
        Assert.Equal("Mueble a medida", libre.Descripcion);
    }

    [Fact]
    public async Task UpdateAsync_ReplacingDetails_RemovesOldLines_NoOrphans()
    {
        using var seedContext = NewContext();
        var cliente = await SeedClienteAsync(seedContext, "replace@test.com");

        // Presupuesto original con dos líneas.
        int presupuestoId;
        using (var setupContext = NewContext())
        {
            var repository = new PresupuestoRepository(setupContext);
            var presupuesto = Presupuesto.Crear(cliente.Id, null, null, null, DateTime.UtcNow);
            presupuesto.AgregarDetalle(PresupuestoDetalle.Crear("Línea vieja 1", 1, 100m, 0));
            presupuesto.AgregarDetalle(PresupuestoDetalle.Crear("Línea vieja 2", 2, 50m, 10m));
            var creado = await repository.AddAsync(presupuesto);
            presupuestoId = creado.Id;
        }

        // Edición: reemplazar las líneas por una sola (como hace el service).
        using (var editContext = NewContext())
        {
            var repository = new PresupuestoRepository(editContext);
            var tracked = await repository.GetByIdAsync(presupuestoId);
            Assert.NotNull(tracked);

            tracked.LimpiarDetalles();
            tracked.AgregarDetalle(PresupuestoDetalle.Crear("Línea nueva", 3, 40m, 0));
            await repository.UpdateAsync(tracked);
        }

        using var verifyContext = NewContext();
        var guardado = await verifyContext.Presupuestos.AsNoTracking()
            .Include(p => p.Detalles)
            .SingleAsync(p => p.Id == presupuestoId);

        var linea = Assert.Single(guardado.Detalles);
        Assert.Equal("Línea nueva", linea.Descripcion);
        Assert.Equal(120m, guardado.Total);

        // Ninguna línea huérfana apuntando al presupuesto.
        var totalLineas = await verifyContext.PresupuestoDetalles.AsNoTracking()
            .CountAsync(d => d.PresupuestoId == presupuestoId);
        Assert.Equal(1, totalLineas);
    }

    [Fact]
    public async Task GetByIdAsync_WithTracking_LoadsDetailsAndClient()
    {
        using var seedContext = NewContext();
        var cliente = await SeedClienteAsync(seedContext, "detail@test.com", nombre: "Rita", apellido: "Pérez");

        int presupuestoId;
        using (var setupContext = NewContext())
        {
            var repository = new PresupuestoRepository(setupContext);
            var presupuesto = Presupuesto.Crear(cliente.Id, null, null, null, DateTime.UtcNow);
            presupuesto.AgregarDetalle(PresupuestoDetalle.Crear("Línea", 1, 100m, 0));
            var creado = await repository.AddAsync(presupuesto);
            presupuestoId = creado.Id;
        }

        using var context = NewContext();
        var readRepository = new PresupuestoRepository(context);

        var tracked = await readRepository.GetByIdAsync(presupuestoId);
        Assert.NotNull(tracked);
        Assert.Single(tracked.Detalles);
        Assert.Equal("Rita Pérez", $"{tracked.Cliente!.Nombre} {tracked.Cliente.Apellido}".Trim());
        Assert.True(context.Entry(tracked).State == EntityState.Unchanged
            || context.Entry(tracked).State == EntityState.Modified);

        var readOnly = await readRepository.GetByIdReadOnlyAsync(presupuestoId);
        Assert.NotNull(readOnly);
        Assert.Single(readOnly.Detalles);
        Assert.Equal(EntityState.Detached, context.Entry(readOnly).State);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByEstado_AndSearchesByCliente()
    {
        using var seedContext = NewContext();
        var ana = await SeedClienteAsync(seedContext, "ana@test.com", nombre: "Ana", apellido: "García");
        var luis = await SeedClienteAsync(seedContext, "luis@test.com", nombre: "Luis", apellido: "Sosa");

        using (var setupContext = NewContext())
        {
            var repository = new PresupuestoRepository(setupContext);

            var borradorAna = Presupuesto.Crear(ana.Id, null, null, null, DateTime.UtcNow);
            borradorAna.AgregarDetalle(PresupuestoDetalle.Crear("A", 1, 100m, 0));
            await repository.AddAsync(borradorAna);

            var enviadoAna = Presupuesto.Crear(ana.Id, null, null, null, DateTime.UtcNow);
            enviadoAna.AgregarDetalle(PresupuestoDetalle.Crear("B", 1, 200m, 0));
            var creado = await repository.AddAsync(enviadoAna);
            creado.Enviar(DateTime.UtcNow);
            await repository.UpdateAsync(creado);

            var borradorLuis = Presupuesto.Crear(luis.Id, null, null, null, DateTime.UtcNow);
            borradorLuis.AgregarDetalle(PresupuestoDetalle.Crear("C", 1, 300m, 0));
            await repository.AddAsync(borradorLuis);
        }

        using var readContext = NewContext();
        var readRepository = new PresupuestoRepository(readContext);

        var enviados = await readRepository.GetPagedAsync(
            1, 20, search: null, clienteId: null, estado: EstadoPresupuesto.Enviado,
            dateFrom: null, dateTo: null);
        Assert.Equal(1, enviados.TotalItems);
        Assert.Equal(EstadoPresupuesto.Enviado, enviados.Items[0].Estado);

        var deAna = await readRepository.GetPagedAsync(
            1, 20, search: null, clienteId: ana.Id, estado: null,
            dateFrom: null, dateTo: null);
        Assert.Equal(2, deAna.TotalItems);
        Assert.All(deAna.Items, p => Assert.Equal(ana.Id, p.ClienteId));

        var porBusqueda = await readRepository.GetPagedAsync(
            1, 20, search: "garcía", clienteId: null, estado: null,
            dateFrom: null, dateTo: null);
        Assert.Equal(2, porBusqueda.TotalItems);

        var porBusquedaLuis = await readRepository.GetPagedAsync(
            1, 20, search: "sosa", clienteId: null, estado: null,
            dateFrom: null, dateTo: null);
        Assert.Single(porBusquedaLuis.Items);
        Assert.Equal(luis.Id, porBusquedaLuis.Items[0].ClienteId);
    }

    [Fact]
    public async Task TryAcceptAndCreateOrder_TransactionPersistsOrder_AndSecondCallLosesCas()
    {
        using var seedContext = NewContext();
        var cliente = await SeedClienteAsync(seedContext, "cas@test.com");

        int presupuestoId;
        using (var setupContext = NewContext())
        {
            var repository = new PresupuestoRepository(setupContext);
            var presupuesto = Presupuesto.Crear(cliente.Id, null, null, null, DateTime.UtcNow);
            presupuesto.AgregarDetalle(PresupuestoDetalle.Crear("Mueble", 1, 300m, 0));
            var creado = await repository.AddAsync(presupuesto);
            creado.Enviar(DateTime.UtcNow);
            await repository.UpdateAsync(creado);
            presupuestoId = creado.Id;
        }

        // Contexto A: acepta. El CAS (ExecuteUpdate sobre Enviado) pasa y el
        // pedido se persiste en la misma transacción (BR-065).
        int orderId;
        using (var contextA = NewContext())
        {
            var repoA = new PresupuestoRepository(contextA);
            var trackedA = await repoA.GetByIdAsync(presupuestoId);
            Assert.NotNull(trackedA);

            trackedA.Aceptar(DateTime.UtcNow);
            var pedido = await repoA.TryAcceptAndCreateOrderAsync(trackedA, DateTime.UtcNow);

            Assert.NotNull(pedido);
            orderId = pedido.Id;
            Assert.Equal(cliente.Id, pedido.ClienteId);
            Assert.Equal(presupuestoId, pedido.PresupuestoId);
            Assert.Equal(300m, pedido.Total);
            Assert.Equal(EstadoPedido.Pendiente, pedido.Estado);
        }

        // Contexto B: la fila ya no está Enviado → 0 filas → rollback, null.
        using (var contextB = NewContext())
        {
            var repoB = new PresupuestoRepository(contextB);
            var trackedB = await repoB.GetByIdAsync(presupuestoId);
            Assert.NotNull(trackedB);

            var segundo = await repoB.TryAcceptAndCreateOrderAsync(trackedB, DateTime.UtcNow);

            Assert.Null(segundo);
        }

        using var verifyContext = NewContext();
        var pedidos = await verifyContext.Pedidos.AsNoTracking()
            .Where(p => p.PresupuestoId == presupuestoId)
            .ToListAsync();
        var unico = Assert.Single(pedidos);
        Assert.Equal(orderId, unico.Id);

        var encontrado = await new PresupuestoRepository(verifyContext)
            .GetOrderIdAsync(presupuestoId);
        Assert.Equal(orderId, encontrado);
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

    private static async Task<Producto> SeedProductoAsync(SistemaGestionDbContext context)
    {
        var sku = $"SKU-PQ-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        var producto = Producto.Crear($"Producto {sku}", sku, TipoProducto.Mueble, precio: 150m);
        await context.Productos.AddAsync(producto);
        await context.SaveChangesAsync();
        return producto;
    }
}
