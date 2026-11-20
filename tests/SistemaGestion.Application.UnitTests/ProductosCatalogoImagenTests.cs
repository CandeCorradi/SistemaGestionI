using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Productos;
using SistemaGestion.Application.Productos.Dtos;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;
using Xunit;

namespace SistemaGestion.Application.UnitTests;

/// <summary>
/// Tests del catÃ¡logo pÃºblico (functional-spec Â§9) y de la gestiÃ³n de
/// imÃ¡genes de producto en storage externo (BR-100/BR-101,
/// AGENTS.md Â§24/Â§40).
/// </summary>
public sealed class ProductosCatalogoImagenTests
{
    private const string DefaultContentType = "image/png";

    // ------------------------------------------------------------------
    // CatÃ¡logo â€” listado
    // ------------------------------------------------------------------

    [Fact]
    public async Task CatalogList_ExcludesInactiveProducts()
    {
        var ctx = CreateContext();
        ctx.Repo.Seed(nombre: "Activo Cat", codigoSku: "CAT-ACT");
        var inactivo = ctx.Repo.Seed(nombre: "Inactivo Cat", codigoSku: "CAT-INACT");
        inactivo.Desactivar();

        var result = await ctx.Service.GetCatalogPagedAsync(1, 20, null, null, null);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items, i => i.Nombre == "Activo Cat");
        Assert.DoesNotContain(result.Value.Items, i => i.Nombre == "Inactivo Cat");
    }

    [Fact]
    public async Task CatalogList_DoesNotExposeInternalFields()
    {
        var ctx = CreateContext();
        ctx.Repo.Seed();

        var result = await ctx.Service.GetCatalogPagedAsync(1, 20, null, null, null);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);

        var propiedades = typeof(CatalogoItemDto).GetProperties().Select(p => p.Name).ToHashSet();
        Assert.DoesNotContain("Costo", propiedades);
        Assert.DoesNotContain("StockActual", propiedades);
        Assert.DoesNotContain("StockMinimo", propiedades);
        Assert.DoesNotContain("ProveedorId", propiedades);
        Assert.DoesNotContain("Activo", propiedades);
        Assert.Contains("Disponible", propiedades);
    }

    [Fact]
    public async Task CatalogList_WithPortada_ReturnsFirstImageUrl()
    {
        var ctx = CreateContext();
        var producto = ctx.Repo.Seed();
        ctx.Imagenes.Seed(producto.Id, $"productos/{producto.Id}/b.jpg", orden: 1);
        ctx.Imagenes.Seed(producto.Id, $"productos/{producto.Id}/a.jpg", orden: 0);

        var result = await ctx.Service.GetCatalogPagedAsync(1, 20, null, null, null);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal($"{ctx.Storage.BaseUrl}/productos/{producto.Id}/a.jpg", item.ImagenUrl);
    }

    [Fact]
    public async Task CatalogList_WithoutImages_ImagenUrlIsNull()
    {
        var ctx = CreateContext();
        ctx.Repo.Seed();

        var result = await ctx.Service.GetCatalogPagedAsync(1, 20, null, null, null);

        Assert.True(result.IsSuccess);
        Assert.Null(Assert.Single(result.Value.Items).ImagenUrl);
    }

    [Fact]
    public async Task CatalogList_WithZeroStock_DisponibleIsFalse()
    {
        var ctx = CreateContext();
        ctx.Repo.Seed(nombre: "Sin Stock", stockActual: 0);
        ctx.Repo.Seed(nombre: "Con Stock", stockActual: 3);

        var result = await ctx.Service.GetCatalogPagedAsync(1, 20, null, null, null);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items, i => i.Nombre == "Con Stock" && i.Disponible);
        Assert.Single(result.Value.Items, i => i.Nombre == "Sin Stock" && !i.Disponible);
    }

    [Fact]
    public async Task CatalogList_WithSearch_FiltersByName()
    {
        var ctx = CreateContext();
        ctx.Repo.Seed(nombre: "Mesa Comedor", codigoSku: "MES-01");
        ctx.Repo.Seed(nombre: "Heladera", codigoSku: "HEL-01");

        var result = await ctx.Service.GetCatalogPagedAsync(1, 20, "mesa", null, null);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal("Mesa Comedor", item.Nombre);
    }

    [Fact]
    public async Task CatalogList_WithInvalidTipo_ReturnsValidation()
    {
        var ctx = CreateContext();

        var result = await ctx.Service.GetCatalogPagedAsync(1, 20, null, (TipoProducto)999, null);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    // ------------------------------------------------------------------
    // CatÃ¡logo â€” detalle
    // ------------------------------------------------------------------

    [Fact]
    public async Task CatalogDetail_Unknown_ReturnsNotFound()
    {
        var ctx = CreateContext();

        var result = await ctx.Service.GetCatalogByIdAsync(999);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task CatalogDetail_Inactive_ReturnsNotFound()
    {
        var ctx = CreateContext();
        var producto = ctx.Repo.Seed();
        producto.Desactivar();

        var result = await ctx.Service.GetCatalogByIdAsync(producto.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task CatalogDetail_WithImages_ReturnsSortedGalleryWithoutInternalFields()
    {
        var ctx = CreateContext();
        var producto = ctx.Repo.Seed(nombre: "Mueble Cat", precio: 250m, stockActual: 0);
        ctx.Imagenes.Seed(producto.Id, $"productos/{producto.Id}/seg.jpg", orden: 1);
        ctx.Imagenes.Seed(producto.Id, $"productos/{producto.Id}/pri.jpg", orden: 0);

        var result = await ctx.Service.GetCatalogByIdAsync(producto.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(250m, result.Value.Precio);
        Assert.False(result.Value.Disponible);
        Assert.Equal(2, result.Value.Imagenes.Count);
        Assert.Equal($"{ctx.Storage.BaseUrl}/productos/{producto.Id}/pri.jpg", result.Value.Imagenes[0].Url);
        Assert.Equal($"{ctx.Storage.BaseUrl}/productos/{producto.Id}/seg.jpg", result.Value.Imagenes[1].Url);

        var propiedades = typeof(CatalogoProductoDto).GetProperties().Select(p => p.Name).ToHashSet();
        Assert.DoesNotContain("Costo", propiedades);
        Assert.DoesNotContain("StockActual", propiedades);
        Assert.DoesNotContain("ProveedorId", propiedades);
    }

    // ------------------------------------------------------------------
    // ImÃ¡genes â€” alta
    // ------------------------------------------------------------------

    [Fact]
    public async Task AddImage_UnknownProduct_ReturnsNotFound()
    {
        var ctx = CreateContext();

        var result = await ctx.Service.AddImageAsync(
            999, "foto.png", DefaultContentType, NewStream(), 10);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
        Assert.Empty(ctx.Storage.UploadedPaths);
    }

    [Fact]
    public async Task AddImage_WithoutFile_ReturnsValidation()
    {
        var ctx = CreateContext();
        var producto = ctx.Repo.Seed();

        var result = await ctx.Service.AddImageAsync(
            producto.Id, null, null, NewStream(), 0);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task AddImage_TooLarge_ReturnsValidation()
    {
        var ctx = CreateContext();
        var producto = ctx.Repo.Seed();

        var result = await ctx.Service.AddImageAsync(
            producto.Id, "grande.png", DefaultContentType, NewStream(), ProductosService.MaxImageBytes + 1);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
        Assert.Contains("5 MB", result.Error.Description);
    }

    [Fact]
    public async Task AddImage_InvalidExtension_ReturnsValidation()
    {
        var ctx = CreateContext();
        var producto = ctx.Repo.Seed();

        var result = await ctx.Service.AddImageAsync(
            producto.Id, "documento.pdf", "application/pdf", NewStream(), 10);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
        Assert.Empty(ctx.Storage.UploadedPaths);
    }

    [Fact]
    public async Task AddImage_InvalidContentType_ReturnsValidation()
    {
        var ctx = CreateContext();
        var producto = ctx.Repo.Seed();

        var result = await ctx.Service.AddImageAsync(
            producto.Id, "foto.png", "text/html", NewStream(), 10);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
        Assert.Empty(ctx.Storage.UploadedPaths);
    }

    [Fact]
    public async Task AddImage_StorageNotConfigured_ReturnsStorageError()
    {
        var ctx = CreateContext();
        var producto = ctx.Repo.Seed();
        ctx.Storage.IsConfigured = false;

        var result = await ctx.Service.AddImageAsync(
            producto.Id, "foto.png", DefaultContentType, NewStream(), 10);

        Assert.True(result.IsFailure);
        Assert.Equal("Storage.Error", result.Error.Code);
    }

    [Fact]
    public async Task AddImage_WhenUploadFails_PropagatesStorageError()
    {
        var ctx = CreateContext();
        var producto = ctx.Repo.Seed();
        ctx.Storage.UploadError = Error.Storage("fallo simulado");

        var result = await ctx.Service.AddImageAsync(
            producto.Id, "foto.png", DefaultContentType, NewStream(), 10);

        Assert.True(result.IsFailure);
        Assert.Equal("Storage.Error", result.Error.Code);
        Assert.Empty(ctx.Imagenes.All);
    }

    [Fact]
    public async Task AddImage_Valid_UploadsAndRegistersReference()
    {
        var ctx = CreateContext();
        var producto = ctx.Repo.Seed();
        ctx.Imagenes.Seed(producto.Id, $"productos/{producto.Id}/existente.jpg", orden: 0);

        var result = await ctx.Service.AddImageAsync(
            producto.Id, "foto.PNG", DefaultContentType, NewStream(), 10);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Orden);
        var path = Assert.Single(ctx.Storage.UploadedPaths);
        Assert.StartsWith($"productos/{producto.Id}/", path);
        Assert.EndsWith(".png", path);
        Assert.Equal($"{ctx.Storage.BaseUrl}/{path}", result.Value.Url);
        Assert.Equal(2, ctx.Imagenes.All.Count);
        Assert.Equal(path, ctx.Imagenes.All.Single(i => i.Orden == 1).Path);
    }

    [Fact]
    public async Task AddImage_WhenPersistFails_DeletesUploadedObject()
    {
        var ctx = CreateContext();
        var producto = ctx.Repo.Seed();
        ctx.Imagenes.ThrowOnAdd = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => ctx.Service.AddImageAsync(
            producto.Id, "foto.png", DefaultContentType, NewStream(), 10));

        var path = Assert.Single(ctx.Storage.UploadedPaths);
        Assert.Equal(path, Assert.Single(ctx.Storage.DeletedPaths));
    }

    // ------------------------------------------------------------------
    // ImÃ¡genes â€” consulta y baja
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetImages_UnknownProduct_ReturnsNotFound()
    {
        var ctx = CreateContext();

        var result = await ctx.Service.GetImagesAsync(999);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task GetImages_ReturnsOrderedPublicUrls()
    {
        var ctx = CreateContext();
        var producto = ctx.Repo.Seed();
        ctx.Imagenes.Seed(producto.Id, $"productos/{producto.Id}/2.jpg", orden: 1);
        ctx.Imagenes.Seed(producto.Id, $"productos/{producto.Id}/1.jpg", orden: 0);

        var result = await ctx.Service.GetImagesAsync(producto.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.EndsWith("/1.jpg", result.Value[0].Url);
        Assert.EndsWith("/2.jpg", result.Value[1].Url);
    }

    [Fact]
    public async Task DeleteImage_Unknown_ReturnsNotFound()
    {
        var ctx = CreateContext();
        var producto = ctx.Repo.Seed();

        var result = await ctx.Service.DeleteImageAsync(producto.Id, 999);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task DeleteImage_OfAnotherProduct_ReturnsNotFound()
    {
        var ctx = CreateContext();
        var productoA = ctx.Repo.Seed();
        var productoB = ctx.Repo.Seed();
        var imagen = ctx.Imagenes.Seed(productoA.Id);

        var result = await ctx.Service.DeleteImageAsync(productoB.Id, imagen.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
        Assert.Single(ctx.Imagenes.All);
        Assert.Empty(ctx.Storage.DeletedPaths);
    }

    [Fact]
    public async Task DeleteImage_Existing_RemovesStorageObjectAndReference()
    {
        var ctx = CreateContext();
        var producto = ctx.Repo.Seed();
        var imagen = ctx.Imagenes.Seed(producto.Id);

        var result = await ctx.Service.DeleteImageAsync(producto.Id, imagen.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(imagen.Path, Assert.Single(ctx.Storage.DeletedPaths));
        Assert.Empty(ctx.Imagenes.All);
    }

    [Fact]
    public async Task DeleteImage_WhenStorageFails_KeepsReference()
    {
        var ctx = CreateContext();
        var producto = ctx.Repo.Seed();
        var imagen = ctx.Imagenes.Seed(producto.Id);
        ctx.Storage.DeleteError = Error.Storage("fallo simulado");

        var result = await ctx.Service.DeleteImageAsync(producto.Id, imagen.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Storage.Error", result.Error.Code);
        Assert.Single(ctx.Imagenes.All);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static MemoryStream NewStream() => new(new byte[] { 1, 2, 3 });

    private static Context CreateContext()
    {
        var repo = new FakeProductoRepository();
        var imagenes = new FakeProductoImagenRepository();
        var storage = new FakeStorageService();
        return new Context(new ProductosService(repo, imagenes, storage), repo, imagenes, storage);
    }

    private sealed record Context(
        ProductosService Service,
        FakeProductoRepository Repo,
        FakeProductoImagenRepository Imagenes,
        FakeStorageService Storage);

    /// <summary>
    /// Fake mÃ­nimo de productos: cubre los filtros que consume el catÃ¡logo.
    /// </summary>
    private sealed class FakeProductoRepository : IProductoRepository
    {
        private readonly List<Producto> _productos = [];

        public Producto Seed(
            string nombre = "Producto Cat",
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


