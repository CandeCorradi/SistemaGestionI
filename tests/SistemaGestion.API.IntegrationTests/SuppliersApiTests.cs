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
using SistemaGestion.Application.Proveedores.Dtos;
using SistemaGestion.Domain.Authorization;
using SistemaGestion.Shared.Results;
using Xunit;

namespace SistemaGestion.API.IntegrationTests;

/// <summary>
/// Tests de API de /api/v1/suppliers (api-spec §11): autenticación,
/// autorización por permisos PROVEEDORES_*, validaciones, duplicados y
/// desactivación lógica sin borrar historial (BR-021) (AGENTS.md §41).
/// Terminología de dominio: "Proveedor", nunca "Mayorista" (BR-020).
/// </summary>
public sealed class SuppliersApiTests : IClassFixture<SuppliersApiTests.Factory>
{
    private const string AdminToken = "suppliers-admin-token";
    private const string LimitedToken = "suppliers-limited-token";
    private const string AdminUid = "uid-suppliers-admin";
    private const string LimitedUid = "uid-suppliers-limited";

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

                services.AddSingleton<IProveedorRepository>(new FakeProveedorRepository());
            });
        }
    }

    private readonly Factory _factory;
    private readonly HttpClient _client;

    public SuppliersApiTests(Factory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private FakeProveedorRepository Proveedores
        => (FakeProveedorRepository)_factory.Services.GetRequiredService<IProveedorRepository>();

    private HttpRequestMessage AuthenticatedRequest(HttpMethod method, string url, string token = AdminToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    [Fact]
    public async Task List_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/suppliers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_WithoutRequiredPermission_ReturnsForbidden()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/suppliers", LimitedToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task List_WithPermission_ReturnsPaginatedSuppliers()
    {
        Proveedores.Seed(email: "listado@mail.com");
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/suppliers?page=1&pageSize=20");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, body.RootElement.GetProperty("page").GetInt32());
        Assert.True(body.RootElement.GetProperty("totalItems").GetInt32() >= 1);
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreated_AndNormalizesEmail()
    {
        var dto = new CreateProveedorDto($"Melaminas {Guid.NewGuid():N}", Email: "  Ventas@Mail.COM ");
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/suppliers");
        request.Content = JsonContent.Create(dto);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ventas@mail.com", body.RootElement.GetProperty("email").GetString());
        Assert.True(body.RootElement.GetProperty("activo").GetBoolean());
        Assert.True(body.RootElement.GetProperty("id").GetInt32() > 0);
    }

    [Fact]
    public async Task Create_WithoutEmail_ReturnsCreated()
    {
        var dto = new CreateProveedorDto($"Sin Email {Guid.NewGuid():N}");
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/suppliers");
        request.Content = JsonContent.Create(dto);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("email").ValueKind);
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_ReturnsConflict()
    {
        Proveedores.Seed(email: "duplicado-proveedor@mail.com");

        var dto = new CreateProveedorDto("Otro Proveedor", Email: "DUPLICADO-proveedor@mail.com");
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/suppliers");
        request.Content = JsonContent.Create(dto);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutNombre_ReturnsBadRequest()
    {
        var dto = new CreateProveedorDto("", Email: "valido@mail.com");
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/suppliers");
        request.Content = JsonContent.Create(dto);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Validation.Error", body.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Create_WithoutPermission_ReturnsForbidden()
    {
        var dto = new CreateProveedorDto("Sin Permiso", Email: "sinpermiso-proveedor@mail.com");
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/suppliers", LimitedToken);
        request.Content = JsonContent.Create(dto);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain(Proveedores.All, p => p.Email == "sinpermiso-proveedor@mail.com");
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/suppliers/99999");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_KnownId_ReturnsSupplier()
    {
        var seeded = Proveedores.Seed("Proveedor Get", email: "get@mail.com");

        var request = AuthenticatedRequest(HttpMethod.Get, $"/api/v1/suppliers/{seeded.Id}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Proveedor Get", body.RootElement.GetProperty("nombre").GetString());
        Assert.Equal("get@mail.com", body.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Patch_ExistingSupplier_UpdatesProvidedFields()
    {
        var seeded = Proveedores.Seed("Proveedor Patch", email: "patch@mail.com");

        var request = AuthenticatedRequest(HttpMethod.Patch, $"/api/v1/suppliers/{seeded.Id}");
        request.Content = JsonContent.Create(
            new UpdateProveedorDto(Contacto: "Juan Perez", Notas: "Entrega los martes"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Proveedor Patch", body.RootElement.GetProperty("nombre").GetString());
        Assert.Equal("patch@mail.com", body.RootElement.GetProperty("email").GetString());
        Assert.Equal("Juan Perez", body.RootElement.GetProperty("contacto").GetString());
        Assert.Equal("Entrega los martes", body.RootElement.GetProperty("notas").GetString());
    }

    [Fact]
    public async Task Patch_WithDuplicateEmail_ReturnsConflict()
    {
        Proveedores.Seed(email: "ocupado@mail.com");
        var target = Proveedores.Seed("Target", email: "propio@mail.com");

        var request = AuthenticatedRequest(HttpMethod.Patch, $"/api/v1/suppliers/{target.Id}");
        request.Content = JsonContent.Create(new UpdateProveedorDto(Email: "ocupado@mail.com"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingSupplier_DeactivatesWithoutRemoving()
    {
        var seeded = Proveedores.Seed("Proveedor Inactivable", email: "borrar-proveedor@mail.com");

        var request = AuthenticatedRequest(HttpMethod.Delete, $"/api/v1/suppliers/{seeded.Id}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(body.RootElement.GetProperty("activo").GetBoolean());

        Assert.Contains(Proveedores.All, p => p.Id == seeded.Id);
        Assert.False(Proveedores.All.Single(p => p.Id == seeded.Id).Activo);
    }

    [Fact]
    public async Task Delete_WithoutPermission_ReturnsForbidden()
    {
        var seeded = Proveedores.Seed("No Se Borra", email: "sinpermiso-delete-proveedor@mail.com");

        var request = AuthenticatedRequest(
            HttpMethod.Delete,
            $"/api/v1/suppliers/{seeded.Id}",
            LimitedToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(Proveedores.All.Single(p => p.Id == seeded.Id).Activo);
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
