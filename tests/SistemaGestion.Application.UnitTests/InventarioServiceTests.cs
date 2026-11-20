using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Inventario;
using SistemaGestion.Application.Inventario.Dtos;
using SistemaGestion.Domain.Common;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;
using Xunit;

namespace SistemaGestion.Application.UnitTests;

/// <summary>
/// Reglas de negocio de inventario (BR-050/051/052/053, functional-spec §11).
/// </summary>
public sealed class InventarioServiceTests
{
    private const string Uid = "uid-stock-user";

    [Fact]
    public async Task RegisterMovement_Ingreso_AppliesMovementAndReturnsStockTransitions()
    {
        var (service, inventario, productos, _) = CreateService();
        productos.Seed(stockActual: 10);

        var result = await service.RegisterMovementAsync(
            Uid,
            new CreateMovimientoStockDto(ProductId: 1, Type: TipoMovimientoStock.Ingreso, Quantity: 5));

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.StockAnterior);
        Assert.Equal(15, result.Value.StockPosterior);
        Assert.Equal(TipoMovimientoStock.Ingreso, result.Value.Tipo);
        Assert.Equal("Juan Pérez", result.Value.UsuarioNombre);
        Assert.Equal(15, (await productos.GetByIdAsync(1))!.StockActual);
        Assert.Single(inventario.All);
    }

    [Fact]
    public async Task RegisterMovement_AjusteNegativo_LowersStock()
    {
        var (service, inventario, productos, _) = CreateService();
        productos.Seed(stockActual: 10);

        var result = await service.RegisterMovementAsync(
            Uid,
            new CreateMovimientoStockDto(ProductId: 1, Type: TipoMovimientoStock.Ajuste, Quantity: -3));

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.StockPosterior);
        Assert.Equal(7, (await productos.GetByIdAsync(1))!.StockActual);
        Assert.Single(inventario.All);
    }

    [Fact]
    public async Task RegisterMovement_UnknownProduct_ReturnsNotFound()
    {
        var (service, _, _, _) = CreateService();

        var result = await service.RegisterMovementAsync(
            Uid, new CreateMovimientoStockDto(99, TipoMovimientoStock.Ingreso, 5));

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task RegisterMovement_InactiveProduct_ReturnsValidation()
    {
        var (service, inventario, productos, _) = CreateService();
        var producto = productos.Seed(stockActual: 10);
        producto.Desactivar();

        var result = await service.RegisterMovementAsync(
            Uid, new CreateMovimientoStockDto(producto.Id, TipoMovimientoStock.Ingreso, 5));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
        Assert.Empty(inventario.All);
        Assert.Equal(10, producto.StockActual);
    }

    [Theory]
    [InlineData(TipoMovimientoStock.Ingreso, 0)]
    [InlineData(TipoMovimientoStock.Ingreso, -5)]
    [InlineData(TipoMovimientoStock.Egreso, -1)]
    public async Task RegisterMovement_InvalidQuantity_ReturnsValidation(
        TipoMovimientoStock tipo,
        int cantidad)
    {
        var (service, inventario, productos, _) = CreateService();
        productos.Seed(stockActual: 10);

        var result = await service.RegisterMovementAsync(
            Uid, new CreateMovimientoStockDto(1, tipo, cantidad));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
        Assert.Empty(inventario.All);
        Assert.Equal(10, (await productos.GetByIdAsync(1))!.StockActual);
    }

    [Fact]
    public async Task RegisterMovement_NegativeResultStock_ReturnsValidation_Br052()
    {
        var (service, inventario, productos, _) = CreateService();
        productos.Seed(stockActual: 3);

        var result = await service.RegisterMovementAsync(
            Uid, new CreateMovimientoStockDto(1, TipoMovimientoStock.Egreso, 10));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
        Assert.Contains("BR-052", result.Error.Description);
        Assert.Empty(inventario.All);
        Assert.Equal(3, (await productos.GetByIdAsync(1))!.StockActual);
    }

    [Fact]
    public async Task RegisterMovement_ConcurrentStockChange_ReturnsConflict_AndKeepsStock_Br053()
    {
        var (service, inventario, productos, _) = CreateService();
        productos.Seed(stockActual: 10);
        inventario.FailNextApply(); // simula que otro proceso modificó el stock

        var result = await service.RegisterMovementAsync(
            Uid, new CreateMovimientoStockDto(1, TipoMovimientoStock.Ingreso, 5));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict.Error", result.Error.Code);
        Assert.Contains("BR-053", result.Error.Description);
        Assert.Empty(inventario.All);
        Assert.Equal(10, (await productos.GetByIdAsync(1))!.StockActual);
    }

    [Fact]
    public async Task RegisterMovement_UnknownInternalUser_ReturnsNotFound()
    {
        var (service, _, productos, _) = CreateService();
        productos.Seed(stockActual: 10);

        var result = await service.RegisterMovementAsync(
            "uid-sin-usuario", new CreateMovimientoStockDto(1, TipoMovimientoStock.Ingreso, 5));

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetStockList_ReturnsOnlyActiveProducts_WithLowStockFlag()
    {
        var (service, _, productos, _) = CreateService();
        productos.Seed(nombre: "Crítico", codigoSku: "LOW-1", stockActual: 2, stockMinimo: 5);
        productos.Seed(nombre: "Sano", codigoSku: "OK-1", stockActual: 50, stockMinimo: 5);
        var inactivo = productos.Seed(nombre: "Inactivo", codigoSku: "OFF-1");
        inactivo.Desactivar();

        var result = await service.GetStockListAsync(page: 1, pageSize: 20, search: null, lowStock: null);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalItems);
        Assert.Contains(result.Value.Items, i => i.CodigoSku == "LOW-1" && i.AbajoDeMinimo);
        Assert.Contains(result.Value.Items, i => i.CodigoSku == "OK-1" && !i.AbajoDeMinimo);
        Assert.DoesNotContain(result.Value.Items, i => i.CodigoSku == "OFF-1");
    }

    [Fact]
    public async Task GetProductStock_ExistingProduct_ReturnsDetail()
    {
        var (service, _, productos, _) = CreateService();
        var producto = productos.Seed(stockActual: 7, stockMinimo: 3);

        var result = await service.GetProductStockAsync(producto.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.StockActual);
        Assert.Equal(3, result.Value.StockMinimo);
        Assert.False(result.Value.AbajoDeMinimo);
    }

    [Fact]
    public async Task GetProductStock_UnknownProduct_ReturnsNotFound()
    {
        var (service, _, _, _) = CreateService();

        var result = await service.GetProductStockAsync(99);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetMovements_ReturnsHistoryNewestFirst_Br051()
    {
        var (service, _, productos, clock) = CreateService();
        productos.Seed(stockActual: 0);

        await service.RegisterMovementAsync(
            Uid, new CreateMovimientoStockDto(1, TipoMovimientoStock.Ingreso, 10));
        clock.Advance(TimeSpan.FromMinutes(5));
        await service.RegisterMovementAsync(
            Uid, new CreateMovimientoStockDto(1, TipoMovimientoStock.Egreso, 4));

        var result = await service.GetMovementsAsync(page: 1, pageSize: 20, productoId: null);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalItems);
        Assert.Equal(TipoMovimientoStock.Egreso, result.Value.Items[0].Tipo);
        Assert.Equal(TipoMovimientoStock.Ingreso, result.Value.Items[1].Tipo);
        Assert.All(result.Value.Items, m => Assert.Equal(1, m.ProductoId));
        Assert.Equal(6, (await productos.GetByIdAsync(1))!.StockActual);
    }

    [Fact]
    public async Task GetMovements_WithProductFilter_ReturnsOnlyThatProduct()
    {
        var (service, _, productos, _) = CreateService();
        var primero = productos.Seed(codigoSku: "P1");
        var segundo = productos.Seed(codigoSku: "P2");

        await service.RegisterMovementAsync(
            Uid, new CreateMovimientoStockDto(primero.Id, TipoMovimientoStock.Ingreso, 5));
        await service.RegisterMovementAsync(
            Uid, new CreateMovimientoStockDto(segundo.Id, TipoMovimientoStock.Egreso, 2));

        var result = await service.GetMovementsAsync(1, 20, productoId: primero.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalItems);
        Assert.Equal(primero.Id, result.Value.Items[0].ProductoId);
        Assert.Equal(TipoMovimientoStock.Ingreso, result.Value.Items[0].Tipo);
    }

    [Fact]
    public async Task GetMovements_InvalidProduct_ReturnsValidation()
    {
        var (service, _, _, _) = CreateService();

        var result = await service.GetMovementsAsync(1, 20, productoId: 0);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    private static (InventarioService Service, FakeInventarioRepository Inventario, FakeProductoRepository Productos, FakeClock Clock)
        CreateService()
    {
        var productos = new FakeProductoRepository();
        var inventario = new FakeInventarioRepository(productos);
        var usuarios = new FakeUsuarioRepository();
        var clock = new FakeClock();

        usuarios.PorUid[Uid] = Usuario.Crear(
            "juan@test.com", "Juan", "Pérez", rolId: 2, firebaseUid: Uid);

        var service = new InventarioService(productos, inventario, usuarios, clock);
        return (service, inventario, productos, clock);
    }

    private sealed class FakeClock : ITimeProvider
    {
        private DateTime _utcNow = new(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc);

        public DateTime UtcNow => _utcNow;

        public void Advance(TimeSpan delta) => _utcNow += delta;
    }

    private sealed class FakeUsuarioRepository : IUsuarioRepository
    {
        public Dictionary<string, Usuario> PorUid { get; } = new();

        public Task<Usuario?> GetByFirebaseUidAsync(string firebaseUid, CancellationToken cancellationToken = default)
            => Task.FromResult(PorUid.GetValueOrDefault(firebaseUid));

        public Task<PagedResult<Usuario>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<Usuario>());

        public Task<Usuario?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<Usuario?>(null);

        public Task<Usuario?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Usuario?>(null);

        public Task<bool> EmailExistsAsync(string email, int? excludeId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<Usuario> AddAsync(Usuario usuario, CancellationToken cancellationToken = default)
            => Task.FromResult(usuario);

        public Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<int>> GetActiveStaffUserIdsAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<int>>(Array.Empty<int>());
    }

    /// <summary>
    /// Comparte la fuente de productos con <see cref="FakeProductoRepository"/>
    /// para que el stock leído y el stock verificado sean el mismo objeto.
    /// </summary>
    private sealed class FakeInventarioRepository : IInventarioRepository
    {
        private readonly FakeProductoRepository _productos;

        private readonly List<MovimientoStock> _movimientos = new();

        private bool _failNextApply;

        public FakeInventarioRepository(FakeProductoRepository productos)
        {
            _productos = productos;
        }

        public IReadOnlyList<MovimientoStock> All => _movimientos;

        public void FailNextApply() => _failNextApply = true;

        public Task<Producto?> GetStockAsync(int productoId, CancellationToken cancellationToken = default)
            => Task.FromResult(_productos.FindById(productoId));

        public Task<PagedResult<MovimientoStock>> GetMovementsPagedAsync(
            int page,
            int pageSize,
            int? productoId,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<MovimientoStock> query = _movimientos;

            if (productoId.HasValue)
            {
                query = query.Where(m => m.ProductoId == productoId.Value);
            }

            var pageItems = query
                .OrderByDescending(m => m.Fecha)
                .ThenByDescending(m => m.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            foreach (var movimiento in pageItems)
            {
                movimiento.AsociarReferencias(_productos.FindById(movimiento.ProductoId), null);
            }

            return Task.FromResult(new PagedResult<MovimientoStock>
            {
                Items = pageItems,
                Page = page,
                PageSize = pageSize,
                TotalItems = query.Count(),
            });
        }

        public Task<bool> TryApplyMovementAsync(
            MovimientoStock movimiento,
            int stockEsperado,
            CancellationToken cancellationToken = default)
        {
            if (_failNextApply)
            {
                _failNextApply = false;
                return Task.FromResult(false);
            }

            var producto = _productos.FindById(movimiento.ProductoId);
            if (producto is null || producto.StockActual != stockEsperado || !producto.Activo)
            {
                return Task.FromResult(false);
            }

            producto.FijarStock(movimiento.StockPosterior);
            movimiento.Id = _movimientos.Count == 0 ? 1 : _movimientos.Max(m => m.Id) + 1;
            _movimientos.Add(movimiento);
            return Task.FromResult(true);
        }
    }

    private sealed class FakeProductoRepository : IProductoRepository
    {
        private readonly List<Producto> _productos = new();

        public Producto Seed(
            string nombre = "Producto SA",
            string? codigoSku = null,
            int stockActual = 10,
            int stockMinimo = 0)
        {
            var producto = Producto.Crear(
                nombre,
                codigoSku ?? $"SKU-{_productos.Count + 1:D4}",
                TipoProducto.Mueble,
                precio: 100m,
                stockActual: stockActual,
                stockMinimo: stockMinimo);
            producto.Id = _productos.Count == 0 ? 1 : _productos.Max(p => p.Id) + 1;
            _productos.Add(producto);
            return producto;
        }

        public Producto? FindById(int id) => _productos.FirstOrDefault(p => p.Id == id);

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
                    || p.CodigoSku.ToLower().Contains(term));
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
            => Task.FromResult(FindById(id));

        public Task<bool> SkuExistsAsync(string codigoSku, int? excludeId = null, CancellationToken cancellationToken = default)
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
