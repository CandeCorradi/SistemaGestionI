using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.IntegrationTests.Fakes;

/// <summary>
/// Fake en memoria de <see cref="IProductoRepository"/> para tests de API.
/// </summary>
public sealed class FakeProductoRepository : IProductoRepository
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
