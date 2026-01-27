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
            checkVerEliminados.CheckedChanged += DisplayHideControlsRestoreButton;
        }

        private void DisplayHideControlsRestoreButton(object? sender, EventArgs e)
        {
            btnRestore.Visible = checkVerEliminados.Checked;
            txtBuscar.Enabled = !checkVerEliminados.Checked;
            btnModificar.Enabled = !checkVerEliminados.Checked;
            btnEliminar.Enabled = !checkVerEliminados.Checked;
            btnAgregar.Enabled = !checkVerEliminados.Checked;
            btnBuscarMay.Enabled = !checkVerEliminados.Checked;
        }

        private async Task GetAllData()
        {
            if (checkVerEliminados.Checked)
            {
                _mayoristas = await _mayoristaService.GetAllDeletedsAsync("");
            }
            else
            {
                _mayoristas = await _mayoristaService.GetAllAsync();
            }
            GridMayorista.DataSource = _mayoristas;
            GridMayorista.Columns["Id"].Visible = false; // Ocultar la columna Mayoristas
            GridMayorista.Columns["IsDeleted"].Visible = false; // Ocultar la columna Eliminado
        }

        private void GridMayorista_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (GridMayorista.RowCount > 0 && GridMayorista.SelectedRows.Count > 0)
            {
                Mayorista mayoristaSeleccionada = (Mayorista)GridMayorista.SelectedRows[0].DataBoundItem;
            }
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnAgregar_Click(object sender, EventArgs e)
        {
            LimpiarControlesAgregarEditar();
            tabControlMayoristas.SelectedTab = tabPageAgregarEditar;
        }

        private void LimpiarControlesAgregarEditar()
        {
            TxtNombre.Clear();
            TxtApellido.Clear();
            TxtDir.Clear();
            TxtEmail.Clear();
            TxtTel.Clear();
            TxtTipoProducto.Clear();
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            tabControlMayoristas.SelectedTab = tabPageLista;
        }

        private async void btnGuardar_Click(object sender, EventArgs e)
        {
            Mayorista mayoristaAGuardar = new Mayorista()
            {
                Nombre = TxtNombre.Text,
                Apellido = TxtApellido.Text,
                Telefono = TxtTel.Text,
                Direccion = TxtDir.Text,
                Email = TxtEmail.Text,
                TipoProducto = TxtTipoProducto.Text
            };
            bool response = false;
            if (_currentMayorista == null)
            {
                response = await _mayoristaService.UpdateAsync(mayoristaAGuardar);
            }
            else
            {
                var nuevoMAyorista = await _mayoristaService.AddAsync(mayoristaAGuardar);
                response = nuevoMAyorista != null;
            }
            if (response)
            {
                _currentMayorista = null;
                LabelStatusMessage.Text = $"Cliente {mayoristaAGuardar.Nombre} guardado correctamente";
                timer1.Start(); // Iniciar el temporizador para mostrar el mensaje en la barra de estado
                await GetAllData();
                LimpiarControlesAgregarEditar();
                tabControlMayoristas.SelectedTab = tabPageLista;
            }
            else
            {
                MessageBox.Show("Ocurrió un error al guardar el mayorista. Por favor, intente nuevamente.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnModificar_Click(object sender, EventArgs e)
        {
            //checheamos que haya un mayorista seleccionado
            if (GridMayorista.RowCount > 0 && GridMayorista.SelectedRows.Count > 0) //rowcount filas
            {
                _currentMayorista = (Mayorista)GridMayorista.SelectedRows[0].DataBoundItem; //dataBoundItem trae el cliente seleccionado
                TxtNombre.Text = _currentMayorista.Nombre;
                TxtApellido.Text = _currentMayorista.Apellido;
                TxtTel.Text = _currentMayorista.Telefono;
                TxtDir.Text = _currentMayorista.Direccion;


                tabControlMayoristas.SelectedTab = tabPageAgregarEditar;
            }
            else
            {
                MessageBox.Show("Debe seleccionar un mayorista para modificarlo", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtBuscar_TextChanged(object sender, EventArgs e)
        {
            btnBuscarMay.PerformClick(); //busca a medida que se escribe
        }

        private async void btnEliminar_Click(object sender, EventArgs e)
        {
            {
                //checheamos que haya mayoristas seleccionados
                if (GridMayorista.RowCount > 0 && GridMayorista.SelectedRows.Count > 0)
                {
                    Cliente entitySelected = (Cliente)GridMayorista.SelectedRows[0].DataBoundItem;
                    var respuesta = MessageBox.Show($"¿Seguro que desea eliminar a este mayorista {entitySelected.Nombre}?", "Confirmar Eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (respuesta == DialogResult.Yes)
                    {
                        if (await _mayoristaService.DeleteAsync(entitySelected.Id))
                        {
                            LabelStatusMessage.Text = $"Mayorista {entitySelected.Nombre} eliminado correctamente";
                            timer1.Start(); // Iniciar el temporizador para mostrar el mensaje en la barra de estado
                            await GetAllData();
                        }
                        else
                        {
                            MessageBox.Show("Error al eliminar a este mayorista", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Debe seleccionar un mayorista para eliminarlo", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            if (GridMayorista.RowCount > 0 && GridMayorista.SelectedRows.Count > 0)
            {
                Cliente entitySelected = (Cliente)GridMayorista.SelectedRows[0].DataBoundItem;
                var respuesta = MessageBox.Show($"¿Seguro que desea restaurar a este cliente {entitySelected.Nombre}?", "Confirmar Restauración", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (respuesta == DialogResult.Yes)//en lo posible poner dentro de un try/catch
                {
                    if (await _mayoristaService.RestoreAsync(entitySelected.Id))
                    {
                        LabelStatusMessage.Text = $"Cliente {entitySelected.Nombre} restaurado correctamente";
                        timer1.Start(); // Iniciar el temporizador para mostrar el mensaje en la barra de estado
                        await GetAllData();
                    }
                    else
                    {
                        MessageBox.Show("Error al restaurar a este mayorista", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Debe seleccionar un mayorista para restaurarlo", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

       

        private void btnBuscarMay_Click(object sender, EventArgs e)
        {
            GridMayorista.DataSource = _mayoristas?.Where(m => m.Nombre.Contains(txtBuscar.Text, StringComparison.OrdinalIgnoreCase) || m.Apellido.Contains(txtBuscar.Text, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }
}
