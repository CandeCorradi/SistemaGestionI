using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SistemaGestion.API;
using SistemaGestion.API.IntegrationTests.Fakes;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using Xunit;

namespace SistemaGestion.API.IntegrationTests;

/// <summary>
/// Tests de API de /api/v1/roles: lectura de los cinco roles fijos (BR-001)
/// y autorización por permiso ROLES_VIEW.
/// </summary>
public sealed class RolesApiTests : IClassFixture<RolesApiTests.Factory>
{
    private const string ValidToken = "roles-test-token";
    private const string ValidUid = "uid-roles-test";

    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IFirebaseAuthService>(new TokenStubFirebaseAuthService());
                services.AddSingleton<IRolRepository>(new FiveFixedRolesRepository());
                services.AddSingleton<IPermisoRepository>(
                    FakePermisoRepository.ConTodosLosPermisos(ValidUid));
            });
        }
    }

    private readonly HttpClient _client;

    public RolesApiTests(Factory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task List_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/roles");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_WithToken_ReturnsFiveFixedRoles()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/roles");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ValidToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, body.RootElement.ValueKind);
        Assert.Equal(5, body.RootElement.GetArrayLength());

        var codigos = body.RootElement.EnumerateArray()
            .Select(r => r.GetProperty("codigo").GetString())
            .ToList();

        Assert.Equal(
            new[] { "SuperAdmin", "Owner", "Admin", "Empleado", "Cliente" },
            codigos);
    }

    private sealed class TokenStubFirebaseAuthService : IFirebaseAuthService
    {
        public Task<FirebaseAuthResult?> VerifyTokenAsync(
            string idToken,
            CancellationToken cancellationToken = default)
        {
            FirebaseAuthResult? result = idToken == ValidToken
                ? new FirebaseAuthResult("uid-roles-test", "test@example.com", EmailVerified: true)
                : null;

            return Task.FromResult(result);
        }
    }

    private sealed class FiveFixedRolesRepository : IRolRepository
    {
        private readonly List<Rol> _roles =
        [
            new(Rol.SuperAdminCodigo, Rol.SuperAdminCodigo) { Id = 1 },
            new(Rol.OwnerCodigo, Rol.OwnerCodigo) { Id = 2 },
            new(Rol.AdminCodigo, Rol.AdminCodigo) { Id = 3 },
            new(Rol.EmpleadoCodigo, Rol.EmpleadoCodigo) { Id = 4 },
            new(Rol.ClienteCodigo, Rol.ClienteCodigo) { Id = 5 },
        ];

        public Task<IReadOnlyList<Rol>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Rol>>(_roles);

        public Task<Rol?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(_roles.FirstOrDefault(r => r.Id == id));
    }
}
