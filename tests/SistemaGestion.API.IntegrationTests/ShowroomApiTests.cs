using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SistemaGestion.API;
using SistemaGestion.API.IntegrationTests.Fakes;
using SistemaGestion.Application.Abstractions;
using Xunit;

namespace SistemaGestion.API.IntegrationTests;

/// <summary>
/// Tests de API de la vidriera pública /api/v1/showroom/works
/// (api-spec §12B, AGENTS.md §23): acceso sin token, exclusión de no
/// publicados (BR-190), privacidad del nombre de cliente (BR-191) y
/// galería de imágenes (AGENTS.md §41).
/// </summary>
public sealed class ShowroomApiTests : IClassFixture<ShowroomApiTests.Factory>
{
    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IFirebaseAuthService>(new TokenStubFirebaseAuthService());
                services.AddSingleton<ITrabajoRealizadoRepository>(new FakeTrabajoRealizadoRepository());
                services.AddSingleton<ITrabajoRealizadoImagenRepository>(new FakeTrabajoRealizadoImagenRepository());
                services.AddSingleton<IStorageService>(new FakeStorageService());
            });
        }
    }

    private readonly Factory _factory;
    private readonly HttpClient _client;

    public ShowroomApiTests(Factory factory)
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

    [Fact]
    public async Task List_WithoutToken_ReturnsOk()
    {
        Trabajos.Seed(nombre: "Obra Pública", publicado: true);

        var response = await _client.GetAsync("/api/v1/showroom/works");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, body.RootElement.GetProperty("page").GetInt32());
        Assert.True(body.RootElement.GetProperty("totalItems").GetInt32() >= 1);
    }

    [Fact]
    public async Task List_ExcludesUnpublished()
    {
        Trabajos.Seed(nombre: "Showroom Pub", publicado: true);
        Trabajos.Seed(nombre: "Showroom Borrador", publicado: false);

        var response = await _client.GetAsync("/api/v1/showroom/works?pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var nombres = body.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(i => i.GetProperty("nombre").GetString())
            .ToList();
        Assert.Contains("Showroom Pub", nombres);
        Assert.DoesNotContain("Showroom Borrador", nombres);
    }

    [Fact]
    public async Task List_DoesNotExposeInternalFields()
    {
        Trabajos.Seed(nombre: "Obra Priv", publicado: true);

        var response = await _client.GetAsync("/api/v1/showroom/works");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var item = body.RootElement.GetProperty("items").EnumerateArray().First();
        var propiedades = item.EnumerateObject().Select(p => p.Name).ToHashSet();
        Assert.DoesNotContain("publicado", propiedades);
        Assert.DoesNotContain("publicarNombreCliente", propiedades);
        Assert.DoesNotContain("fechaCreacion", propiedades);
        Assert.DoesNotContain("fechaActualizacion", propiedades);
        Assert.Contains("nombre", propiedades);
        Assert.Contains("destacado", propiedades);
    }

    [Fact]
    public async Task List_HidesClienteNombre_WhenNotConsented()
    {
        Trabajos.Seed(
            nombre: "Obra Sin Consentimiento",
            clienteNombre: "Laura Gómez",
            publicarNombreCliente: false,
            publicado: true);
        Trabajos.Seed(
            nombre: "Obra Con Consentimiento",
            clienteNombre: "Martín Pérez",
            publicarNombreCliente: true,
            publicado: true);

        var response = await _client.GetAsync("/api/v1/showroom/works?pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = body.RootElement.GetProperty("items").EnumerateArray().ToList();

        var sinConsentimiento = items.Single(i =>
            i.GetProperty("nombre").GetString() == "Obra Sin Consentimiento");
        Assert.Equal(JsonValueKind.Null, sinConsentimiento.GetProperty("clienteNombre").ValueKind);

        var conConsentimiento = items.Single(i =>
            i.GetProperty("nombre").GetString() == "Obra Con Consentimiento");
        Assert.Equal("Martín Pérez", conConsentimiento.GetProperty("clienteNombre").GetString());
    }

    [Fact]
    public async Task List_WithSearch_ReturnsOnlyMatches()
    {
        Trabajos.Seed(nombre: "Cocina Rosario", publicado: true);
        Trabajos.Seed(nombre: "Ropero Centro", publicado: true);

        var response = await _client.GetAsync("/api/v1/showroom/works?search=cocina");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = body.RootElement.GetProperty("items").EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Equal("Cocina Rosario", items[0].GetProperty("nombre").GetString());
    }

    [Fact]
    public async Task List_FeaturedFirst()
    {
        Trabajos.Seed(nombre: "Normal Order", destacado: false, publicado: true);
        Trabajos.Seed(nombre: "Destacado Order", destacado: true, publicado: true);

        var response = await _client.GetAsync("/api/v1/showroom/works?pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var first = body.RootElement.GetProperty("items").EnumerateArray().First();
        Assert.Equal("Destacado Order", first.GetProperty("nombre").GetString());
    }

    [Fact]
    public async Task List_WithPortada_ReturnsFirstImageUrl()
    {
        var trabajo = Trabajos.Seed(nombre: "Con Portada", publicado: true);
        Imagenes.Seed(trabajo.Id, $"trabajos/{trabajo.Id}/seg.jpg", orden: 1);
        Imagenes.Seed(trabajo.Id, $"trabajos/{trabajo.Id}/pri.jpg", orden: 0);

        var response = await _client.GetAsync("/api/v1/showroom/works");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var item = body.RootElement.GetProperty("items").EnumerateArray().First();
        Assert.Equal($"{Storage.BaseUrl}/trabajos/{trabajo.Id}/pri.jpg",
            item.GetProperty("imagenUrl").GetString());
    }

    [Fact]
    public async Task Detail_Unknown_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/v1/showroom/works/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Detail_Unpublished_ReturnsNotFound()
    {
        var trabajo = Trabajos.Seed(nombre: "No Publicado", publicado: false);

        var response = await _client.GetAsync($"/api/v1/showroom/works/{trabajo.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Detail_HidesClienteNombre_WhenNotConsented()
    {
        var trabajo = Trabajos.Seed(
            nombre: "Obra Detalle",
            clienteNombre: "Laura",
            publicarNombreCliente: false,
            publicado: true);

        var response = await _client.GetAsync($"/api/v1/showroom/works/{trabajo.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("clienteNombre").ValueKind);
    }

    [Fact]
    public async Task Detail_WithImages_ReturnsSortedGallery()
    {
        var trabajo = Trabajos.Seed(nombre: "Obra Galería", publicado: true);
        Imagenes.Seed(trabajo.Id, $"trabajos/{trabajo.Id}/seg.jpg", orden: 1);
        Imagenes.Seed(trabajo.Id, $"trabajos/{trabajo.Id}/pri.jpg", orden: 0);

        var response = await _client.GetAsync($"/api/v1/showroom/works/{trabajo.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var imagenes = body.RootElement.GetProperty("imagenes").EnumerateArray().ToList();
        Assert.Equal(2, imagenes.Count);
        Assert.EndsWith("/pri.jpg", imagenes[0].GetProperty("url").GetString());
        Assert.EndsWith("/seg.jpg", imagenes[1].GetProperty("url").GetString());
    }

    private sealed class TokenStubFirebaseAuthService : IFirebaseAuthService
    {
        public Task<FirebaseAuthResult?> VerifyTokenAsync(
            string idToken,
            CancellationToken cancellationToken = default)
            => Task.FromResult<FirebaseAuthResult?>(null);
    }
}
