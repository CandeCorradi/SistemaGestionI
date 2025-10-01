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
using System.Data;
using System.Threading.Tasks;

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
            _ = GetAllData();
            checkVerEliminados.CheckedChanged += DisplayHideControlsRestoreButton;
        }

        private void DisplayHideControlsRestoreButton(object? sender, EventArgs e)
        {
            btnRestore.Visible = checkVerEliminados.Checked;
            txtBuscar.Enabled = !checkVerEliminados.Checked;
            btnModificar.Enabled = !checkVerEliminados.Checked;
            btnEliminar.Enabled = !checkVerEliminados.Checked;
            btnAgregar.Enabled = !checkVerEliminados.Checked;
            btnBuscar.Enabled = !checkVerEliminados.Checked;
        }

        private async Task GetAllData()
        {
            if (checkVerEliminados.Checked)
            {
                _clientes = await _clienteService.GetAllDeletedsAsync("");
            }

            else
            {
                _clientes = await _clienteService.GetAllAsync();
            }
            GridClientes.DataSource = _clientes;
            GridClientes.Columns["Id"].Visible = false; // Ocultar la columna Clientes
            GridClientes.Columns["IsDeleted"].Visible = false; // Ocultar la columna Eliminado

        }

        private void GridClientes_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (GridClientes.RowCount > 0 && GridClientes.SelectedRows.Count > 0)
            {
                //Cliente clienteSeleccionada = (Cliente)dataGridClientes.SelectedRows[0].DataBoundItem;
            }
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnAgregar_Click(object sender, EventArgs e)
        {
            LimpiarControlesAgregarEditar();
            tabControlClientes.SelectedTab = tabPageAgregarEditar;
        }
        private void LimpiarControlesAgregarEditar()
        {
            TxtNombre.Clear();
            TxtApellido.Clear();
            TxtTel.Clear();
            TxtDir.Clear();
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            tabControlClientes.SelectedTab = tabPageLista;
        }

        private async void btnGuardar_Click(object sender, EventArgs e)
        {
            Cliente clienteAGuardar = new Cliente
            {
                Id = _currentCliente?.Id ?? 0,
                Nombre = TxtNombre.Text,
                Apellido = TxtApellido.Text,
                Telefono = TxtTel.Text,
                Direccion = TxtDir.Text
            };

            bool response = false;
            if (_currentCliente != null)
            {
                response = await _clienteService.UpdateAsync(clienteAGuardar);
            }
            else
            {
                var nuevoCliente = await _clienteService.AddAsync(clienteAGuardar);
                response = nuevoCliente != null; //si es distinto de null es porque se guardo correctamente
            }
            if (response)
            {
                _currentCliente = null; // Reset the modified movie after saving
                LabelStatusMessage.Text = $"Cliente {clienteAGuardar.Nombre} guardado correctamente";
                TimerStatusBar.Start(); // Iniciar el temporizador para mostrar el mensaje en la barra de estado
                await GetAllData();
                LimpiarControlesAgregarEditar();
                tabControlClientes.SelectedTab = tabPageLista;
            }
            else
            {
                MessageBox.Show("Error al guardar el cliente", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnModificar_Click(object sender, EventArgs e)
        {
            //checheamos que haya una capacitacion seleccionada
            if (GridClientes.RowCount > 0 && GridClientes.SelectedRows.Count > 0) //rowcount filas
            {
                _currentCliente = (Cliente)GridClientes.SelectedRows[0].DataBoundItem; //dataBoundItem trae el cliente seleccionado
                TxtNombre.Text = _currentCliente.Nombre;
                TxtApellido.Text = _currentCliente.Apellido;
                TxtTel.Text = _currentCliente.Telefono;
                TxtDir.Text = _currentCliente.Direccion;


                tabControlClientes.SelectedTab = tabPageAgregarEditar;
            }
            else
            {
                MessageBox.Show("Debe seleccionar un cliente para modificarlo", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtBuscar_TextChanged(object sender, EventArgs e)
        {
            //btnBuscar.PerformClick(); //busca a medida que se escribe
        }

        private async void btnEliminar_Click(object sender, EventArgs e)
        {

            {
                //checheamos que haya peliculas seleccionadas
                if (GridClientes.RowCount > 0 && GridClientes.SelectedRows.Count > 0)
                {
                    Cliente entitySelected = (Cliente)GridClientes.SelectedRows[0].DataBoundItem;
                    var respuesta = MessageBox.Show($"¿Seguro que desea eliminar a este cliente {entitySelected.Nombre}?", "Confirmar Eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (respuesta == DialogResult.Yes)
                    {
                        if (await _clienteService.DeleteAsync(entitySelected.Id))
                        {
                            LabelStatusMessage.Text = $"Cliente {entitySelected.Nombre} eliminado correctamente";
                            TimerStatusBar.Start(); // Iniciar el temporizador para mostrar el mensaje en la barra de estado
                            await GetAllData();
                        }
                        else
                        {
                            MessageBox.Show("Error al eliminar a este cliente", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Debe seleccionar un cliente para eliminarlo", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void checkVerEliminados_CheckedChanged(object sender, EventArgs e)
        {
            await GetAllData();
        }

        private async void btnRestore_Click(object sender, EventArgs e)
        {
            if (!checkVerEliminados.Checked) return;

            if (GridClientes.RowCount > 0 && GridClientes.SelectedRows.Count > 0)
            {
                Cliente entitySelected = (Cliente)GridClientes.SelectedRows[0].DataBoundItem;
                var respuesta = MessageBox.Show($"¿Seguro que desea restaurar a este cliente {entitySelected.Nombre}?", "Confirmar Restauración", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (respuesta == DialogResult.Yes)//en lo posible poner dentro de un try/catch
                {
                    if (await _clienteService.RestoreAsync(entitySelected.Id))
                    {
                        LabelStatusMessage.Text = $"Cliente {entitySelected.Nombre} restaurado correctamente";
                        TimerStatusBar.Start(); // Iniciar el temporizador para mostrar el mensaje en la barra de estado
                        await GetAllData();
                    }
                    else
                    {
                        MessageBox.Show("Error al restaurar a este cliente", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Debe seleccionar un cliente para restaurarlo", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnBuscar_Click(object sender, EventArgs e)
        {
            GridClientes.DataSource = await _clienteService.GetAllAsync(txtBuscar.Text);
        }
    }
}
