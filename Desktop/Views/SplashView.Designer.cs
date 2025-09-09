namespace Desktop.Views
{
    partial class SplashView
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
            pictureBox1 = new PictureBox();
            ProgressBar = new ProgressBar();
            Timer = new System.Windows.Forms.Timer(components);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.freepik__background__54410;
            pictureBox1.Location = new Point(169, 12);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(187, 190);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // ProgressBar
            // 
            ProgressBar.Location = new Point(29, 209);
            ProgressBar.Name = "ProgressBar";
            ProgressBar.Size = new Size(465, 23);
            ProgressBar.TabIndex = 1;
            // 
            // Timer
            // 
            Timer.Tick += timer_Tick;
            // 
            // SplashView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.MidnightBlue;
            ClientSize = new Size(516, 244);
            Controls.Add(ProgressBar);
            Controls.Add(pictureBox1);
            FormBorderStyle = FormBorderStyle.None;
            Name = "SplashView";
            Text = "SplashView";
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private PictureBox pictureBox1;
        private ProgressBar ProgressBar;
        private System.Windows.Forms.Timer Timer;
    }
}