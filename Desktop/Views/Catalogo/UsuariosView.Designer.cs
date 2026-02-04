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
            label2 = new Label();
            SuspendLayout();
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Times New Roman", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.Location = new Point(226, 9);
            label2.Name = "label2";
            label2.Size = new Size(220, 26);
            label2.TabIndex = 4;
            label2.Text = "✦ Tipo de Usuario ✦";
            // 
            // UsuariosView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(662, 450);
            Controls.Add(label2);
            Name = "UsuariosView";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "UsuariosView";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label2;
    }
}