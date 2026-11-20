using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGestion.API.Authorization;
using SistemaGestion.Application.Reports;
using SistemaGestion.Domain.Authorization;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.Controllers;

/// <summary>
/// Reportes operativos (api-spec §20, BR-140/BR-141): preview JSON y
/// exportación XLSX/CSV/PDF de pedidos, pagos, presupuestos, ventas
/// (facturas) e inventario. La visualización requiere REPORTES_VIEW y la
/// exportación REPORTES_EXPORT.
/// </summary>
[ApiController]
[Route("api/v1/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly ReportsService _reports;

    public ReportsController(ReportsService reports)
    {
        _reports = reports;
    }

    /// <summary>Preview JSON del reporte con filtros comunes.</summary>
    [HttpGet("{key}")]
    [RequirePermission(Permisos.ReportesView)]
    [ProducesResponseType(typeof(ReportDocument), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        string key,
        [FromQuery] ReportFilters filters,
        CancellationToken cancellationToken)
    {
        var result = await _reports.BuildAsync(key, filters, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ProblemFromError(result.Error);
    }

    /// <summary>Exporta el reporte como archivo (format=xlsx|csv|pdf).</summary>
    [HttpGet("{key}/export")]
    [RequirePermission(Permisos.ReportesExport)]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Export(
        string key,
        [FromQuery] ReportFilters filters,
        [FromQuery] string? format,
        CancellationToken cancellationToken)
    {
        if (!TryParseFormat(format, out var parsedFormat))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation.Error",
                detail: "Formato inválido; use xlsx, csv o pdf.");
        }

        var result = await _reports.ExportAsync(key, filters, parsedFormat, cancellationToken);
        return result.IsSuccess
            ? File(result.Value.Content, result.Value.ContentType, result.Value.FileName)
            : ProblemFromError(result.Error);
    }

    private static bool TryParseFormat(string? format, out ReportFormat parsed)
    {
        parsed = default;
        if (string.IsNullOrWhiteSpace(format))
        {
            return false;
        }

        switch (format.Trim().ToLowerInvariant())
        {
            case "xlsx":
                parsed = ReportFormat.Xlsx;
                return true;
            case "csv":
                parsed = ReportFormat.Csv;
                return true;
            case "pdf":
                parsed = ReportFormat.Pdf;
                return true;
            default:
                return false;
        }
    }

    private IActionResult ProblemFromError(Error error)
        => Problem(
            statusCode: error.Code switch
            {
                "Validation.Error" => StatusCodes.Status400BadRequest,
                "NotFound.Error" => StatusCodes.Status404NotFound,
                "Conflict.Error" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError,
            },
            title: error.Code,
            detail: error.Description);
}
