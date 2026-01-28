namespace Desktop.Views.Catalogo
{
    partial class ProductosView
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            label2 = new Label();
            tabControlProductos = new TabControl();
            tabPageLista = new TabPage();
            btnImprimirProductos = new Button();
            btnRestore = new Button();
            checkVerEliminados = new CheckBox();
            btnSalir = new Button();
            btnEliminar = new Button();
            btnModificar = new Button();
            btnAgregar = new Button();
            txtBuscar = new TextBox();
            btnBuscarProducto = new Button();
            label1 = new Label();
            GridProductos = new DataGridView();
            tabPageAgregarEditar = new TabPage();
            CmbCategoria = new ComboBox();
            TxtPrecio = new TextBox();
            label8 = new Label();
            TxtStock = new TextBox();
            label7 = new Label();
            label6 = new Label();
            TxtMedida = new TextBox();
            TxtDescripcionProducto = new TextBox();
            TxtNombre = new TextBox();
            btnCancelar = new Button();
            btnGuardar = new Button();
            label5 = new Label();
            label4 = new Label();
            label3 = new Label();
            statusStrip1 = new StatusStrip();
            TimerStatusBar = new ToolStripStatusLabel();
            timer2 = new System.Windows.Forms.Timer(components);
            tabControlProductos.SuspendLayout();
            tabPageLista.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)GridProductos).BeginInit();
            tabPageAgregarEditar.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Times New Roman", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.Location = new Point(8, 10);
            label2.Name = "label2";
            label2.Size = new Size(116, 26);
            label2.TabIndex = 5;
            label2.Text = "Productos";
            // 
            // tabControlProductos
            // 
            tabControlProductos.Controls.Add(tabPageLista);
            tabControlProductos.Controls.Add(tabPageAgregarEditar);
            tabControlProductos.Location = new Point(8, 39);
            tabControlProductos.Name = "tabControlProductos";
            tabControlProductos.SelectedIndex = 0;
            tabControlProductos.Size = new Size(646, 441);
            tabControlProductos.TabIndex = 4;
            // 
            // tabPageLista
            // 
            tabPageLista.Controls.Add(btnImprimirProductos);
            tabPageLista.Controls.Add(btnRestore);
            tabPageLista.Controls.Add(checkVerEliminados);
            tabPageLista.Controls.Add(btnSalir);
            tabPageLista.Controls.Add(btnEliminar);
            tabPageLista.Controls.Add(btnModificar);
            tabPageLista.Controls.Add(btnAgregar);
            tabPageLista.Controls.Add(txtBuscar);
            tabPageLista.Controls.Add(btnBuscarProducto);
            tabPageLista.Controls.Add(label1);
            tabPageLista.Controls.Add(GridProductos);
            tabPageLista.Location = new Point(4, 24);
            tabPageLista.Name = "tabPageLista";
            tabPageLista.Padding = new Padding(3);
            tabPageLista.Size = new Size(638, 413);
            tabPageLista.TabIndex = 0;
            tabPageLista.Text = "Lista";
            tabPageLista.UseVisualStyleBackColor = true;
            // 
            // btnImprimirProductos
            // 
            btnImprimirProductos.Location = new Point(391, 384);
            btnImprimirProductos.Name = "btnImprimirProductos";
            btnImprimirProductos.Size = new Size(126, 23);
            btnImprimirProductos.TabIndex = 18;
            btnImprimirProductos.Text = "Imprimir productos";
            btnImprimirProductos.UseVisualStyleBackColor = true;
            btnImprimirProductos.Click += btnImprimirProductos_Click;
            // 
            // btnRestore
            // 
            btnRestore.Location = new Point(296, 384);
            btnRestore.Name = "btnRestore";
            btnRestore.Size = new Size(75, 23);
            btnRestore.TabIndex = 17;
            btnRestore.Text = "Restaurar";
            btnRestore.UseVisualStyleBackColor = true;
            btnRestore.Click += btnRestore_Click;
            // 
            // checkVerEliminados
            // 
            checkVerEliminados.AutoSize = true;
            checkVerEliminados.Location = new Point(523, 17);
            checkVerEliminados.Name = "checkVerEliminados";
            checkVerEliminados.Size = new Size(103, 19);
            checkVerEliminados.TabIndex = 16;
            checkVerEliminados.Text = "Ver eliminados";
            checkVerEliminados.UseVisualStyleBackColor = true;
            checkVerEliminados.CheckedChanged += checkVerEliminados_CheckedChanged;
            // 
            // btnSalir
            // 
            btnSalir.Location = new Point(551, 384);
            btnSalir.Name = "btnSalir";
            btnSalir.Size = new Size(75, 23);
            btnSalir.TabIndex = 15;
            btnSalir.Text = "Salir";
            btnSalir.UseVisualStyleBackColor = true;
            btnSalir.Click += btnSalir_Click;
            // 
            // btnEliminar
            // 
            btnEliminar.Location = new Point(199, 384);
            btnEliminar.Name = "btnEliminar";
            btnEliminar.Size = new Size(75, 23);
            btnEliminar.TabIndex = 14;
            btnEliminar.Text = "Eliminar";
            btnEliminar.UseVisualStyleBackColor = true;
            btnEliminar.Click += btnEliminar_Click;
            // 
            // btnModificar
            // 
            btnModificar.Location = new Point(104, 384);
            btnModificar.Name = "btnModificar";
            btnModificar.Size = new Size(75, 23);
            btnModificar.TabIndex = 13;
            btnModificar.Text = "Modificar";
            btnModificar.UseVisualStyleBackColor = true;
            btnModificar.Click += btnModificar_Click;
            // 
            // btnAgregar
            // 
            btnAgregar.Location = new Point(10, 384);
            btnAgregar.Name = "btnAgregar";
            btnAgregar.Size = new Size(75, 23);
            btnAgregar.TabIndex = 12;
            btnAgregar.Text = "Agregar";
            btnAgregar.UseVisualStyleBackColor = true;
            btnAgregar.Click += btnAgregar_Click;
            // 
            // txtBuscar
            // 
            txtBuscar.Location = new Point(91, 15);
            txtBuscar.Name = "txtBuscar";
            txtBuscar.Size = new Size(426, 23);
            txtBuscar.TabIndex = 11;
            txtBuscar.TextChanged += txtBuscar_TextChanged;
            // 
            // btnBuscarProducto
            // 
            btnBuscarProducto.Location = new Point(10, 15);
            btnBuscarProducto.Name = "btnBuscarProducto";
            btnBuscarProducto.Size = new Size(75, 23);
            btnBuscarProducto.TabIndex = 10;
            btnBuscarProducto.Text = "Buscar";
            btnBuscarProducto.UseVisualStyleBackColor = true;
            btnBuscarProducto.Click += btnBuscarProducto_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Times New Roman", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(132, -26);
            label1.Name = "label1";
            label1.Size = new Size(209, 26);
            label1.TabIndex = 9;
            label1.Text = "Agenda de Clientes";
            // 
            // GridProductos
            // 
            GridProductos.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            GridProductos.Location = new Point(10, 44);
            GridProductos.Name = "GridProductos";
            GridProductos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            GridProductos.Size = new Size(616, 334);
            GridProductos.TabIndex = 8;
            GridProductos.CellContentClick += GridProductos_CellContentClick;
            // 
            // tabPageAgregarEditar
            // 
            tabPageAgregarEditar.Controls.Add(CmbCategoria);
            tabPageAgregarEditar.Controls.Add(TxtPrecio);
            tabPageAgregarEditar.Controls.Add(label8);
            tabPageAgregarEditar.Controls.Add(TxtStock);
            tabPageAgregarEditar.Controls.Add(label7);
            tabPageAgregarEditar.Controls.Add(label6);
            tabPageAgregarEditar.Controls.Add(TxtMedida);
            tabPageAgregarEditar.Controls.Add(TxtDescripcionProducto);
            tabPageAgregarEditar.Controls.Add(TxtNombre);
            tabPageAgregarEditar.Controls.Add(btnCancelar);
            tabPageAgregarEditar.Controls.Add(btnGuardar);
            tabPageAgregarEditar.Controls.Add(label5);
            tabPageAgregarEditar.Controls.Add(label4);
            tabPageAgregarEditar.Controls.Add(label3);
            tabPageAgregarEditar.Location = new Point(4, 24);
            tabPageAgregarEditar.Name = "tabPageAgregarEditar";
            tabPageAgregarEditar.Padding = new Padding(3);
            tabPageAgregarEditar.Size = new Size(638, 413);
            tabPageAgregarEditar.TabIndex = 1;
            tabPageAgregarEditar.Text = "Agregar/Editar";
            tabPageAgregarEditar.UseVisualStyleBackColor = true;
            // 
            // CmbCategoria
            // 
            CmbCategoria.FormattingEnabled = true;
            CmbCategoria.Items.AddRange(new object[] { "Melamina", "Accesorio" });
            CmbCategoria.Location = new Point(261, 53);
            CmbCategoria.Name = "CmbCategoria";
            CmbCategoria.Size = new Size(207, 23);
            CmbCategoria.TabIndex = 17;
            CmbCategoria.SelectedIndexChanged += CmbCategoria_SelectedIndexChanged;
            // 
            // TxtPrecio
            // 
            TxtPrecio.Location = new Point(261, 198);
            TxtPrecio.Name = "TxtPrecio";
            TxtPrecio.Size = new Size(207, 23);
            TxtPrecio.TabIndex = 13;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(176, 206);
            label8.Name = "label8";
            label8.Size = new Size(43, 15);
            label8.TabIndex = 12;
            label8.Text = "Precio:";
            // 
            // TxtStock
            // 
            TxtStock.Location = new Point(261, 169);
            TxtStock.Name = "TxtStock";
            TxtStock.Size = new Size(207, 23);
            TxtStock.TabIndex = 11;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(176, 177);
            label7.Name = "label7";
            label7.Size = new Size(39, 15);
            label7.TabIndex = 10;
            label7.Text = "Stock:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(176, 61);
            label6.Name = "label6";
            label6.Size = new Size(61, 15);
            label6.TabIndex = 8;
            label6.Text = "Categoría:";
            // 
            // TxtMedida
            // 
            TxtMedida.Location = new Point(261, 140);
            TxtMedida.Name = "TxtMedida";
            TxtMedida.Size = new Size(207, 23);
            TxtMedida.TabIndex = 7;
            // 
            // TxtDescripcionProducto
            // 
            TxtDescripcionProducto.Location = new Point(261, 111);
            TxtDescripcionProducto.Name = "TxtDescripcionProducto";
            TxtDescripcionProducto.Size = new Size(207, 23);
            TxtDescripcionProducto.TabIndex = 6;
            // 
            // TxtNombre
            // 
            TxtNombre.Location = new Point(261, 82);
            TxtNombre.Name = "TxtNombre";
            TxtNombre.Size = new Size(207, 23);
            TxtNombre.TabIndex = 5;
            // 
            // btnCancelar
            // 
            btnCancelar.Location = new Point(393, 283);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(75, 23);
            btnCancelar.TabIndex = 4;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
            // 
            // btnGuardar
            // 
            btnGuardar.Location = new Point(176, 283);
            btnGuardar.Name = "btnGuardar";
            btnGuardar.Size = new Size(75, 23);
            btnGuardar.TabIndex = 3;
            btnGuardar.Text = "Guardar";
            btnGuardar.UseVisualStyleBackColor = true;
            btnGuardar.Click += btnGuardar_Click;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(176, 148);
            label5.Name = "label5";
            label5.Size = new Size(50, 15);
            label5.TabIndex = 2;
            label5.Text = "Medida:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(176, 119);
            label4.Name = "label4";
            label4.Size = new Size(72, 15);
            label4.TabIndex = 1;
            label4.Text = "Descripción:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(176, 93);
            label3.Name = "label3";
            label3.Size = new Size(54, 15);
            label3.TabIndex = 0;
            label3.Text = "Nombre:";
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { TimerStatusBar });
            statusStrip1.Location = new Point(0, 486);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(659, 22);
            statusStrip1.TabIndex = 6;
            statusStrip1.Text = "statusStrip1";
            // 
            // TimerStatusBar
            // 
            TimerStatusBar.Name = "TimerStatusBar";
            TimerStatusBar.Size = new Size(0, 17);
            // 
            // ProductosView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(659, 508);
            Controls.Add(statusStrip1);
            Controls.Add(label2);
            Controls.Add(tabControlProductos);
            Name = "ProductosView";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "ProductosView";
            tabControlProductos.ResumeLayout(false);
            tabPageLista.ResumeLayout(false);
            tabPageLista.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)GridProductos).EndInit();
            tabPageAgregarEditar.ResumeLayout(false);
            tabPageAgregarEditar.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label2;
        private TabControl tabControlProductos;
        private TabPage tabPageLista;
        private Button btnImprimirProductos;
        private Button btnRestore;
        private CheckBox checkVerEliminados;
        private Button btnSalir;
        private Button btnEliminar;
        private Button btnModificar;
        private Button btnAgregar;
        private TextBox txtBuscar;
        private Button btnBuscarProducto;
        private Label label1;
        private DataGridView GridProductos;
        private TabPage tabPageAgregarEditar;
        private TextBox TxtPrecio;
        private Label label8;
        private Label label7;
        private Label label6;
        private TextBox TxtMedida;
        private TextBox TxtDescripcionProducto;
        private TextBox TxtNombre;
        private Button btnCancelar;
        private Button btnGuardar;
        private Label label5;
        private Label label4;
        private Label label3;
        private ComboBox CmbCategoria;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel TimerStatusBar;
        private System.Windows.Forms.Timer timer2;
        private TextBox TxtStock;
    }
}