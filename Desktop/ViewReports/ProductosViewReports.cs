using Microsoft.Reporting.WinForms;
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

        public ProductosViewReports()
        {
            InitializeComponent();
            _report = new ReportViewer();
            _report.Dock = DockStyle.Fill;
            this.Controls.Add(_report);
        }

        private void ProductosViewReports_Load(object sender, EventArgs e)
        {
            _report.LocalReport.ReportEmbeddedResource = "Desktop.Reports.ProductosReport.rdlc";
            _report.SetDisplayMode(DisplayMode.PrintLayout); // Establece el modo de visualización a PrintLayout
            _report.ZoomMode = ZoomMode.Percent;
            _report.ZoomPercent = 100; // Establece el zoom al 100%
            _report.RefreshReport();
        }
    }
}
