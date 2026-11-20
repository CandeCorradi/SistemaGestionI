using Firebase.Auth;
using Firebase.Auth.Providers;
using Microsoft.Extensions.Caching.Memory;
using Cliente.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Desktop.Views
{
    public partial class LoginView : Form
    {
        private readonly IMemoryCache? _memoryCache;
        UsuarioService _usuarioService;
        int loginFailsCount = 0;

        public LoginView(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
            _usuarioService = new UsuarioService(_memoryCache);
            InitializeComponent();

        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            try
            {
                this.Enabled = false;
                var login = await _usuarioService.LoginInSystem(TxtEmail.Text, TxtContrasena.Text);
                if (login)
                {
                    this.Hide();
                    var menuPrincipalView = new MenuPrincipalView(_memoryCache);
                    menuPrincipalView.ShowDialog();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al iniciar sesión. Verifique sus credenciales.");
                loginFailsCount++;
                if (loginFailsCount >= 3)
                {
                    MessageBox.Show("Ha excedido el número máximo de intentos fallidos. La aplicación se cerrará.");
                    this.Close();
                }
                this.Enabled = true;
            }
        }

        private void CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            TxtContrasena.PasswordChar = CheckVerContrasena.Checked ? '\0' : '*';
        }

        private void TxtEmail_TextChanged(object sender, EventArgs e)
        {
            //si presiono enter, seleccioni el siguiente campo
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true; // Evita que se agregue un salto de línea
                TxtContrasena.Focus(); // Mueve el foco al campo de contraseña
            }
        }

        private void TxtContrasena_TextChanged(object sender, EventArgs e)
        {
            //si se presiona enter, se intenta iniciar sesión
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true; // Evita que se agregue un salto de línea
                BtnLogin.PerformClick(); // Simula el clic en el botón de iniciar sesión
            }
        }
        private void BtnCancelar_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
