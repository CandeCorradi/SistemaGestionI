namespace Desktop.Views
{
    partial class Mayoristas
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
            tabControlMayoristas = new TabControl();
            tabPageLista = new TabPage();
            btnRestore = new Button();
            checkVerEliminados = new CheckBox();
            btnSalir = new Button();
            btnEliminar = new Button();
            btnModificar = new Button();
            btnAgregar = new Button();
            txtBuscar = new TextBox();
            btnBuscarMay = new Button();
            label1 = new Label();
            GridMayorista = new DataGridView();
            tabPageAgregarEditar = new TabPage();
            CmbProducto = new ComboBox();
            label8 = new Label();
            TxtEmail = new TextBox();
            label7 = new Label();
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
            statusBarMay = new StatusStrip();
            LabelStatusMessage = new ToolStripStatusLabel();
            timer1 = new System.Windows.Forms.Timer(components);
            panel1 = new Panel();
            label9 = new Label();
            tabControlMayoristas.SuspendLayout();
            tabPageLista.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)GridMayorista).BeginInit();
            tabPageAgregarEditar.SuspendLayout();
            statusBarMay.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // tabControlMayoristas
            // 
            tabControlMayoristas.Controls.Add(tabPageLista);
            tabControlMayoristas.Controls.Add(tabPageAgregarEditar);
            tabControlMayoristas.Location = new Point(17, 38);
            tabControlMayoristas.Name = "tabControlMayoristas";
            tabControlMayoristas.SelectedIndex = 0;
            tabControlMayoristas.Size = new Size(626, 434);
            tabControlMayoristas.TabIndex = 2;
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
            tabPageLista.Controls.Add(btnBuscarMay);
            tabPageLista.Controls.Add(label1);
            tabPageLista.Controls.Add(GridMayorista);
            tabPageLista.Location = new Point(4, 24);
            tabPageLista.Name = "tabPageLista";
            tabPageLista.Padding = new Padding(3);
            tabPageLista.Size = new Size(618, 406);
            tabPageLista.TabIndex = 0;
            tabPageLista.Text = "Lista";
            tabPageLista.UseVisualStyleBackColor = true;
            // 
            // btnRestore
            // 
            btnRestore.Location = new Point(296, 377);
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
            checkVerEliminados.Location = new Point(509, 17);
            checkVerEliminados.Name = "checkVerEliminados";
            checkVerEliminados.Size = new Size(103, 19);
            checkVerEliminados.TabIndex = 16;
            checkVerEliminados.Text = "Ver eliminados";
            checkVerEliminados.UseVisualStyleBackColor = true;
            checkVerEliminados.CheckedChanged += checkVerEliminados_CheckedChanged;
            // 
            // btnSalir
            // 
            btnSalir.Location = new Point(537, 377);
            btnSalir.Name = "btnSalir";
            btnSalir.Size = new Size(75, 23);
            btnSalir.TabIndex = 15;
            btnSalir.Text = "Salir";
            btnSalir.UseVisualStyleBackColor = true;
            btnSalir.Click += btnSalir_Click;
            // 
            // btnEliminar
            // 
            btnEliminar.Location = new Point(199, 377);
            btnEliminar.Name = "btnEliminar";
            btnEliminar.Size = new Size(75, 23);
            btnEliminar.TabIndex = 14;
            btnEliminar.Text = "Eliminar";
            btnEliminar.UseVisualStyleBackColor = true;
            btnEliminar.Click += btnEliminar_Click;
            // 
            // btnModificar
            // 
            btnModificar.Location = new Point(104, 377);
            btnModificar.Name = "btnModificar";
            btnModificar.Size = new Size(75, 23);
            btnModificar.TabIndex = 13;
            btnModificar.Text = "Modificar";
            btnModificar.UseVisualStyleBackColor = true;
            btnModificar.Click += btnModificar_Click;
            // 
            // btnAgregar
            // 
            btnAgregar.Location = new Point(10, 377);
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
            txtBuscar.Size = new Size(412, 23);
            txtBuscar.TabIndex = 11;
            txtBuscar.TextChanged += txtBuscar_TextChanged;
            // 
            // btnBuscarMay
            // 
            btnBuscarMay.Location = new Point(10, 15);
            btnBuscarMay.Name = "btnBuscarMay";
            btnBuscarMay.Size = new Size(75, 23);
            btnBuscarMay.TabIndex = 10;
            btnBuscarMay.Text = "Buscar";
            btnBuscarMay.UseVisualStyleBackColor = true;
            btnBuscarMay.Click += btnBuscarMay_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Times New Roman", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(129, -29);
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
            GridMayorista.Size = new Size(602, 327);
            GridMayorista.TabIndex = 8;
            GridMayorista.CellContentClick += GridMayorista_CellContentClick;
            // 
            // tabPageAgregarEditar
            // 
            tabPageAgregarEditar.Controls.Add(CmbProducto);
            tabPageAgregarEditar.Controls.Add(label8);
            tabPageAgregarEditar.Controls.Add(TxtEmail);
            tabPageAgregarEditar.Controls.Add(label7);
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
            tabPageAgregarEditar.Size = new Size(618, 406);
            tabPageAgregarEditar.TabIndex = 1;
            tabPageAgregarEditar.Text = "Agregar/Editar";
            tabPageAgregarEditar.UseVisualStyleBackColor = true;
            // 
            // CmbProducto
            // 
            CmbProducto.FormattingEnabled = true;
            CmbProducto.Items.AddRange(new object[] { "Melamina", "Accesorio" });
            CmbProducto.Location = new Point(215, 187);
            CmbProducto.Name = "CmbProducto";
            CmbProducto.Size = new Size(207, 23);
            CmbProducto.TabIndex = 13;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(56, 190);
            label8.Name = "label8";
            label8.Size = new Size(101, 15);
            label8.TabIndex = 12;
            label8.Text = "Tipo de producto:";
            // 
            // TxtEmail
            // 
            TxtEmail.Location = new Point(215, 129);
            TxtEmail.Name = "TxtEmail";
            TxtEmail.Size = new Size(207, 23);
            TxtEmail.TabIndex = 11;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(56, 132);
            label7.Name = "label7";
            label7.Size = new Size(39, 15);
            label7.TabIndex = 10;
            label7.Text = "Email:";
            // 
            // TxtDir
            // 
            TxtDir.Location = new Point(215, 158);
            TxtDir.Name = "TxtDir";
            TxtDir.Size = new Size(207, 23);
            TxtDir.TabIndex = 9;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(56, 161);
            label6.Name = "label6";
            label6.Size = new Size(60, 15);
            label6.TabIndex = 8;
            label6.Text = "Dirección:";
            // 
            // TxtTel
            // 
            TxtTel.Location = new Point(215, 100);
            TxtTel.Name = "TxtTel";
            TxtTel.Size = new Size(207, 23);
            TxtTel.TabIndex = 7;
            // 
            // TxtApellido
            // 
            TxtApellido.Location = new Point(215, 71);
            TxtApellido.Name = "TxtApellido";
            TxtApellido.Size = new Size(207, 23);
            TxtApellido.TabIndex = 6;
            // 
            // TxtNombre
            // 
            TxtNombre.Location = new Point(215, 42);
            TxtNombre.Name = "TxtNombre";
            TxtNombre.Size = new Size(207, 23);
            TxtNombre.TabIndex = 5;
            // 
            // btnCancelar
            // 
            btnCancelar.Location = new Point(270, 280);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(75, 23);
            btnCancelar.TabIndex = 4;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
            // 
            // btnGuardar
            // 
            btnGuardar.Location = new Point(114, 280);
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
            label5.Location = new Point(56, 103);
            label5.Name = "label5";
            label5.Size = new Size(55, 15);
            label5.TabIndex = 2;
            label5.Text = "Teléfono:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(56, 74);
            label4.Name = "label4";
            label4.Size = new Size(54, 15);
            label4.TabIndex = 1;
            label4.Text = "Apellido:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(56, 48);
            label3.Name = "label3";
            label3.Size = new Size(54, 15);
            label3.TabIndex = 0;
            label3.Text = "Nombre:";
            // 
            // statusBarMay
            // 
            statusBarMay.Items.AddRange(new ToolStripItem[] { LabelStatusMessage });
            statusBarMay.Location = new Point(0, 472);
            statusBarMay.Name = "statusBarMay";
            statusBarMay.Size = new Size(655, 22);
            statusBarMay.TabIndex = 4;
            // 
            // LabelStatusMessage
            // 
            LabelStatusMessage.Name = "LabelStatusMessage";
            LabelStatusMessage.Size = new Size(0, 17);
            // 
            // panel1
            // 
            panel1.BackColor = Color.DarkBlue;
            panel1.Controls.Add(label9);
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(663, 31);
            panel1.TabIndex = 8;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Font = new Font("Times New Roman", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label9.ForeColor = Color.White;
            label9.Location = new Point(183, 2);
            label9.Name = "label9";
            label9.Size = new Size(285, 26);
            label9.TabIndex = 5;
            label9.Text = "✦ Agenda de Mayoristas ✦";
            // 
            // Mayoristas
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(655, 494);
            Controls.Add(panel1);
            Controls.Add(statusBarMay);
            Controls.Add(tabControlMayoristas);
            Name = "Mayoristas";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Mayoristas";
            tabControlMayoristas.ResumeLayout(false);
            tabPageLista.ResumeLayout(false);
            tabPageLista.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)GridMayorista).EndInit();
            tabPageAgregarEditar.ResumeLayout(false);
            tabPageAgregarEditar.PerformLayout();
            statusBarMay.ResumeLayout(false);
            statusBarMay.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private TabControl tabControlMayoristas;
        private TabPage tabPageLista;
        private Button btnRestore;
        private CheckBox checkVerEliminados;
        private Button btnSalir;
        private Button btnEliminar;
        private Button btnModificar;
        private Button btnAgregar;
        private TextBox txtBuscar;
        private Button btnBuscarMay;
        private Label label1;
        private DataGridView GridMayorista;
        private TabPage tabPageAgregarEditar;
        private TextBox TxtDir;
        private Label label6;
        private TextBox TxtTel;
        private TextBox TxtApellido;
        private TextBox TxtNombre;
        private Button btnCancelar;
        private Button btnGuardar;
        private Label label5;
        private Label label4;
        private Label label3;
        private Label label8;
        private TextBox TxtEmail;
        private Label label7;
        private StatusStrip statusBarMay;
        private System.Windows.Forms.Timer timer1;
        private ToolStripStatusLabel LabelStatusMessage;
        private ComboBox CmbProducto;
        private Panel panel1;
        private Label label9;
    }
}