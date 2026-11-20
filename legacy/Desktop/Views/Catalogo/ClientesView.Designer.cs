namespace Desktop.Views
{
    partial class ClientesView
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
            tabControlClientes = new TabControl();
            tabPageLista = new TabPage();
            btnRestore = new Button();
            checkVerEliminados = new CheckBox();
            btnSalir = new Button();
            btnEliminar = new Button();
            btnModificar = new Button();
            btnAgregar = new Button();
            txtBuscar = new TextBox();
            btnBuscar = new Button();
            label1 = new Label();
            GridClientes = new DataGridView();
            tabPageAgregarEditar = new TabPage();
            TxtDir = new TextBox();
            label6 = new Label();
            TxtTel = new TextBox();
            TxtApellido = new TextBox();
            TxtNombre = new TextBox();
            btnCancelar = new Button();
            btnGuardar = new Button();
            label5 = new Label();
            label4 = new Label();
            label3 = new Label();
            TimerStatusBar = new System.Windows.Forms.Timer(components);
            statusBar = new StatusStrip();
            LabelStatusMessage = new ToolStripStatusLabel();
            panel1 = new Panel();
            label7 = new Label();
            tabControlClientes.SuspendLayout();
            tabPageLista.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)GridClientes).BeginInit();
            tabPageAgregarEditar.SuspendLayout();
            statusBar.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // tabControlClientes
            // 
            tabControlClientes.Controls.Add(tabPageLista);
            tabControlClientes.Controls.Add(tabPageAgregarEditar);
            tabControlClientes.Location = new Point(12, 38);
            tabControlClientes.Name = "tabControlClientes";
            tabControlClientes.SelectedIndex = 0;
            tabControlClientes.Size = new Size(635, 426);
            tabControlClientes.TabIndex = 0;
            // 
            // tabPageLista
            // 
            tabPageLista.Controls.Add(btnRestore);
            tabPageLista.Controls.Add(checkVerEliminados);
            tabPageLista.Controls.Add(btnSalir);
            tabPageLista.Controls.Add(btnEliminar);
            tabPageLista.Controls.Add(btnModificar);
            tabPageLista.Controls.Add(btnAgregar);
            tabPageLista.Controls.Add(txtBuscar);
            tabPageLista.Controls.Add(btnBuscar);
            tabPageLista.Controls.Add(label1);
            tabPageLista.Controls.Add(GridClientes);
            tabPageLista.Location = new Point(4, 24);
            tabPageLista.Name = "tabPageLista";
            tabPageLista.Padding = new Padding(3);
            tabPageLista.Size = new Size(627, 398);
            tabPageLista.TabIndex = 0;
            tabPageLista.Text = "Lista";
            tabPageLista.UseVisualStyleBackColor = true;
            // 
            // btnRestore
            // 
            btnRestore.Location = new Point(296, 369);
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
            checkVerEliminados.Location = new Point(518, 18);
            checkVerEliminados.Name = "checkVerEliminados";
            checkVerEliminados.Size = new Size(103, 19);
            checkVerEliminados.TabIndex = 16;
            checkVerEliminados.Text = "Ver eliminados";
            checkVerEliminados.UseVisualStyleBackColor = true;
            checkVerEliminados.CheckedChanged += checkVerEliminados_CheckedChanged;
            // 
            // btnSalir
            // 
            btnSalir.Location = new Point(546, 369);
            btnSalir.Name = "btnSalir";
            btnSalir.Size = new Size(75, 23);
            btnSalir.TabIndex = 15;
            btnSalir.Text = "Salir";
            btnSalir.UseVisualStyleBackColor = true;
            btnSalir.Click += btnSalir_Click;
            // 
            // btnEliminar
            // 
            btnEliminar.Location = new Point(199, 369);
            btnEliminar.Name = "btnEliminar";
            btnEliminar.Size = new Size(75, 23);
            btnEliminar.TabIndex = 14;
            btnEliminar.Text = "Eliminar";
            btnEliminar.UseVisualStyleBackColor = true;
            btnEliminar.Click += btnEliminar_Click;
            // 
            // btnModificar
            // 
            btnModificar.Location = new Point(104, 369);
            btnModificar.Name = "btnModificar";
            btnModificar.Size = new Size(75, 23);
            btnModificar.TabIndex = 13;
            btnModificar.Text = "Modificar";
            btnModificar.UseVisualStyleBackColor = true;
            btnModificar.Click += btnModificar_Click;
            // 
            // btnAgregar
            // 
            btnAgregar.Location = new Point(10, 369);
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
            txtBuscar.Size = new Size(421, 23);
            txtBuscar.TabIndex = 11;
            txtBuscar.TextChanged += txtBuscar_TextChanged;
            // 
            // btnBuscar
            // 
            btnBuscar.Location = new Point(10, 15);
            btnBuscar.Name = "btnBuscar";
            btnBuscar.Size = new Size(75, 23);
            btnBuscar.TabIndex = 10;
            btnBuscar.Text = "Buscar";
            btnBuscar.UseVisualStyleBackColor = true;
            btnBuscar.Click += btnBuscar_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Times New Roman", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(126, -32);
            label1.Name = "label1";
            label1.Size = new Size(209, 26);
            label1.TabIndex = 9;
            label1.Text = "Agenda de Clientes";
            // 
            // GridClientes
            // 
            GridClientes.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            GridClientes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            GridClientes.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            GridClientes.Location = new Point(10, 44);
            GridClientes.Name = "GridClientes";
            GridClientes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            GridClientes.Size = new Size(611, 319);
            GridClientes.TabIndex = 8;
            GridClientes.CellContentClick += GridClientes_CellContentClick;
            // 
            // tabPageAgregarEditar
            // 
            tabPageAgregarEditar.Controls.Add(TxtDir);
            tabPageAgregarEditar.Controls.Add(label6);
            tabPageAgregarEditar.Controls.Add(TxtTel);
            tabPageAgregarEditar.Controls.Add(TxtApellido);
            tabPageAgregarEditar.Controls.Add(TxtNombre);
            tabPageAgregarEditar.Controls.Add(btnCancelar);
            tabPageAgregarEditar.Controls.Add(btnGuardar);
            tabPageAgregarEditar.Controls.Add(label5);
            tabPageAgregarEditar.Controls.Add(label4);
            tabPageAgregarEditar.Controls.Add(label3);
            tabPageAgregarEditar.Location = new Point(4, 24);
            tabPageAgregarEditar.Name = "tabPageAgregarEditar";
            tabPageAgregarEditar.Padding = new Padding(3);
            tabPageAgregarEditar.Size = new Size(627, 398);
            tabPageAgregarEditar.TabIndex = 1;
            tabPageAgregarEditar.Text = "Agregar/Editar";
            tabPageAgregarEditar.UseVisualStyleBackColor = true;
            // 
            // TxtDir
            // 
            TxtDir.Location = new Point(284, 186);
            TxtDir.Name = "TxtDir";
            TxtDir.Size = new Size(207, 23);
            TxtDir.TabIndex = 9;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(122, 186);
            label6.Name = "label6";
            label6.Size = new Size(60, 15);
            label6.TabIndex = 8;
            label6.Text = "Dirección:";
            // 
            // TxtTel
            // 
            TxtTel.Location = new Point(284, 142);
            TxtTel.Name = "TxtTel";
            TxtTel.Size = new Size(207, 23);
            TxtTel.TabIndex = 7;
            // 
            // TxtApellido
            // 
            TxtApellido.Location = new Point(284, 98);
            TxtApellido.Name = "TxtApellido";
            TxtApellido.Size = new Size(207, 23);
            TxtApellido.TabIndex = 6;
            // 
            // TxtNombre
            // 
            TxtNombre.Location = new Point(284, 52);
            TxtNombre.Name = "TxtNombre";
            TxtNombre.Size = new Size(207, 23);
            TxtNombre.TabIndex = 5;
            // 
            // btnCancelar
            // 
            btnCancelar.Location = new Point(346, 270);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(75, 23);
            btnCancelar.TabIndex = 4;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
            // 
            // btnGuardar
            // 
            btnGuardar.Location = new Point(190, 270);
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
            label5.Location = new Point(122, 142);
            label5.Name = "label5";
            label5.Size = new Size(55, 15);
            label5.TabIndex = 2;
            label5.Text = "Teléfono:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(122, 98);
            label4.Name = "label4";
            label4.Size = new Size(54, 15);
            label4.TabIndex = 1;
            label4.Text = "Apellido:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(122, 55);
            label3.Name = "label3";
            label3.Size = new Size(54, 15);
            label3.TabIndex = 0;
            label3.Text = "Nombre:";
            // 
            // statusBar
            // 
            statusBar.Items.AddRange(new ToolStripItem[] { LabelStatusMessage });
            statusBar.Location = new Point(0, 468);
            statusBar.Name = "statusBar";
            statusBar.Size = new Size(659, 22);
            statusBar.TabIndex = 2;
            statusBar.Text = "statusStrip1";
            // 
            // LabelStatusMessage
            // 
            LabelStatusMessage.Name = "LabelStatusMessage";
            LabelStatusMessage.Size = new Size(0, 17);
            // 
            // panel1
            // 
            panel1.BackColor = Color.DarkBlue;
            panel1.Controls.Add(label7);
            panel1.Location = new Point(-1, 1);
            panel1.Name = "panel1";
            panel1.Size = new Size(663, 31);
            panel1.TabIndex = 9;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Times New Roman", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label7.ForeColor = Color.White;
            label7.Location = new Point(203, 2);
            label7.Name = "label7";
            label7.Size = new Size(253, 26);
            label7.TabIndex = 5;
            label7.Text = "✦ Agenda de Clientes ✦";
            // 
            // ClientesView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(659, 490);
            Controls.Add(panel1);
            Controls.Add(statusBar);
            Controls.Add(tabControlClientes);
            Name = "ClientesView";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Agenda de Clientes";
            tabControlClientes.ResumeLayout(false);
            tabPageLista.ResumeLayout(false);
            tabPageLista.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)GridClientes).EndInit();
            tabPageAgregarEditar.ResumeLayout(false);
            tabPageAgregarEditar.PerformLayout();
            statusBar.ResumeLayout(false);
            statusBar.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TabControl tabControlClientes;
        private TabPage tabPageLista;
        private Button btnSalir;
        private Button btnEliminar;
        private Button btnModificar;
        private Button btnAgregar;
        private TextBox txtBuscar;
        private Button btnBuscar;
        private Label label1;
        private DataGridView GridClientes;
        private TabPage tabPageAgregarEditar;
        private TextBox TxtTel;
        private TextBox TxtApellido;
        private TextBox TxtNombre;
        private Button btnCancelar;
        private Button btnGuardar;
        private Label label5;
        private Label label4;
        private Label label3;
        private TextBox TxtDir;
        private Label label6;
        private Button btnRestore;
        private CheckBox checkVerEliminados;
        private System.Windows.Forms.Timer TimerStatusBar;
        private StatusStrip statusBar;
        private ToolStripStatusLabel LabelStatusMessage;
        private Panel panel1;
        private Label label7;
    }
}