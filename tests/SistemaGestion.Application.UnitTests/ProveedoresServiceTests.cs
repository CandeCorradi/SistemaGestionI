using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Proveedores;
using SistemaGestion.Application.Proveedores.Dtos;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;
using Xunit;

namespace SistemaGestion.Application.UnitTests;

public sealed class ProveedoresServiceTests
{
    [Fact]
    public async Task Create_WithValidData_ReturnsSupplier_AndNormalizesEmail()
    {
        var (service, repository) = CreateService();

        var result = await service.CreateAsync(
            new CreateProveedorDto("Melaminas del Sur", Email: "  Ventas@Mail.COM "));

        Assert.True(result.IsSuccess);
        Assert.Equal("ventas@mail.com", result.Value.Email);
        Assert.Equal("Melaminas del Sur", result.Value.Nombre);
        Assert.True(result.Value.Activo);
        Assert.Single(repository.All);
    }

    [Fact]
    public async Task Create_WithoutEmail_IsAllowed()
    {
        var (service, repository) = CreateService();

        var result = await service.CreateAsync(new CreateProveedorDto("Accesorios SA"));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Email);
        Assert.Single(repository.All);
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_ReturnsConflict()
    {
        var (service, repository) = CreateService();
        repository.Seed(email: "ventas@mail.com");

        var result = await service.CreateAsync(
            new CreateProveedorDto("Otro Proveedor", Email: "VENTAS@mail.com"));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
        Assert.Single(repository.All);
    }

    [Fact]
    public async Task Create_WithBlankEmail_DoesNotCheckUniqueness()
    {
        var (service, repository) = CreateService();
        repository.Seed(email: "ventas@mail.com");
        repository.Seed(email: "ventas@mail.com");

        var result = await service.CreateAsync(new CreateProveedorDto("Sin Email", Email: "  "));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, repository.All.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WithoutNombre_ReturnsValidation(string nombre)
    {
        var (service, _) = CreateService();

        var result = await service.CreateAsync(new CreateProveedorDto(nombre));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task Create_WithInvalidEmail_ReturnsValidation()
    {
        var (service, _) = CreateService();

        var result = await service.CreateAsync(
            new CreateProveedorDto("Proveedor SA", Email: "no-es-email"));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetById_KnownId_ReturnsSupplier()
    {
        var (service, repository) = CreateService();
        var seeded = repository.Seed();

        var result = await service.GetByIdAsync(seeded.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(seeded.Id, result.Value.Id);
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
            repository.Seed(email: $"proveedor{i}@mail.com");
        }

        var result = await service.GetListAsync(page: 1, pageSize: 500, search: null, activo: null);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProveedoresService.MaxPageSize, result.Value.PageSize);
        Assert.Equal(105, result.Value.TotalItems);
        Assert.Equal(100, result.Value.Items.Count);
    }

    [Fact]
    public async Task GetList_WithSearch_FiltersByName()
    {
        var (service, repository) = CreateService();
        repository.Seed(nombre: "Melaminas del Sur", email: "melaminas@mail.com");
        repository.Seed(nombre: "Ferreteria Norte", email: "ferreteria@mail.com");

        var result = await service.GetListAsync(page: 1, pageSize: 20, search: "melaminas", activo: null);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("Melaminas del Sur", result.Value.Items[0].Nombre);
    }

    [Fact]
    public async Task GetList_WithActiveFilter_ReturnsOnlyInactive()
    {
        var (service, repository) = CreateService();
        var activo = repository.Seed(email: "activo@mail.com");
        var inactivo = repository.Seed(email: "inactivo@mail.com");
        inactivo.Desactivar();

        var result = await service.GetListAsync(page: 1, pageSize: 20, search: null, activo: false);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(inactivo.Id, result.Value.Items[0].Id);
        Assert.NotEqual(activo.Id, result.Value.Items[0].Id);
    }

    [Fact]
    public async Task Update_PatchReplacesOnlyProvidedFields()
    {
        var (service, repository) = CreateService();
        var seeded = repository.Seed(email: "contacto@mail.com");

        var result = await service.UpdateAsync(
            seeded.Id,
            new UpdateProveedorDto(Contacto: "Juan Perez", Notas: "Entrega los martes"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Proveedor SA", result.Value.Nombre);
        Assert.Equal("contacto@mail.com", result.Value.Email);
        Assert.Equal("Juan Perez", result.Value.Contacto);
        Assert.Equal("Entrega los martes", result.Value.Notas);
    }

    [Fact]
    public async Task Update_WithEmailOfAnotherSupplier_ReturnsConflict()
    {
        var (service, repository) = CreateService();
        repository.Seed(email: "ocupado@mail.com");
        var target = repository.Seed(email: "propio@mail.com");

        var result = await service.UpdateAsync(
            target.Id,
            new UpdateProveedorDto(Email: "ocupado@mail.com"));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
    }

    [Fact]
    public async Task Update_WithSameEmailOnSameSupplier_ReturnsOk()
    {
        var (service, repository) = CreateService();
        var target = repository.Seed(email: "propio@mail.com");

        var result = await service.UpdateAsync(
            target.Id,
            new UpdateProveedorDto(Email: "PROPIO@mail.com", Nombre: "Nombre Nuevo"));

        Assert.True(result.IsSuccess);
        Assert.Equal("propio@mail.com", result.Value.Email);
        Assert.Equal("Nombre Nuevo", result.Value.Nombre);
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNotFound()
    {
        var (service, _) = CreateService();

        var result = await service.UpdateAsync(999, new UpdateProveedorDto(Nombre: "Nuevo"));

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task Deactivate_ExistingSupplier_SetsInactiveAndKeepsRecord()
    {
        var (service, repository) = CreateService();
        var seeded = repository.Seed();

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

    private static (ProveedoresService Service, FakeProveedorRepository Repository) CreateService()
    {
        var repository = new FakeProveedorRepository();
        return (new ProveedoresService(repository), repository);
    }

    private sealed class FakeProveedorRepository : IProveedorRepository
    {
        private readonly List<Proveedor> _proveedores = new();

        public IReadOnlyList<Proveedor> All => _proveedores;

        public Proveedor Seed(
            string nombre = "Proveedor SA",
            string? email = null)
        {
            var proveedor = Proveedor.Crear(nombre, email: email);
            proveedor.Id = _proveedores.Count == 0 ? 1 : _proveedores.Max(p => p.Id) + 1;
            _proveedores.Add(proveedor);
            return proveedor;
        }

        public Task<PagedResult<Proveedor>> GetPagedAsync(
            int page,
            int pageSize,
            string? search,
            bool? activo,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Proveedor> query = _proveedores;

            if (activo.HasValue)
            {
                query = query.Where(p => p.Activo == activo.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                query = query.Where(p => p.Nombre.ToLower().Contains(term));
            }

            var list = query.OrderBy(p => p.Id).ToList();

            return Task.FromResult(new PagedResult<Proveedor>
            {
                Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = list.Count,
            });
        }

        public Task<Proveedor?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(_proveedores.FirstOrDefault(p => p.Id == id));

        public Task<bool> EmailExistsAsync(
            string email,
            int? excludeId = null,
            CancellationToken cancellationToken = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return Task.FromResult(_proveedores.Any(p =>
                p.Email == normalized && (excludeId == null || p.Id != excludeId)));
        }

        public Task<Proveedor> AddAsync(Proveedor proveedor, CancellationToken cancellationToken = default)
        {
            proveedor.Id = _proveedores.Count == 0 ? 1 : _proveedores.Max(p => p.Id) + 1;
            _proveedores.Add(proveedor);
            return Task.FromResult(proveedor);
        }

        public Task UpdateAsync(Proveedor proveedor, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
