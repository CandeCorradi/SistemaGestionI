using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SistemaGestion.API;
using SistemaGestion.API.IntegrationTests.Fakes;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Enums;
using Xunit;

namespace SistemaGestion.API.IntegrationTests;

/// <summary>
/// Tests de API del catálogo público /api/v1/catalog/products
/// (api-spec §12A, functional-spec §9): acceso sin token, exclusión de
/// productos inactivos, privacidad de campos internos (BR-004) y
/// galería de imágenes (AGENTS.md §41).
/// </summary>
public sealed class CatalogoApiTests : IClassFixture<CatalogoApiTests.Factory>
{
    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IFirebaseAuthService>(new TokenStubFirebaseAuthService());
                services.AddSingleton<IProductoRepository>(new FakeProductoRepository());
                services.AddSingleton<IProductoImagenRepository>(new FakeProductoImagenRepository());
                services.AddSingleton<IStorageService>(new FakeStorageService());
            });
        }
    }

    private readonly Factory _factory;
    private readonly HttpClient _client;

    public CatalogoApiTests(Factory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private FakeProductoRepository Productos
        => (FakeProductoRepository)_factory.Services.GetRequiredService<IProductoRepository>();

    private FakeProductoImagenRepository Imagenes
        => (FakeProductoImagenRepository)_factory.Services.GetRequiredService<IProductoImagenRepository>();

    private FakeStorageService Storage
        => (FakeStorageService)_factory.Services.GetRequiredService<IStorageService>();

    [Fact]
    public async Task List_WithoutToken_ReturnsOk()
    {
        Productos.Seed(codigoSku: "PUB-001");

        var response = await _client.GetAsync("/api/v1/catalog/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, body.RootElement.GetProperty("page").GetInt32());
        Assert.True(body.RootElement.GetProperty("totalItems").GetInt32() >= 1);
        Assert.True(body.RootElement.GetProperty("items").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task List_ExcludesInactiveProducts()
    {
        Productos.Seed(nombre: "Cat Activo", codigoSku: "PUB-ACT");
        var inactivo = Productos.Seed(nombre: "Cat Inactivo", codigoSku: "PUB-INACT");
        inactivo.Desactivar();

        var response = await _client.GetAsync("/api/v1/catalog/products?pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var nombres = body.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(i => i.GetProperty("nombre").GetString())
            .ToList();
        Assert.Contains("Cat Activo", nombres);
        Assert.DoesNotContain("Cat Inactivo", nombres);
    }

    [Fact]
    public async Task List_DoesNotExposeInternalFields()
    {
        Productos.Seed(codigoSku: "PUB-PRIV", precio: 150m, stockActual: 7);

        var response = await _client.GetAsync("/api/v1/catalog/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var item = body.RootElement.GetProperty("items").EnumerateArray().First();
        var propiedades = item.EnumerateObject().Select(p => p.Name).ToHashSet();
        Assert.DoesNotContain("costo", propiedades);
        Assert.DoesNotContain("stockActual", propiedades);
        Assert.DoesNotContain("stockMinimo", propiedades);
        Assert.DoesNotContain("proveedorId", propiedades);
        Assert.DoesNotContain("activo", propiedades);
        Assert.Contains("precio", propiedades);
        Assert.Contains("disponible", propiedades);
    }

    [Fact]
    public async Task List_WithSearch_ReturnsOnlyMatches()
    {
        Productos.Seed(nombre: "Mesa Entrada", codigoSku: "PUB-MESA");
        Productos.Seed(nombre: "Silla Taller", codigoSku: "PUB-SILLA");

        var response = await _client.GetAsync("/api/v1/catalog/products?search=mesa");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = body.RootElement.GetProperty("items").EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Equal("Mesa Entrada", items[0].GetProperty("nombre").GetString());
    }

    [Fact]
    public async Task List_WithInvalidType_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/v1/catalog/products?type=999");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Detail_Unknown_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/v1/catalog/products/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Detail_Inactive_ReturnsNotFound()
    {
        var producto = Productos.Seed(codigoSku: "PUB-DET-INACT");
        producto.Desactivar();

        var response = await _client.GetAsync($"/api/v1/catalog/products/{producto.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Detail_WithImages_ReturnsSortedGallery()
    {
        var producto = Productos.Seed(nombre: "Vidriera Cat", codigoSku: "PUB-DET", precio: 300m);
        Imagenes.Seed(producto.Id, $"productos/{producto.Id}/seg.jpg", orden: 1);
        Imagenes.Seed(producto.Id, $"productos/{producto.Id}/pri.jpg", orden: 0);

        var response = await _client.GetAsync($"/api/v1/catalog/products/{producto.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(300m, body.RootElement.GetProperty("precio").GetDecimal());
        var imagenes = body.RootElement.GetProperty("imagenes").EnumerateArray().ToList();
        Assert.Equal(2, imagenes.Count);
        Assert.EndsWith("/pri.jpg", imagenes[0].GetProperty("url").GetString());
        Assert.EndsWith("/seg.jpg", imagenes[1].GetProperty("url").GetString());
        Assert.Equal($"{Storage.BaseUrl}/productos/{producto.Id}/pri.jpg", imagenes[0].GetProperty("url").GetString());
    }

    [Fact]
    public async Task Detail_WithoutImages_ReturnsEmptyGallery()
    {
        var producto = Productos.Seed(codigoSku: "PUB-DET-NOIMG");

        var response = await _client.GetAsync($"/api/v1/catalog/products/{producto.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Empty(body.RootElement.GetProperty("imagenes").EnumerateArray());
    }

    private sealed class TokenStubFirebaseAuthService : IFirebaseAuthService
    {
        public Task<FirebaseAuthResult?> VerifyTokenAsync(
            string idToken,
            CancellationToken cancellationToken = default)
            => Task.FromResult<FirebaseAuthResult?>(null);
    }
}
