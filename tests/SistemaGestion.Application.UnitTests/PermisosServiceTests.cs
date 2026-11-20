using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Permisos;
using SistemaGestion.Application.Permisos.Dtos;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;
using Xunit;
using PermisosCatalogo = SistemaGestion.Domain.Authorization.Permisos;

namespace SistemaGestion.Application.UnitTests;

public sealed class PermisosServiceTests
{
    [Fact]
    public async Task GetCatalog_ReturnsAllPermissionsOrderedById()
    {
        var (service, _, _) = CreateService();

        var result = await service.GetCatalogAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(PermisosCatalogo.Catalogo.Count, result.Value.Count);
        Assert.Equal(PermisosCatalogo.Todos, result.Value.Select(p => p.Codigo).ToList());
        Assert.Equal(
            result.Value.Select(p => p.Id).OrderBy(id => id),
            result.Value.Select(p => p.Id));
    }

    [Fact]
    public async Task GetUsuarioPermisos_KnownUser_ReturnsExplicitCodes()
    {
        var (service, usuarios, permisos) = CreateService();
        var usuario = await usuarios.SeedAsync();
        permisos.Explicitos[usuario.Id] = new List<int> { 1, 2 };

        var result = await service.GetUsuarioPermisosAsync(usuario.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(usuario.Id, result.Value.UsuarioId);
        Assert.Equal(new[] { "CLIENTES_VIEW", "CLIENTES_CREATE" }, result.Value.Codigos);
    }

    [Fact]
    public async Task GetUsuarioPermisos_UnknownUser_ReturnsNotFound()
    {
        var (service, _, _) = CreateService();

        var result = await service.GetUsuarioPermisosAsync(999);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetEffectivePermisos_KnownUid_ReturnsUnionOfRoleAndExplicit()
    {
        var (service, _, permisos) = CreateService();
        permisos.Efectivos["uid-1"] = new List<string> { PermisosCatalogo.ClientesView, PermisosCatalogo.PresupuestosView };

        var result = await service.GetEffectivePermisosAsync("uid-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { PermisosCatalogo.ClientesView, PermisosCatalogo.PresupuestosView }, result.Value.Codigos);
    }

    [Fact]
    public async Task GetEffectivePermisos_UnknownUid_ReturnsNotFound()
    {
        var (service, _, _) = CreateService();

        var result = await service.GetEffectivePermisosAsync("uid-inexistente");

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetEffectivePermisos_WithoutUid_ReturnsNotFound()
    {
        var (service, _, _) = CreateService();

        var result = await service.GetEffectivePermisosAsync(null);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task Update_WithValidCodes_ReplacesExplicitPermissions()
    {
        var (service, usuarios, permisos) = CreateService();
        var usuario = await usuarios.SeedAsync();
        permisos.Explicitos[usuario.Id] = new List<int> { 1 };

        var result = await service.UpdateUsuarioPermisosAsync(
            usuario.Id,
            new UpdatePermisosUsuarioDto { Codigos = new[] { PermisosCatalogo.UsuariosView, PermisosCatalogo.ClientesView } });

        Assert.True(result.IsSuccess);
        Assert.Equal(
            new[] { PermisosCatalogo.ClientesView, PermisosCatalogo.UsuariosView },
            result.Value.Codigos.OrderBy(c => c, StringComparer.Ordinal).ToArray());

        var guardados = await service.GetUsuarioPermisosAsync(usuario.Id);
        Assert.Equal(2, guardados.Value.Codigos.Count);
        Assert.DoesNotContain(PermisosCatalogo.ClientesCreate, guardados.Value.Codigos);
    }

    [Fact]
    public async Task Update_WithEmptyList_ClearsExplicitPermissions()
    {
        var (service, usuarios, permisos) = CreateService();
        var usuario = await usuarios.SeedAsync();
        permisos.Explicitos[usuario.Id] = new List<int> { 1, 2 };

        var result = await service.UpdateUsuarioPermisosAsync(
            usuario.Id,
            new UpdatePermisosUsuarioDto { Codigos = Array.Empty<string>() });

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Codigos);

        var guardados = await service.GetUsuarioPermisosAsync(usuario.Id);
        Assert.Empty(guardados.Value.Codigos);
    }

    [Fact]
    public async Task Update_WithUnknownCode_ReturnsValidation()
    {
        var (service, usuarios, _) = CreateService();
        var usuario = await usuarios.SeedAsync();

        var result = await service.UpdateUsuarioPermisosAsync(
            usuario.Id,
            new UpdatePermisosUsuarioDto { Codigos = new[] { "PERMISO_INEXISTENTE" } });

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task Update_WithNullCodes_ReturnsValidation()
    {
        var (service, usuarios, _) = CreateService();
        var usuario = await usuarios.SeedAsync();

        var result = await service.UpdateUsuarioPermisosAsync(
            usuario.Id,
            new UpdatePermisosUsuarioDto { Codigos = null });

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Update_WithEmptyCode_ReturnsValidation(string codigo)
    {
        var (service, usuarios, _) = CreateService();
        var usuario = await usuarios.SeedAsync();

        var result = await service.UpdateUsuarioPermisosAsync(
            usuario.Id,
            new UpdatePermisosUsuarioDto { Codigos = new[] { codigo } });

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task Update_WithDuplicatedCodes_StoresEachPermissionOnce()
    {
        var (service, usuarios, _) = CreateService();
        var usuario = await usuarios.SeedAsync();

        var result = await service.UpdateUsuarioPermisosAsync(
            usuario.Id,
            new UpdatePermisosUsuarioDto
            {
                Codigos = new[] { PermisosCatalogo.ClientesView, PermisosCatalogo.ClientesView, "  CLIENTES_VIEW " },
            });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Codigos);
    }

    [Fact]
    public async Task Update_UnknownUser_ReturnsNotFound()
    {
        var (service, _, _) = CreateService();

        var result = await service.UpdateUsuarioPermisosAsync(
            999,
            new UpdatePermisosUsuarioDto { Codigos = Array.Empty<string>() });

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    private static (PermisosService Service, FakeUsuarioRepository Usuarios, FakePermisoRepository Permisos)
        CreateService()
    {
        var usuarios = new FakeUsuarioRepository();
        var permisos = new FakePermisoRepository();
        return (new PermisosService(permisos, usuarios), usuarios, permisos);
    }

    private sealed class FakePermisoRepository : IPermisoRepository
    {
        private readonly List<Permiso> _catalogo = PermisosCatalogo.Catalogo
            .Select(p => new Permiso(p.Codigo, p.Nombre) { Id = p.Id })
            .ToList();

        public Dictionary<int, List<int>> Explicitos { get; } = new();

        public Dictionary<string, List<string>> Efectivos { get; } = new(StringComparer.Ordinal);

        public Task<IReadOnlyList<Permiso>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Permiso>>(_catalogo.OrderBy(p => p.Id).ToList());

        public Task<IReadOnlyList<string>> GetExplicitCodesAsync(
            int usuarioId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<string> codigos = Explicitos.TryGetValue(usuarioId, out var ids)
                ? ids.OrderBy(id => id)
                    .Select(id => _catalogo.First(p => p.Id == id).Codigo)
                    .ToList()
                : new List<string>();

            return Task.FromResult(codigos);
        }

        public Task<IReadOnlyList<string>?> GetEffectiveCodesByFirebaseUidAsync(
            string firebaseUid,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<string>? codigos = Efectivos.TryGetValue(firebaseUid, out var lista)
                ? lista
                : null;

            return Task.FromResult(codigos);
        }

        public Task ReplaceExplicitAsync(
            int usuarioId,
            IReadOnlyCollection<int> permisoIds,
            CancellationToken cancellationToken = default)
        {
            Explicitos[usuarioId] = permisoIds.ToList();
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUsuarioRepository : IUsuarioRepository
    {
        private readonly List<Usuario> _usuarios = new();

        public async Task<Usuario> SeedAsync()
        {
            var usuario = Usuario.Crear("usuario@mail.com", "Juan", "Perez", rolId: 1);
            await AddAsync(usuario);
            return usuario;
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
}
