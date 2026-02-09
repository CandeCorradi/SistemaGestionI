namespace Desktop.Views
{
    partial class UsuariosView
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
            panel1 = new Panel();
            label2 = new Label();
            tabControlUsuarios = new TabControl();
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
            GridUsuario = new DataGridView();
            tabPageAgregarEditar = new TabPage();
            label9 = new Label();
            label7 = new Label();
            TxtPassword2 = new TextBox();
            LabelPassword2 = new Label();
            TxtPassword = new TextBox();
            LabelPassword = new Label();
            TxtEmail = new TextBox();
            label5 = new Label();
            ComboTiposDeUsuarios = new ComboBox();
            label8 = new Label();
            TxtDni = new TextBox();
            label6 = new Label();
            TxtApellido = new TextBox();
            label4 = new Label();
            TxtNombre = new TextBox();
            label3 = new Label();
            btnCancelar = new Button();
            btnGuardar = new Button();
            StatusBar = new StatusStrip();
            LabelStatusMessage = new ToolStripStatusLabel();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            TimerStatusBar = new System.Windows.Forms.Timer(components);
            panel1.SuspendLayout();
            tabControlUsuarios.SuspendLayout();
            tabPageLista.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)GridUsuario).BeginInit();
            tabPageAgregarEditar.SuspendLayout();
            StatusBar.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BackColor = Color.DarkBlue;
            panel1.Controls.Add(label2);
            panel1.Location = new Point(1, 1);
            panel1.Name = "panel1";
            panel1.Size = new Size(711, 31);
            panel1.TabIndex = 8;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Times New Roman", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.White;
            label2.Location = new Point(248, 2);
            label2.Name = "label2";
            label2.Size = new Size(216, 26);
            label2.TabIndex = 5;
            label2.Text = "✦ Tipo de usuario ✦";
            // 
            // tabControlUsuarios
            // 
            tabControlUsuarios.Controls.Add(tabPageLista);
            tabControlUsuarios.Controls.Add(tabPageAgregarEditar);
            tabControlUsuarios.Location = new Point(1, 37);
            tabControlUsuarios.Name = "tabControlUsuarios";
            tabControlUsuarios.SelectedIndex = 0;
            tabControlUsuarios.Size = new Size(711, 426);
            tabControlUsuarios.TabIndex = 9;
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
            tabPageLista.Controls.Add(GridUsuario);
            tabPageLista.Location = new Point(4, 24);
            tabPageLista.Name = "tabPageLista";
            tabPageLista.Padding = new Padding(3);
            tabPageLista.Size = new Size(703, 398);
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
            checkVerEliminados.Location = new Point(592, 19);
            checkVerEliminados.Name = "checkVerEliminados";
            checkVerEliminados.Size = new Size(103, 19);
            checkVerEliminados.TabIndex = 16;
            checkVerEliminados.Text = "Ver eliminados";
            checkVerEliminados.UseVisualStyleBackColor = true;
            checkVerEliminados.CheckedChanged += checkVerEliminados_CheckedChanged;
            // 
            // btnSalir
            // 
            btnSalir.Location = new Point(622, 369);
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
            txtBuscar.Size = new Size(495, 23);
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
            label1.Location = new Point(129, -29);
            label1.Name = "label1";
            label1.Size = new Size(209, 26);
            label1.TabIndex = 9;
            label1.Text = "Agenda de Clientes";
            // 
            // GridUsuario
            // 
            GridUsuario.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            GridUsuario.Location = new Point(10, 44);
            GridUsuario.Name = "GridUsuario";
            GridUsuario.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            GridUsuario.Size = new Size(687, 319);
            GridUsuario.TabIndex = 8;
            GridUsuario.CellContentClick += GridUsuario_CellContentClick;
            // 
            // tabPageAgregarEditar
            // 
            tabPageAgregarEditar.Controls.Add(label9);
            tabPageAgregarEditar.Controls.Add(label7);
            tabPageAgregarEditar.Controls.Add(TxtPassword2);
            tabPageAgregarEditar.Controls.Add(LabelPassword2);
            tabPageAgregarEditar.Controls.Add(TxtPassword);
            tabPageAgregarEditar.Controls.Add(LabelPassword);
            tabPageAgregarEditar.Controls.Add(TxtEmail);
            tabPageAgregarEditar.Controls.Add(label5);
            tabPageAgregarEditar.Controls.Add(ComboTiposDeUsuarios);
            tabPageAgregarEditar.Controls.Add(label8);
            tabPageAgregarEditar.Controls.Add(TxtDni);
            tabPageAgregarEditar.Controls.Add(label6);
            tabPageAgregarEditar.Controls.Add(TxtApellido);
            tabPageAgregarEditar.Controls.Add(label4);
            tabPageAgregarEditar.Controls.Add(TxtNombre);
            tabPageAgregarEditar.Controls.Add(label3);
            tabPageAgregarEditar.Controls.Add(btnCancelar);
            tabPageAgregarEditar.Controls.Add(btnGuardar);
            tabPageAgregarEditar.Location = new Point(4, 24);
            tabPageAgregarEditar.Name = "tabPageAgregarEditar";
            tabPageAgregarEditar.Padding = new Padding(3);
            tabPageAgregarEditar.Size = new Size(703, 398);
            tabPageAgregarEditar.TabIndex = 1;
            tabPageAgregarEditar.Text = "Agregar/Editar";
            tabPageAgregarEditar.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(71, 223);
            label9.Margin = new Padding(2, 0, 2, 0);
            label9.Name = "label9";
            label9.Size = new Size(108, 15);
            label9.TabIndex = 52;
            label9.Text = "Repetir contraseña:";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(109, 183);
            label7.Margin = new Padding(2, 0, 2, 0);
            label7.Name = "label7";
            label7.Size = new Size(70, 15);
            label7.TabIndex = 51;
            label7.Text = "Contraseña:";
            // 
            // TxtPassword2
            // 
            TxtPassword2.Location = new Point(197, 220);
            TxtPassword2.Margin = new Padding(2);
            TxtPassword2.Name = "TxtPassword2";
            TxtPassword2.PasswordChar = '*';
            TxtPassword2.PlaceholderText = "Mínimo 6 caracteres";
            TxtPassword2.Size = new Size(382, 23);
            TxtPassword2.TabIndex = 49;
            TxtPassword2.UseSystemPasswordChar = true;
            // 
            // LabelPassword2
            // 
            LabelPassword2.Location = new Point(-47, 210);
            LabelPassword2.Margin = new Padding(2, 0, 2, 0);
            LabelPassword2.Name = "LabelPassword2";
            LabelPassword2.Size = new Size(10, 17);
            LabelPassword2.TabIndex = 50;
            LabelPassword2.Text = "Repetir contraseña:";
            LabelPassword2.TextAlign = ContentAlignment.TopRight;
            // 
            // TxtPassword
            // 
            TxtPassword.Location = new Point(197, 180);
            TxtPassword.Margin = new Padding(2);
            TxtPassword.Name = "TxtPassword";
            TxtPassword.PasswordChar = '*';
            TxtPassword.PlaceholderText = "Mínimo 6 caracteres";
            TxtPassword.Size = new Size(382, 23);
            TxtPassword.TabIndex = 47;
            TxtPassword.UseSystemPasswordChar = true;
            // 
            // LabelPassword
            // 
            LabelPassword.Location = new Point(-47, 168);
            LabelPassword.Margin = new Padding(2, 0, 2, 0);
            LabelPassword.Name = "LabelPassword";
            LabelPassword.Size = new Size(10, 17);
            LabelPassword.TabIndex = 48;
            LabelPassword.Text = "Contraseña:";
            LabelPassword.TextAlign = ContentAlignment.TopRight;
            // 
            // TxtEmail
            // 
            TxtEmail.Location = new Point(197, 141);
            TxtEmail.Margin = new Padding(2);
            TxtEmail.Name = "TxtEmail";
            TxtEmail.Size = new Size(382, 23);
            TxtEmail.TabIndex = 45;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(140, 142);
            label5.Margin = new Padding(2, 0, 2, 0);
            label5.Name = "label5";
            label5.Size = new Size(39, 15);
            label5.TabIndex = 46;
            label5.Text = "Email:";
            // 
            // ComboTiposDeUsuarios
            // 
            ComboTiposDeUsuarios.FormattingEnabled = true;
            ComboTiposDeUsuarios.Location = new Point(197, 264);
            ComboTiposDeUsuarios.Margin = new Padding(2);
            ComboTiposDeUsuarios.Name = "ComboTiposDeUsuarios";
            ComboTiposDeUsuarios.Size = new Size(382, 23);
            ComboTiposDeUsuarios.TabIndex = 44;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(88, 264);
            label8.Margin = new Padding(2, 0, 2, 0);
            label8.Name = "label8";
            label8.Size = new Size(91, 15);
            label8.TabIndex = 43;
            label8.Text = "Tipo de usuario:";
            // 
            // TxtDni
            // 
            TxtDni.Location = new Point(197, 102);
            TxtDni.Margin = new Padding(2);
            TxtDni.Name = "TxtDni";
            TxtDni.Size = new Size(382, 23);
            TxtDni.TabIndex = 40;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(146, 102);
            label6.Margin = new Padding(2, 0, 2, 0);
            label6.Name = "label6";
            label6.Size = new Size(28, 15);
            label6.TabIndex = 42;
            label6.Text = "Dni:";
            // 
            // TxtApellido
            // 
            TxtApellido.Location = new Point(197, 68);
            TxtApellido.Margin = new Padding(2);
            TxtApellido.Name = "TxtApellido";
            TxtApellido.Size = new Size(382, 23);
            TxtApellido.TabIndex = 39;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(123, 70);
            label4.Margin = new Padding(2, 0, 2, 0);
            label4.Name = "label4";
            label4.Size = new Size(54, 15);
            label4.TabIndex = 41;
            label4.Text = "Apellido:";
            // 
            // TxtNombre
            // 
            TxtNombre.Location = new Point(197, 34);
            TxtNombre.Margin = new Padding(2);
            TxtNombre.Name = "TxtNombre";
            TxtNombre.Size = new Size(382, 23);
            TxtNombre.TabIndex = 38;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(123, 38);
            label3.Margin = new Padding(2, 0, 2, 0);
            label3.Name = "label3";
            label3.Size = new Size(54, 15);
            label3.TabIndex = 37;
            label3.Text = "Nombre:";
            // 
            // btnCancelar
            // 
            btnCancelar.Location = new Point(385, 342);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(75, 23);
            btnCancelar.TabIndex = 4;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
            // 
            // btnGuardar
            // 
            btnGuardar.Location = new Point(229, 342);
            btnGuardar.Name = "btnGuardar";
            btnGuardar.Size = new Size(75, 23);
            btnGuardar.TabIndex = 3;
            btnGuardar.Text = "Guardar";
            btnGuardar.UseVisualStyleBackColor = true;
            btnGuardar.Click += btnGuardar_Click;
            // 
            // StatusBar
            // 
            StatusBar.Items.AddRange(new ToolStripItem[] { LabelStatusMessage, toolStripStatusLabel1 });
            StatusBar.Location = new Point(0, 459);
            StatusBar.Name = "StatusBar";
            StatusBar.Size = new Size(712, 22);
            StatusBar.TabIndex = 2;
            StatusBar.Text = "statusStrip1";
            // 
            // LabelStatusMessage
            // 
            LabelStatusMessage.Name = "LabelStatusMessage";
            LabelStatusMessage.Size = new Size(0, 17);
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(0, 17);
            // 
            // TimerStatusBar
            // 
            TimerStatusBar.Tick += TimerStatusBar_Tick;
            // 
            // UsuariosView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(712, 481);
            Controls.Add(StatusBar);
            Controls.Add(tabControlUsuarios);
            Controls.Add(panel1);
            Name = "UsuariosView";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "UsuariosView";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            tabControlUsuarios.ResumeLayout(false);
            tabPageLista.ResumeLayout(false);
            tabPageLista.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)GridUsuario).EndInit();
            tabPageAgregarEditar.ResumeLayout(false);
            tabPageAgregarEditar.PerformLayout();
            StatusBar.ResumeLayout(false);
            StatusBar.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panel1;
        private Label label2;
        private TabControl tabControlUsuarios;
        private TabPage tabPageLista;
        private Button btnRestore;
        private CheckBox checkVerEliminados;
        private Button btnSalir;
        private Button btnEliminar;
        private Button btnModificar;
        private Button btnAgregar;
        private TextBox txtBuscar;
        private Button btnBuscar;
        private Label label1;
        private DataGridView GridUsuario;
        private TabPage tabPageAgregarEditar;
        private Button btnCancelar;
        private Button btnGuardar;
        private StatusStrip StatusBar;
        private ToolStripStatusLabel LabelStatusMessage;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private TextBox TxtPassword2;
        private Label LabelPassword2;
        private TextBox TxtPassword;
        private Label LabelPassword;
        private TextBox TxtEmail;
        private Label label5;
        private ComboBox ComboTiposDeUsuarios;
        private Label label8;
        private TextBox TxtDni;
        private Label label6;
        private TextBox TxtApellido;
        private Label label4;
        private TextBox TxtNombre;
        private Label label3;
        private Label label9;
        private Label label7;
        private System.Windows.Forms.Timer TimerStatusBar;
    }
}