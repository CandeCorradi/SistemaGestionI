using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Usuarios;
using SistemaGestion.Application.Usuarios.Dtos;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;
using Xunit;

namespace SistemaGestion.Application.UnitTests;

public sealed class UsuariosServiceTests
{
    private const int RolEmpleado = 4;
    private const int RolInexistente = 999;

    [Fact]
    public async Task Create_WithValidData_ReturnsUser_AndNormalizesEmail()
    {
        var (service, repository, _) = CreateService();

        var result = await service.CreateAsync(
            new CreateUsuarioDto("Juan.Perez@Email.COM", "Juan", "Perez", RolEmpleado));

        Assert.True(result.IsSuccess);
        Assert.Equal("juan.perez@email.com", result.Value.Email);
        Assert.Equal(RolEmpleado, result.Value.RolId);
        Assert.True(result.Value.Activo);
        Assert.Single(repository.All);
    }

    [Fact]
    public async Task Create_WithUnknownRole_ReturnsValidation()
    {
        var (service, _, _) = CreateService();

        var result = await service.CreateAsync(
            new CreateUsuarioDto("juan@mail.com", "Juan", "Perez", RolInexistente));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_ReturnsConflict()
    {
        var (service, repository, _) = CreateService();
        await repository.SeedAsync("juan@mail.com");

        var result = await service.CreateAsync(
            new CreateUsuarioDto("JUAN@MAIL.COM", "Otro", "Usuario", RolEmpleado));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
    }

    [Theory]
    [InlineData("", "Juan", "Perez")]
    [InlineData("juan@mail.com", "", "Perez")]
    [InlineData("juan@mail.com", "Juan", "")]
    public async Task Create_WithMissingRequiredField_ReturnsValidation(
        string email,
        string nombre,
        string apellido)
    {
        var (service, _, _) = CreateService();

        var result = await service.CreateAsync(new CreateUsuarioDto(email, nombre, apellido, RolEmpleado));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetById_KnownId_ReturnsUser()
    {
        var (service, repository, _) = CreateService();
        var created = await repository.SeedAsync("juan@mail.com");

        var result = await service.GetByIdAsync(created.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(created.Id, result.Value.Id);
        Assert.Equal("juan@mail.com", result.Value.Email);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var (service, _, _) = CreateService();

        var result = await service.GetByIdAsync(999);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetProfile_LinkedUid_ReturnsProfileWithRole()
    {
        var (service, repository, _) = CreateService();
        var usuario = await repository.SeedAsync("juan@mail.com");
        usuario.VincularFirebaseUid("uid-linked");

        var result = await service.GetProfileAsync("uid-linked");

        Assert.True(result.IsSuccess);
        Assert.Equal("juan@mail.com", result.Value.Email);
        Assert.Equal("Juan", result.Value.Nombre);
        Assert.Equal("Perez", result.Value.Apellido);
        Assert.Equal("SuperAdmin", result.Value.Rol);
        Assert.True(result.Value.Activo);
    }

    [Fact]
    public async Task GetProfile_UnknownUid_ReturnsNotFound()
    {
        var (service, _, _) = CreateService();

        var result = await service.GetProfileAsync("uid-inexistente");

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetProfile_MissingUid_ReturnsNotFound(string? uid)
    {
        var (service, _, _) = CreateService();

        var result = await service.GetProfileAsync(uid);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetList_ClampsPageSizeToMaximum()
    {
        var (service, repository, _) = CreateService();
        for (var i = 0; i < 105; i++)
        {
            await repository.SeedAsync($"usuario{i}@mail.com");
        }

        var result = await service.GetListAsync(page: 1, pageSize: 500, search: null);

        Assert.True(result.IsSuccess);
        Assert.Equal(UsuariosService.MaxPageSize, result.Value.PageSize);
        Assert.Equal(105, result.Value.TotalItems);
        Assert.Equal(100, result.Value.Items.Count);
    }

    [Fact]
    public async Task GetList_WithSearch_FiltersByEmail()
    {
        var (service, repository, _) = CreateService();
        await repository.SeedAsync("juan@mail.com");
        await repository.SeedAsync("maria@mail.com");
        await repository.SeedAsync("pedro@otro.com");

        var result = await service.GetListAsync(page: 1, pageSize: 20, search: "MARIA");

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal("maria@mail.com", item.Email);
    }

    [Fact]
    public async Task Update_PartialPatch_KeepsCurrentValues()
    {
        var (service, repository, _) = CreateService();
        var created = await repository.SeedAsync("juan@mail.com");

        var result = await service.UpdateAsync(
            created.Id,
            new UpdateUsuarioDto(Apellido: "Gomez"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Juan", result.Value.Nombre);
        Assert.Equal("Gomez", result.Value.Apellido);
        Assert.Equal("juan@mail.com", result.Value.Email);
    }

    [Fact]
    public async Task Update_DeactivatesAccount()
    {
        var (service, repository, _) = CreateService();
        var created = await repository.SeedAsync("juan@mail.com");

        var result = await service.UpdateAsync(created.Id, new UpdateUsuarioDto(Activo: false));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Activo);
    }

    [Fact]
    public async Task Update_ChangesRole()
    {
        var (service, repository, _) = CreateService();
        var created = await repository.SeedAsync("juan@mail.com");

        var result = await service.UpdateAsync(created.Id, new UpdateUsuarioDto(RolId: 5));

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.RolId);
    }

    [Fact]
    public async Task Update_WithUnknownRole_ReturnsValidation()
    {
        var (service, repository, _) = CreateService();
        var created = await repository.SeedAsync("juan@mail.com");

        var result = await service.UpdateAsync(created.Id, new UpdateUsuarioDto(RolId: RolInexistente));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task Update_WithEmailOfAnotherUser_ReturnsConflict()
    {
        var (service, repository, _) = CreateService();
        await repository.SeedAsync("ocupado@mail.com");
        var target = await repository.SeedAsync("juan@mail.com");

        var result = await service.UpdateAsync(
            target.Id,
            new UpdateUsuarioDto(Email: "ocupado@mail.com"));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNotFound()
    {
        var (service, _, _) = CreateService();

        var result = await service.UpdateAsync(999, new UpdateUsuarioDto(Nombre: "Nuevo"));

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    private static (UsuariosService Service, FakeUsuarioRepository Repository, FakeRolRepository Roles)
        CreateService()
        => CreateService(new FakeFirebaseUserService());

    private static (UsuariosService Service, FakeUsuarioRepository Repository, FakeRolRepository Roles)
        CreateService(FakeFirebaseUserService firebase)
    {
        var repository = new FakeUsuarioRepository();
        var roles = new FakeRolRepository();
        var clientes = new FakeClienteRepository();
        return (new UsuariosService(repository, roles, firebase, clientes), repository, roles);
    }

    // ===== Autorregistro (RegisterAsync) =====

    [Fact]
    public async Task Register_WithValidData_CreatesUserWithClienteRole_AndLinksFirebaseUid()
    {
        var firebase = new FakeFirebaseUserService();
        var (service, repository, _) = CreateService(firebase);

        var result = await service.RegisterAsync(
            new RegisterDto("Nuevo.Cliente@Email.COM", "secreto123", "Nueva", "Cliente"));

        Assert.True(result.IsSuccess);
        Assert.Equal("nuevo.cliente@email.com", result.Value.Email);
        Assert.Equal("Cliente", result.Value.Rol);
        Assert.Single(repository.All);
        Assert.False(string.IsNullOrWhiteSpace(repository.All[0].FirebaseUid));
        Assert.Single(firebase.CreatedEmails);
    }

    [Fact]
    public async Task Register_DuplicateInternalEmail_ReturnsConflict_WithoutCallingFirebase()
    {
        var firebase = new FakeFirebaseUserService();
        var (service, repository, _) = CreateService(firebase);
        await repository.SeedAsync("ocupado@mail.com");

        var result = await service.RegisterAsync(
            new RegisterDto("OCUPADO@MAIL.COM", "secreto123", "Otro", "Cliente"));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
        Assert.Empty(firebase.CreatedEmails);
    }

    [Fact]
    public async Task Register_WithEmailAlreadyInFirebase_ReturnsConflict()
    {
        var firebase = new FakeFirebaseUserService();
        firebase.ExistingEmails.Add("existente@mail.com");
        var (service, _, _) = CreateService(firebase);

        var result = await service.RegisterAsync(
            new RegisterDto("existente@mail.com", "secreto123", "Otro", "Cliente"));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
        Assert.Empty(firebase.DeletedUids);
    }

    [Theory]
    [InlineData("", "secreto123", "Juan", "Perez")]
    [InlineData("juan@mail.com", "secreto123", "", "Perez")]
    [InlineData("juan@mail.com", "secreto123", "Juan", "")]
    [InlineData("juan@mail.com", "", "Juan", "Perez")]
    [InlineData("juan@mail.com", "12345", "Juan", "Perez")]
    public async Task Register_WithMissingOrWeakInput_ReturnsValidation(
        string email,
        string password,
        string nombre,
        string apellido)
    {
        var firebase = new FakeFirebaseUserService();
        var (service, _, _) = CreateService(firebase);

        var result = await service.RegisterAsync(new RegisterDto(email, password, nombre, apellido));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
        Assert.Empty(firebase.CreatedEmails);
    }

    [Fact]
    public async Task Register_InvalidEmail_ReturnsValidation_WithoutCallingFirebase()
    {
        var firebase = new FakeFirebaseUserService();
        var (service, _, _) = CreateService(firebase);

        var result = await service.RegisterAsync(
            new RegisterDto("no-es-email", "secreto123", "Juan", "Perez"));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
        Assert.Empty(firebase.CreatedEmails);
    }

    [Fact]
    public async Task Register_WhenDatabaseInsertFails_RollsBackFirebaseAccount()
    {
        var firebase = new FakeFirebaseUserService();
        var (service, repository, _) = CreateService(firebase);
        repository.FailOnAdd = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterAsync(new RegisterDto("rollback@mail.com", "secreto123", "Juan", "Perez")));

        Assert.Single(firebase.CreatedEmails);
        Assert.Single(firebase.DeletedUids);
        Assert.Empty(repository.All);
    }

    // ===== Aprovisionamiento externo (Google) =====

    private static UsuariosService CreateProvisionService(
        out FakeUsuarioRepository repository,
        out FakeClienteRepository clientes)
    {
        repository = new FakeUsuarioRepository();
        clientes = new FakeClienteRepository();
        return new UsuariosService(
            repository,
            new FakeRolRepository(),
            new FakeFirebaseUserService(),
            clientes);
    }

    [Fact]
    public async Task ProvisionExternal_NewAccount_CreatesClienteUserAndClient()
    {
        var service = CreateProvisionService(out var repository, out var clientes);

        var result = await service.ProvisionExternalAsync(
            "uid-google-1", "Google.User@Mail.com", "Ana María Gómez");

        Assert.True(result.IsSuccess);
        Assert.Equal("google.user@mail.com", result.Value.Email);
        Assert.Equal("Cliente", result.Value.Rol);
        Assert.Equal("Ana", result.Value.Nombre);
        Assert.Equal("María Gómez", result.Value.Apellido);
        Assert.Single(repository.All);
        Assert.Equal("uid-google-1", repository.All[0].FirebaseUid);
        Assert.Single(clientes.All);
        Assert.Equal("uid-google-1", clientes.All[0].FirebaseUid);
    }

    [Fact]
    public async Task ProvisionExternal_ExistingEmail_LinksUidWithoutDuplicating()
    {
        var service = CreateProvisionService(out var repository, out _);
        await repository.SeedAsync("existente@mail.com");

        var result = await service.ProvisionExternalAsync(
            "uid-google-2", "EXISTENTE@MAIL.COM", "Juan Perez");

        Assert.True(result.IsSuccess);
        Assert.Single(repository.All);
        Assert.Equal("uid-google-2", repository.All[0].FirebaseUid);
    }

    [Fact]
    public async Task ProvisionExternal_SameUidTwice_IsIdempotent()
    {
        var service = CreateProvisionService(out var repository, out _);

        var first = await service.ProvisionExternalAsync(
            "uid-google-3", "idem@mail.com", "Ana Gomez");
        var second = await service.ProvisionExternalAsync(
            "uid-google-3", "idem@mail.com", "Ana Gomez");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Single(repository.All);
    }

    [Fact]
    public async Task ProvisionExternal_MissingEmail_ReturnsValidation()
    {
        var service = CreateProvisionService(out _, out _);

        var result = await service.ProvisionExternalAsync("uid-google-4", " ", "Ana Gomez");

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task ProvisionExternal_InactiveUser_ReturnsValidation()
    {
        var service = CreateProvisionService(out var repository, out _);
        var usuario = await repository.SeedAsync("inactivo@mail.com");
        usuario.Actualizar(activo: false);

        var result = await service.ProvisionExternalAsync(
            "uid-google-5", "inactivo@mail.com", "Ana Gomez");

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task ProvisionExternal_SingleWordName_UsesFallbackApellido()
    {
        var service = CreateProvisionService(out _, out _);

        var result = await service.ProvisionExternalAsync(
            "uid-google-6", "solo@mail.com", "Cher");

        Assert.True(result.IsSuccess);
        Assert.Equal("Cher", result.Value.Nombre);
        Assert.Equal("No informado", result.Value.Apellido);
    }

    private sealed class FakeFirebaseUserService : IFirebaseUserService
    {
        private int _sequence;

        public HashSet<string> ExistingEmails { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<string> CreatedEmails { get; } = new();

        public List<string> DeletedUids { get; } = new();

        public Task<Result<string>> CreateAuthUserAsync(
            string email,
            string password,
            string nombre,
            string apellido,
            CancellationToken cancellationToken = default)
        {
            CreatedEmails.Add(email);

            if (ExistingEmails.Contains(email))
            {
                return Task.FromResult<Result<string>>(
                    Error.Conflict($"Ya existe una cuenta con el email {email}."));
            }

            return Task.FromResult<Result<string>>($"uid-{++_sequence}");
        }

        public Task DeleteAuthUserAsync(string firebaseUid, CancellationToken cancellationToken = default)
        {
            DeletedUids.Add(firebaseUid);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRolRepository : IRolRepository
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

    private sealed class FakeUsuarioRepository : IUsuarioRepository
    {
        private readonly List<Usuario> _usuarios = new();

        public IReadOnlyList<Usuario> All => _usuarios;

        public bool FailOnAdd { get; set; }

        public async Task<Usuario> SeedAsync(string email)
        {
            var usuario = Usuario.Crear(email, "Juan", "Perez", rolId: 1);
            await AddAsync(usuario);
            return usuario;
        }

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
            if (FailOnAdd)
            {
                throw new InvalidOperationException("Fallo simulado de persistencia.");
            }

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

    private sealed class FakeClienteRepository : IClienteRepository
    {
        private readonly List<Cliente> _clientes = new();

        public IReadOnlyList<Cliente> All => _clientes;

        public Task<PagedResult<Cliente>> GetPagedAsync(
            int page, int pageSize, string? search, bool? activo,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<Cliente>());

        public Task<Cliente?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(_clientes.FirstOrDefault(c => c.Id == id));

        public Task<Cliente?> GetByFirebaseUidAsync(
            string firebaseUid,
            CancellationToken cancellationToken = default)
            => Task.FromResult(string.IsNullOrWhiteSpace(firebaseUid)
                ? null
                : _clientes.FirstOrDefault(c => c.FirebaseUid == firebaseUid && c.Activo));

        public Task<Cliente?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return Task.FromResult(_clientes.FirstOrDefault(c => c.Email == normalized && c.Activo));
        }

        public Task<bool> EmailExistsAsync(
            string email, int? excludeId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<Cliente> AddAsync(Cliente cliente, CancellationToken cancellationToken = default)
        {
            cliente.Id = _clientes.Count == 0 ? 1 : _clientes.Max(c => c.Id) + 1;
            _clientes.Add(cliente);
            return Task.FromResult(cliente);
        }

        public Task UpdateAsync(Cliente cliente, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
