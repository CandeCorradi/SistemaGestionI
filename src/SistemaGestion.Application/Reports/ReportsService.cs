using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Common;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Reports;

/// <summary>
/// Construye los documentos de reporte (functional-spec §21, roadmap phase
/// 15) y orquesta su exportación. Los datos salen de
/// <see cref="IReportRepository"/> con snapshot histórico (BR-141); la
/// autorización (BR-140) la aplica el controller con REPORTES_VIEW y
/// REPORTES_EXPORT.
/// </summary>
public sealed class ReportsService
{
    private readonly IReportRepository _repository;
    private readonly IReportExporter _exporter;
    private readonly ITimeProvider _timeProvider;

    public ReportsService(
        IReportRepository repository,
        IReportExporter exporter,
        ITimeProvider timeProvider)
    {
        _repository = repository;
        _exporter = exporter;
        _timeProvider = timeProvider;
    }

    /// <summary>Construye el documento del reporte indicado, o NotFound si la clave no existe.</summary>
    public async Task<Result<ReportDocument>> BuildAsync(
        string? key,
        ReportFilters filters,
        CancellationToken cancellationToken = default)
    {
        if (!EsClaveValida(key))
        {
            return Result.Failure<ReportDocument>(Error.NotFound(
                $"Reporte desconocido '{key}'. Claves válidas: {string.Join(", ", ReportKeys.Todos)}."));
        }

        if (filters.From.HasValue && filters.To.HasValue && filters.From.Value > filters.To.Value)
        {
            return Result.Failure<ReportDocument>(Error.Validation(
                "El filtro 'desde' no puede ser posterior al 'hasta'."));
        }

        var normalized = key!.Trim().ToLowerInvariant();
        return normalized switch
        {
            ReportKeys.Orders => await BuildOrdersAsync(filters, cancellationToken),
            ReportKeys.Payments => await BuildPaymentsAsync(filters, cancellationToken),
            ReportKeys.Quotes => await BuildQuotesAsync(filters, cancellationToken),
            ReportKeys.Sales => await BuildSalesAsync(filters, cancellationToken),
            _ => await BuildInventoryAsync(filters, cancellationToken),
        };
    }

    /// <summary>Construye el documento y lo serializa al formato pedido.</summary>
    public async Task<Result<ReportExport>> ExportAsync(
        string? key,
        ReportFilters filters,
        ReportFormat format,
        CancellationToken cancellationToken = default)
    {
        var build = await BuildAsync(key, filters, cancellationToken);
        if (build.IsFailure)
        {
            return Result.Failure<ReportExport>(build.Error);
        }

        var document = build.Value;
        var extension = format switch
        {
            ReportFormat.Xlsx => "xlsx",
            ReportFormat.Csv => "csv",
            _ => "pdf",
        };
        var contentType = format switch
        {
            ReportFormat.Xlsx => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ReportFormat.Csv => "text/csv; charset=utf-8",
            _ => "application/pdf",
        };

        var content = _exporter.Export(document, format);
        var fileName = $"reporte-{document.Key}-{_timeProvider.UtcNow:yyyyMMdd-HHmmss}.{extension}";
        return Result.Success<ReportExport>(
            new ReportExport(document, content, contentType, fileName));
    }

    public static bool EsClaveValida(string? key)
        => !string.IsNullOrWhiteSpace(key)
           && ReportKeys.Todos.Contains(key.Trim(), StringComparer.OrdinalIgnoreCase);

    // ------------------------------------------------------------------
    // orders
    // ------------------------------------------------------------------

    private async Task<Result<ReportDocument>> BuildOrdersAsync(
        ReportFilters filters,
        CancellationToken cancellationToken)
    {
        EstadoPedido? estado = null;
        if (filters.Status.HasValue)
        {
            if (!Enum.IsDefined(typeof(EstadoPedido), filters.Status.Value))
            {
                return EstadoInvalido<ReportDocument>($"{filters.Status.Value} no es un estado de pedido válido.");
            }

            estado = (EstadoPedido)filters.Status.Value;
        }

        var pedidos = await _repository.GetPedidosAsync(
            filters.From, filters.To, estado, filters.CustomerId, cancellationToken);
        var sumasPagadas = await _repository.GetSumasPagadasAsync(
            pedidos.Select(p => p.Id).ToList(), cancellationToken);

        var columns = new List<ReportColumn>
        {
            new("numero", "Pedido", "left"),
            new("cliente", "Cliente", "left"),
            new("fecha", "Fecha", "left"),
            new("estado", "Estado", "center"),
            new("total", "Total", "right"),
            new("pagado", "Pagado", "right"),
            new("saldo", "Saldo", "right"),
        };

        decimal totalSuma = 0, pagadoSuma = 0, saldoSuma = 0;
        var rows = new List<ReportRow>();
        foreach (var pedido in pedidos.OrderBy(p => p.FechaCreacion).ThenBy(p => p.Id))
        {
            var pagado = sumasPagadas.TryGetValue(pedido.Id, out var monto) ? monto : 0m;
            var saldo = pedido.Total - pagado;
            totalSuma += pedido.Total;
            pagadoSuma += pagado;
            saldoSuma += saldo;

            rows.Add(Row(
                ("numero", $"#{pedido.Id}"),
                ("cliente", NombreCliente(pedido.Cliente, pedido.ClienteId)),
                ("fecha", pedido.FechaCreacion),
                ("estado", Etiqueta(pedido.Estado)),
                ("total", pedido.Total),
                ("pagado", pagado),
                ("saldo", saldo)));
        }

        var summary = new List<ReportSummaryItem>
        {
            new("Pedidos", rows.Count),
            new("Total", totalSuma),
            new("Pagado", pagadoSuma),
            new("Saldo", saldoSuma),
        };

        return Result.Success<ReportDocument>(Document(
            ReportKeys.Orders, "Reporte de pedidos", filters, columns, rows, summary));
    }

    // ------------------------------------------------------------------
    // payments
    // ------------------------------------------------------------------

    private async Task<Result<ReportDocument>> BuildPaymentsAsync(
        ReportFilters filters,
        CancellationToken cancellationToken)
    {
        MetodoPago? metodo = null;
        if (filters.Method.HasValue)
        {
            if (!Enum.IsDefined(typeof(MetodoPago), filters.Method.Value))
            {
                return EstadoInvalido<ReportDocument>($"{filters.Method.Value} no es un método de pago válido.");
            }

            metodo = (MetodoPago)filters.Method.Value;
        }

        var pagos = await _repository.GetPagosAsync(
            filters.From, filters.To, filters.CustomerId, metodo, cancellationToken);

        var columns = new List<ReportColumn>
        {
            new("fecha", "Fecha", "left"),
            new("pedido", "Pedido", "left"),
            new("cliente", "Cliente", "left"),
            new("monto", "Monto", "right"),
            new("metodo", "Método", "center"),
            new("referencia", "Referencia", "left"),
            new("registradoPor", "Registrado por", "left"),
        };

        decimal montoSuma = 0;
        var rows = new List<ReportRow>();
        foreach (var pago in pagos.OrderBy(p => p.FechaPago).ThenBy(p => p.Id))
        {
            montoSuma += pago.Monto;
            rows.Add(Row(
                ("fecha", pago.FechaPago),
                ("pedido", $"#{pago.PedidoId}"),
                ("cliente", NombreCliente(pago.Pedido?.Cliente, pago.Pedido?.ClienteId ?? 0)),
                ("monto", pago.Monto),
                ("metodo", Etiqueta(pago.Metodo)),
                ("referencia", (object?)pago.Referencia ?? string.Empty),
                ("registradoPor", pago.Usuario is null
                    ? string.Empty
                    : $"{pago.Usuario.Nombre} {pago.Usuario.Apellido}".Trim())));
        }

        var summary = new List<ReportSummaryItem>
        {
            new("Pagos", rows.Count),
            new("Monto total", montoSuma),
        };

        return Result.Success<ReportDocument>(Document(
            ReportKeys.Payments, "Reporte de pagos", filters, columns, rows, summary));
    }

    // ------------------------------------------------------------------
    // quotes
    // ------------------------------------------------------------------

    private async Task<Result<ReportDocument>> BuildQuotesAsync(
        ReportFilters filters,
        CancellationToken cancellationToken)
    {
        EstadoPresupuesto? estado = null;
        if (filters.Status.HasValue)
        {
            if (!Enum.IsDefined(typeof(EstadoPresupuesto), filters.Status.Value))
            {
                return EstadoInvalido<ReportDocument>($"{filters.Status.Value} no es un estado de presupuesto válido.");
            }

            estado = (EstadoPresupuesto)filters.Status.Value;
        }

        var presupuestos = await _repository.GetPresupuestosAsync(
            filters.From, filters.To, estado, filters.CustomerId, cancellationToken);

        var columns = new List<ReportColumn>
        {
            new("numero", "Presupuesto", "left"),
            new("cliente", "Cliente", "left"),
            new("fecha", "Fecha", "left"),
            new("estado", "Estado", "center"),
            new("subtotal", "Subtotal", "right"),
            new("descuento", "Descuento", "right"),
            new("total", "Total", "right"),
        };

        decimal subtotalSuma = 0, descuentoSuma = 0, totalSuma = 0;
        var rows = new List<ReportRow>();
        foreach (var presupuesto in presupuestos
            .OrderBy(p => p.FechaCreacion).ThenBy(p => p.Id))
        {
            subtotalSuma += presupuesto.Subtotal;
            descuentoSuma += presupuesto.Descuento;
            totalSuma += presupuesto.Total;

            rows.Add(Row(
                ("numero", $"#{presupuesto.Id}"),
                ("cliente", NombreCliente(presupuesto.Cliente, presupuesto.ClienteId)),
                ("fecha", presupuesto.FechaCreacion),
                ("estado", Etiqueta(presupuesto.Estado)),
                ("subtotal", presupuesto.Subtotal),
                ("descuento", presupuesto.Descuento),
                ("total", presupuesto.Total)));
        }

        var summary = new List<ReportSummaryItem>
        {
            new("Presupuestos", rows.Count),
            new("Subtotal", subtotalSuma),
            new("Descuento", descuentoSuma),
            new("Total", totalSuma),
        };

        return Result.Success<ReportDocument>(Document(
            ReportKeys.Quotes, "Reporte de presupuestos", filters, columns, rows, summary));
    }

    // ------------------------------------------------------------------
    // sales (facturas emitidas: ingresos facturados con snapshot BR-141)
    // ------------------------------------------------------------------

    private async Task<Result<ReportDocument>> BuildSalesAsync(
        ReportFilters filters,
        CancellationToken cancellationToken)
    {
        var facturas = await _repository.GetFacturasAsync(
            filters.From, filters.To, filters.CustomerId, cancellationToken);

        var columns = new List<ReportColumn>
        {
            new("numero", "Documento", "left"),
            new("fecha", "Fecha", "left"),
            new("pedido", "Pedido", "left"),
            new("cliente", "Cliente", "left"),
            new("total", "Total", "right"),
            new("condicion", "Condición", "center"),
        };

        decimal totalSuma = 0;
        var rows = new List<ReportRow>();
        foreach (var factura in facturas
            .OrderBy(f => f.FechaEmision).ThenBy(f => f.Id))
        {
            totalSuma += factura.Total;
            rows.Add(Row(
                ("numero", factura.Numero),
                ("fecha", factura.FechaEmision),
                ("pedido", $"#{factura.PedidoId}"),
                ("cliente", factura.ClienteNombre),
                ("total", factura.Total),
                ("condicion", factura.EsSimulada ? "Simulada" : "Emitida")));
        }

        var summary = new List<ReportSummaryItem>
        {
            new("Facturas", rows.Count),
            new("Total facturado", totalSuma),
        };

        return Result.Success<ReportDocument>(Document(
            ReportKeys.Sales, "Reporte de ventas (facturas)", filters, columns, rows, summary));
    }

    // ------------------------------------------------------------------
    // inventory
    // ------------------------------------------------------------------

    private async Task<Result<ReportDocument>> BuildInventoryAsync(
        ReportFilters filters,
        CancellationToken cancellationToken)
    {
        TipoProducto? tipo = null;
        if (filters.Type.HasValue)
        {
            if (!Enum.IsDefined(typeof(TipoProducto), filters.Type.Value))
            {
                return EstadoInvalido<ReportDocument>($"{filters.Type.Value} no es un tipo de producto válido.");
            }

            tipo = (TipoProducto)filters.Type.Value;
        }

        var productos = await _repository.GetProductosAsync(tipo, filters.Active, cancellationToken);

        var columns = new List<ReportColumn>
        {
            new("sku", "SKU", "left"),
            new("producto", "Producto", "left"),
            new("tipo", "Tipo", "center"),
            new("categoria", "Categoría", "left"),
            new("stock", "Stock", "right"),
            new("minimo", "Mínimo", "right"),
            new("costo", "Costo", "right"),
            new("precio", "Precio", "right"),
            new("valor", "Valor stock", "right"),
        };

        int unidades = 0;
        decimal valorSuma = 0;
        var rows = new List<ReportRow>();
        foreach (var producto in productos.OrderBy(p => p.CodigoSku))
        {
            // Valor de inventario a costo de adquisición; sin costo no se
            // estima valor (no se usa el precio de venta como sustituto).
            decimal? valor = producto.Costo is null
                ? null
                : producto.Costo.Value * producto.StockActual;

            unidades += producto.StockActual;
            if (valor.HasValue)
            {
                valorSuma += valor.Value;
            }

            rows.Add(Row(
                ("sku", producto.CodigoSku),
                ("producto", producto.Nombre),
                ("tipo", Etiqueta(producto.Tipo)),
                ("categoria", (object?)producto.Categoria ?? string.Empty),
                ("stock", producto.StockActual),
                ("minimo", producto.StockMinimo),
                ("costo", (object?)producto.Costo ?? string.Empty),
                ("precio", producto.Precio),
                ("valor", (object?)valor ?? string.Empty)));
        }

        var summary = new List<ReportSummaryItem>
        {
            new("Productos", rows.Count),
            new("Unidades en stock", unidades),
            new("Valor inventario (costo)", valorSuma),
        };

        return Result.Success<ReportDocument>(Document(
            ReportKeys.Inventory, "Reporte de inventario", filters, columns, rows, summary));
    }

    // ------------------------------------------------------------------
    // helpers
    // ------------------------------------------------------------------

    private ReportDocument Document(
        string key,
        string title,
        ReportFilters filters,
        List<ReportColumn> columns,
        List<ReportRow> rows,
        List<ReportSummaryItem> summary)
        => new(key, title, _timeProvider.UtcNow, AplicarFiltros(filters), columns, rows, summary);

    private static IReadOnlyList<KeyValuePair<string, string>> AplicarFiltros(ReportFilters filters)
    {
        var chips = new List<KeyValuePair<string, string>>();
        if (filters.From.HasValue || filters.To.HasValue)
        {
            var desde = filters.From?.ToString("yyyy-MM-dd") ?? "…";
            var hasta = filters.To?.ToString("yyyy-MM-dd") ?? "…";
            chips.Add(new("Período", $"{desde} al {hasta}"));
        }

        if (filters.CustomerId.HasValue)
        {
            chips.Add(new("Cliente", $"#{filters.CustomerId}"));
        }

        if (filters.Status.HasValue)
        {
            chips.Add(new("Estado", filters.Status.Value.ToString()));
        }

        if (filters.Method.HasValue)
        {
            chips.Add(new("Método de pago", filters.Method.Value.ToString()));
        }

        if (filters.Type.HasValue)
        {
            chips.Add(new("Tipo de producto", filters.Type.Value.ToString()));
        }

        if (filters.Active.HasValue)
        {
            chips.Add(new("Activo", filters.Active.Value ? "Sí" : "No"));
        }

        return chips;
    }

    private static ReportRow Row(params (string Key, object? Value)[] cells)
        => new(cells.ToDictionary(c => c.Key, c => c.Value));

    private static string NombreCliente(Domain.Entities.Cliente? cliente, int clienteId)
        => cliente is null ? $"#{clienteId}" : $"{cliente.Nombre} {cliente.Apellido}".Trim();

    private static Result<T> EstadoInvalido<T>(string description)
        => Result.Failure<T>(Error.Validation(description));

    private static string Etiqueta(EstadoPedido estado) => estado switch
    {
        EstadoPedido.Pendiente => "Pendiente",
        EstadoPedido.Confirmado => "Confirmado",
        EstadoPedido.EnProduccion => "En producción",
        EstadoPedido.Listo => "Listo",
        EstadoPedido.Entregado => "Entregado",
        EstadoPedido.Completado => "Completado",
        EstadoPedido.Cancelado => "Cancelado",
        _ => estado.ToString(),
    };

    private static string Etiqueta(EstadoPresupuesto estado) => estado switch
    {
        EstadoPresupuesto.Borrador => "Borrador",
        EstadoPresupuesto.Enviado => "Enviado",
        EstadoPresupuesto.Aceptado => "Aceptado",
        EstadoPresupuesto.Rechazado => "Rechazado",
        EstadoPresupuesto.Anulado => "Anulado",
        EstadoPresupuesto.Expirado => "Expirado",
        _ => estado.ToString(),
    };

    private static string Etiqueta(MetodoPago metodo) => metodo switch
    {
        MetodoPago.Efectivo => "Efectivo",
        MetodoPago.Transferencia => "Transferencia",
        MetodoPago.Tarjeta => "Tarjeta",
        MetodoPago.Cheque => "Cheque",
        MetodoPago.Otro => "Otro",
        _ => metodo.ToString(),
    };

    private static string Etiqueta(TipoProducto tipo) => tipo switch
    {
        TipoProducto.Mueble => "Mueble",
        TipoProducto.Melamina => "Melamina",
        TipoProducto.Accesorio => "Accesorio",
        TipoProducto.Insumo => "Insumo",
        TipoProducto.Otro => "Otro",
        _ => tipo.ToString(),
    };
}
