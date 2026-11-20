using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Presupuestos.Dtos;
using SistemaGestion.Domain.Common;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Presupuestos;

/// <summary>
/// Casos de uso de presupuestos (functional-spec §12, BR-060..BR-064):
/// creación con snapshot comercial (BR-063), totales calculados sólo en
/// backend (BR-062), edición sólo en borrador y transiciones de estado
/// validadas (BR-064). La aceptación atómica con creación de pedido
/// (BR-065) se implementa en el paso 19.
/// </summary>
public sealed class PresupuestosService
{
    /// <summary>Tamaño de página máximo permitido (AGENTS.md §30).</summary>
    public const int MaxPageSize = 100;

    public const int DefaultPageSize = 20;

    private readonly IPresupuestoRepository _presupuestos;

    private readonly IClienteRepository _clientes;

    private readonly IProductoRepository _productos;

    private readonly ITimeProvider _clock;

    private readonly INotificacionPublisher _notificaciones;

    public PresupuestosService(
        IPresupuestoRepository presupuestos,
        IClienteRepository clientes,
        IProductoRepository productos,
        ITimeProvider clock,
        INotificacionPublisher notificaciones)
    {
        _presupuestos = presupuestos;
        _clientes = clientes;
        _productos = productos;
        _clock = clock;
        _notificaciones = notificaciones;
    }

    /// <summary>Listado paginado con filtros de cliente, estado y fechas.</summary>
    public async Task<Result<PagedResult<QuoteListDto>>> GetListAsync(
        int page,
        int pageSize,
        string? search,
        int? customerId,
        EstadoPresupuesto? status,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        if (customerId is <= 0)
        {
            return Error.Validation("El cliente indicado no es válido.");
        }

        if (dateFrom.HasValue && dateTo.HasValue && dateFrom.Value.Date > dateTo.Value.Date)
        {
            return Error.Validation("El rango de fechas no es válido: el inicio no puede ser posterior al fin.");
        }

        var hoy = _clock.UtcNow;
        var paged = await _presupuestos.GetPagedAsync(
            page, pageSize, search, customerId, status, dateFrom, dateTo, cancellationToken);

        return new PagedResult<QuoteListDto>
        {
            Items = paged.Items.Select(p => QuoteListDto.From(p, hoy)).ToList(),
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalItems = paged.TotalItems,
        };
    }

    /// <summary>Detalle de un presupuesto con sus líneas.</summary>
    public async Task<Result<QuoteDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Error.Validation("El presupuesto indicado no es válido.");
        }

        var presupuesto = await _presupuestos.GetByIdReadOnlyAsync(id, cancellationToken);
        if (presupuesto is null)
        {
            return Error.NotFound($"El presupuesto {id} no existe.");
        }

        var orderId = await _presupuestos.GetOrderIdAsync(presupuesto.Id, cancellationToken);
        return QuoteDto.From(presupuesto, _clock.UtcNow, orderId);
    }

    /// <summary>
    /// Crea un presupuesto en borrador con su snapshot: las líneas con
    /// producto congelan el precio vigente (BR-063); las líneas sin
    /// producto usan el precio declarado (mueble a medida, BR-041).
    /// Los totales se calculan aquí, nunca se confían en el frontend (BR-062).
    /// </summary>
    public async Task<Result<QuoteDto>> CreateAsync(
        CreateQuoteDto dto,
        CancellationToken cancellationToken = default)
    {
        var preparado = await PrepararAsync(dto.CustomerId, dto.ExpirationDate, dto.Items, cancellationToken);
        if (preparado.IsFailure)
        {
            return preparado.Error;
        }

        var (cliente, fechaValidez, detalles) = preparado.Value;

        Presupuesto presupuesto;
        try
        {
            presupuesto = Presupuesto.Crear(
                clienteId: cliente.Id,
                fechaValidezUtc: fechaValidez,
                notas: dto.Notes,
                condiciones: dto.Conditions,
                fechaCreacionUtc: _clock.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(ex.Message);
        }

        foreach (var detalle in detalles)
        {
            presupuesto.AgregarDetalle(detalle);
        }

        presupuesto.ValidarAlMenosUnDetalle();

        var creado = await _presupuestos.AddAsync(presupuesto, cancellationToken);
        var resultado = await _presupuestos.GetByIdReadOnlyAsync(creado.Id, cancellationToken);
        return QuoteDto.From(resultado ?? creado, _clock.UtcNow);
    }

    /// <summary>
    /// Editar sólo en borrador (los presupuestos enviados son históricos).
    /// Si se envía <c>Items</c> se reemplazan todas las líneas (snapshot nuevo).
    /// </summary>
    public async Task<Result<QuoteDto>> UpdateAsync(
        int id,
        UpdateQuoteDto dto,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Error.Validation("El presupuesto indicado no es válido.");
        }

        var presupuesto = await _presupuestos.GetByIdAsync(id, cancellationToken);
        if (presupuesto is null)
        {
            return Error.NotFound($"El presupuesto {id} no existe.");
        }

        if (presupuesto.Estado != EstadoPresupuesto.Borrador)
        {
            return Error.Conflict(
                $"Sólo se puede editar un presupuesto en borrador; estado actual: {presupuesto.Estado}.");
        }

        var customerId = dto.CustomerId ?? presupuesto.ClienteId;
        var items = dto.Items ?? presupuesto.Detalles
            .Select(d => new CreateQuoteItemDto(d.ProductoId, d.Descripcion, d.Cantidad, d.PrecioUnitario, d.Descuento))
            .ToList();

        var preparado = await PrepararAsync(customerId, dto.ExpirationDate, items, cancellationToken);
        if (preparado.IsFailure)
        {
            return preparado.Error;
        }

        var (cliente, fechaValidez, detalles) = preparado.Value;

        try
        {
            presupuesto.Actualizar(
                clienteId: cliente.Id,
                fechaValidezUtc: fechaValidez,
                notas: dto.Notes,
                condiciones: dto.Conditions,
                fechaModificacionUtc: _clock.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(ex.Message);
        }

        presupuesto.LimpiarDetalles();
        foreach (var detalle in detalles)
        {
            presupuesto.AgregarDetalle(detalle);
        }

        presupuesto.ValidarAlMenosUnDetalle();

        await _presupuestos.UpdateAsync(presupuesto, cancellationToken);
        var resultado = await _presupuestos.GetByIdReadOnlyAsync(presupuesto.Id, cancellationToken);
        return QuoteDto.From(resultado ?? presupuesto, _clock.UtcNow);
    }

    /// <summary>Borrador → Enviado (BR-061: exige ítems).</summary>
    public async Task<Result<QuoteDto>> SendAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await TransicionarAsync(id, presupuesto => presupuesto.Enviar(_clock.UtcNow));

        if (result.IsSuccess)
        {
            var quote = result.Value;
            await _notificaciones.PublicarAsync(
                new NotificacionDraft(
                    Tipo: TipoNotificacion.PresupuestoEnviado,
                    Titulo: "Presupuesto enviado",
                    Mensaje: $"El presupuesto #{quote.Id} fue enviado al cliente {quote.CustomerName}.",
                    ClienteId: quote.CustomerId,
                    EntidadTipo: "Presupuesto",
                    EntidadId: quote.Id),
                cancellationToken);
        }

        return result;
    }

    /// <summary>Enviado → Rechazado.</summary>
    public async Task<Result<QuoteDto>> RejectAsync(int id, string? reason, CancellationToken cancellationToken = default)
        => await TransicionarAsync(id, presupuesto => presupuesto.Rechazar(reason));

    /// <summary>Borrador/Enviado → Anulado.</summary>
    public async Task<Result<QuoteDto>> CancelAsync(int id, string? reason, CancellationToken cancellationToken = default)
        => await TransicionarAsync(id, presupuesto => presupuesto.Anular(reason));

    /// <summary>
    /// Aceptación atómica (BR-064/BR-065): valida con el dominio (sólo
    /// Enviado y vigente) y luego acepta + crea el pedido en una única
    /// transacción con CAS sobre el estado. Si otro proceso aceptó primero
    /// devuelve Conflict sin crear un segundo pedido.
    /// </summary>
    public async Task<Result<QuoteDto>> AcceptAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Error.Validation("El presupuesto indicado no es válido.");
        }

        var presupuesto = await _presupuestos.GetByIdAsync(id, cancellationToken);
        if (presupuesto is null)
        {
            return Error.NotFound($"El presupuesto {id} no existe.");
        }

        try
        {
            presupuesto.Aceptar(_clock.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Error.Conflict(ex.Message);
        }

        var pedido = await _presupuestos.TryAcceptAndCreateOrderAsync(
            presupuesto, _clock.UtcNow, cancellationToken);

        if (pedido is null)
        {
            return Error.Conflict(
                "El presupuesto fue aceptado concurrentemente por otro proceso; "
                + "no se creó ningún pedido (BR-065). Recargue y verifique el estado.");
        }

        var dto = QuoteDto.From(presupuesto, _clock.UtcNow, pedido.Id);

        await _notificaciones.PublicarAsync(
            new NotificacionDraft(
                Tipo: TipoNotificacion.PresupuestoAceptado,
                Titulo: "Presupuesto aceptado",
                Mensaje: $"El presupuesto #{dto.Id} fue aceptado y se creó el pedido #{dto.OrderId}.",
                ParaEquipo: true,
                EntidadTipo: "Pedido",
                EntidadId: dto.OrderId ?? dto.Id),
            cancellationToken);

        return dto;
    }

    /// <summary>
    /// Ejecuta una transición de estado definida en el dominio (fuente única
    /// de verdad de BR-064). Estado inválido → Conflict; datos inválidos →
    /// Validation.
    /// </summary>
    private async Task<Result<QuoteDto>> TransicionarAsync(
        int id,
        Action<Presupuesto> accion)
    {
        if (id <= 0)
        {
            return Error.Validation("El presupuesto indicado no es válido.");
        }

        var presupuesto = await _presupuestos.GetByIdAsync(id, cancellationToken: default);
        if (presupuesto is null)
        {
            return Error.NotFound($"El presupuesto {id} no existe.");
        }

        try
        {
            accion(presupuesto);
        }
        catch (InvalidOperationException ex)
        {
            return Error.Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(ex.Message);
        }

        await _presupuestos.UpdateAsync(presupuesto);
        var resultado = await _presupuestos.GetByIdReadOnlyAsync(presupuesto.Id);
        return QuoteDto.From(resultado ?? presupuesto, _clock.UtcNow);
    }

    private async Task<Result<(Cliente Cliente, DateTime? FechaValidez, List<PresupuestoDetalle> Detalles)>>
        PrepararAsync(
            int customerId,
            DateTime? expirationDate,
            List<CreateQuoteItemDto>? items,
            CancellationToken cancellationToken)
    {
        if (customerId <= 0)
        {
            return Error.Validation("El cliente es obligatorio (BR-060).");
        }

        if (items is null || items.Count == 0)
        {
            return Error.Validation("El presupuesto debe tener al menos un ítem (BR-061).");
        }

        var cliente = await _clientes.GetByIdAsync(customerId, cancellationToken);
        if (cliente is null)
        {
            return Error.NotFound($"El cliente {customerId} no existe.");
        }

        if (!cliente.Activo)
        {
            return Error.Validation("El cliente está desactivado; reactívelo antes de usarlo en un presupuesto.");
        }

        DateTime? fechaValidez = null;
        if (expirationDate.HasValue)
        {
            var normalizada = expirationDate.Value.Kind == DateTimeKind.Local
                ? expirationDate.Value.ToUniversalTime()
                : expirationDate.Value;

            if (normalizada.Date < _clock.UtcNow.Date)
            {
                return Error.Validation("La fecha de validez no puede ser anterior a hoy.");
            }

            fechaValidez = DateTime.SpecifyKind(normalizada.Date, DateTimeKind.Utc);
        }

        var detalles = new List<PresupuestoDetalle>();
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var preparadoItem = await PrepararDetalleAsync(item, cancellationToken);
            if (preparadoItem.IsFailure)
            {
                // NotFound (producto inexistente) se propaga tal cual; el
                // resto se etiqueta con la posición de la línea.
                return preparadoItem.Error.Code == "NotFound.Error"
                    ? preparadoItem.Error
                    : Error.Validation($"Ítem {i + 1}: {preparadoItem.Error.Description}");
            }

            detalles.Add(preparadoItem.Value);
        }

        return (cliente, fechaValidez, detalles);
    }

    private async Task<Result<PresupuestoDetalle>> PrepararDetalleAsync(
        CreateQuoteItemDto item,
        CancellationToken cancellationToken)
    {
        string descripcion;
        decimal precioUnitario;

        if (item.ProductId.HasValue)
        {
            var producto = await _productos.GetByIdAsync(item.ProductId.Value, cancellationToken);
            if (producto is null)
            {
                return Error.NotFound($"El producto {item.ProductId} no existe.");
            }

            if (!producto.Activo)
            {
                return Error.Validation($"El producto '{producto.Nombre}' está desactivado.");
            }

            // Snapshot comercial (BR-063): se congela el precio vigente.
            descripcion = producto.Nombre;
            precioUnitario = producto.Precio;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(item.Description))
            {
                return Error.Validation("La descripción es obligatoria en líneas sin producto.");
            }

            if (!item.UnitPrice.HasValue)
            {
                return Error.Validation("El precio unitario es obligatorio en líneas sin producto.");
            }

            descripcion = item.Description;
            precioUnitario = item.UnitPrice.Value;
        }

        if (!item.Quantity.HasValue)
        {
            return Error.Validation("La cantidad es obligatoria.");
        }

        try
        {
            return PresupuestoDetalle.Crear(
                descripcion: descripcion,
                cantidad: item.Quantity.Value,
                precioUnitario: precioUnitario,
                descuento: item.Discount ?? 0,
                productoId: item.ProductId);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(ex.Message);
        }
    }
}

