using Microsoft.Reporting.WinForms;
using Service.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Desktop.ViewReports
{
    public partial class ProductosViewReports : Form
    {
        ReportViewer _report;
        List<Producto> _productos;

        public ProductosViewReports(List<Producto> productos) //Le mando todos los productos del servicio
        {
            InitializeComponent();
            _report = new ReportViewer();
            _report.Dock = DockStyle.Fill;
            _productos = productos ?? new List<Producto>();
            this.Controls.Add(_report);
            this.Load += ProductosViewReports_Load;
        }

        private void ProductosViewReports_Load(object? sender, EventArgs e)
        {
            _report.LocalReport.ReportEmbeddedResource = "Desktop.Reports.ProductosReport.rdlc";

            _report.LocalReport.DataSources.Clear(); // Limpiamos por las dudas

            var listaMelamina = _productos
                 .Where(p => string.Equals(p.Categoria, "Melamina", StringComparison.OrdinalIgnoreCase))
                 .Select(p => new
                 {
                     Melamina = p.Nombre,   
                     Stock = (decimal)p.Stock, // tipo numérico para coincidir con el XSD (Stock decimal)
                     Precio = p.Precio
                 })
                 .ToList();

            var listaAccesorio = _productos
                .Where(p => string.Equals(p.Categoria, "Accesorio", StringComparison.OrdinalIgnoreCase))
                .Select(p => new
                {
                    Accesorio = p.Nombre,  // coincide con Fields!Accesorio en el RDL
                    Stock = (decimal)p.Stock,
                    Precio = p.Precio
                })
                .ToList();

            // Vinculamos a los DataSources que ya tenés en tu panel izquierdo
            _report.LocalReport.DataSources.Add(new ReportDataSource("DSProductosMelamina", listaMelamina));
            _report.LocalReport.DataSources.Add(new ReportDataSource("DSProductosAccesorios", listaAccesorio));

            _report.SetDisplayMode(Microsoft.Reporting.WinForms.DisplayMode.PrintLayout);
            _report.ZoomMode = Microsoft.Reporting.WinForms.ZoomMode.Percent;
            _report.ZoomPercent = 100;

            _report.RefreshReport();
        }
    }
}
