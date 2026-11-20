using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;

namespace SistemaGestion.API.IntegrationTests.Fakes;

/// <summary>
/// Fake en memoria de <see cref="IProductoImagenRepository"/> para tests de API.
/// </summary>
public sealed class FakeProductoImagenRepository : IProductoImagenRepository
{
    private readonly List<ProductoImagen> _imagenes = [];

    public IReadOnlyList<ProductoImagen> All => _imagenes;

    public ProductoImagen Seed(int productoId, string? path = null, int orden = 0)
    {
        var imagen = ProductoImagen.Crear(
            productoId,
            path ?? $"productos/{productoId}/img-{_imagenes.Count + 1}.jpg",
            orden);
        imagen.Id = _imagenes.Count == 0 ? 1 : _imagenes.Max(i => i.Id) + 1;
        _imagenes.Add(imagen);
        return imagen;
    }

    public Task<IReadOnlyList<ProductoImagen>> GetByProductoAsync(
        int productoId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ProductoImagen>>(
            _imagenes
                .Where(i => i.ProductoId == productoId)
                .OrderBy(i => i.Orden)
                .ToList());

    public Task<IReadOnlyList<ProductoImagen>> GetByProductoIdsAsync(
        IReadOnlyCollection<int> productoIds,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ProductoImagen>>(
            _imagenes
                .Where(i => productoIds.Contains(i.ProductoId))
                .OrderBy(i => i.ProductoId)
                .ThenBy(i => i.Orden)
                .ToList());

    public Task<ProductoImagen?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_imagenes.FirstOrDefault(i => i.Id == id));

    public Task<int> CountByProductoAsync(int productoId, CancellationToken cancellationToken = default)
        => Task.FromResult(_imagenes.Count(i => i.ProductoId == productoId));

    public Task<ProductoImagen> AddAsync(ProductoImagen imagen, CancellationToken cancellationToken = default)
    {
        imagen.Id = _imagenes.Count == 0 ? 1 : _imagenes.Max(i => i.Id) + 1;
        _imagenes.Add(imagen);
        return Task.FromResult(imagen);
    }

    public Task DeleteAsync(ProductoImagen imagen, CancellationToken cancellationToken = default)
    {
        _imagenes.RemoveAll(i => i.Id == imagen.Id);
        return Task.CompletedTask;
    }
}
