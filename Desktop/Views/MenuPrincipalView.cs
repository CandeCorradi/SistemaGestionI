using Desktop.Views;
using Desktop.Views.Catalogo;

namespace Desktop
{
    public partial class MenuPrincipalView : Form
    {
        public MenuPrincipalView()
        {
            InitializeComponent();
        }

        private void SubMenuSalir_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void subMenuClientes_Click(object sender, EventArgs e)
        {
            var clientesView = new ClientesView();
            clientesView.MdiParent = this;
            clientesView.Show();
        }

        private void subMenuMayoristas_Click(object sender, EventArgs e)
        {
            var mayoristasView = new Mayoristas();
            mayoristasView.MdiParent = this;
            mayoristasView.Show();
        }

        private void SubMenuProductos_Click(object sender, EventArgs e)
        {
            var productosView = new ProductosView();
            productosView.MdiParent = this;
            productosView.Show();
        }
    }
}
