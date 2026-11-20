using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.IntegrationTests.Fakes;

/// <summary>
/// Fake en memoria de <see cref="IPedidoRepository"/> para tests de API.
/// Los pedidos nacen de la aceptación de presupuestos (vía
/// <see cref="FakePresupuestoRepository"/>) y conservan su historial
/// inicial (BR-075).
/// </summary>
public sealed class FakePedidoRepository : IPedidoRepository
{
    private readonly List<Pedido> _pedidos = new();

    private readonly List<PedidoHistorial> _historial = new();

    private readonly FakeUsuarioRepository? _usuarios;

    private int _nextId = 1;

    private int _nextHistorialId = 1;

    public FakePedidoRepository(FakeUsuarioRepository? usuarios = null)
    {
        _usuarios = usuarios;
    }

    public IReadOnlyList<Pedido> Pedidos => _pedidos;

    public IReadOnlyList<PedidoHistorial> Historial => _historial;

    /// <summary>
    /// Crea el pedido al aceptar el presupuesto. Devuelve null si ya existe
    /// uno para ese presupuesto (barrera del índice único, BR-065).
    /// </summary>
    public Pedido? CrearDesdePresupuesto(Presupuesto presupuesto, DateTime fechaUtc)
    {
        if (_pedidos.Any(p => p.PresupuestoId == presupuesto.Id))
        {
            return null;
        }

        var pedido = Pedido.Crear(
            presupuesto.ClienteId,
            presupuesto.Id,
            presupuesto.Total,
            fechaUtc);
        pedido.Id = _nextId++;

        typeof(Pedido).GetProperty(nameof(Pedido.Cliente))!
            .SetValue(pedido, presupuesto.Cliente);
        typeof(Pedido).GetProperty(nameof(Pedido.Presupuesto))!
            .SetValue(pedido, presupuesto);

        _pedidos.Add(pedido);
        _historial.Add(PedidoHistorial.Crear(
            pedido.Id,
            estadoAnterior: null,
            estadoNuevo: EstadoPedido.Pendiente,
            fechaUtc: fechaUtc,
            usuarioId: null,
            comentario: $"Creado al aceptar el presupuesto #{presupuesto.Id}"));
        return pedido;
    }

    public Task<PagedResult<Pedido>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        int? clienteId,
        EstadoPedido? estado,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Pedido> query = _pedidos;

        if (clienteId.HasValue)
        {
            query = query.Where(p => p.ClienteId == clienteId.Value);
        }

        if (estado.HasValue)
        {
            query = query.Where(p => p.Estado == estado.Value);
        }

        if (dateFrom.HasValue)
        {
            var desde = DateTime.SpecifyKind(dateFrom.Value.Date, DateTimeKind.Utc);
            query = query.Where(p => p.FechaCreacion >= desde);
        }

        if (dateTo.HasValue)
        {
            var hasta = DateTime.SpecifyKind(dateTo.Value.Date, DateTimeKind.Utc).AddDays(1);
            query = query.Where(p => p.FechaCreacion < hasta);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(p =>
                p.Cliente!.Nombre.ToLower().Contains(term)
                || p.Cliente!.Apellido.ToLower().Contains(term)
                || p.Cliente!.Email.ToLower().Contains(term));
        }

        var list = query.OrderByDescending(p => p.Id).ToList();
        return Task.FromResult(new PagedResult<Pedido>
        {
            Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = list.Count,
        });
    }

    public Task<Pedido?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_pedidos.FirstOrDefault(p => p.Id == id));

    public Task<Pedido?> GetByIdReadOnlyAsync(int id, CancellationToken cancellationToken = default)
        => GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<PedidoHistorial>> GetHistorialAsync(
        int pedidoId,
        CancellationToken cancellationToken = default)
    {
        var filas = _historial
            .Where(h => h.PedidoId == pedidoId)
            .OrderByDescending(h => h.FechaUtc)
            .ThenByDescending(h => h.Id)
            .ToList();

        foreach (var fila in filas)
        {
            AttachUsuario(fila);
        }

        return Task.FromResult<IReadOnlyList<PedidoHistorial>>(filas);
    }

    public Task UpdateWithHistoryAsync(
        Pedido pedido,
        PedidoHistorial historial,
        CancellationToken cancellationToken = default)
    {
        historial.Id = _nextHistorialId++;
        AttachUsuario(historial);
        _historial.Add(historial);
        return Task.CompletedTask;
    }

    /// <summary>La edición muta la referencia viva de la lista (no-op).</summary>
    public Task UpdateAsync(Pedido pedido, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    private void AttachUsuario(PedidoHistorial historial)
    {
        if (historial.UsuarioId is null || historial.Usuario is not null)
        {
            return;
        }

        var usuario = _usuarios?.FindById(historial.UsuarioId.Value);
        if (usuario is not null)
        {
            typeof(PedidoHistorial).GetProperty(nameof(PedidoHistorial.Usuario))!
                .SetValue(historial, usuario);
        }
    }
}
