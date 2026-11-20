using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Clientes;
using SistemaGestion.Application.Clientes.Dtos;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;
using Xunit;

namespace SistemaGestion.Application.UnitTests;

public sealed class ClientesServiceTests
{
    [Fact]
    public async Task Create_WithValidData_ReturnsClient_AndNormalizesEmail()
    {
        var (service, repository) = CreateService();

        var result = await service.CreateAsync(
            new CreateClienteDto("Maria", "Gonzalez", "  Maria.Gonzalez@Email.COM "));

        Assert.True(result.IsSuccess);
        Assert.Equal("maria.gonzalez@email.com", result.Value.Email);
        Assert.Equal("Maria", result.Value.Nombre);
        Assert.True(result.Value.Activo);
        Assert.Single(repository.All);
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_ReturnsConflict()
    {
        var (service, repository) = CreateService();
        repository.Seed("maria@mail.com");

        var result = await service.CreateAsync(
            new CreateClienteDto("Otra", "Persona", "MARIA@MAIL.COM"));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
        Assert.Single(repository.All);
    }

    [Theory]
    [InlineData("", "Gonzalez", "maria@mail.com")]
    [InlineData("Maria", "", "maria@mail.com")]
    [InlineData("Maria", "Gonzalez", "")]
    public async Task Create_WithMissingRequiredField_ReturnsValidation(
        string nombre,
        string apellido,
        string email)
    {
        var (service, _) = CreateService();

        var result = await service.CreateAsync(new CreateClienteDto(nombre, apellido, email));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetById_KnownId_ReturnsClient()
    {
        var (service, repository) = CreateService();
        var seeded = repository.Seed("maria@mail.com");

        var result = await service.GetByIdAsync(seeded.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(seeded.Id, result.Value.Id);
        Assert.Equal("maria@mail.com", result.Value.Email);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var (service, _) = CreateService();

        var result = await service.GetByIdAsync(999);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetList_ClampsPageSizeToMaximum()
    {
        var (service, repository) = CreateService();
        for (var i = 0; i < 105; i++)
        {
            repository.Seed($"cliente{i}@mail.com");
        }

        var result = await service.GetListAsync(page: 1, pageSize: 500, search: null, activo: null);

        Assert.True(result.IsSuccess);
        Assert.Equal(ClientesService.MaxPageSize, result.Value.PageSize);
        Assert.Equal(105, result.Value.TotalItems);
        Assert.Equal(100, result.Value.Items.Count);
    }

    [Fact]
    public async Task GetList_WithSearch_FiltersByNameAndEmail()
    {
        var (service, repository) = CreateService();
        repository.Seed("maria@mail.com", "Maria", "Gonzalez");
        repository.Seed("pedro@otro.com", "Pedro", "Gomez");

        var result = await service.GetListAsync(page: 1, pageSize: 20, search: "maria", activo: null);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("maria@mail.com", result.Value.Items[0].Email);
    }

    [Fact]
    public async Task GetList_WithActiveFilter_ReturnsOnlyActive()
    {
        var (service, repository) = CreateService();
        var activo = repository.Seed("activo@mail.com");
        repository.Seed("inactivo@mail.com");
        repository.All.First(c => c.Id != activo.Id).Desactivar();

        var result = await service.GetListAsync(page: 1, pageSize: 20, search: null, activo: false);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.False(result.Value.Items[0].Activo);
    }

    [Fact]
    public async Task Update_PatchReplacesOnlyProvidedFields()
    {
        var (service, repository) = CreateService();
        var seeded = repository.Seed("maria@mail.com", "Maria", "Gonzalez");

        var result = await service.UpdateAsync(
            seeded.Id,
            new UpdateClienteDto(Apellido: "Perez", Notas: "Prefiere WhatsApp"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Maria", result.Value.Nombre);
        Assert.Equal("Perez", result.Value.Apellido);
        Assert.Equal("maria@mail.com", result.Value.Email);
        Assert.Equal("Prefiere WhatsApp", result.Value.Notas);
    }

    [Fact]
    public async Task Update_WithEmailOfAnotherClient_ReturnsConflict()
    {
        var (service, repository) = CreateService();
        repository.Seed("ocupado@mail.com");
        var target = repository.Seed("maria@mail.com");

        var result = await service.UpdateAsync(
            target.Id,
            new UpdateClienteDto(Email: "ocupado@mail.com"));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
    }

    [Fact]
    public async Task Update_WithSameEmailOnSameClient_ReturnsOk()
    {
        var (service, repository) = CreateService();
        var target = repository.Seed("maria@mail.com");

        var result = await service.UpdateAsync(
            target.Id,
            new UpdateClienteDto(Email: "MARIA@mail.com", Nombre: "Ana"));

        Assert.True(result.IsSuccess);
        Assert.Equal("maria@mail.com", result.Value.Email);
        Assert.Equal("Ana", result.Value.Nombre);
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNotFound()
    {
        var (service, _) = CreateService();

        var result = await service.UpdateAsync(999, new UpdateClienteDto(Nombre: "Nuevo"));

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task Deactivate_ExistingClient_SetsInactiveAndKeepsRecord()
    {
        var (service, repository) = CreateService();
        var seeded = repository.Seed("maria@mail.com");

        var result = await service.DeactivateAsync(seeded.Id);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Activo);
        Assert.Single(repository.All);
        Assert.False(repository.All[0].Activo);
    }

    [Fact]
    public async Task Deactivate_UnknownId_ReturnsNotFound()
    {
        var (service, _) = CreateService();

        var result = await service.DeactivateAsync(999);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    private static (ClientesService Service, FakeClienteRepository Repository) CreateService()
    {
        var repository = new FakeClienteRepository();
        return (new ClientesService(repository), repository);
    }

    private sealed class FakeClienteRepository : IClienteRepository
    {
        private readonly List<Cliente> _clientes = new();

        public IReadOnlyList<Cliente> All => _clientes;

        public Cliente Seed(
            string email,
            string nombre = "Cliente",
            string apellido = "Test")
        {
            var cliente = Cliente.Crear(nombre, apellido, email);
            cliente.Id = _clientes.Count == 0 ? 1 : _clientes.Max(c => c.Id) + 1;
            _clientes.Add(cliente);
            return cliente;
        }

        public Task<PagedResult<Cliente>> GetPagedAsync(
            int page,
            int pageSize,
            string? search,
            bool? activo,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Cliente> query = _clientes;

            if (activo.HasValue)
            {
                query = query.Where(c => c.Activo == activo.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                query = query.Where(c =>
                    c.Nombre.ToLower().Contains(term)
                    || c.Apellido.ToLower().Contains(term)
                    || c.Email.ToLower().Contains(term));
            }

            var list = query.OrderBy(c => c.Id).ToList();

            return Task.FromResult(new PagedResult<Cliente>
            {
                Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = list.Count,
            });
        }

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
            string email,
            int? excludeId = null,
            CancellationToken cancellationToken = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return Task.FromResult(_clientes.Any(c =>
                c.Email == normalized && (excludeId == null || c.Id != excludeId)));
        }

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
