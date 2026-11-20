using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.IntegrationTests.Fakes;

/// <summary>
/// Fake en memoria de <see cref="IFacturaRepository"/> para tests de API.
/// Adjunta Usuario a las filas leídas para que los DTOs puedan resolver
/// el nombre de quien emitió la factura. Emular la restricción de unicidad
/// de PedidoId (una factura por pedido) hace que AddAsync falle igual que
/// el índice real de PostgreSQL.
/// </summary>
public sealed class FakeFacturaRepository : IFacturaRepository
{
    private readonly List<Factura> _facturas = new();

    private readonly FakeUsuarioRepository? _usuarios;

    private readonly FakePedidoRepository? _pedidos;

    private int _nextId = 1;

    public FakeFacturaRepository(
        FakeUsuarioRepository? usuarios = null,
        FakePedidoRepository? pedidos = null)
    {
        _usuarios = usuarios;
        _pedidos = pedidos;
    }

    public IReadOnlyList<Factura> Facturas => _facturas;

    public Task<bool> ExistsByPedidoAsync(
        int pedidoId, CancellationToken cancellationToken = default)
        => Task.FromResult(_facturas.Any(f => f.PedidoId == pedidoId));

    public Task AddAsync(Factura factura, CancellationToken cancellationToken = default)
    {
        if (_facturas.Any(f => f.PedidoId == factura.PedidoId))
        {
            throw new InvalidOperationException(
                "Ya existe una factura para el pedido indicado.");
        }

        factura.Id = _nextId++;
        _facturas.Add(factura);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
        => await operation(cancellationToken);

    public Task<Factura?> GetByIdReadOnlyAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var factura = _facturas.FirstOrDefault(f => f.Id == id);
        Attach(factura);
        return Task.FromResult(factura);
    }

    public Task<PagedResult<Factura>> GetPagedAsync(
        int page, int pageSize, int? pedidoId, int? clienteId,
        DateTime? dateFrom, DateTime? dateTo, string? search,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Factura> query = _facturas;

        if (pedidoId.HasValue)
        {
            query = query.Where(f => f.PedidoId == pedidoId.Value);
        }

        if (clienteId.HasValue)
        {
            query = query.Where(f => f.ClienteId == clienteId.Value);
        }

        if (dateFrom.HasValue)
        {
            var desde = DateTime.SpecifyKind(dateFrom.Value.Date, DateTimeKind.Utc);
            query = query.Where(f => f.FechaEmision >= desde);
        }

        if (dateTo.HasValue)
        {
            var hasta = DateTime.SpecifyKind(dateTo.Value.Date, DateTimeKind.Utc).AddDays(1);
            query = query.Where(f => f.FechaEmision < hasta);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(f =>
                f.Numero.ToLower().Contains(term)
                || f.ClienteNombre.ToLower().Contains(term));
        }

        var list = query
            .OrderByDescending(f => f.FechaEmision)
            .ThenByDescending(f => f.Id)
            .ToList();

        foreach (var fila in list)
        {
            Attach(fila);
        }

        return Task.FromResult(new PagedResult<Factura>
        {
            Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = list.Count,
        });
    }

    private void Attach(Factura? factura)
    {
        if (factura is null)
        {
            return;
        }

        if (factura.Usuario is null && factura.UsuarioId is int usuarioId)
        {
            var usuario = _usuarios?.FindById(usuarioId);
            if (usuario is not null)
            {
                typeof(Factura).GetProperty(nameof(Factura.Usuario))!
                    .SetValue(factura, usuario);
            }
        }

        if (factura.Pedido is null && _pedidos is not null)
        {
            var pedido = _pedidos.Pedidos.FirstOrDefault(p => p.Id == factura.PedidoId);
            if (pedido is not null)
            {
                typeof(Factura).GetProperty(nameof(Factura.Pedido))!
                    .SetValue(factura, pedido);
            }
        }
    }
}
