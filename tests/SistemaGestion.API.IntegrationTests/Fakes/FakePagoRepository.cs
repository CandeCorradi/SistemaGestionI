using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.IntegrationTests.Fakes;

/// <summary>
/// Fake en memoria de <see cref="IPagoRepository"/> para tests de API.
/// Adjunta Usuario y Pedido a las filas leídas para que los DTOs puedan
/// resolver nombre de usuario y de cliente.
/// </summary>
public sealed class FakePagoRepository : IPagoRepository
{
    private readonly List<Pago> _pagos = new();

    private readonly FakeUsuarioRepository? _usuarios;

    private readonly FakePedidoRepository? _pedidos;

    private int _nextId = 1;

    public FakePagoRepository(
        FakeUsuarioRepository? usuarios = null,
        FakePedidoRepository? pedidos = null)
    {
        _usuarios = usuarios;
        _pedidos = pedidos;
    }

    public IReadOnlyList<Pago> Pagos => _pagos;

    public Task<decimal> GetTotalPagadoAsync(
        int pedidoId, CancellationToken cancellationToken = default)
        => Task.FromResult(_pagos.Where(p => p.PedidoId == pedidoId).Sum(p => p.Monto));

    public Task AddAsync(Pago pago, CancellationToken cancellationToken = default)
    {
        pago.Id = _nextId++;
        _pagos.Add(pago);
        return Task.CompletedTask;
    }

    public async Task ExecuteSerializableAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
        => await operation(cancellationToken);

    public Task<IReadOnlyList<Pago>> GetByPedidoAsync(
        int pedidoId, CancellationToken cancellationToken = default)
    {
        var filas = _pagos
            .Where(p => p.PedidoId == pedidoId)
            .OrderByDescending(p => p.FechaPago)
            .ThenByDescending(p => p.Id)
            .ToList();

        foreach (var fila in filas)
        {
            Attach(fila);
        }

        return Task.FromResult<IReadOnlyList<Pago>>(filas);
    }

    public Task<PagedResult<Pago>> GetPagedAsync(
        int page,
        int pageSize,
        int? pedidoId,
        int? clienteId,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? search,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Pago> query = _pagos;

        if (pedidoId.HasValue)
        {
            query = query.Where(p => p.PedidoId == pedidoId.Value);
        }

        if (clienteId.HasValue)
        {
            // Resuelve el pedido sin depender de Attach: el filtrado corre
            // antes de poblar las navegaciones.
            query = query.Where(p =>
            {
                var pedido = p.Pedido
                    ?? _pedidos?.Pedidos.FirstOrDefault(x => x.Id == p.PedidoId);
                return pedido is not null && pedido.ClienteId == clienteId.Value;
            });
        }

        if (dateFrom.HasValue)
        {
            var desde = DateTime.SpecifyKind(dateFrom.Value.Date, DateTimeKind.Utc);
            query = query.Where(p => p.FechaPago >= desde);
        }

        if (dateTo.HasValue)
        {
            var hasta = DateTime.SpecifyKind(dateTo.Value.Date, DateTimeKind.Utc).AddDays(1);
            query = query.Where(p => p.FechaPago < hasta);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(p =>
                (p.Referencia != null && p.Referencia.ToLower().Contains(term))
                || (p.Notas != null && p.Notas.ToLower().Contains(term)));
        }

        var list = query
            .OrderByDescending(p => p.FechaPago)
            .ThenByDescending(p => p.Id)
            .ToList();

        foreach (var fila in list)
        {
            Attach(fila);
        }

        return Task.FromResult(new PagedResult<Pago>
        {
            Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = list.Count,
        });
    }

    public Task<ClientePagosResumen> GetResumenClienteAsync(
        int clienteId, CancellationToken cancellationToken = default)
    {
        var pedidosDelCliente = _pedidos?.Pedidos
            .Where(p => p.ClienteId == clienteId)
            .ToList() ?? new List<Pedido>();

        var totalPedidos = pedidosDelCliente
            .Where(p => p.Estado != EstadoPedido.Cancelado)
            .Sum(p => p.Total);

        var idsPedidos = pedidosDelCliente.Select(p => p.Id).ToHashSet();
        var totalPagado = _pagos
            .Where(p => idsPedidos.Contains(p.PedidoId))
            .Sum(p => p.Monto);

        return Task.FromResult(new ClientePagosResumen(totalPedidos, totalPagado));
    }

    private void Attach(Pago pago)
    {
        if (pago.Usuario is null && pago.UsuarioId is int usuarioId)
        {
            var usuario = _usuarios?.FindById(usuarioId);
            if (usuario is not null)
            {
                typeof(Pago).GetProperty(nameof(Pago.Usuario))!
                    .SetValue(pago, usuario);
            }
        }

        if (pago.Pedido is null && _pedidos is not null)
        {
            var pedido = _pedidos.Pedidos.FirstOrDefault(p => p.Id == pago.PedidoId);
            if (pedido is not null)
            {
                typeof(Pago).GetProperty(nameof(Pago.Pedido))!
                    .SetValue(pago, pedido);
            }
        }
    }
}
