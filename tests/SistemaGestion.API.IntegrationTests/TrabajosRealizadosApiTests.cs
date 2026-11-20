using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SistemaGestion.API;
using SistemaGestion.API.IntegrationTests.Fakes;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Authorization;
using Xunit;

namespace SistemaGestion.API.IntegrationTests;

/// <summary>
/// Tests de API del CRUD administrativo /api/v1/trabajos-realizados
/// (api-spec §12C, AGENTS.md §23/§41): autenticación, autorización
/// TRABAJOS_*, validación de datos y gestión de imágenes.
/// </summary>
public sealed class TrabajosRealizadosApiTests : IClassFixture<TrabajosRealizadosApiTests.Factory>
{
    private const string AdminToken = "trabajos-admin-token";
    private const string LimitedToken = "trabajos-limited-token";
    private const string AdminUid = "uid-trabajos-admin";
    private const string LimitedUid = "uid-trabajos-limited";

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

                services.AddSingleton<ITrabajoRealizadoRepository>(new FakeTrabajoRealizadoRepository());
                services.AddSingleton<ITrabajoRealizadoImagenRepository>(
                    new FakeTrabajoRealizadoImagenRepository());
                services.AddSingleton<IStorageService>(new FakeStorageService());
            });
        }
    }

    private readonly Factory _factory;
    private readonly HttpClient _client;

    public TrabajosRealizadosApiTests(Factory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private FakeTrabajoRealizadoRepository Trabajos
        => (FakeTrabajoRealizadoRepository)_factory.Services
            .GetRequiredService<ITrabajoRealizadoRepository>();

    private FakeTrabajoRealizadoImagenRepository Imagenes
        => (FakeTrabajoRealizadoImagenRepository)_factory.Services
            .GetRequiredService<ITrabajoRealizadoImagenRepository>();

    private FakeStorageService Storage
        => (FakeStorageService)_factory.Services.GetRequiredService<IStorageService>();

    private HttpRequestMessage AuthenticatedRequest(HttpMethod method, string url, string token = AdminToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static StringContent Json(object payload)
        => new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

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
    // Autorización
    // ------------------------------------------------------------------

    [Fact]
    public async Task List_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/trabajos-realizados");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_WithoutPermission_ReturnsForbidden()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/trabajos-realizados", LimitedToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync(
            "/api/v1/trabajos-realizados", Json(new { nombre = "X" }));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ------------------------------------------------------------------
    // CRUD
    // ------------------------------------------------------------------

    [Fact]
    public async Task Create_WithPermission_ReturnsCreated()
    {
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/trabajos-realizados");
        request.Content = Json(new
        {
            nombre = "Cocina Nueva",
            tipoMueble = "Cocina",
            categoria = "Cocinas",
            tags = new[] { "cocina", "a-medida" },
            publicado = true,
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Cocina Nueva", body.RootElement.GetProperty("nombre").GetString());
        Assert.True(body.RootElement.GetProperty("publicado").GetBoolean());
        Assert.Equal(2, body.RootElement.GetProperty("tags").GetArrayLength());
    }

    [Fact]
    public async Task Create_WithoutNombre_ReturnsBadRequest()
    {
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/trabajos-realizados");
        request.Content = Json(new { nombre = "   " });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithPermission_ReturnsDetailWithGallery()
    {
        var trabajo = Trabajos.Seed(nombre: "Obra Detalle Admin", publicado: false);
        Imagenes.Seed(trabajo.Id, $"trabajos/{trabajo.Id}/a.jpg");

        var request = AuthenticatedRequest(HttpMethod.Get, $"/api/v1/trabajos-realizados/{trabajo.Id}");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Obra Detalle Admin", body.RootElement.GetProperty("nombre").GetString());
        Assert.False(body.RootElement.GetProperty("publicado").GetBoolean());
        Assert.Single(body.RootElement.GetProperty("imagenes").EnumerateArray());
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNotFound()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/trabajos-realizados/99999");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Patch_UpdatesFields()
    {
        var trabajo = Trabajos.Seed(nombre: "Antes", tipoMueble: "Ropero");

        var request = AuthenticatedRequest(HttpMethod.Patch, $"/api/v1/trabajos-realizados/{trabajo.Id}");
        request.Content = Json(new { nombre = "Después", publicado = true, destacado = true });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Después", body.RootElement.GetProperty("nombre").GetString());
        Assert.Equal("Ropero", body.RootElement.GetProperty("tipoMueble").GetString());
        Assert.True(body.RootElement.GetProperty("publicado").GetBoolean());
        Assert.True(body.RootElement.GetProperty("destacado").GetBoolean());
    }

    [Fact]
    public async Task Patch_Unknown_ReturnsNotFound()
    {
        var request = AuthenticatedRequest(HttpMethod.Patch, "/api/v1/trabajos-realizados/99999");
        request.Content = Json(new { nombre = "X" });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithoutPermission_ReturnsForbidden()
    {
        var trabajo = Trabajos.Seed();
        var before = Trabajos.All.Count;
        var request = AuthenticatedRequest(
            HttpMethod.Delete, $"/api/v1/trabajos-realizados/{trabajo.Id}", LimitedToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(before, Trabajos.All.Count);
    }

    [Fact]
    public async Task Delete_Existing_ReturnsNoContentAndRemovesWorkAndImages()
    {
        var trabajo = Trabajos.Seed();
        Imagenes.Seed(trabajo.Id, $"trabajos/{trabajo.Id}/a.jpg");
        var before = Trabajos.All.Count;
        var deletesBefore = Storage.DeletedPaths.Count;

        var request = AuthenticatedRequest(HttpMethod.Delete, $"/api/v1/trabajos-realizados/{trabajo.Id}");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(before - 1, Trabajos.All.Count);
        Assert.Empty(Imagenes.All);
        Assert.Equal(deletesBefore + 1, Storage.DeletedPaths.Count);
    }

    [Fact]
    public async Task Delete_Unknown_ReturnsNotFound()
    {
        var request = AuthenticatedRequest(HttpMethod.Delete, "/api/v1/trabajos-realizados/99999");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ------------------------------------------------------------------
    // Imágenes
    // ------------------------------------------------------------------

    [Fact]
    public async Task UploadImage_WithPermission_ReturnsCreated()
    {
        var trabajo = Trabajos.Seed();

        var request = AuthenticatedRequest(
            HttpMethod.Post, $"/api/v1/trabajos-realizados/{trabajo.Id}/images");
        request.Content = ImageContent("foto.png", [1, 2, 3]);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.StartsWith($"{Storage.BaseUrl}/trabajos/{trabajo.Id}/", body.RootElement.GetProperty("url").GetString());
    }

    [Fact]
    public async Task UploadImage_WithInvalidExtension_ReturnsBadRequest()
    {
        var trabajo = Trabajos.Seed();

        var request = AuthenticatedRequest(
            HttpMethod.Post, $"/api/v1/trabajos-realizados/{trabajo.Id}/images");
        request.Content = ImageContent("mal.exe", [1, 2, 3], "application/octet-stream");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadImage_UnknownWork_ReturnsNotFound()
    {
        var request = AuthenticatedRequest(
            HttpMethod.Post, "/api/v1/trabajos-realizados/99999/images");
        request.Content = ImageContent("foto.png", [1, 2, 3]);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteImage_Existing_ReturnsNoContent()
    {
        var trabajo = Trabajos.Seed();
        var imagen = Imagenes.Seed(trabajo.Id, $"trabajos/{trabajo.Id}/a.jpg");
        var deletesBefore = Storage.DeletedPaths.Count;

        var request = AuthenticatedRequest(
            HttpMethod.Delete, $"/api/v1/trabajos-realizados/{trabajo.Id}/images/{imagen.Id}");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(deletesBefore + 1, Storage.DeletedPaths.Count);
        Assert.DoesNotContain(Imagenes.All, i => i.Id == imagen.Id);
    }

    [Fact]
    public async Task DeleteImage_OfAnotherWork_ReturnsNotFound()
    {
        var trabajoA = Trabajos.Seed();
        var trabajoB = Trabajos.Seed();
        var imagen = Imagenes.Seed(trabajoB.Id);
        var imagenesBefore = Imagenes.All.Count;

        var request = AuthenticatedRequest(
            HttpMethod.Delete, $"/api/v1/trabajos-realizados/{trabajoA.Id}/images/{imagen.Id}");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(imagenesBefore, Imagenes.All.Count);
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
