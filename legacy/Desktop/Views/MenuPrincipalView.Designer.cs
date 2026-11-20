namespace Desktop
{
    partial class MenuPrincipalView
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            MenuPrincipal = new MenuStrip();
            SubMenuPrincipal = new FontAwesome.Sharp.IconMenuItem();
            subMenuClientes = new ToolStripMenuItem();
            subMenuMayoristas = new ToolStripMenuItem();
            SubMenuProductos = new ToolStripMenuItem();
            SubMenuUsuarios = new ToolStripMenuItem();
            detalleDePedidosToolStripMenuItem = new ToolStripMenuItem();
            detalleDePresupuestosToolStripMenuItem = new ToolStripMenuItem();
            pagosToolStripMenuItem = new ToolStripMenuItem();
            pedidosToolStripMenuItem = new ToolStripMenuItem();
            presupuestosToolStripMenuItem = new ToolStripMenuItem();
            presupuestoDeProductosToolStripMenuItem = new ToolStripMenuItem();
            SubMenuSalir = new ToolStripMenuItem();
            SubMenuSalirDelSistema = new ToolStripMenuItem();
            toolStrip = new ToolStrip();
            iconToolStripButton1 = new FontAwesome.Sharp.IconToolStripButton();
            MenuPrincipal.SuspendLayout();
            toolStrip.SuspendLayout();
            SuspendLayout();
            // 
            // MenuPrincipal
            // 
            MenuPrincipal.Items.AddRange(new ToolStripItem[] { SubMenuPrincipal, SubMenuSalir });
            MenuPrincipal.Location = new Point(0, 0);
            MenuPrincipal.Name = "MenuPrincipal";
            MenuPrincipal.Size = new Size(637, 25);
            MenuPrincipal.TabIndex = 1;
            MenuPrincipal.Text = "menuStrip1";
            // 
            // SubMenuPrincipal
            // 
            SubMenuPrincipal.BackColor = Color.White;
            SubMenuPrincipal.DropDownItems.AddRange(new ToolStripItem[] { subMenuClientes, subMenuMayoristas, SubMenuProductos, SubMenuUsuarios, detalleDePedidosToolStripMenuItem, detalleDePresupuestosToolStripMenuItem, pagosToolStripMenuItem, pedidosToolStripMenuItem, presupuestosToolStripMenuItem, presupuestoDeProductosToolStripMenuItem });
            SubMenuPrincipal.Font = new Font("Times New Roman", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            SubMenuPrincipal.IconChar = FontAwesome.Sharp.IconChar.House;
            SubMenuPrincipal.IconColor = Color.Black;
            SubMenuPrincipal.IconFont = FontAwesome.Sharp.IconFont.Auto;
            SubMenuPrincipal.Name = "SubMenuPrincipal";
            SubMenuPrincipal.Size = new Size(93, 21);
            SubMenuPrincipal.Text = "Principal";
            // 
            // subMenuClientes
            // 
            subMenuClientes.Name = "subMenuClientes";
            subMenuClientes.Size = new Size(247, 22);
            subMenuClientes.Text = "Clientes";
            subMenuClientes.Click += subMenuClientes_Click;
            // 
            // subMenuMayoristas
            // 
            subMenuMayoristas.Name = "subMenuMayoristas";
            subMenuMayoristas.Size = new Size(247, 22);
            subMenuMayoristas.Text = "Mayoristas";
            subMenuMayoristas.Click += subMenuMayoristas_Click;
            // 
            // SubMenuProductos
            // 
            SubMenuProductos.Name = "SubMenuProductos";
            SubMenuProductos.Size = new Size(247, 22);
            SubMenuProductos.Text = "Productos";
            SubMenuProductos.Click += SubMenuProductos_Click;
            // 
            // SubMenuUsuarios
            // 
            SubMenuUsuarios.Name = "SubMenuUsuarios";
            SubMenuUsuarios.Size = new Size(247, 22);
            SubMenuUsuarios.Text = "Usuarios";
            SubMenuUsuarios.Click += SubMenuUsuarios_Click;
            // 
            // detalleDePedidosToolStripMenuItem
            // 
            detalleDePedidosToolStripMenuItem.ForeColor = SystemColors.ControlDark;
            detalleDePedidosToolStripMenuItem.Name = "detalleDePedidosToolStripMenuItem";
            detalleDePedidosToolStripMenuItem.Size = new Size(247, 22);
            detalleDePedidosToolStripMenuItem.Text = "Detalle de pedidos";
            // 
            // detalleDePresupuestosToolStripMenuItem
            // 
            detalleDePresupuestosToolStripMenuItem.ForeColor = SystemColors.ControlDark;
            detalleDePresupuestosToolStripMenuItem.Name = "detalleDePresupuestosToolStripMenuItem";
            detalleDePresupuestosToolStripMenuItem.Size = new Size(247, 22);
            detalleDePresupuestosToolStripMenuItem.Text = "Detalle de presupuestos";
            // 
            // pagosToolStripMenuItem
            // 
            pagosToolStripMenuItem.ForeColor = SystemColors.ControlDark;
            pagosToolStripMenuItem.Name = "pagosToolStripMenuItem";
            pagosToolStripMenuItem.Size = new Size(247, 22);
            pagosToolStripMenuItem.Text = "Pagos";
            // 
            // pedidosToolStripMenuItem
            // 
            pedidosToolStripMenuItem.ForeColor = SystemColors.ControlDark;
            pedidosToolStripMenuItem.Name = "pedidosToolStripMenuItem";
            pedidosToolStripMenuItem.Size = new Size(247, 22);
            pedidosToolStripMenuItem.Text = "Pedidos";
            // 
            // presupuestosToolStripMenuItem
            // 
            presupuestosToolStripMenuItem.ForeColor = SystemColors.ControlDark;
            presupuestosToolStripMenuItem.Name = "presupuestosToolStripMenuItem";
            presupuestosToolStripMenuItem.Size = new Size(247, 22);
            presupuestosToolStripMenuItem.Text = "Presupuestos";
            // 
            // presupuestoDeProductosToolStripMenuItem
            // 
            presupuestoDeProductosToolStripMenuItem.ForeColor = SystemColors.ControlDark;
            presupuestoDeProductosToolStripMenuItem.Name = "presupuestoDeProductosToolStripMenuItem";
            presupuestoDeProductosToolStripMenuItem.Size = new Size(247, 22);
            presupuestoDeProductosToolStripMenuItem.Text = "Presupuesto de productos";
            // 
            // SubMenuSalir
            // 
            SubMenuSalir.BackColor = Color.White;
            SubMenuSalir.DropDownItems.AddRange(new ToolStripItem[] { SubMenuSalirDelSistema });
            SubMenuSalir.Font = new Font("Times New Roman", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            SubMenuSalir.Name = "SubMenuSalir";
            SubMenuSalir.Size = new Size(64, 21);
            SubMenuSalir.Text = "▶ Salir";
            // 
            // SubMenuSalirDelSistema
            // 
            SubMenuSalirDelSistema.Name = "SubMenuSalirDelSistema";
            SubMenuSalirDelSistema.Size = new Size(183, 22);
            SubMenuSalirDelSistema.Text = "Salir del sistema";
            SubMenuSalirDelSistema.Click += SubMenuSalirDelSistema_Click;
            // 
            // toolStrip
            // 
            toolStrip.Items.AddRange(new ToolStripItem[] { iconToolStripButton1 });
            toolStrip.Location = new Point(0, 25);
            toolStrip.Name = "toolStrip";
            toolStrip.Size = new Size(637, 25);
            toolStrip.TabIndex = 3;
            toolStrip.Text = "toolStrip1";
            // 
            // iconToolStripButton1
            // 
            iconToolStripButton1.DisplayStyle = ToolStripItemDisplayStyle.Image;
            iconToolStripButton1.IconChar = FontAwesome.Sharp.IconChar.None;
            iconToolStripButton1.IconColor = Color.Black;
            iconToolStripButton1.IconFont = FontAwesome.Sharp.IconFont.Auto;
            iconToolStripButton1.ImageTransparentColor = Color.Magenta;
            iconToolStripButton1.Name = "iconToolStripButton1";
            iconToolStripButton1.Size = new Size(23, 22);
            iconToolStripButton1.Text = "iconToolStripButton1";
            // 
            // MenuPrincipalView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(637, 388);
            Controls.Add(toolStrip);
            Controls.Add(MenuPrincipal);
            IsMdiContainer = true;
            MainMenuStrip = MenuPrincipal;
            Name = "MenuPrincipalView";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Sistema de Gestión";
            WindowState = FormWindowState.Maximized;
            MenuPrincipal.ResumeLayout(false);
            MenuPrincipal.PerformLayout();
            toolStrip.ResumeLayout(false);
            toolStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip MenuPrincipal;
        private FontAwesome.Sharp.IconMenuItem SubMenuPrincipal;
        private ToolStripMenuItem SubMenuSalir;
        private ToolStripMenuItem SubMenuSalirDelSistema;
        private ToolStrip toolStrip;
        private FontAwesome.Sharp.IconToolStripButton iconToolStripButton1;
        private ToolStripMenuItem subMenuClientes;
        private ToolStripMenuItem detalleDePedidosToolStripMenuItem;
        private ToolStripMenuItem detalleDePresupuestosToolStripMenuItem;
        private ToolStripMenuItem subMenuMayoristas;
        private ToolStripMenuItem pagosToolStripMenuItem;
        private ToolStripMenuItem pedidosToolStripMenuItem;
        private ToolStripMenuItem presupuestosToolStripMenuItem;
        private ToolStripMenuItem SubMenuProductos;
        private ToolStripMenuItem presupuestoDeProductosToolStripMenuItem;
        private ToolStripMenuItem SubMenuUsuarios;
    }
}
