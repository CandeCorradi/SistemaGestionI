using SistemaGestion.Domain.Enums;

namespace SistemaGestion.Application.Reports;

/// <summary>
/// Formatos de exportación de reportes (functional-spec §21).
/// </summary>
public enum ReportFormat
{
    Xlsx = 0,
    Csv = 1,
    Pdf = 2,
}

/// <summary>
/// Claves canónicas de los reportes; coinciden con el segmento de ruta en
/// api-specification.md §20.
/// </summary>
public static class ReportKeys
{
    public const string Orders = "orders";
    public const string Payments = "payments";
    public const string Quotes = "quotes";
    public const string Sales = "sales";
    public const string Inventory = "inventory";

    public static readonly IReadOnlyList<string> Todos =
        new[] { Orders, Payments, Quotes, Sales, Inventory };
}

/// <summary>
/// Filtros comunes de reportes (api-specification.md §20). Los enums
/// viajan como números opcionales y el servicio valida que estén
/// definidos para el reporte concreto.
/// </summary>
public sealed record ReportFilters
{
    public DateTime? From { get; init; }

    public DateTime? To { get; init; }

    public int? CustomerId { get; init; }

    /// <summary>Estado de pedido (orders) o de presupuesto (quotes).</summary>
    public int? Status { get; init; }

    /// <summary>Método de pago (payments).</summary>
    public int? Method { get; init; }

    /// <summary>Tipo de producto (inventory).</summary>
    public int? Type { get; init; }

    /// <summary>Activo/inactivo (inventory).</summary>
    public bool? Active { get; init; }
}

/// <summary>Columna del documento de reporte; <c>Align</c> es left/right/center.</summary>
public sealed record ReportColumn(string Key, string Header, string Align);

/// <summary>Fila del reporte: pares clave→valor (string, decimal, DateTime, int, bool o null).</summary>
public sealed record ReportRow(IReadOnlyDictionary<string, object?> Cells);

/// <summary>Elemento del bloque de resumen (totales) del reporte.</summary>
public sealed record ReportSummaryItem(string Label, object? Value);

/// <summary>
/// Documento de reporte genérico: el preview JSON y las tres exportaciones
/// consumen exactamente la misma estructura.
/// </summary>
public sealed record ReportDocument(
    string Key,
    string Title,
    DateTime GeneratedAtUtc,
    IReadOnlyList<KeyValuePair<string, string>> AppliedFilters,
    IReadOnlyList<ReportColumn> Columns,
    IReadOnlyList<ReportRow> Rows,
    IReadOnlyList<ReportSummaryItem> Summary);

/// <summary>Resultado de una exportación: bytes + metadatos de descarga.</summary>
public sealed record ReportExport(
    ReportDocument Document,
    byte[] Content,
    string ContentType,
    string FileName);
