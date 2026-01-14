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
            label2 = new Label();
            tabControlClientes = new TabControl();
            tabPageLista = new TabPage();
            btnImprimir = new Button();
            btnRestore = new Button();
            checkVerEliminados = new CheckBox();
            btnSalir = new Button();
            btnEliminar = new Button();
            btnModificar = new Button();
            btnAgregar = new Button();
            txtBuscar = new TextBox();
            btnBuscar = new Button();
            label1 = new Label();
            GridMayorista = new DataGridView();
            tabPageAgregarEditar = new TabPage();
            TxtPrecio = new TextBox();
            label8 = new Label();
            TxtStock = new TextBox();
            label7 = new Label();
            label6 = new Label();
            TxtMedida = new TextBox();
            TxtDescripcionPedido = new TextBox();
            TxtNombre = new TextBox();
            btnCancelar = new Button();
            btnGuardar = new Button();
            label5 = new Label();
            label4 = new Label();
            label3 = new Label();
            TxtUrl = new TextBox();
            label9 = new Label();
            PicUrl = new PictureBox();
            CmbCategoria = new ComboBox();
            pictureBox1 = new PictureBox();
            tabControlClientes.SuspendLayout();
            tabPageLista.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)GridMayorista).BeginInit();
            tabPageAgregarEditar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)PicUrl).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
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
            // tabControlClientes
            // 
            tabControlClientes.Controls.Add(tabPageLista);
            tabControlClientes.Controls.Add(tabPageAgregarEditar);
            tabControlClientes.Location = new Point(8, 39);
            tabControlClientes.Name = "tabControlClientes";
            tabControlClientes.SelectedIndex = 0;
            tabControlClientes.Size = new Size(822, 441);
            tabControlClientes.TabIndex = 4;
            // 
            // tabPageLista
            // 
            tabPageLista.Controls.Add(pictureBox1);
            tabPageLista.Controls.Add(btnImprimir);
            tabPageLista.Controls.Add(btnRestore);
            tabPageLista.Controls.Add(checkVerEliminados);
            tabPageLista.Controls.Add(btnSalir);
            tabPageLista.Controls.Add(btnEliminar);
            tabPageLista.Controls.Add(btnModificar);
            tabPageLista.Controls.Add(btnAgregar);
            tabPageLista.Controls.Add(txtBuscar);
            tabPageLista.Controls.Add(btnBuscar);
            tabPageLista.Controls.Add(label1);
            tabPageLista.Controls.Add(GridMayorista);
            tabPageLista.Location = new Point(4, 24);
            tabPageLista.Name = "tabPageLista";
            tabPageLista.Padding = new Padding(3);
            tabPageLista.Size = new Size(814, 413);
            tabPageLista.TabIndex = 0;
            tabPageLista.Text = "Lista";
            tabPageLista.UseVisualStyleBackColor = true;
            // 
            // btnImprimir
            // 
            btnImprimir.Location = new Point(514, 384);
            btnImprimir.Name = "btnImprimir";
            btnImprimir.Size = new Size(112, 23);
            btnImprimir.TabIndex = 18;
            btnImprimir.Text = "Imprimir informe";
            btnImprimir.UseVisualStyleBackColor = true;
            // 
            // btnRestore
            // 
            btnRestore.Location = new Point(296, 384);
            btnRestore.Name = "btnRestore";
            btnRestore.Size = new Size(75, 23);
            btnRestore.TabIndex = 17;
            btnRestore.Text = "Restaurar";
            btnRestore.UseVisualStyleBackColor = true;
            // 
            // checkVerEliminados
            // 
            checkVerEliminados.AutoSize = true;
            checkVerEliminados.Location = new Point(666, 17);
            checkVerEliminados.Name = "checkVerEliminados";
            checkVerEliminados.Size = new Size(103, 19);
            checkVerEliminados.TabIndex = 16;
            checkVerEliminados.Text = "Ver eliminados";
            checkVerEliminados.UseVisualStyleBackColor = true;
            // 
            // btnSalir
            // 
            btnSalir.Location = new Point(733, 384);
            btnSalir.Name = "btnSalir";
            btnSalir.Size = new Size(75, 23);
            btnSalir.TabIndex = 15;
            btnSalir.Text = "Salir";
            btnSalir.UseVisualStyleBackColor = true;
            // 
            // btnEliminar
            // 
            btnEliminar.Location = new Point(199, 384);
            btnEliminar.Name = "btnEliminar";
            btnEliminar.Size = new Size(75, 23);
            btnEliminar.TabIndex = 14;
            btnEliminar.Text = "Eliminar";
            btnEliminar.UseVisualStyleBackColor = true;
            // 
            // btnModificar
            // 
            btnModificar.Location = new Point(104, 384);
            btnModificar.Name = "btnModificar";
            btnModificar.Size = new Size(75, 23);
            btnModificar.TabIndex = 13;
            btnModificar.Text = "Modificar";
            btnModificar.UseVisualStyleBackColor = true;
            // 
            // btnAgregar
            // 
            btnAgregar.Location = new Point(10, 384);
            btnAgregar.Name = "btnAgregar";
            btnAgregar.Size = new Size(75, 23);
            btnAgregar.TabIndex = 12;
            btnAgregar.Text = "Agregar";
            btnAgregar.UseVisualStyleBackColor = true;
            // 
            // txtBuscar
            // 
            txtBuscar.Location = new Point(91, 15);
            txtBuscar.Name = "txtBuscar";
            txtBuscar.Size = new Size(535, 23);
            txtBuscar.TabIndex = 11;
            // 
            // btnBuscar
            // 
            btnBuscar.Location = new Point(10, 15);
            btnBuscar.Name = "btnBuscar";
            btnBuscar.Size = new Size(75, 23);
            btnBuscar.TabIndex = 10;
            btnBuscar.Text = "Buscar";
            btnBuscar.UseVisualStyleBackColor = true;
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
            // GridMayorista
            // 
            GridMayorista.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            GridMayorista.Location = new Point(10, 44);
            GridMayorista.Name = "GridMayorista";
            GridMayorista.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            GridMayorista.Size = new Size(616, 334);
            GridMayorista.TabIndex = 8;
            // 
            // tabPageAgregarEditar
            // 
            tabPageAgregarEditar.Controls.Add(CmbCategoria);
            tabPageAgregarEditar.Controls.Add(PicUrl);
            tabPageAgregarEditar.Controls.Add(TxtUrl);
            tabPageAgregarEditar.Controls.Add(label9);
            tabPageAgregarEditar.Controls.Add(TxtPrecio);
            tabPageAgregarEditar.Controls.Add(label8);
            tabPageAgregarEditar.Controls.Add(TxtStock);
            tabPageAgregarEditar.Controls.Add(label7);
            tabPageAgregarEditar.Controls.Add(label6);
            tabPageAgregarEditar.Controls.Add(TxtMedida);
            tabPageAgregarEditar.Controls.Add(TxtDescripcionPedido);
            tabPageAgregarEditar.Controls.Add(TxtNombre);
            tabPageAgregarEditar.Controls.Add(btnCancelar);
            tabPageAgregarEditar.Controls.Add(btnGuardar);
            tabPageAgregarEditar.Controls.Add(label5);
            tabPageAgregarEditar.Controls.Add(label4);
            tabPageAgregarEditar.Controls.Add(label3);
            tabPageAgregarEditar.Location = new Point(4, 24);
            tabPageAgregarEditar.Name = "tabPageAgregarEditar";
            tabPageAgregarEditar.Padding = new Padding(3);
            tabPageAgregarEditar.Size = new Size(626, 413);
            tabPageAgregarEditar.TabIndex = 1;
            tabPageAgregarEditar.Text = "Agregar/Editar";
            tabPageAgregarEditar.UseVisualStyleBackColor = true;
            // 
            // TxtPrecio
            // 
            TxtPrecio.Location = new Point(142, 220);
            TxtPrecio.Name = "TxtPrecio";
            TxtPrecio.Size = new Size(207, 23);
            TxtPrecio.TabIndex = 13;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(57, 228);
            label8.Name = "label8";
            label8.Size = new Size(43, 15);
            label8.TabIndex = 12;
            label8.Text = "Precio:";
            // 
            // TxtStock
            // 
            TxtStock.Location = new Point(142, 162);
            TxtStock.Name = "TxtStock";
            TxtStock.Size = new Size(207, 23);
            TxtStock.TabIndex = 11;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(57, 170);
            label7.Name = "label7";
            label7.Size = new Size(39, 15);
            label7.TabIndex = 10;
            label7.Text = "Stock:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(57, 199);
            label6.Name = "label6";
            label6.Size = new Size(61, 15);
            label6.TabIndex = 8;
            label6.Text = "Categoría:";
            // 
            // TxtMedida
            // 
            TxtMedida.Location = new Point(142, 133);
            TxtMedida.Name = "TxtMedida";
            TxtMedida.Size = new Size(207, 23);
            TxtMedida.TabIndex = 7;
            // 
            // TxtDescripcionPedido
            // 
            TxtDescripcionPedido.Location = new Point(142, 104);
            TxtDescripcionPedido.Name = "TxtDescripcionPedido";
            TxtDescripcionPedido.Size = new Size(207, 23);
            TxtDescripcionPedido.TabIndex = 6;
            // 
            // TxtNombre
            // 
            TxtNombre.Location = new Point(142, 75);
            TxtNombre.Name = "TxtNombre";
            TxtNombre.Size = new Size(207, 23);
            TxtNombre.TabIndex = 5;
            // 
            // btnCancelar
            // 
            btnCancelar.Location = new Point(379, 350);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(75, 23);
            btnCancelar.TabIndex = 4;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            // 
            // btnGuardar
            // 
            btnGuardar.Location = new Point(223, 350);
            btnGuardar.Name = "btnGuardar";
            btnGuardar.Size = new Size(75, 23);
            btnGuardar.TabIndex = 3;
            btnGuardar.Text = "Guardar";
            btnGuardar.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(57, 141);
            label5.Name = "label5";
            label5.Size = new Size(50, 15);
            label5.TabIndex = 2;
            label5.Text = "Medida:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(57, 112);
            label4.Name = "label4";
            label4.Size = new Size(72, 15);
            label4.TabIndex = 1;
            label4.Text = "Descripción:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(57, 86);
            label3.Name = "label3";
            label3.Size = new Size(54, 15);
            label3.TabIndex = 0;
            label3.Text = "Nombre:";
            // 
            // TxtUrl
            // 
            TxtUrl.Location = new Point(142, 249);
            TxtUrl.Name = "TxtUrl";
            TxtUrl.Size = new Size(207, 23);
            TxtUrl.TabIndex = 15;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(57, 257);
            label9.Name = "label9";
            label9.Size = new Size(31, 15);
            label9.TabIndex = 14;
            label9.Text = "URL:";
            // 
            // PicUrl
            // 
            PicUrl.Location = new Point(402, 86);
            PicUrl.Name = "PicUrl";
            PicUrl.Size = new Size(176, 168);
            PicUrl.TabIndex = 16;
            PicUrl.TabStop = false;
            // 
            // CmbCategoria
            // 
            CmbCategoria.FormattingEnabled = true;
            CmbCategoria.Location = new Point(142, 191);
            CmbCategoria.Name = "CmbCategoria";
            CmbCategoria.Size = new Size(207, 23);
            CmbCategoria.TabIndex = 17;
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new Point(632, 120);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(176, 168);
            pictureBox1.TabIndex = 19;
            pictureBox1.TabStop = false;
            // 
            // ProductosView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(842, 492);
            Controls.Add(label2);
            Controls.Add(tabControlClientes);
            Name = "ProductosView";
            Text = "ProductosView";
            tabControlClientes.ResumeLayout(false);
            tabPageLista.ResumeLayout(false);
            tabPageLista.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)GridMayorista).EndInit();
            tabPageAgregarEditar.ResumeLayout(false);
            tabPageAgregarEditar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)PicUrl).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label2;
        private TabControl tabControlClientes;
        private TabPage tabPageLista;
        private Button btnImprimir;
        private Button btnRestore;
        private CheckBox checkVerEliminados;
        private Button btnSalir;
        private Button btnEliminar;
        private Button btnModificar;
        private Button btnAgregar;
        private TextBox txtBuscar;
        private Button btnBuscar;
        private Label label1;
        private DataGridView GridMayorista;
        private TabPage tabPageAgregarEditar;
        private TextBox TxtPrecio;
        private Label label8;
        private TextBox TxtStock;
        private Label label7;
        private Label label6;
        private TextBox TxtMedida;
        private TextBox TxtDescripcionPedido;
        private TextBox TxtNombre;
        private Button btnCancelar;
        private Button btnGuardar;
        private Label label5;
        private Label label4;
        private Label label3;
        private PictureBox PicUrl;
        private TextBox TxtUrl;
        private Label label9;
        private ComboBox CmbCategoria;
        private PictureBox pictureBox1;
    }
}