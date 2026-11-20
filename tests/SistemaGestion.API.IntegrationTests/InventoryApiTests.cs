using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SistemaGestion.API;
using SistemaGestion.API.IntegrationTests.Fakes;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Inventario.Dtos;
using SistemaGestion.Domain.Authorization;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using Xunit;

namespace SistemaGestion.API.IntegrationTests;

/// <summary>
/// Tests de API de /api/v1/inventory (api-spec §14): autenticación,
/// autorización dinámica STOCK_ENTRY/STOCK_ADJUST según el tipo de
/// movimiento, BR-052 (sin stock negativo) y BR-051 (historial).
/// </summary>
public sealed class InventoryApiTests : IClassFixture<InventoryApiTests.Factory>
{
    private const string AdminToken = "inventory-admin-token";
    private const string EntryToken = "inventory-entry-token";
    private const string AdjustToken = "inventory-adjust-token";
    private const string ViewToken = "inventory-view-token";
    private const string NoStockToken = "inventory-nostock-token";

    private const string AdminUid = "uid-inv-admin";
    private const string EntryUid = "uid-inv-entry";
    private const string AdjustUid = "uid-inv-adjust";
    private const string ViewUid = "uid-inv-view";
    private const string NoStockUid = "uid-inv-nostock";

    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IFirebaseAuthService>(new TokenStubFirebaseAuthService());

                var permisos = new FakePermisoRepository();
                permisos.EfectivosPorUid[AdminUid] = Permisos.Todos.ToList();
                permisos.EfectivosPorUid[EntryUid] = new List<string>
                {
                    Permisos.StockView, Permisos.StockEntry,
                };
                permisos.EfectivosPorUid[AdjustUid] = new List<string>
                {
                    Permisos.StockView, Permisos.StockAdjust,
                };
                permisos.EfectivosPorUid[ViewUid] = new List<string> { Permisos.StockView };
                permisos.EfectivosPorUid[NoStockUid] = new List<string> { Permisos.UsuariosView };
                services.AddSingleton<IPermisoRepository>(permisos);

                var productos = new FakeProductoRepository();
                services.AddSingleton<IProductoRepository>(productos);

                var usuarios = new FakeUsuarioRepository();
                usuarios.Seed(AdminUid, "Admin", "Sistema");
                usuarios.Seed(EntryUid, "Ingreso", "Usuario");
                usuarios.Seed(AdjustUid, "Ajuste", "Usuario");
                usuarios.Seed(ViewUid, "Solo", "Lectura");
                services.AddSingleton<IUsuarioRepository>(usuarios);

                services.AddSingleton<IInventarioRepository>(
                    new FakeInventarioRepository(productos, usuarios));
            });
        }
    }

    private readonly Factory _factory;

    private readonly HttpClient _client;

    public InventoryApiTests(Factory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private FakeProductoRepository Productos
        => (FakeProductoRepository)_factory.Services.GetRequiredService<IProductoRepository>();

    private HttpRequestMessage AuthenticatedRequest(HttpMethod method, string url, string token = AdminToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    [Fact]
    public async Task List_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/inventory");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_WithoutStockView_ReturnsForbidden()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/inventory", NoStockToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task List_WithStockView_ReturnsStockRows()
    {
        Productos.Seed(codigoSku: "INV-001", stockActual: 3, stockMinimo: 5);

        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/inventory?lowStock=true");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = body.RootElement.GetProperty("items");
        Assert.True(items.GetArrayLength() >= 1);
        var primero = items[0];
        Assert.Equal(3, primero.GetProperty("stockActual").GetInt32());
        Assert.True(primero.GetProperty("abajoDeMinimo").GetBoolean());
    }

    [Fact]
    public async Task GetProductStock_ExistingProduct_ReturnsDetail()
    {
        var producto = Productos.Seed(codigoSku: "INV-DET", stockActual: 7, stockMinimo: 2);

        var request = AuthenticatedRequest(
            HttpMethod.Get, $"/api/v1/inventory/products/{producto.Id}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(7, body.RootElement.GetProperty("stockActual").GetInt32());
        Assert.Equal("INV-DET", body.RootElement.GetProperty("codigoSku").GetString());
    }

    [Fact]
    public async Task GetProductStock_UnknownProduct_ReturnsNotFound()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/inventory/products/99999");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateMovement_IngresoWithStockEntry_ReturnsCreatedAndUpdatesStock()
    {
        var producto = Productos.Seed(codigoSku: "INV-IN", stockActual: 10);

        var request = AuthenticatedRequest(
            HttpMethod.Post, "/api/v1/inventory/movements", EntryToken);
        request.Content = JsonContent.Create(new
        {
            productId = producto.Id,
            type = (int)TipoMovimientoStock.Ingreso,
            quantity = 5,
            reason = "Recepción de compra",
            reference = "OC-1",
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(10, body.RootElement.GetProperty("stockAnterior").GetInt32());
        Assert.Equal(15, body.RootElement.GetProperty("stockPosterior").GetInt32());
        Assert.Equal(15, Productos.FindById(producto.Id)!.StockActual);
    }

    [Fact]
    public async Task CreateMovement_IngresoWithOnlyStockAdjust_ReturnsForbidden()
    {
        var producto = Productos.Seed(codigoSku: "INV-IN-403", stockActual: 10);

        var request = AuthenticatedRequest(
            HttpMethod.Post, "/api/v1/inventory/movements", AdjustToken);
        request.Content = JsonContent.Create(new
        {
            productId = producto.Id,
            type = (int)TipoMovimientoStock.Ingreso,
            quantity = 5,
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(10, Productos.FindById(producto.Id)!.StockActual);
    }

    [Fact]
    public async Task CreateMovement_AjusteWithStockAdjust_ReturnsCreated()
    {
        var producto = Productos.Seed(codigoSku: "INV-ADJ", stockActual: 10);

        var request = AuthenticatedRequest(
            HttpMethod.Post, "/api/v1/inventory/movements", AdjustToken);
        request.Content = JsonContent.Create(new
        {
            productId = producto.Id,
            type = (int)TipoMovimientoStock.Ajuste,
            quantity = -2,
            reason = "Corrección por conteo",
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(8, Productos.FindById(producto.Id)!.StockActual);
    }

    [Fact]
    public async Task CreateMovement_AjusteWithOnlyStockEntry_ReturnsForbidden()
    {
        var producto = Productos.Seed(codigoSku: "INV-ADJ-403", stockActual: 10);

        var request = AuthenticatedRequest(
            HttpMethod.Post, "/api/v1/inventory/movements", EntryToken);
        request.Content = JsonContent.Create(new
        {
            productId = producto.Id,
            type = (int)TipoMovimientoStock.Ajuste,
            quantity = -2,
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(10, Productos.FindById(producto.Id)!.StockActual);
    }

    [Fact]
    public async Task CreateMovement_WithoutAnyStockPermission_ReturnsForbidden()
    {
        var producto = Productos.Seed(codigoSku: "INV-NOPER", stockActual: 10);

        var request = AuthenticatedRequest(
            HttpMethod.Post, "/api/v1/inventory/movements", NoStockToken);
        request.Content = JsonContent.Create(new
        {
            productId = producto.Id,
            type = (int)TipoMovimientoStock.Ingreso,
            quantity = 5,
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(10, Productos.FindById(producto.Id)!.StockActual);
    }

    [Fact]
    public async Task CreateMovement_InvalidQuantity_ReturnsBadRequest()
    {
        var producto = Productos.Seed(codigoSku: "INV-QTY", stockActual: 10);

        var request = AuthenticatedRequest(
            HttpMethod.Post, "/api/v1/inventory/movements", EntryToken);
        request.Content = JsonContent.Create(new
        {
            productId = producto.Id,
            type = (int)TipoMovimientoStock.Ingreso,
            quantity = 0,
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(10, Productos.FindById(producto.Id)!.StockActual);
    }

    [Fact]
    public async Task CreateMovement_WouldGoNegative_ReturnsBadRequest_Br052()
    {
        var producto = Productos.Seed(codigoSku: "INV-NEG", stockActual: 3);

        var request = AuthenticatedRequest(
            HttpMethod.Post, "/api/v1/inventory/movements", AdjustToken);
        request.Content = JsonContent.Create(new
        {
            productId = producto.Id,
            type = (int)TipoMovimientoStock.Egreso,
            quantity = 50,
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(3, Productos.FindById(producto.Id)!.StockActual);
    }

    [Fact]
    public async Task CreateMovement_UnknownProduct_ReturnsNotFound()
    {
        var request = AuthenticatedRequest(
            HttpMethod.Post, "/api/v1/inventory/movements", EntryToken);
        request.Content = JsonContent.Create(new
        {
            productId = 99999,
            type = (int)TipoMovimientoStock.Ingreso,
            quantity = 5,
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Movements_WithStockView_ReturnsHistory()
    {
        var producto = Productos.Seed(codigoSku: "INV-HIST", stockActual: 10);

        var create = AuthenticatedRequest(
            HttpMethod.Post, "/api/v1/inventory/movements", EntryToken);
        create.Content = JsonContent.Create(new
        {
            productId = producto.Id,
            type = (int)TipoMovimientoStock.Ingreso,
            quantity = 5,
        });
        await _client.SendAsync(create);

        var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/api/v1/inventory/movements?productId={producto.Id}",
            ViewToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, body.RootElement.GetProperty("totalItems").GetInt32());
        var movimiento = body.RootElement.GetProperty("items")[0];
        Assert.Equal(10, movimiento.GetProperty("stockAnterior").GetInt32());
        Assert.Equal(15, movimiento.GetProperty("stockPosterior").GetInt32());
        Assert.Equal("Ingreso Usuario", movimiento.GetProperty("usuarioNombre").GetString());
    }

    private sealed class TokenStubFirebaseAuthService : IFirebaseAuthService
    {
        public Task<FirebaseAuthResult?> VerifyTokenAsync(
            string idToken,
            CancellationToken cancellationToken = default)
        {
            FirebaseAuthResult? result = idToken switch
            {
                AdminToken => new FirebaseAuthResult(AdminUid, "admin@test.com", EmailVerified: true),
                EntryToken => new FirebaseAuthResult(EntryUid, "entry@test.com", EmailVerified: true),
                AdjustToken => new FirebaseAuthResult(AdjustUid, "adjust@test.com", EmailVerified: true),
                ViewToken => new FirebaseAuthResult(ViewUid, "view@test.com", EmailVerified: true),
                NoStockToken => new FirebaseAuthResult(NoStockUid, "nostock@test.com", EmailVerified: true),
                _ => null,
            };

            return Task.FromResult(result);
        }
    }
}
