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

namespace Desktop.Views.Catalogo
{
    public partial class ProductosView : Form
    {
        GenericService<Producto> _productoService = new GenericService<Producto>();
        Producto _currentProducto;
        List<Producto>? _productos;
        public ProductosView()
        {
            InitializeComponent();
            _ = GetAllData();
            checkVerEliminados.CheckedChanged += DisplayHideControlsRestoreButton;
        }

        private async Task GetAllData()
        {
            if (checkVerEliminados.Checked)
            {
                _productos = await _productoService.GetAllDeletedsAsync("");
            }

            else
            {
                _productos = await _productoService.GetAllAsync();
            }
            GridProductos.DataSource = _productos;
            GridProductos.Columns["Id"].Visible = false; // Ocultar la columna Clientes
            GridProductos.Columns["IsDeleted"].Visible = false; // Ocultar la columna Eliminado
        }

        private void DisplayHideControlsRestoreButton(object? sender, EventArgs e)
        {
            btnRestore.Visible = checkVerEliminados.Checked;
            txtBuscar.Enabled = !checkVerEliminados.Checked;
            btnModificar.Enabled = !checkVerEliminados.Checked;
            btnEliminar.Enabled = !checkVerEliminados.Checked;
            btnAgregar.Enabled = !checkVerEliminados.Checked;
            btnBuscarProducto.Enabled = !checkVerEliminados.Checked;
        }

        private void GridProductos_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (GridProductos.RowCount > 0 && GridProductos.SelectedRows.Count > 0)
            {
                Producto productoSeleccionada = (Producto)GridProductos.SelectedRows[0].DataBoundItem;
            }
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnAgregar_Click(object sender, EventArgs e)
        {
            LimpiarControlesAgregarEditar();
            tabControlProductos.SelectedTab = tabPageAgregarEditar;
        }

        private void LimpiarControlesAgregarEditar()
        {
            TxtNombre.Clear();
            TxtDescripcionPedido.Clear();
            TxtMedida.Clear();
            TxtStock.Clear();
            CmbCategoria.SelectedIndex = -1;
            TxtPrecio.Clear();
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            tabControlProductos.SelectedTab = tabPageLista;
        }

        private async void btnGuardar_Click(object sender, EventArgs e)
        {
            Producto productoAGuardar = new Producto()
            {
                Nombre = TxtNombre.Text,
                Descripcion = TxtDescripcionPedido.Text,
                Medida = TxtMedida.Text,
                Stock = int.Parse(TxtStock.Text),
                Categoria = CmbCategoria.SelectedItem.ToString(),
                Precio = decimal.Parse(TxtPrecio.Text)
            };
            bool response = false;
            if (_currentProducto != null)
            {
                response = await _productoService.UpdateAsync(productoAGuardar);
            }
            else
            {
                var nuevoCliente = await _productoService.AddAsync(productoAGuardar);
                response = nuevoCliente != null; //si es distinto de null es porque se guardo correctamente
            }
            if (response)
            {
                _currentProducto = null; // Reset the modified movie after saving
                TimerStatusBar.Text = $"Producto {productoAGuardar.Nombre} guardado correctamente";
                timer2.Start(); // Iniciar el temporizador para mostrar el mensaje en la barra de estado
                await GetAllData();
                LimpiarControlesAgregarEditar();
                tabControlProductos.SelectedTab = tabPageLista;
            }
            else
            {
                MessageBox.Show("Error al guardar el producto", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnModificar_Click(object sender, EventArgs e)
        {
            if (GridProductos.RowCount > 0 && GridProductos.SelectedRows.Count > 0) //rowcount filas
            {
                _currentProducto = (Producto)GridProductos.SelectedRows[0].DataBoundItem; //dataBoundItem trae el producto seleccionado
                TxtNombre.Text = _currentProducto.Nombre;
                TxtDescripcionPedido.Text = _currentProducto.Descripcion;
                TxtMedida.Text = _currentProducto.Medida;
                TxtStock.Text = _currentProducto.Stock.ToString();
                CmbCategoria.SelectedItem = _currentProducto.Categoria;
                TxtPrecio.Text = _currentProducto.Precio.ToString();


                tabControlProductos.SelectedTab = tabPageAgregarEditar;
            }
            else
            {
                MessageBox.Show("Debe seleccionar un producto para modificarlo", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtBuscar_TextChanged(object sender, EventArgs e)
        {
            btnBuscarProducto.Enabled = txtBuscar.Text.Length > 0;
        }

        private async void btnEliminar_Click(object sender, EventArgs e)
        {
            {
                //checheamos que haya productos seleccionados
                if (GridProductos.RowCount > 0 && GridProductos.SelectedRows.Count > 0)
                {
                    Producto entitySelected = (Producto)GridProductos.SelectedRows[0].DataBoundItem;
                    var respuesta = MessageBox.Show($"¿Seguro que desea eliminar este producto {entitySelected.Nombre}?", "Confirmar Eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (respuesta == DialogResult.Yes)
                    {
                        if (await _productoService.DeleteAsync(entitySelected.Id))
                        {
                            TimerStatusBar.Text = $"Producto {entitySelected.Nombre} eliminado correctamente";
                            timer2.Start(); // Iniciar el temporizador para mostrar el mensaje en la barra de estado
                            await GetAllData();
                        }
                        else
                        {
                            MessageBox.Show("Error al eliminar este producto", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Debe seleccionar un producto para eliminarlo", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            if (GridProductos.RowCount > 0 && GridProductos.SelectedRows.Count > 0)
            {
                Producto entitySelected = (Producto)GridProductos.SelectedRows[0].DataBoundItem;
                var respuesta = MessageBox.Show($"¿Seguro que desea restaurar este producto {entitySelected.Nombre}?", "Confirmar Restauración", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (respuesta == DialogResult.Yes)//en lo posible poner dentro de un try/catch
                {
                    if (await _productoService.RestoreAsync(entitySelected.Id))
                    {
                        TimerStatusBar.Text = $"Producto {entitySelected.Nombre} restaurado correctamente";
                        timer2.Start(); // Iniciar el temporizador para mostrar el mensaje en la barra de estado
                        await GetAllData();
                    }
                    else
                    {
                        MessageBox.Show("Error al restaurar este producto", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Debe seleccionar un producto para restaurarlo", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnBuscarProducto_Click(object sender, EventArgs e)
        {
            GridProductos.DataSource = await _productoService.GetAllAsync(txtBuscar.Text.Trim());
        }

        private void btnImprimirProductos_Click(object sender, EventArgs e)
        {
            if (_productos == null || !_productos.Any())
            {
                MessageBox.Show(
                    "Por favor, espera a que los productos se carguen.", 
                    "Información", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var productosEnStock = _productos
                .Where(p => p.Stock > 0)
                .OrderByDescending(p => p.Stock)
                .ToList();
            if (!productosEnStock.Any())
            {
                MessageBox.Show(
                    "No hay productos en stock para imprimir.", 
                    "Información", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var reporteProductos = new ViewReports.ProductosView(productosEnStock);
            reporteProductos.MdiParent = this.MdiParent; //hace referencia a la ventana principal
            reporteProductos.Show();

        }
    }
}
