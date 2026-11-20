using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Productos;
using SistemaGestion.Application.Productos.Dtos;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;
using Xunit;

namespace SistemaGestion.Application.UnitTests;

public sealed class ProductosServiceTests
{
    [Fact]
    public async Task Create_WithValidData_ReturnsProduct_AndNormalizesSku()
    {
        var (service, repository) = CreateService();

        var result = await service.CreateAsync(new CreateProductoDto(
            Nombre: "Melamina Blanca 18mm",
            CodigoSku: "  mel-001  ",
            Tipo: TipoProducto.Melamina,
            Precio: 1500.50m));

        Assert.True(result.IsSuccess);
        Assert.Equal("MEL-001", result.Value.CodigoSku);
        Assert.Equal(TipoProducto.Melamina, result.Value.Tipo);
        Assert.Equal(1500.50m, result.Value.Precio);
        Assert.True(result.Value.Activo);
        Assert.Single(repository.All);
    }

    [Fact]
    public async Task Create_WithDuplicateSku_ReturnsConflict()
    {
        var (service, repository) = CreateService();
        repository.Seed(codigoSku: "MEL-001");

        var result = await service.CreateAsync(new CreateProductoDto(
            Nombre: "Otra Melamina",
            CodigoSku: "mel-001",
            Tipo: TipoProducto.Melamina,
            Precio: 100m));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
        Assert.Single(repository.All);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WithoutNombre_ReturnsValidation(string nombre)
    {
        var (service, _) = CreateService();

        var result = await service.CreateAsync(new CreateProductoDto(
            nombre, "SKU-1", TipoProducto.Mueble, 100m));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WithoutSku_ReturnsValidation(string sku)
    {
        var (service, _) = CreateService();

        var result = await service.CreateAsync(new CreateProductoDto(
            "Mesa", sku, TipoProducto.Mueble, 100m));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task Create_WithNegativePrecio_ReturnsValidation()
    {
        var (service, _) = CreateService();

        var result = await service.CreateAsync(new CreateProductoDto(
            "Mesa", "SKU-1", TipoProducto.Mueble, -1m));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task Create_WithUndefinedTipo_ReturnsValidation()
    {
        var (service, _) = CreateService();

        var result = await service.CreateAsync(new CreateProductoDto(
            "Mesa", "SKU-1", (TipoProducto)99, 100m));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetById_KnownId_ReturnsProduct()
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
            repository.Seed(codigoSku: $"SKU-{i:D3}");
        }

        var result = await service.GetListAsync(
            1, 500, search: null, tipo: null, categoria: null,
            activo: null, showroom: null, lowStock: null);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProductosService.MaxPageSize, result.Value.PageSize);
        Assert.Equal(105, result.Value.TotalItems);
        Assert.Equal(100, result.Value.Items.Count);
    }

    [Fact]
    public async Task GetList_WithSearchAndTipoAndCategoria_Filters()
    {
        var (service, repository) = CreateService();
        repository.Seed(
            nombre: "Melamina Blanca",
            codigoSku: "MEL-100",
            tipo: TipoProducto.Melamina,
            categoria: "Melaminas");
        repository.Seed(
            nombre: "Mesa Roble",
            codigoSku: "MES-100",
            tipo: TipoProducto.Mueble,
            categoria: "Mesas");

        var result = await service.GetListAsync(
            1, 20, "melamina", TipoProducto.Melamina, "melaminas",
            activo: null, showroom: null, lowStock: null);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("Melamina Blanca", result.Value.Items[0].Nombre);
    }

    [Fact]
    public async Task GetList_WithLowStockFilter_ReturnsOnlyProductsAtOrBelowMinimum()
    {
        var (service, repository) = CreateService();
        repository.Seed(codigoSku: "LOW-1", stockActual: 3, stockMinimo: 5);
        repository.Seed(codigoSku: "OK-1", stockActual: 50, stockMinimo: 5);
        repository.Seed(codigoSku: "NO-MIN", stockActual: 0, stockMinimo: 0);

        var result = await service.GetListAsync(
            1, 20, search: null, tipo: null, categoria: null,
            activo: null, showroom: null, lowStock: true);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("LOW-1", result.Value.Items[0].CodigoSku);
    }

    [Fact]
    public async Task Create_WithEsPersonalizado_ReturnsCustomProduct()
    {
        var (service, _) = CreateService();

        var result = await service.CreateAsync(new CreateProductoDto(
            "Mesa a medida", "CUST-SVC-01", TipoProducto.Mueble, 300m, EsPersonalizado: true));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.EsPersonalizado);
    }

    [Fact]
    public async Task GetList_WithIsCustomFilter_FiltersByCustomFlag()
    {
        var (service, repository) = CreateService();
        repository.Seed(nombre: "Mesa Estándar", codigoSku: "STD-01");
        repository.Seed(nombre: "Mesa Custom", codigoSku: "CUS-01", esPersonalizado: true);

        var result = await service.GetListAsync(
            1, 20, search: null, tipo: null, categoria: null,
            activo: null, showroom: null, lowStock: null, isCustom: true);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("CUS-01", result.Value.Items[0].CodigoSku);
        Assert.True(result.Value.Items[0].EsPersonalizado);
    }

    [Fact]
    public async Task GetList_WithInvalidTipo_ReturnsValidation()
    {
        var (service, _) = CreateService();

        var result = await service.GetListAsync(
            1, 20, search: null, (TipoProducto)99, categoria: null,
            activo: null, showroom: null, lowStock: null);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task Update_PatchReplacesOnlyProvidedFields()
    {
        var (service, repository) = CreateService();
        var seeded = repository.Seed(codigoSku: "SKU-1", categoria: "Mesas");

        var result = await service.UpdateAsync(
            seeded.Id,
            new UpdateProductoDto(Precio: 999.99m, Showroom: true));

        Assert.True(result.IsSuccess);
        Assert.Equal("Producto SA", result.Value.Nombre);
        Assert.Equal("SKU-1", result.Value.CodigoSku);
        Assert.Equal("Mesas", result.Value.Categoria);
        Assert.Equal(999.99m, result.Value.Precio);
        Assert.True(result.Value.Showroom);
    }

    [Fact]
    public async Task Update_WithSkuOfAnotherProduct_ReturnsConflict()
    {
        var (service, repository) = CreateService();
        repository.Seed(codigoSku: "OCUPADO");
        var target = repository.Seed(codigoSku: "PROPIO");

        var result = await service.UpdateAsync(
            target.Id,
            new UpdateProductoDto(CodigoSku: "ocupado"));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
    }

    [Fact]
    public async Task Update_WithSameSkuOnSameProduct_ReturnsOk()
    {
        var (service, repository) = CreateService();
        var target = repository.Seed(codigoSku: "PROPIO");

        var result = await service.UpdateAsync(
            target.Id,
            new UpdateProductoDto(CodigoSku: "propio", Nombre: "Nombre Nuevo"));

        Assert.True(result.IsSuccess);
        Assert.Equal("PROPIO", result.Value.CodigoSku);
        Assert.Equal("Nombre Nuevo", result.Value.Nombre);
    }

    [Fact]
    public async Task Update_WithNegativePrecio_ReturnsValidation()
    {
        var (service, repository) = CreateService();
        var target = repository.Seed();

        var result = await service.UpdateAsync(target.Id, new UpdateProductoDto(Precio: -5m));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNotFound()
    {
        var (service, _) = CreateService();

        var result = await service.UpdateAsync(999, new UpdateProductoDto(Nombre: "Nuevo"));

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task Deactivate_ExistingProduct_SetsInactiveAndKeepsRecord()
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

    private static (ProductosService Service, FakeProductoRepository Repository) CreateService()
    {
        var repository = new FakeProductoRepository();
        return (
            new ProductosService(repository, new FakeProductoImagenRepository(), new FakeStorageService()),
            repository);
    }

    private sealed class FakeProductoRepository : IProductoRepository
    {
        private readonly List<Producto> _productos = new();

        public IReadOnlyList<Producto> All => _productos;

        public Producto Seed(
            string nombre = "Producto SA",
            string? codigoSku = null,
            TipoProducto tipo = TipoProducto.Mueble,
            decimal precio = 100m,
            string? categoria = null,
            int stockActual = 10,
            int stockMinimo = 0,
            bool esPersonalizado = false)
        {
            var producto = Producto.Crear(
                nombre,
                codigoSku ?? $"SKU-{_productos.Count + 1:D4}",
                tipo,
                precio,
                categoria: categoria,
                stockActual: stockActual,
                stockMinimo: stockMinimo,
                esPersonalizado: esPersonalizado);
            producto.Id = _productos.Count == 0 ? 1 : _productos.Max(p => p.Id) + 1;
            _productos.Add(producto);
            return producto;
        }

        public Task<PagedResult<Producto>> GetPagedAsync(
            int page,
            int pageSize,
            string? search,
            TipoProducto? tipo,
            string? categoria,
            bool? activo,
            bool? showroom,
            bool? lowStock,
            bool? isCustom,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Producto> query = _productos;

            if (activo.HasValue)
            {
                query = query.Where(p => p.Activo == activo.Value);
            }

            if (showroom.HasValue)
            {
                query = query.Where(p => p.Showroom == showroom.Value);
            }

            if (isCustom.HasValue)
            {
                query = query.Where(p => p.EsPersonalizado == isCustom.Value);
            }

            if (tipo.HasValue)
            {
                query = query.Where(p => p.Tipo == tipo.Value);
            }

            if (!string.IsNullOrWhiteSpace(categoria))
            {
                var cat = categoria.ToLower();
                query = query.Where(p => p.Categoria != null && p.Categoria.ToLower() == cat);
            }

            if (lowStock == true)
            {
                query = query.Where(p => p.StockMinimo > 0 && p.StockActual <= p.StockMinimo);
            }
            else if (lowStock == false)
            {
                query = query.Where(p => p.StockMinimo == 0 || p.StockActual > p.StockMinimo);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                query = query.Where(p =>
                    p.Nombre.ToLower().Contains(term)
                    || p.CodigoSku.ToLower().Contains(term)
                    || (p.Descripcion != null && p.Descripcion.ToLower().Contains(term)));
            }

            var list = query.OrderBy(p => p.Id).ToList();

            return Task.FromResult(new PagedResult<Producto>
            {
                Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = list.Count,
            });
        }

        public Task<Producto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(_productos.FirstOrDefault(p => p.Id == id));

        public Task<bool> SkuExistsAsync(
            string codigoSku,
            int? excludeId = null,
            CancellationToken cancellationToken = default)
        {
            var normalized = codigoSku.Trim().ToUpperInvariant();
            return Task.FromResult(_productos.Any(p =>
                p.CodigoSku == normalized && (excludeId == null || p.Id != excludeId)));
        }

        public Task<Producto> AddAsync(Producto producto, CancellationToken cancellationToken = default)
        {
            producto.Id = _productos.Count == 0 ? 1 : _productos.Max(p => p.Id) + 1;
            _productos.Add(producto);
            return Task.FromResult(producto);
        }

        public Task UpdateAsync(Producto producto, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
