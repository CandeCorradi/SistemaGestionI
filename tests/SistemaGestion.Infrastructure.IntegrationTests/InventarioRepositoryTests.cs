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
/// Cubren BR-050 (operación atómica o sin efecto), BR-051 (historial
/// consultable) y BR-053 (concurrencia: CAS sobre el stock leído, sin
/// sobrescribir cambios concurrentes). Comparte colección con
/// <see cref="PresupuestoRepositoryTests"/> para que no paralelicen sobre
/// la misma BD aislada.
/// </summary>
[Collection("postgres-test-db")]
public sealed class InventarioRepositoryTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=sistemagestion_test;Username=postgres;Password=1234";

    public InventarioRepositoryTests()
    {
        using var context = NewContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task TryApplyMovement_WithMatchingStock_PersistsMovementAndUpdatesStock()
    {
        using var seedContext = NewContext();
        var producto = await SeedProductoAsync(seedContext, stockActual: 10);

        using var context = NewContext();
        var repository = new InventarioRepository(context);

        var movimiento = MovimientoStock.Crear(
            producto.Id,
            TipoMovimientoStock.Ingreso,
            cantidad: 5,
            stockAnterior: 10,
            usuarioId: null,
            fechaUtc: DateTime.UtcNow,
            motivo: "Recepción PG");

        var aplicado = await repository.TryApplyMovementAsync(movimiento, stockEsperado: 10);

        Assert.True(aplicado);
        Assert.True(movimiento.Id > 0);

        using var verifyContext = NewContext();
        var stock = await verifyContext.Productos.AsNoTracking()
            .SingleAsync(p => p.Id == producto.Id);
        Assert.Equal(15, stock.StockActual);

        var guardado = await verifyContext.MovimientosStock.AsNoTracking()
            .SingleAsync(m => m.ProductoId == producto.Id);
        Assert.Equal(10, guardado.StockAnterior);
        Assert.Equal(15, guardado.StockPosterior);
        Assert.Equal("Recepción PG", guardado.Motivo);
    }

    [Fact]
    public async Task TryApplyMovement_ConcurrentStaleStock_FailsWithoutModifyingAnything()
    {
        // BR-053: dos operadores leen el mismo stock; la segunda debe
        // fallar sin pisar el cambio de la primera (BR-050).
        using var seedContext = NewContext();
        var producto = await SeedProductoAsync(seedContext, stockActual: 10);

        var movimientoA = MovimientoStock.Crear(
            producto.Id, TipoMovimientoStock.Ingreso, 5, stockAnterior: 10,
            usuarioId: null, fechaUtc: DateTime.UtcNow);
        var movimientoB = MovimientoStock.Crear(
            producto.Id, TipoMovimientoStock.Egreso, 3, stockAnterior: 10,
            usuarioId: null, fechaUtc: DateTime.UtcNow);

        // Contexto A aplica con éxito.
        using (var contextA = NewContext())
        {
            var repositoryA = new InventarioRepository(contextA);
            Assert.True(await repositoryA.TryApplyMovementAsync(movimientoA, stockEsperado: 10));
        }

        // Contexto B (otra conexión) aún cree que el stock es 10.
        using (var contextB = NewContext())
        {
            var repositoryB = new InventarioRepository(contextB);
            Assert.False(await repositoryB.TryApplyMovementAsync(movimientoB, stockEsperado: 10));
        }

        using var verifyContext = NewContext();
        var stock = await verifyContext.Productos.AsNoTracking()
            .SingleAsync(p => p.Id == producto.Id);
        Assert.Equal(15, stock.StockActual);

        var movimientos = await verifyContext.MovimientosStock.AsNoTracking()
            .Where(m => m.ProductoId == producto.Id)
            .ToListAsync();
        Assert.Single(movimientos);
        Assert.Equal(TipoMovimientoStock.Ingreso, movimientos[0].Tipo);
    }

    [Fact]
    public async Task GetMovementsPaged_ReturnsNewestFirst_AndFiltersByProduct()
    {
        // BR-051: el historial permite reconstruir los movimientos.
        using var seedContext = NewContext();
        var productoA = await SeedProductoAsync(seedContext, stockActual: 0, skuSuffix: "A");
        var productoB = await SeedProductoAsync(seedContext, stockActual: 0, skuSuffix: "B");

        var baseTime = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

        using (var context = NewContext())
        {
            var repository = new InventarioRepository(context);

            var primero = MovimientoStock.Crear(
                productoA.Id, TipoMovimientoStock.Ingreso, 10, 0, null, baseTime);
            var segundo = MovimientoStock.Crear(
                productoB.Id, TipoMovimientoStock.Ingreso, 4, 0, null, baseTime.AddHours(1));
            var tercero = MovimientoStock.Crear(
                productoA.Id, TipoMovimientoStock.Egreso, 2, 10, null, baseTime.AddHours(2));

            Assert.True(await repository.TryApplyMovementAsync(primero, 0));
            Assert.True(await repository.TryApplyMovementAsync(segundo, 0));
            Assert.True(await repository.TryApplyMovementAsync(tercero, 10));
        }

        using var readContext = NewContext();
        var readRepository = new InventarioRepository(readContext);

        var historial = await readRepository.GetMovementsPagedAsync(page: 1, pageSize: 20, productoId: null);
        Assert.Equal(3, historial.TotalItems);
        Assert.Equal(TipoMovimientoStock.Egreso, historial.Items[0].Tipo);
        Assert.Equal(TipoMovimientoStock.Ingreso, historial.Items[1].Tipo);

        var soloProductoA = await readRepository.GetMovementsPagedAsync(
            page: 1, pageSize: 20, productoId: productoA.Id);
        Assert.Equal(2, soloProductoA.TotalItems);
        Assert.All(soloProductoA.Items, m => Assert.Equal(productoA.Id, m.ProductoId));
    }

    private static SistemaGestionDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<SistemaGestionDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new SistemaGestionDbContext(options);
    }

    private static async Task<Producto> SeedProductoAsync(
        SistemaGestionDbContext context,
        int stockActual,
        string skuSuffix = "T")
    {
        var sku = $"SKU-{skuSuffix}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        var producto = Producto.Crear(
            $"Producto {sku}",
            sku,
            TipoProducto.Mueble,
            precio: 100m,
            stockActual: stockActual);

        await context.Productos.AddAsync(producto);
        await context.SaveChangesAsync();
        return producto;
    }
}
