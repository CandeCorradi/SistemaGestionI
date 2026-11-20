using Desktop.Views;
using Desktop.Views.Catalogo;
using Microsoft.Extensions.Caching.Memory;

namespace Desktop
{
    public partial class MenuPrincipalView : Form
    {
        IMemoryCache _memoryCache;
        public MenuPrincipalView(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
            InitializeComponent();
        }
       

        private void subMenuClientes_Click(object sender, EventArgs e)
        {
            var clientesView = new ClientesView(_memoryCache);
            clientesView.MdiParent = this;
            clientesView.Show();
        }

        private void subMenuMayoristas_Click(object sender, EventArgs e)
        {
            var mayoristasView = new Mayoristas(_memoryCache);
            mayoristasView.MdiParent = this;
            mayoristasView.Show();
        }

        private void SubMenuProductos_Click(object sender, EventArgs e)
        {
            var productosView = new ProductosView(_memoryCache);
            productosView.MdiParent = this;
            productosView.Show();
        }


        private void SubMenuUsuarios_Click(object sender, EventArgs e)
        {
            var usuariosView = new UsuariosView(_memoryCache);
            usuariosView.MdiParent = this;
            usuariosView.Show();
        }

        private void SubMenuSalirDelSistema_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
