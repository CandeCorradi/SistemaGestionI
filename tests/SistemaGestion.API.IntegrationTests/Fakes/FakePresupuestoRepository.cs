using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.IntegrationTests.Fakes;

/// <summary>
/// Fake en memoria de <see cref="IPresupuestoRepository"/> para tests de
/// API. Puebla la navegación de Cliente desde el fake de clientes pasado.
/// </summary>
    public sealed class FakePresupuestoRepository : IPresupuestoRepository
    {
        private readonly List<Presupuesto> _presupuestos = new();

        private readonly FakePedidoRepository _pedidos;

        private readonly FakeClienteRepository _clientes;

        public FakePresupuestoRepository(
            FakeClienteRepository clientes,
            FakePedidoRepository? pedidos = null)
        {
            _clientes = clientes;
            _pedidos = pedidos ?? new FakePedidoRepository();
        }

        public IReadOnlyList<Presupuesto> All => _presupuestos;

        public IReadOnlyList<Pedido> Pedidos => _pedidos.Pedidos;

        /// <summary>
        /// Simula el CAS perdiendo la carrera (otro proceso aceptó primero):
        /// la siguiente llamada a TryAcceptAndCreateOrderAsync devuelve null
        /// sin crear el pedido, como el ExecuteUpdate con 0 filas.
        /// </summary>
        public bool FailNextAccept { get; set; }

    private void AttachCliente(Presupuesto p)
    {
        var cliente = _clientes.All.FirstOrDefault(c => c.Id == p.ClienteId);
        if (cliente is not null)
        {
            typeof(Presupuesto)
                .GetProperty(nameof(Presupuesto.Cliente))!
                .SetValue(p, cliente);
        }
    }

    public Task<PagedResult<Presupuesto>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        int? clienteId,
        EstadoPresupuesto? estado,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Presupuesto> query = _presupuestos;

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
            {
                var c = _clientes.All.FirstOrDefault(x => x.Id == p.ClienteId);
                return c is not null
                    && (c.Nombre.ToLower().Contains(term)
                        || c.Apellido.ToLower().Contains(term)
                        || c.Email.ToLower().Contains(term));
            });
        }

        var list = query.OrderByDescending(p => p.Id).ToList();
        foreach (var p in list)
        {
            AttachCliente(p);
        }

        return Task.FromResult(new PagedResult<Presupuesto>
        {
            Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = list.Count,
        });
    }

    public Task<Presupuesto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var presupuesto = _presupuestos.FirstOrDefault(p => p.Id == id);
        if (presupuesto is not null)
        {
            AttachCliente(presupuesto);
        }

        return Task.FromResult(presupuesto);
    }

    public Task<Presupuesto?> GetByIdReadOnlyAsync(int id, CancellationToken cancellationToken = default)
        => GetByIdAsync(id, cancellationToken);

    public Task<Presupuesto> AddAsync(Presupuesto presupuesto, CancellationToken cancellationToken = default)
    {
        presupuesto.Id = _presupuestos.Count == 0 ? 1 : _presupuestos.Max(p => p.Id) + 1;
        var lineas = presupuesto.Detalles.ToList();
        for (var i = 0; i < lineas.Count; i++)
        {
            lineas[i].Id = i + 1;
        }

        _presupuestos.Add(presupuesto);
        return Task.FromResult(presupuesto);
    }

    public Task UpdateAsync(Presupuesto presupuesto, CancellationToken cancellationToken = default)
    {
        // El objeto ya está en la lista y mutado en memoria (equivalente
        // al SaveChanges de EF con tracking).
        if (!_presupuestos.Contains(presupuesto))
        {
            _presupuestos.Add(presupuesto);
        }

        return Task.CompletedTask;
    }

    public Task<Pedido?> TryAcceptAndCreateOrderAsync(
        Presupuesto presupuesto,
        DateTime fechaUtc,
        CancellationToken cancellationToken = default)
    {
        // El CAS de la fila real mira la BD (que aún dice Enviado); aquí la
        // carrera se simula con FailNextAccept o con el índice único.
        if (FailNextAccept)
        {
            FailNextAccept = false;
            return Task.FromResult<Pedido?>(null);
        }

        // El store aplica la barrera del índice único sobre PresupuestoId
        // (BR-065) y registra el historial inicial (BR-075).
        return Task.FromResult(_pedidos.CrearDesdePresupuesto(presupuesto, fechaUtc));
    }

    public Task<int?> GetOrderIdAsync(int presupuestoId, CancellationToken cancellationToken = default)
    {
        var pedido = _pedidos.Pedidos.FirstOrDefault(p => p.PresupuestoId == presupuestoId);
        return Task.FromResult<int?>(pedido?.Id);
    }
}
