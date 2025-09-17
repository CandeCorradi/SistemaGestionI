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
    public partial class MayoristasView : Form
    {
        GenericService<Mayorista> _mayoristaService = new GenericService<Mayorista>();
        Mayorista _currentMayorista;
        List<Mayorista>? _mayoristas;
        public MayoristasView()
        {
            InitializeComponent();
            _ = GetAllData();
        }
        private async Task GetAllData()
        {
            _mayoristas = await _mayoristaService.GetAllAsync(null);
            dataGridMayoristas.DataSource = _mayoristas;
            dataGridMayoristas.Columns["Id"].Visible = false; // Ocultar la columna MayoristasId
            dataGridMayoristas.Columns["IsDeleted"].Visible = false; // Ocultar la columna Eliminado
        }

        private void dataGridClientes_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridMayoristas.RowCount > 0 && dataGridMayoristas.SelectedRows.Count > 0)
            {
                //Cliente clienteSeleccionada = (Cliente)dataGridClientes.SelectedRows[0].DataBoundItem;
            }
        }
    }
}
