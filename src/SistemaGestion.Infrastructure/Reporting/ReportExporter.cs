using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Reports;

namespace SistemaGestion.Infrastructure.Reporting;

/// <summary>
/// Exportador genérico de <see cref="ReportDocument"/>: un solo generador
/// alimentado por columnas/filas, con tres formatos — XLSX (ClosedXML),
/// CSV (RFC 4180 con BOM UTF-8) y PDF (QuestPDF, A4 horizontal).
/// AGENTS.md §48: la generación vive en Infrastructure, fuera de controllers.
/// </summary>
public sealed class ReportExporter : IReportExporter
{
    static ReportExporter()
    {
        // Licencia Community de QuestPDF: aplica a empresas pequeñas
        // (<10 empleados / <1M USD); la carpintería entra de lleno.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Export(ReportDocument document, ReportFormat format)
        => format switch
        {
            ReportFormat.Xlsx => GenerarXlsx(document),
            ReportFormat.Csv => GenerarCsv(document),
            _ => GenerarPdf(document),
        };

    // ------------------------------------------------------------------
    // XLSX
    // ------------------------------------------------------------------

    private static byte[] GenerarXlsx(ReportDocument document)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Reporte");

        sheet.Cell(1, 1).Value = document.Title;
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;

        sheet.Cell(2, 1).Value =
            $"Generado: {document.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC · Sistema de Gestión";
        sheet.Cell(2, 1).Style.Font.FontSize = 9;

        if (document.AppliedFilters.Count > 0)
        {
            sheet.Cell(3, 1).Value = "Filtros: " + string.Join(
                " · ",
                document.AppliedFilters.Select(f => $"{f.Key}: {f.Value}"));
            sheet.Cell(3, 1).Style.Font.FontSize = 9;
        }

        const int headerRow = 5;
        for (var c = 0; c < document.Columns.Count; c++)
        {
            var cell = sheet.Cell(headerRow, c + 1);
            cell.Value = document.Columns[c].Header;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E9ECEF");
        }

        var row = headerRow + 1;
        foreach (var dataRow in document.Rows)
        {
            for (var c = 0; c < document.Columns.Count; c++)
            {
                sheet.Cell(row, c + 1).Value =
                    XLCellValue.FromObject(dataRow.Cells.GetValueOrDefault(document.Columns[c].Key));
            }

            row++;
        }

        if (document.Summary.Count > 0)
        {
            row++;
            sheet.Cell(row, 1).Value = "Resumen";
            sheet.Cell(row, 1).Style.Font.Bold = true;
            row++;
            foreach (var item in document.Summary)
            {
                sheet.Cell(row, 1).Value = item.Label;
                sheet.Cell(row, 2).Value = XLCellValue.FromObject(item.Value);
                row++;
            }
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // ------------------------------------------------------------------
    // CSV
    // ------------------------------------------------------------------

    private static byte[] GenerarCsv(ReportDocument document)
    {
        var builder = new StringBuilder();

        // BOM UTF-8 para que Excel reconozca la codificación.
        builder.Append('﻿');

        builder.AppendLine(string.Join(",", document.Columns.Select(c => Escapar(c.Header))));
        foreach (var row in document.Rows)
        {
            builder.AppendLine(string.Join(",", document.Columns.Select(c =>
                Escapar(FormatearCelda(row.Cells.GetValueOrDefault(c.Key))))));
        }

        if (document.Summary.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Resumen");
            foreach (var item in document.Summary)
            {
                builder.AppendLine($"{Escapar(item.Label)},{Escapar(FormatearCelda(item.Value))}");
            }
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static string Escapar(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }

    // ------------------------------------------------------------------
    // PDF
    // ------------------------------------------------------------------

    private static byte[] GenerarPdf(ReportDocument document)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(t => t.FontSize(9));

                page.Content().Column(column =>
                {
                    column.Item().Text(document.Title).FontSize(15).Bold();
                    column.Item().Text(
                        $"Generado: {document.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC · Sistema de Gestión")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);

                    if (document.AppliedFilters.Count > 0)
                    {
                        column.Item().PaddingTop(4).Text(
                            "Filtros: " + string.Join(
                                " · ",
                                document.AppliedFilters.Select(f => $"{f.Key}: {f.Value}")))
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                    }

                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            foreach (var col in document.Columns)
                            {
                                var weight = col.Align == "right" ? 0.9f : 1.2f;
                                columns.RelativeColumn(weight);
                            }
                        });

                        table.Header(header =>
                        {
                            foreach (var col in document.Columns)
                            {
                                var cell = header.Cell()
                                    .Background(Colors.Grey.Lighten3)
                                    .Padding(4);
                                var text = cell.Text(col.Header).Bold();
                                if (col.Align == "right")
                                {
                                    text.AlignRight();
                                }
                                else if (col.Align == "center")
                                {
                                    text.AlignCenter();
                                }
                            }
                        });

                        foreach (var row in document.Rows)
                        {
                            foreach (var col in document.Columns)
                            {
                                var cell = table.Cell().Padding(4);
                                var text = cell.Text(
                                    FormatearCelda(row.Cells.GetValueOrDefault(col.Key)));
                                if (col.Align == "right")
                                {
                                    text.AlignRight();
                                }
                                else if (col.Align == "center")
                                {
                                    text.AlignCenter();
                                }
                            }
                        }
                    });

                    if (document.Summary.Count > 0)
                    {
                        column.Item().PaddingTop(10).Text("Resumen").Bold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                            });

                            foreach (var item in document.Summary)
                            {
                                table.Cell().Padding(2).Text(item.Label);
                                table.Cell().Padding(2).AlignRight()
                                    .Text(FormatearCelda(item.Value));
                            }
                        });
                    }
                });
            });
        }).GeneratePdf();
    }

    // ------------------------------------------------------------------
    // helpers
    // ------------------------------------------------------------------

    private static string FormatearCelda(object? value)
        => value switch
        {
            null => string.Empty,
            string text => text,
            DateTime fecha => fecha.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            decimal monto => monto.ToString("0.00", CultureInfo.InvariantCulture),
            double numero => numero.ToString("0.00", CultureInfo.InvariantCulture),
            int entero => entero.ToString(CultureInfo.InvariantCulture),
            long entero => entero.ToString(CultureInfo.InvariantCulture),
            bool flag => flag ? "Sí" : "No",
            IFormattable formateable => formateable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
}
