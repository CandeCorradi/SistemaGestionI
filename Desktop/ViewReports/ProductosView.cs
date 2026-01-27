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
using Microsoft.ReportingServices.Interfaces;
using Desktop.ViewReports;

namespace Desktop.ViewReports
{
    public partial class ProductosView : Form
    {
        ReportViewer _report;
        List<Producto> _productos;

        public ProductosView(List<Producto> productosEnStock)
        {
            InitializeComponent();
            _report = new ReportViewer();
            _report.Dock = DockStyle.Fill;
            _productos = productosEnStock;
            this.Controls.Add(_report);            

        }

       

        private void ProductosView_Load_1(object sender, EventArgs e)
        {
           
                _report.LocalReport.ReportEmbeddedResource = "Desktop.Reports.ProductosReport.rdlc";

                var datos = _productos.Select(p => new
                {
                    Nombre = p.Nombre,
                    StockActual = p.Stock,
                    Detalle = $"Producto: {p.Nombre} - Cantidad: {p.Stock}"
                });
                
                _report.LocalReport.DataSources.Add(new ReportDataSource("ProductosDataSet", datos));
                _report.SetDisplayMode(DisplayMode.PrintLayout); // Modo de visualización para impresión
                _report.ZoomMode = ZoomMode.Percent;
                _report.ZoomPercent = 100; // Ajusta el zoom al 100%

                _report.RefreshReport();          
           
        }
    }
}
