using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.IntegrationTests.Fakes;

/// <summary>
/// Fake en memoria de <see cref="ITrabajoRealizadoRepository"/> para
/// tests de API.
/// </summary>
public sealed class FakeTrabajoRealizadoRepository : ITrabajoRealizadoRepository
{
    private readonly List<TrabajoRealizado> _trabajos = new();

    public IReadOnlyList<TrabajoRealizado> All => _trabajos;

    public TrabajoRealizado Seed(
        string nombre = "Trabajo API",
        string? tipoMueble = null,
        string? clienteNombre = null,
        bool publicarNombreCliente = false,
        DateOnly? fecha = null,
        string? descripcion = null,
        string? categoria = null,
        IEnumerable<string>? tags = null,
        bool destacado = false,
        bool publicado = true)
    {
        var trabajo = TrabajoRealizado.Crear(
            nombre,
            tipoMueble,
            clienteNombre,
            publicarNombreCliente,
            fecha,
            descripcion,
            categoria: categoria,
            tags: tags,
            destacado: destacado,
            publicado: publicado);
        trabajo.Id = _trabajos.Count == 0 ? 1 : _trabajos.Max(t => t.Id) + 1;
        _trabajos.Add(trabajo);
        return trabajo;
    }

    public Task<PagedResult<TrabajoRealizado>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        bool? publicado,
        bool? destacado,
        string? categoria,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<TrabajoRealizado> query = _trabajos;

        if (publicado.HasValue)
        {
            query = query.Where(t => t.Publicado == publicado.Value);
        }

        if (destacado.HasValue)
        {
            query = query.Where(t => t.Destacado == destacado.Value);
        }

        if (!string.IsNullOrWhiteSpace(categoria))
        {
            var cat = categoria.ToLower();
            query = query.Where(t => t.Categoria != null && t.Categoria.ToLower() == cat);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(t =>
                t.Nombre.ToLower().Contains(term)
                || (t.Descripcion != null && t.Descripcion.ToLower().Contains(term))
                || (t.TipoMueble != null && t.TipoMueble.ToLower().Contains(term))
                || (t.ClienteNombre != null && t.ClienteNombre.ToLower().Contains(term)));
        }

        var list = query
            .OrderByDescending(t => t.Destacado)
            .ThenByDescending(t => t.Fecha)
            .ThenByDescending(t => t.Id)
            .ToList();

        return Task.FromResult(new PagedResult<TrabajoRealizado>
        {
            Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = list.Count,
        });
    }

    public Task<PagedResult<TrabajoRealizado>> GetShowroomPagedAsync(
        int page,
        int pageSize,
        string? search,
        string? categoria,
        string? tipoMueble,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<TrabajoRealizado> query = _trabajos.Where(t => t.Publicado);

        if (!string.IsNullOrWhiteSpace(categoria))
        {
            var cat = categoria.ToLower();
            query = query.Where(t => t.Categoria != null && t.Categoria.ToLower() == cat);
        }

        if (!string.IsNullOrWhiteSpace(tipoMueble))
        {
            var tipo = tipoMueble.ToLower();
            query = query.Where(t => t.TipoMueble != null && t.TipoMueble.ToLower() == tipo);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(t =>
                t.Nombre.ToLower().Contains(term)
                || (t.Descripcion != null && t.Descripcion.ToLower().Contains(term))
                || (t.Categoria != null && t.Categoria.ToLower().Contains(term)));
        }

        var list = query
            .OrderByDescending(t => t.Destacado)
            .ThenByDescending(t => t.Fecha)
            .ThenByDescending(t => t.Id)
            .ToList();

        return Task.FromResult(new PagedResult<TrabajoRealizado>
        {
            Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = list.Count,
        });
    }

    public Task<TrabajoRealizado?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_trabajos.FirstOrDefault(t => t.Id == id));

    public Task<TrabajoRealizado?> GetPublishedByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_trabajos.FirstOrDefault(t => t.Id == id && t.Publicado));

    public Task<TrabajoRealizado> AddAsync(TrabajoRealizado trabajo, CancellationToken cancellationToken = default)
    {
        trabajo.Id = _trabajos.Count == 0 ? 1 : _trabajos.Max(t => t.Id) + 1;
        _trabajos.Add(trabajo);
        return Task.FromResult(trabajo);
    }

    public Task UpdateAsync(TrabajoRealizado trabajo, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task DeleteAsync(TrabajoRealizado trabajo, CancellationToken cancellationToken = default)
    {
        _trabajos.RemoveAll(t => t.Id == trabajo.Id);
        return Task.CompletedTask;
    }
}
