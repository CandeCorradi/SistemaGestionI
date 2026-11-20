using System.Text;
using ClosedXML.Excel;
using SistemaGestion.Application.Reports;
using SistemaGestion.Infrastructure.Reporting;
using Xunit;

namespace SistemaGestion.Infrastructure.IntegrationTests;

/// <summary>
/// Exportador genérico de reportes (functional-spec §21, AGENTS.md §48):
/// CSV RFC 4180, XLSX legible con ClosedXML y PDF con cabecera %PDF.
/// </summary>
public sealed class ReportExporterTests
{
    private static ReportDocument DocumentoDeEjemplo()
    {
        var columns = new List<ReportColumn>
        {
            new("cliente", "Cliente", "left"),
            new("monto", "Monto", "right"),
        };
        var rows = new List<ReportRow>
        {
            new(new Dictionary<string, object?>
            {
                ["cliente"] = "Ana, \"La Gomes\"",
                ["monto"] = 1234.5m,
            }),
        };
        var summary = new List<ReportSummaryItem> { new("Monto total", 1234.5m) };

        return new ReportDocument(
            "orders",
            "Reporte de pedidos",
            new DateTime(2026, 10, 9, 12, 0, 0, DateTimeKind.Utc),
            new[] { new KeyValuePair<string, string>("Período", "2026-10-01 al 2026-10-31") },
            columns,
            rows,
            summary);
    }

    [Fact]
    public void Csv_EscapesCommasAndQuotesAndUsesInvariantDecimals()
    {
        var exporter = new ReportExporter();

        var bytes = exporter.Export(DocumentoDeEjemplo(), ReportFormat.Csv);
        var csv = Encoding.UTF8.GetString(bytes);

        Assert.Contains("Cliente,Monto", csv);
        Assert.Contains("\"Ana, \"\"La Gomes\"\"\"", csv);
        Assert.Contains("1234.50", csv);
        Assert.Contains("Monto total,1234.50", csv);
    }

    [Fact]
    public void Xlsx_WritesHeadersAndRows()
    {
        var exporter = new ReportExporter();

        var bytes = exporter.Export(DocumentoDeEjemplo(), ReportFormat.Xlsx);

        Assert.NotEmpty(bytes);
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();
        Assert.Equal("Reporte de pedidos", sheet.Cell(1, 1).GetString());
        Assert.Equal("Cliente", sheet.Cell(5, 1).GetString());
        Assert.Equal("Monto", sheet.Cell(5, 2).GetString());
        Assert.Equal("Ana, \"La Gomes\"", sheet.Cell(6, 1).GetString());
        Assert.Equal(1234.5d, sheet.Cell(6, 2).GetDouble(), precision: 2);
    }

    [Fact]
    public void Pdf_GeneratesDocumentWithPdfHeader()
    {
        var exporter = new ReportExporter();

        var bytes = exporter.Export(DocumentoDeEjemplo(), ReportFormat.Pdf);

        Assert.True(bytes.Length > 100);
        var header = Encoding.ASCII.GetString(bytes, 0, 4);
        Assert.Equal("%PDF", header);
    }
}
