using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Application.UnitTests;

/// <summary>
/// Repositorio de imágenes de trabajos en memoria para tests de unidad.
/// </summary>
public sealed class FakeTrabajoRealizadoImagenRepository : ITrabajoRealizadoImagenRepository
{
    private readonly List<TrabajoRealizadoImagen> _imagenes = [];

    public IReadOnlyList<TrabajoRealizadoImagen> All => _imagenes;

    /// <summary>Simula un fallo de persistencia al agregar (limpieza de huérfanos).</summary>
    public bool ThrowOnAdd { get; set; }

    public TrabajoRealizadoImagen Seed(int trabajoRealizadoId, string? path = null, int orden = 0)
    {
        var imagen = TrabajoRealizadoImagen.Crear(
            trabajoRealizadoId,
            path ?? $"trabajos/{trabajoRealizadoId}/img-{_imagenes.Count + 1}.jpg",
            orden);
        imagen.Id = _imagenes.Count == 0 ? 1 : _imagenes.Max(i => i.Id) + 1;
        _imagenes.Add(imagen);
        return imagen;
    }

    public Task<IReadOnlyList<TrabajoRealizadoImagen>> GetByTrabajoAsync(
        int trabajoRealizadoId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TrabajoRealizadoImagen>>(
            _imagenes
                .Where(i => i.TrabajoRealizadoId == trabajoRealizadoId)
                .OrderBy(i => i.Orden)
                .ToList());

    public Task<IReadOnlyList<TrabajoRealizadoImagen>> GetByTrabajoIdsAsync(
        IReadOnlyCollection<int> trabajoIds,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TrabajoRealizadoImagen>>(
            _imagenes
                .Where(i => trabajoIds.Contains(i.TrabajoRealizadoId))
                .OrderBy(i => i.TrabajoRealizadoId)
                .ThenBy(i => i.Orden)
                .ToList());

    public Task<TrabajoRealizadoImagen?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_imagenes.FirstOrDefault(i => i.Id == id));

    public Task<int> CountByTrabajoAsync(int trabajoRealizadoId, CancellationToken cancellationToken = default)
        => Task.FromResult(_imagenes.Count(i => i.TrabajoRealizadoId == trabajoRealizadoId));

    public Task<TrabajoRealizadoImagen> AddAsync(
        TrabajoRealizadoImagen imagen,
        CancellationToken cancellationToken = default)
    {
        if (ThrowOnAdd)
        {
            throw new InvalidOperationException("Fallo simulado de persistencia de imagen.");
        }

        imagen.Id = _imagenes.Count == 0 ? 1 : _imagenes.Max(i => i.Id) + 1;
        _imagenes.Add(imagen);
        return Task.FromResult(imagen);
    }

    public Task DeleteAsync(TrabajoRealizadoImagen imagen, CancellationToken cancellationToken = default)
    {
        _imagenes.RemoveAll(i => i.Id == imagen.Id);
        return Task.CompletedTask;
    }

    public Task DeleteByTrabajoAsync(int trabajoRealizadoId, CancellationToken cancellationToken = default)
    {
        _imagenes.RemoveAll(i => i.TrabajoRealizadoId == trabajoRealizadoId);
        return Task.CompletedTask;
    }
}
