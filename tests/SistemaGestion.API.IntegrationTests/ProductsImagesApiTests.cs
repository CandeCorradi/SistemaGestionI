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
using SistemaGestion.Application.Productos;
using SistemaGestion.Domain.Authorization;
using SistemaGestion.Shared.Results;
using Xunit;

namespace SistemaGestion.API.IntegrationTests;

/// <summary>
/// Tests de API de imágenes de producto en /api/v1/products/{id}/images
/// (api-spec §13, AGENTS.md §24/§41): autenticación, autorización
/// PRODUCTOS_*, validación de extensión/MIME/tamaño (≤ 5 MB), alta,
/// baja y manejo de storage no configurado (503).
/// </summary>
public sealed class ProductsImagesApiTests : IClassFixture<ProductsImagesApiTests.Factory>
{
    private const string AdminToken = "images-admin-token";
    private const string LimitedToken = "images-limited-token";
    private const string AdminUid = "uid-images-admin";
    private const string LimitedUid = "uid-images-limited";

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
                services.AddSingleton<IProductoImagenRepository>(new FakeProductoImagenRepository());
                services.AddSingleton<IStorageService>(new FakeStorageService());
            });
        }
    }

    private readonly Factory _factory;
    private readonly HttpClient _client;

    public ProductsImagesApiTests(Factory factory)
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

    private HttpRequestMessage AuthenticatedRequest(HttpMethod method, string url, string token = AdminToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static MultipartFormDataContent ImageContent(
        string fileName,
        byte[] bytes,
        string contentType = "image/png")
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(file, "file", fileName);
        return content;
    }

    // ------------------------------------------------------------------
    // Consulta
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetImages_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/products/1/images");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetImages_WithoutPermission_ReturnsForbidden()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/products/1/images", LimitedToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetImages_UnknownProduct_ReturnsNotFound()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/products/99999/images");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetImages_WithPermission_ReturnsOrderedGallery()
    {
        var producto = Productos.Seed(codigoSku: "IMG-GET");
        Imagenes.Seed(producto.Id, $"productos/{producto.Id}/2.png", orden: 1);
        Imagenes.Seed(producto.Id, $"productos/{producto.Id}/1.png", orden: 0);

        var request = AuthenticatedRequest(HttpMethod.Get, $"/api/v1/products/{producto.Id}/images");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = body.RootElement.EnumerateArray().ToList();
        Assert.Equal(2, items.Count);
        Assert.EndsWith("/1.png", items[0].GetProperty("url").GetString());
        Assert.EndsWith("/2.png", items[1].GetProperty("url").GetString());
    }

    // ------------------------------------------------------------------
    // Alta
    // ------------------------------------------------------------------

    [Fact]
    public async Task Upload_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync(
            "/api/v1/products/1/images",
            ImageContent("foto.png", [1, 2, 3]));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithoutPermission_ReturnsForbidden()
    {
        var producto = Productos.Seed(codigoSku: "IMG-403");
        var uploadsBefore = Storage.UploadedPaths.Count;
        var request = AuthenticatedRequest(
            HttpMethod.Post, $"/api/v1/products/{producto.Id}/images", LimitedToken);
        request.Content = ImageContent("foto.png", [1, 2, 3]);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(uploadsBefore, Storage.UploadedPaths.Count);
    }

    [Fact]
    public async Task Upload_WithoutFile_ReturnsBadRequest()
    {
        var producto = Productos.Seed(codigoSku: "IMG-NOFILE");
        var uploadsBefore = Storage.UploadedPaths.Count;
        var request = AuthenticatedRequest(
            HttpMethod.Post, $"/api/v1/products/{producto.Id}/images");
        request.Content = new MultipartFormDataContent();

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(uploadsBefore, Storage.UploadedPaths.Count);
    }

    [Fact]
    public async Task Upload_InvalidExtension_ReturnsBadRequest()
    {
        var producto = Productos.Seed(codigoSku: "IMG-EXT");
        var uploadsBefore = Storage.UploadedPaths.Count;
        var request = AuthenticatedRequest(
            HttpMethod.Post, $"/api/v1/products/{producto.Id}/images");
        request.Content = ImageContent(
            "documento.txt", [1, 2, 3], "text/plain");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(uploadsBefore, Storage.UploadedPaths.Count);
    }

    [Fact]
    public async Task Upload_TooLarge_ReturnsBadRequest()
    {
        var producto = Productos.Seed(codigoSku: "IMG-GRADE");
        var uploadsBefore = Storage.UploadedPaths.Count;
        var bytes = new byte[ProductosService.MaxImageBytes + 1024];
        var request = AuthenticatedRequest(
            HttpMethod.Post, $"/api/v1/products/{producto.Id}/images");
        request.Content = ImageContent("grande.png", bytes);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(uploadsBefore, Storage.UploadedPaths.Count);
    }

    [Fact]
    public async Task Upload_UnknownProduct_ReturnsNotFound()
    {
        var uploadsBefore = Storage.UploadedPaths.Count;
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/products/99999/images");
        request.Content = ImageContent("foto.png", [1, 2, 3]);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(uploadsBefore, Storage.UploadedPaths.Count);
    }

    [Fact]
    public async Task Upload_Valid_ReturnsCreatedWithPublicUrl()
    {
        var producto = Productos.Seed(codigoSku: "IMG-OK");
        var imagenesBefore = Imagenes.All.Count;
        var request = AuthenticatedRequest(
            HttpMethod.Post, $"/api/v1/products/{producto.Id}/images");
        request.Content = ImageContent("foto.png", [137, 80, 78, 71]);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var path = Assert.Single(
            Storage.UploadedPaths.Skip(Math.Max(0, Storage.UploadedPaths.Count - 1)));
        Assert.StartsWith($"productos/{producto.Id}/", path);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal($"{Storage.BaseUrl}/{path}", body.RootElement.GetProperty("url").GetString());

        Assert.Equal(imagenesBefore + 1, Imagenes.All.Count);
        Assert.Contains(Imagenes.All, i => i.Path == path && i.ProductoId == producto.Id);
    }

    [Fact]
    public async Task Upload_WhenStorageNotConfigured_ReturnsServiceUnavailable()
    {
        var producto = Productos.Seed(codigoSku: "IMG-503");
        var uploadsBefore = Storage.UploadedPaths.Count;
        Storage.IsConfigured = false;
        try
        {
            var request = AuthenticatedRequest(
                HttpMethod.Post, $"/api/v1/products/{producto.Id}/images");
            request.Content = ImageContent("foto.png", [1, 2, 3]);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal(uploadsBefore, Storage.UploadedPaths.Count);

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Storage.Error", body.RootElement.GetProperty("title").GetString());
        }
        finally
        {
            Storage.IsConfigured = true;
        }
    }

    // ------------------------------------------------------------------
    // Baja
    // ------------------------------------------------------------------

    [Fact]
    public async Task Delete_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.DeleteAsync("/api/v1/products/1/images/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithoutPermission_ReturnsForbidden()
    {
        var producto = Productos.Seed(codigoSku: "IMG-DEL-403");
        var imagen = Imagenes.Seed(producto.Id);
        var imagenesBefore = Imagenes.All.Count;
        var request = AuthenticatedRequest(
            HttpMethod.Delete,
            $"/api/v1/products/{producto.Id}/images/{imagen.Id}",
            LimitedToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(imagenesBefore, Imagenes.All.Count);
        Assert.Contains(Imagenes.All, i => i.Id == imagen.Id);
    }

    [Fact]
    public async Task Delete_Unknown_ReturnsNotFound()
    {
        var producto = Productos.Seed(codigoSku: "IMG-DEL-404");
        var deletesBefore = Storage.DeletedPaths.Count;
        var request = AuthenticatedRequest(
            HttpMethod.Delete, $"/api/v1/products/{producto.Id}/images/99999");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(deletesBefore, Storage.DeletedPaths.Count);
    }

    [Fact]
    public async Task Delete_OfAnotherProduct_ReturnsNotFound()
    {
        var productoA = Productos.Seed(codigoSku: "IMG-DEL-A");
        var productoB = Productos.Seed(codigoSku: "IMG-DEL-B");
        var imagen = Imagenes.Seed(productoA.Id);
        var imagenesBefore = Imagenes.All.Count;
        var request = AuthenticatedRequest(
            HttpMethod.Delete, $"/api/v1/products/{productoB.Id}/images/{imagen.Id}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(imagenesBefore, Imagenes.All.Count);
        Assert.Contains(Imagenes.All, i => i.Id == imagen.Id);
    }

    [Fact]
    public async Task Delete_Existing_ReturnsNoContentAndRemovesObject()
    {
        var producto = Productos.Seed(codigoSku: "IMG-DEL-OK");
        var imagen = Imagenes.Seed(producto.Id);
        var deletesBefore = Storage.DeletedPaths.Count;
        var imagenesBefore = Imagenes.All.Count;
        var request = AuthenticatedRequest(
            HttpMethod.Delete, $"/api/v1/products/{producto.Id}/images/{imagen.Id}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(deletesBefore + 1, Storage.DeletedPaths.Count);
        Assert.Equal(imagen.Path, Storage.DeletedPaths.Last());
        Assert.Equal(imagenesBefore - 1, Imagenes.All.Count);
        Assert.DoesNotContain(Imagenes.All, i => i.Id == imagen.Id);
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
