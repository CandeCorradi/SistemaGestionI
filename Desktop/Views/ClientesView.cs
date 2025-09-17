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
    public partial class ClientesView : Form
    {
        GenericService<Cliente> _clienteService = new GenericService<Cliente>();
        Cliente _currentCliente;
        List<Cliente>? _clientes;
        public ClientesView()
        {
            InitializeComponent();
            _=GetAllData();
        }
        private async Task GetAllData()
        {
            _clientes = await _clienteService.GetAllAsync();
            dataGridClientes.DataSource = _clientes;
            dataGridClientes.Columns["Id"].Visible = false; // Ocultar la columna ClientesId
            dataGridClientes.Columns["IsDeleted"].Visible = false; // Ocultar la columna Eliminado
        }

        private void dataGridClientes_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridClientes.RowCount > 0 && dataGridClientes.SelectedRows.Count > 0)
            {
                //Cliente clienteSeleccionada = (Cliente)dataGridClientes.SelectedRows[0].DataBoundItem;
            }
        }
    }
}
