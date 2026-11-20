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
using SistemaGestion.Application.Permisos.Dtos;
using SistemaGestion.Domain.Authorization;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;
using Xunit;

namespace SistemaGestion.API.IntegrationTests;

/// <summary>
/// Tests de API de autorizaciÃ³n granular (BR-002/BR-003, roadmap Â§10.4):
/// 401 sin token, 403 sin permiso, 200 con permiso, catÃ¡logo, permisos
/// efectivos del autenticado y reemplazo de permisos explÃ­citos.
/// </summary>
public sealed class PermissionsApiTests : IClassFixture<PermissionsApiTests.Factory>
{
    private const string AdminToken = "permissions-admin-token";
    private const string LimitedToken = "permissions-limited-token";
    private const string AdminUid = "uid-permissions-admin";
    private const string LimitedUid = "uid-permissions-limited";

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
                permisos.EfectivosPorUid[LimitedUid] = new List<string> { Permisos.ClientesView };
                services.AddSingleton<IPermisoRepository>(permisos);

                services.AddSingleton<IUsuarioRepository>(new InMemoryUsuarioRepository());
            });
        }
    }

    private readonly HttpClient _client;

    public PermissionsApiTests(Factory factory)
    {
        _client = factory.CreateClient();
    }

    private HttpRequestMessage AuthenticatedRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    [Fact]
    public async Task Catalog_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/permissions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Catalog_WithoutRequiredPermission_ReturnsForbidden()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/permissions", LimitedToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Catalog_WithPermission_ReturnsFullCatalog()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/permissions", AdminToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, body.RootElement.ValueKind);
        Assert.Equal(Permisos.Catalogo.Count, body.RootElement.GetArrayLength());

        var codigos = body.RootElement.EnumerateArray()
            .Select(p => p.GetProperty("codigo").GetString()!)
            .ToList();

        Assert.Equal(Permisos.Todos, codigos);
    }

    [Fact]
    public async Task MyPermissions_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/auth/permissions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MyPermissions_WithValidToken_ReturnsEffectivePermissions()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/auth/permissions", LimitedToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var codigos = body.RootElement.GetProperty("codigos")
            .EnumerateArray()
            .Select(c => c.GetString())
            .ToList();

        Assert.Equal(new[] { Permisos.ClientesView }, codigos);
    }

    [Fact]
    public async Task MyPermissions_WithAdminToken_ReturnsAllPermissions()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/auth/permissions", AdminToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(Permisos.Catalogo.Count, body.RootElement.GetProperty("codigos").GetArrayLength());
    }

    [Fact]
    public async Task UsersList_WithoutPermission_ReturnsForbidden()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/users", LimitedToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UsersList_WithPermission_ReturnsOk()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/users", AdminToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePermissions_WithPermission_ReplacesExplicitPermissions()
    {
        var request = AuthenticatedRequest(
            HttpMethod.Put,
            "/api/v1/users/1/permissions",
            AdminToken);
        request.Content = JsonContent.Create(new UpdatePermisosUsuarioDto
        {
            Codigos = new[] { Permisos.UsuariosView, Permisos.ClientesView },
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, body.RootElement.GetProperty("usuarioId").GetInt32());

        var codigos = body.RootElement.GetProperty("codigos")
            .EnumerateArray()
            .Select(c => c.GetString())
            .ToList();

        Assert.Equal(
            new[] { Permisos.ClientesView, Permisos.UsuariosView },
            codigos);

        var get = AuthenticatedRequest(HttpMethod.Get, "/api/v1/users/1/permissions", AdminToken);
        var getResponse = await _client.SendAsync(get);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var getBody = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, getBody.RootElement.GetProperty("codigos").GetArrayLength());
    }

    [Fact]
    public async Task UpdatePermissions_WithUnknownCode_ReturnsBadRequest()
    {
        var request = AuthenticatedRequest(
            HttpMethod.Put,
            "/api/v1/users/1/permissions",
            AdminToken);
        request.Content = JsonContent.Create(new UpdatePermisosUsuarioDto
        {
            Codigos = new[] { "PERMISO_INEXISTENTE" },
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Validation.Error", body.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task UpdatePermissions_UnknownUser_ReturnsNotFound()
    {
        var request = AuthenticatedRequest(
            HttpMethod.Put,
            "/api/v1/users/99999/permissions",
            AdminToken);
        request.Content = JsonContent.Create(new UpdatePermisosUsuarioDto
        {
            Codigos = Array.Empty<string>(),
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePermissions_WithoutPermission_ReturnsForbidden()
    {
        var request = AuthenticatedRequest(
            HttpMethod.Put,
            "/api/v1/users/1/permissions",
            LimitedToken);
        request.Content = JsonContent.Create(new UpdatePermisosUsuarioDto
        {
            Codigos = Array.Empty<string>(),
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePermissions_WithoutToken_ReturnsUnauthorized()
    {
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/users/1/permissions")
        {
            Content = JsonContent.Create(new UpdatePermisosUsuarioDto
            {
                Codigos = Array.Empty<string>(),
            }),
        };

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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

    private sealed class InMemoryUsuarioRepository : IUsuarioRepository
    {
        private readonly List<Usuario> _usuarios = new();

        public InMemoryUsuarioRepository()
        {
            var admin = Usuario.Crear("admin@test.com", "Ada", "Lopez", rolId: 1);
            admin.Id = 1;

            var empleado = Usuario.Crear("empleado@test.com", "Ema", "Rios", rolId: 4);
            empleado.Id = 2;

            _usuarios.Add(admin);
            _usuarios.Add(empleado);
        }

        public Task<PagedResult<Usuario>> GetPagedAsync(
            int page,
            int pageSize,
            string? search,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<Usuario>
            {
                Items = _usuarios.OrderBy(u => u.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList(),
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
            usuario.Id = _usuarios.Max(u => u.Id) + 1;
            _usuarios.Add(usuario);
            return Task.FromResult(usuario);
        }

        public Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<int>> GetActiveStaffUserIdsAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<int>>(Array.Empty<int>());
    }
}
