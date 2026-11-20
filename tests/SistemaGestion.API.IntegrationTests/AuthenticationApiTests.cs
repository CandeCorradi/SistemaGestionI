using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SistemaGestion.API;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;
using Xunit;

namespace SistemaGestion.API.IntegrationTests;

/// <summary>
/// Gate de autenticación (roadmap §10.5): 401 sin token, 401 con token
/// inválido, 200 con token válido, y rutas públicas sin protección.
/// </summary>
public sealed class AuthenticationApiTests : IClassFixture<AuthenticationApiTests.Factory>
{
    private const string ExpectedUid = "uid-test-123";
    private const string ExpectedEmail = "cliente@test.com";

    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IFirebaseAuthService>(new StubFirebaseAuthService());
                services.AddSingleton<IUsuarioRepository>(new StubUsuarioRepository());
                services.AddSingleton<IRolRepository>(new StubRolRepository());
            });
        }
    }

    private readonly HttpClient _client;

    public AuthenticationApiTests(Factory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithInvalidToken_ReturnsUnauthorized()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "token-invalido");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithMalformedAuthorizationHeader_ReturnsUnauthorized()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", "dXNlcjpwYXNz");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsAuthenticatedIdentity()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", StubFirebaseAuthService.ValidToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(ExpectedUid, body.RootElement.GetProperty("uid").GetString());
        Assert.Equal(ExpectedEmail, body.RootElement.GetProperty("email").GetString());
        Assert.True(body.RootElement.GetProperty("emailVerified").GetBoolean());
    }

    [Fact]
    public async Task Health_WithoutToken_RemainsAnonymous()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Profile_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/auth/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_WithValidToken_ReturnsLinkedProfile()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/profile");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", StubFirebaseAuthService.ValidToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("admin@test.com", body.RootElement.GetProperty("email").GetString());
        Assert.Equal("Ada", body.RootElement.GetProperty("nombre").GetString());
        Assert.Equal("SuperAdmin", body.RootElement.GetProperty("rol").GetString());
        Assert.True(body.RootElement.GetProperty("activo").GetBoolean());
    }

    [Fact]
    public async Task Profile_WithUnlinkedUid_ReturnsNotFound()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/profile");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", StubFirebaseAuthService.UnlinkedToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed class StubUsuarioRepository : IUsuarioRepository
    {
        private readonly List<Usuario> _usuarios = new();

        public StubUsuarioRepository()
        {
            var admin = Usuario.Crear("admin@test.com", "Ada", "Lopez", rolId: 1);
            admin.Id = 1;
            admin.VincularFirebaseUid(ExpectedUid);
            _usuarios.Add(admin);
        }

        public Task<PagedResult<Usuario>> GetPagedAsync(
            int page,
            int pageSize,
            string? search,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<Usuario>
            {
                Items = _usuarios,
                Page = page,
                PageSize = pageSize,
                TotalItems = _usuarios.Count,
            });

        public Task<Usuario?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(_usuarios.FirstOrDefault(u => u.Id == id));

        public Task<Usuario?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return Task.FromResult(_usuarios.FirstOrDefault(u => u.Email == normalized));
        }

        public Task<Usuario?> GetByFirebaseUidAsync(
            string firebaseUid,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_usuarios.FirstOrDefault(u =>
                u.FirebaseUid == firebaseUid && u.Activo));

        public Task<bool> EmailExistsAsync(
            string email,
            int? excludeId = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<Usuario> AddAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            _usuarios.Add(usuario);
            return Task.FromResult(usuario);
        }

        public Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<int>> GetActiveStaffUserIdsAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<int>>(Array.Empty<int>());
    }

    private sealed class StubRolRepository : IRolRepository
    {
        private readonly List<Rol> _roles =
        [
            new(Rol.SuperAdminCodigo, Rol.SuperAdminCodigo) { Id = 1 },
        ];

        public Task<IReadOnlyList<Rol>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Rol>>(_roles);

        public Task<Rol?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(_roles.FirstOrDefault(r => r.Id == id));
    }

    private sealed class StubFirebaseAuthService : IFirebaseAuthService
    {
        public const string ValidToken = "valid-test-token";

        public const string UnlinkedToken = "unlinked-test-token";

        public Task<FirebaseAuthResult?> VerifyTokenAsync(
            string idToken,
            CancellationToken cancellationToken = default)
        {
            FirebaseAuthResult? result = idToken switch
            {
                ValidToken => new FirebaseAuthResult(ExpectedUid, ExpectedEmail, EmailVerified: true),
                UnlinkedToken => new FirebaseAuthResult("uid-sin-vinculo", "otro@test.com", EmailVerified: true),
                _ => null,
            };

            return Task.FromResult(result);
        }
    }
}
