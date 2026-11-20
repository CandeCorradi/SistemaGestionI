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
using SistemaGestion.Application.Usuarios.Dtos;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;
using Xunit;

namespace SistemaGestion.API.IntegrationTests;

/// <summary>
/// Tests de API de /api/v1/users: gate de autenticación, autorización por
/// permisos (USUARIOS_VIEW, etc.) y comportamiento HTTP de los casos de uso,
/// con repositorios en memoria (AGENTS.md §41).
/// </summary>
public sealed class UsersApiTests : IClassFixture<UsersApiTests.Factory>
{
    private const string ValidToken = "users-test-token";
    private const string ValidUid = "uid-users-test";

    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IFirebaseAuthService>(new TokenStubFirebaseAuthService());
                services.AddSingleton<IUsuarioRepository>(new InMemoryUsuarioRepository());
                services.AddSingleton<IRolRepository>(new InMemoryRolRepository());
                services.AddSingleton<IPermisoRepository>(
                    FakePermisoRepository.ConTodosLosPermisos(ValidUid));
            });
        }
    }

    private readonly HttpClient _client;

    public UsersApiTests(Factory factory)
    {
        _client = factory.CreateClient();
    }

    private HttpRequestMessage AuthenticatedRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ValidToken);
        return request;
    }

    [Fact]
    public async Task List_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_WithToken_ReturnsPaginatedUsers()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/users?page=1&pageSize=20");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, body.RootElement.GetProperty("page").GetInt32());
        Assert.Equal(20, body.RootElement.GetProperty("pageSize").GetInt32());
        Assert.True(body.RootElement.GetProperty("totalItems").GetInt32() >= 0);
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreated()
    {
        var dto = new CreateUsuarioDto($"creado-{Guid.NewGuid():N}@mail.com", "Ana", "Torres", RolId: 4);
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/users");
        request.Content = JsonContent.Create(dto);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Ana", body.RootElement.GetProperty("nombre").GetString());
        Assert.Equal(4, body.RootElement.GetProperty("rolId").GetInt32());
        Assert.True(body.RootElement.GetProperty("id").GetInt32() > 0);
    }

    [Fact]
    public async Task Create_WithUnknownRole_ReturnsBadRequest()
    {
        var dto = new CreateUsuarioDto($"rol-{Guid.NewGuid():N}@mail.com", "Ana", "Torres", RolId: 999);
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/users");
        request.Content = JsonContent.Create(dto);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutEmail_ReturnsBadRequest()
    {
        var dto = new CreateUsuarioDto("", "Ana", "Torres", RolId: 4);
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/users");
        request.Content = JsonContent.Create(dto);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Validation.Error", body.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/users/99999");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Patch_ExistingUser_UpdatesAndDeactivates()
    {
        var createDto = new CreateUsuarioDto($"patch-{Guid.NewGuid():N}@mail.com", "Luis", "Sosa", RolId: 4);
        var createRequest = AuthenticatedRequest(HttpMethod.Post, "/api/v1/users");
        createRequest.Content = JsonContent.Create(createDto);
        var createResponse = await _client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetInt32();

        var patchRequest = AuthenticatedRequest(HttpMethod.Patch, $"/api/v1/users/{id}");
        patchRequest.Content = JsonContent.Create(new UpdateUsuarioDto(Apellido: "Ramirez", Activo: false));
        var patchResponse = await _client.SendAsync(patchRequest);

        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        using var patched = JsonDocument.Parse(await patchResponse.Content.ReadAsStringAsync());
        Assert.Equal("Ramirez", patched.RootElement.GetProperty("apellido").GetString());
        Assert.False(patched.RootElement.GetProperty("activo").GetBoolean());
        Assert.Equal("Luis", patched.RootElement.GetProperty("nombre").GetString());
    }

    private sealed class TokenStubFirebaseAuthService : IFirebaseAuthService
    {
        public Task<FirebaseAuthResult?> VerifyTokenAsync(
            string idToken,
            CancellationToken cancellationToken = default)
        {
            FirebaseAuthResult? result = idToken == ValidToken
                ? new FirebaseAuthResult("uid-users-test", "test@example.com", EmailVerified: true)
                : null;

            return Task.FromResult(result);
        }
    }

    private sealed class InMemoryUsuarioRepository : IUsuarioRepository
    {
        private readonly List<Usuario> _usuarios = new();

        public Task<PagedResult<Usuario>> GetPagedAsync(
            int page,
            int pageSize,
            string? search,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Usuario> query = _usuarios;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                query = query.Where(u =>
                    u.Nombre.ToLower().Contains(term)
                    || u.Apellido.ToLower().Contains(term)
                    || u.Email.ToLower().Contains(term));
            }

            var list = query.OrderBy(u => u.Id).ToList();

            return Task.FromResult(new PagedResult<Usuario>
            {
                Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = list.Count,
            });
        }

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
        {
            var normalized = email.Trim().ToLowerInvariant();
            return Task.FromResult(_usuarios.Any(u =>
                u.Email == normalized && (excludeId == null || u.Id != excludeId)));
        }

        public Task<Usuario> AddAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            usuario.Id = _usuarios.Count == 0 ? 1 : _usuarios.Max(u => u.Id) + 1;
            _usuarios.Add(usuario);
            return Task.FromResult(usuario);
        }

        public Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<int>> GetActiveStaffUserIdsAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<int>>(Array.Empty<int>());
    }

    private sealed class InMemoryRolRepository : IRolRepository
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
