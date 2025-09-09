namespace Desktop.Views
{
    partial class LoginView
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
            pictureBox1 = new PictureBox();
            label1 = new Label();
            label2 = new Label();
            TxtEmail = new TextBox();
            TxtContrasena = new TextBox();
            CheckVerContrasena = new CheckBox();
            BtnCancelar = new Button();
            BtnLogin = new Button();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.freepik__background__54410;
            pictureBox1.Location = new Point(29, 12);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(188, 225);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(242, 48);
            label1.Name = "label1";
            label1.Size = new Size(39, 15);
            label1.TabIndex = 1;
            label1.Text = "Email:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(242, 90);
            label2.Name = "label2";
            label2.Size = new Size(70, 15);
            label2.TabIndex = 2;
            label2.Text = "Contraseña:";
            // 
            // TxtEmail
            // 
            TxtEmail.Location = new Point(327, 48);
            TxtEmail.Name = "TxtEmail";
            TxtEmail.Size = new Size(241, 23);
            TxtEmail.TabIndex = 3;
            // 
            // TxtContrasena
            // 
            TxtContrasena.Location = new Point(327, 87);
            TxtContrasena.Name = "TxtContrasena";
            TxtContrasena.PasswordChar = '*';
            TxtContrasena.Size = new Size(241, 23);
            TxtContrasena.TabIndex = 4;
            // 
            // CheckVerContrasena
            // 
            CheckVerContrasena.AutoSize = true;
            CheckVerContrasena.Location = new Point(327, 136);
            CheckVerContrasena.Name = "CheckVerContrasena";
            CheckVerContrasena.Size = new Size(103, 19);
            CheckVerContrasena.TabIndex = 5;
            CheckVerContrasena.Text = "Ver contraseña";
            CheckVerContrasena.UseVisualStyleBackColor = true;
            CheckVerContrasena.CheckedChanged += CheckBox_CheckedChanged;
            // 
            // BtnCancelar
            // 
            BtnCancelar.ForeColor = Color.Black;
            BtnCancelar.Location = new Point(493, 194);
            BtnCancelar.Name = "BtnCancelar";
            BtnCancelar.Size = new Size(75, 23);
            BtnCancelar.TabIndex = 7;
            BtnCancelar.Text = "Cancelar";
            BtnCancelar.UseVisualStyleBackColor = true;
            // 
            // BtnLogin
            // 
            BtnLogin.ForeColor = Color.Black;
            BtnLogin.Location = new Point(393, 194);
            BtnLogin.Name = "BtnLogin";
            BtnLogin.Size = new Size(75, 23);
            BtnLogin.TabIndex = 6;
            BtnLogin.Text = "Iniciar sesión";
            BtnLogin.UseVisualStyleBackColor = true;
            BtnLogin.Click += BtnLogin_Click;
            // 
            // LoginView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.MidnightBlue;
            ClientSize = new Size(629, 249);
            Controls.Add(BtnCancelar);
            Controls.Add(BtnLogin);
            Controls.Add(CheckVerContrasena);
            Controls.Add(TxtContrasena);
            Controls.Add(TxtEmail);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(pictureBox1);
            ForeColor = Color.White;
            FormBorderStyle = FormBorderStyle.None;
            Name = "LoginView";
            Text = "LoginView";
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private PictureBox pictureBox1;
        private Label label1;
        private Label label2;
        private TextBox TxtEmail;
        private TextBox TxtContrasena;
        private CheckBox CheckVerContrasena;
        private Button BtnCancelar;
        private Button BtnLogin;
    }
}