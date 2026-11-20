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
using SistemaGestion.Application.Clientes.Dtos;
using SistemaGestion.Domain.Authorization;
using SistemaGestion.Shared.Results;
using Xunit;

namespace SistemaGestion.API.IntegrationTests;

/// <summary>
/// Tests de API de /api/v1/customers (api-spec §10): autenticación,
/// autorización por permisos CLIENTES_*, validaciones, duplicados y
/// desactivación lógica (BR-011) (AGENTS.md §41).
/// </summary>
public sealed class CustomersApiTests : IClassFixture<CustomersApiTests.Factory>
{
    private const string AdminToken = "customers-admin-token";
    private const string LimitedToken = "customers-limited-token";
    private const string AdminUid = "uid-customers-admin";
    private const string LimitedUid = "uid-customers-limited";

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

                services.AddSingleton<IClienteRepository>(new FakeClienteRepository());
            });
        }
    }

    private readonly Factory _factory;
    private readonly HttpClient _client;

    public CustomersApiTests(Factory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private FakeClienteRepository Clientes
        => (FakeClienteRepository)_factory.Services.GetRequiredService<IClienteRepository>();

    private HttpRequestMessage AuthenticatedRequest(HttpMethod method, string url, string token = AdminToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    [Fact]
    public async Task List_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/customers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_WithoutRequiredPermission_ReturnsForbidden()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/customers", LimitedToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task List_WithPermission_ReturnsPaginatedCustomers()
    {
        Clientes.Seed("maria@mail.com");
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/customers?page=1&pageSize=20");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, body.RootElement.GetProperty("page").GetInt32());
        Assert.True(body.RootElement.GetProperty("totalItems").GetInt32() >= 1);
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreated_AndNormalizesEmail()
    {
        var dto = new CreateClienteDto($"cliente-{Guid.NewGuid():N}", "Lopez", "Nuevo.Mail@Email.COM");
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/customers");
        request.Content = JsonContent.Create(dto);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("nuevo.mail@email.com", body.RootElement.GetProperty("email").GetString());
        Assert.Equal("Lopez", body.RootElement.GetProperty("apellido").GetString());
        Assert.True(body.RootElement.GetProperty("activo").GetBoolean());
        Assert.True(body.RootElement.GetProperty("id").GetInt32() > 0);
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_ReturnsConflict()
    {
        Clientes.Seed("duplicado@mail.com");

        var dto = new CreateClienteDto("Otra", "Persona", "DUPLICADO@mail.com");
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/customers");
        request.Content = JsonContent.Create(dto);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutNombre_ReturnsBadRequest()
    {
        var dto = new CreateClienteDto("", "Lopez", "nuevo@mail.com");
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/customers");
        request.Content = JsonContent.Create(dto);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Validation.Error", body.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Create_WithoutPermission_ReturnsForbidden()
    {
        var dto = new CreateClienteDto("Sin", "Permiso", "sinpermiso@mail.com");
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/customers", LimitedToken);
        request.Content = JsonContent.Create(dto);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/customers/99999");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Patch_ExistingCustomer_UpdatesProvidedFields()
    {
        var seeded = Clientes.Seed("patch@mail.com", "Ana", "Ruiz");

        var request = AuthenticatedRequest(HttpMethod.Patch, $"/api/v1/customers/{seeded.Id}");
        request.Content = JsonContent.Create(new UpdateClienteDto(Apellido: "Gomez", Notas: "Llamar por WhatsApp"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Gomez", body.RootElement.GetProperty("apellido").GetString());
        Assert.Equal("Ana", body.RootElement.GetProperty("nombre").GetString());
        Assert.Equal("Llamar por WhatsApp", body.RootElement.GetProperty("notas").GetString());
    }

    [Fact]
    public async Task Delete_ExistingCustomer_DeactivatesWithoutRemoving()
    {
        var seeded = Clientes.Seed("borrar@mail.com");

        var request = AuthenticatedRequest(HttpMethod.Delete, $"/api/v1/customers/{seeded.Id}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(body.RootElement.GetProperty("activo").GetBoolean());

        Assert.Contains(Clientes.All, c => c.Id == seeded.Id);
        Assert.False(Clientes.All.Single(c => c.Id == seeded.Id).Activo);
    }

    [Fact]
    public async Task Delete_WithoutPermission_ReturnsForbidden()
    {
        var seeded = Clientes.Seed("sinpermiso-delete@mail.com");

        var request = AuthenticatedRequest(
            HttpMethod.Delete,
            $"/api/v1/customers/{seeded.Id}",
            LimitedToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(Clientes.All.Single(c => c.Id == seeded.Id).Activo);
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
