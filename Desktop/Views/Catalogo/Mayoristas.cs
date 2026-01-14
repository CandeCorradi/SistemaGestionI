using Service.Models;
using Service.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Desktop.Views
{
    public partial class Mayoristas : Form
    {
        GenericService<Mayorista> _mayoristaService = new GenericService<Mayorista>();
        Mayorista _currentMayorista;
        List<Mayorista>? _mayoristas;
        public Mayoristas()
        {
            InitializeComponent();
            _ = GetAllData();
        }

        private async Task GetAllData()
        {
            try
            {
                _mayoristas = await _mayoristaService.GetAllAsync();
                GridMayorista.DataSource = _mayoristas;
                GridMayorista.Columns["Id"].Visible = false; // Ocultar la columna Mayoristas
                GridMayorista.Columns["IsDeleted"].Visible = false; // Ocultar la columna Eliminado
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar los mayoristas: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GridMayorista_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (GridMayorista.RowCount > 0 && GridMayorista.SelectedRows.Count > 0)
            {
                //Mayorista mayoristaSeleccionada = (Mayorista)dataGridMayoristas.SelectedRows[0].DataBoundItem;
            }
        }
    }
}
