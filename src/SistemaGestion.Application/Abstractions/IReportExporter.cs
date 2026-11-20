namespace SistemaGestion.Application.Abstractions;

/// <summary>
/// Genera el archivo de exportación de un <see cref="Reports.ReportDocument"/>
/// (AGENTS.md §48: la generación vive fuera de los controllers). Las
/// implementaciones (ClosedXML/CSV/QuestPDF) viven en Infrastructure.
/// </summary>
public interface IReportExporter
{
    byte[] Export(Reports.ReportDocument document, Reports.ReportFormat format);
}
