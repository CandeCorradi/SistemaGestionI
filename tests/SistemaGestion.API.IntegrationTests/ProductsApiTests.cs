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
using SistemaGestion.Application.Productos.Dtos;
using SistemaGestion.Domain.Authorization;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;
using Xunit;

namespace SistemaGestion.API.IntegrationTests;

/// <summary>
/// Tests de API de /api/v1/products (api-spec §12): autenticación,
/// autorización por permisos PRODUCTOS_*, SKU único (BR-030),
/// validaciones y desactivación lógica (BR-032) (AGENTS.md §41).
/// </summary>
public sealed class ProductsApiTests : IClassFixture<ProductsApiTests.Factory>
{
    private const string AdminToken = "products-admin-token";
    private const string LimitedToken = "products-limited-token";
    private const string AdminUid = "uid-products-admin";
    private const string LimitedUid = "uid-products-limited";

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
                permisos.EfectivosPorUid[LimitedUid] = new List<string> { Permisos.UsuariosView };
                services.AddSingleton<IPermisoRepository>(permisos);

                services.AddSingleton<IProductoRepository>(new FakeProductoRepository());
            });
        }
    }

    private readonly Factory _factory;
    private readonly HttpClient _client;

    public ProductsApiTests(Factory factory)
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
        var response = await _client.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_WithoutRequiredPermission_ReturnsForbidden()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/products", LimitedToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task List_WithPermission_ReturnsPaginatedProducts()
    {
        Productos.Seed(codigoSku: "LIST-001");
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/products?page=1&pageSize=20");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, body.RootElement.GetProperty("page").GetInt32());
        Assert.True(body.RootElement.GetProperty("totalItems").GetInt32() >= 1);
    }

    [Fact]
    public async Task List_WithFilters_FiltersByTypeAndLowStock()
    {
        Productos.Seed(
            nombre: "Melamina Filtro",
            codigoSku: "FILT-MEL",
            tipo: TipoProducto.Melamina,
            stockActual: 2,
            stockMinimo: 5);
        Productos.Seed(nombre: "Mesa Filtro", codigoSku: "FILT-MES", tipo: TipoProducto.Mueble);

        var request = AuthenticatedRequest(
            HttpMethod.Get, "/api/v1/products?type=1&lowStock=true");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = body.RootElement.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal("FILT-MEL", items[0].GetProperty("codigoSku").GetString());
    }

    [Fact]
    public async Task Create_WithEsPersonalizado_ReturnsCustomFlag()
    {
        var dto = new CreateProductoDto(
            Nombre: "Mesa a medida API",
            CodigoSku: "CUST-API-01",
            Tipo: TipoProducto.Mueble,
            Precio: 500m,
            EsPersonalizado: true);
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/products");
        request.Content = JsonContent.Create(dto);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("esPersonalizado").GetBoolean());
    }

    [Fact]
    public async Task List_WithIsCustomFilter_ReturnsOnlyCustomProducts()
    {
        Productos.Seed(nombre: "Mesa Estándar Filtro", codigoSku: "FLC-STD");
        Productos.Seed(nombre: "Mesa Custom Filtro", codigoSku: "FLC-CUS", esPersonalizado: true);

        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/products?isCustom=true");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = body.RootElement.GetProperty("items").EnumerateArray().ToList();
        Assert.NotEmpty(items);
        Assert.All(items, item => Assert.True(item.GetProperty("esPersonalizado").GetBoolean()));
        Assert.Contains(items, item => item.GetProperty("codigoSku").GetString() == "FLC-CUS");
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreated_AndNormalizesSku()
    {
        var dto = new CreateProductoDto(
            Nombre: $"Melamina {Guid.NewGuid():N}",
            CodigoSku: "  mel-nuevo-01  ",
            Tipo: TipoProducto.Melamina,
            Precio: 1500.50m);
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/products");
        request.Content = JsonContent.Create(dto);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("MEL-NUEVO-01", body.RootElement.GetProperty("codigoSku").GetString());
        Assert.Equal(1500.50m, body.RootElement.GetProperty("precio").GetDecimal());
        Assert.True(body.RootElement.GetProperty("activo").GetBoolean());
        Assert.True(body.RootElement.GetProperty("id").GetInt32() > 0);
    }

    [Fact]
    public async Task Create_WithDuplicateSku_ReturnsConflict()
    {
        Productos.Seed(codigoSku: "DUP-SKU");

        var dto = new CreateProductoDto("Otro Producto", "dup-sku", TipoProducto.Insumo, 100m);
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/products");
        request.Content = JsonContent.Create(dto);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithNegativePrice_ReturnsBadRequest()
    {
        var dto = new CreateProductoDto("Mesa", "NEG-SKU", TipoProducto.Mueble, -10m);
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/products");
        request.Content = JsonContent.Create(dto);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Validation.Error", body.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Create_WithoutPermission_ReturnsForbidden()
    {
        var dto = new CreateProductoDto("Sin Permiso", "SINPERM-SKU", TipoProducto.Mueble, 100m);
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/products", LimitedToken);
        request.Content = JsonContent.Create(dto);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain(Productos.All, p => p.CodigoSku == "SINPERM-SKU");
    }

    [Fact]
    public async Task GetById_KnownId_ReturnsProduct()
    {
        var seeded = Productos.Seed("Producto Get", codigoSku: "GET-SKU");

        var request = AuthenticatedRequest(HttpMethod.Get, $"/api/v1/products/{seeded.Id}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Producto Get", body.RootElement.GetProperty("nombre").GetString());
        Assert.Equal("GET-SKU", body.RootElement.GetProperty("codigoSku").GetString());
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/products/99999");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Patch_ExistingProduct_UpdatesProvidedFields()
    {
        var seeded = Productos.Seed("Producto Patch", codigoSku: "PATCH-SKU");

        var request = AuthenticatedRequest(HttpMethod.Patch, $"/api/v1/products/{seeded.Id}");
        request.Content = JsonContent.Create(
            new UpdateProductoDto(Precio: 777.25m, Showroom: true, Categoria: "Mesas"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Producto Patch", body.RootElement.GetProperty("nombre").GetString());
        Assert.Equal("PATCH-SKU", body.RootElement.GetProperty("codigoSku").GetString());
        Assert.Equal(777.25m, body.RootElement.GetProperty("precio").GetDecimal());
        Assert.True(body.RootElement.GetProperty("showroom").GetBoolean());
        Assert.Equal("Mesas", body.RootElement.GetProperty("categoria").GetString());
    }

    [Fact]
    public async Task Patch_WithDuplicateSku_ReturnsConflict()
    {
        Productos.Seed(codigoSku: "PATCH-OCUPADO");
        var target = Productos.Seed("Target", codigoSku: "PATCH-PROPIO");

        var request = AuthenticatedRequest(HttpMethod.Patch, $"/api/v1/products/{target.Id}");
        request.Content = JsonContent.Create(new UpdateProductoDto(CodigoSku: "patch-ocupado"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingProduct_DeactivatesWithoutRemoving()
    {
        var seeded = Productos.Seed("Producto Inactivable", codigoSku: "DEL-SKU");

        var request = AuthenticatedRequest(HttpMethod.Delete, $"/api/v1/products/{seeded.Id}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(body.RootElement.GetProperty("activo").GetBoolean());

        Assert.Contains(Productos.All, p => p.Id == seeded.Id);
        Assert.False(Productos.All.Single(p => p.Id == seeded.Id).Activo);
    }

    [Fact]
    public async Task Delete_WithoutPermission_ReturnsForbidden()
    {
        var seeded = Productos.Seed("No Se Borra", codigoSku: "DEL-NOPER");

        var request = AuthenticatedRequest(
            HttpMethod.Delete,
            $"/api/v1/products/{seeded.Id}",
            LimitedToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(Productos.All.Single(p => p.Id == seeded.Id).Activo);
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
                LimitedToken => new FirebaseAuthResult(LimitedUid, "limited@test.com", EmailVerified: true),
                _ => null,
            };

            return Task.FromResult(result);
        }
    }
}
