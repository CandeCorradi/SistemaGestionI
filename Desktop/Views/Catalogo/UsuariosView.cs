using Firebase.Auth;
using Firebase.Auth.Providers;
using Newtonsoft.Json.Linq;
using Service.Enums;
using Service.Models;
using Service.Services;
using System.Net.Http.Headers;
using System.Windows.Controls;

namespace Desktop.Views
{
    public partial class UsuariosView : Form
    {
        GenericService<Usuario> _usuarioService = new();
        Usuario _currentUsuario;
        List<Usuario>? _usuarios;
        FirebaseAuthClient _firebaseAuthClient;

        public UsuariosView()
        {
            InitializeComponent();
            _ = GetAllData();
            SettingFirebase();
            checkVerEliminados.CheckedChanged += DisplayHideControlsRestoreButton;

        }

        private void SettingFirebase()
        {
            var config = new FirebaseAuthConfig()
            {
                ApiKey = Service.Properties.Resources.ApiKeyFirebase,
                AuthDomain = Service.Properties.Resources.AuthDomainFirebase,
                Providers = new FirebaseAuthProvider[]
                {
            new EmailProvider()
                }
            };
            _firebaseAuthClient = new FirebaseAuthClient(config);
        }

        private void DisplayHideControlsRestoreButton(object? sender, EventArgs e)
        {
            btnAgregar.Enabled = !checkVerEliminados.Checked;
            btnModificar.Enabled = !checkVerEliminados.Checked;
            btnEliminar.Enabled = !checkVerEliminados.Checked;
            btnRestore.Visible = checkVerEliminados.Checked;
            btnBuscar.Enabled = !checkVerEliminados.Checked;
            txtBuscar.Enabled = !checkVerEliminados.Checked;
        }

        private async Task GetAllData()
        {
            try
            {
                // 1. Buscamos la lista actualizada según el CheckBox
                if (checkVerEliminados.Checked)
                {
                    _usuarios = await _usuarioService.GetAllDeletedsAsync();
                }
                else
                {
                    var listaCompleta = await _usuarioService.GetAllAsync();
                    _usuarios = listaCompleta.Where(u => u.IsDeleted == false).ToList();
                }

                // 2. EL TRUCO: Primero ponemos null para "limpiar" la vista
                GridUsuario.DataSource = null;
                // 3. Ahora le pasamos la lista fresca
                GridUsuario.DataSource = _usuarios;

                // 4. Ocultamos las columnas técnicas (con seguridad por si son null)
                if (GridUsuario.Columns["Id"] != null) GridUsuario.Columns["Id"].Visible = false;
                if (GridUsuario.Columns["IsDeleted"] != null) GridUsuario.Columns["IsDeleted"].Visible = false;

                // RECOMENDACIÓN: Ocultamos la contraseña por seguridad
                if (GridUsuario.Columns["Password"] != null) GridUsuario.Columns["Password"].Visible = false;

                GetComboTiposDeUsuarios(); //
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener los usuarios: {ex.Message}", "Error");
            }
        }

        private void GetComboTiposDeUsuarios()
        {
            //cargo el combo de tipos de usuarios con el enum de TipoUsuarioEnum
            ComboTiposDeUsuarios.DataSource = Enum.GetValues(typeof(TipoUsuarioEnum));
        }



        private void btnSalir_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void GridUsuario_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (GridUsuario.RowCount > 0 && GridUsuario.SelectedRows.Count > 0)
            {

            }
        }

        private async void btnEliminar_Click(object sender, EventArgs e)
        {
            //checheamos que haya peliculas seleccionadas
            if (GridUsuario.RowCount > 0 && GridUsuario.SelectedRows.Count > 0)
            {
                Usuario entitySelected = (Usuario)GridUsuario.SelectedRows[0].DataBoundItem;
                var respuesta = MessageBox.Show($"¿Seguro que desea eliminar el usuario {entitySelected.Nombre}?", "Confirmar Eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (respuesta == DialogResult.Yes)
                {
                    if (await _usuarioService.DeleteAsync(entitySelected.Id))
                    {
                        LabelStatusMessage.Text = $"Usuario {entitySelected.Nombre} eliminado correctamente";
                        TimerStatusBar.Start(); // Iniciar el temporizador para mostrar el mensaje en la barra de estado
                        await GetAllData();
                    }
                    else
                    {
                        MessageBox.Show("Error al eliminar el usuario", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Debe seleccionar un usuario a eliminar", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAgregar_Click(object sender, EventArgs e)
        {
            LimpiarControlesAgregarEditar();
            tabControlUsuarios.SelectedTab = tabPageAgregarEditar;
        }

        private void LimpiarControlesAgregarEditar()
        {
            TxtNombre.Clear();
            TxtDni.Clear();
            TxtApellido.Clear();
            TxtEmail.Clear();
            TxtPassword.Clear();
            TxtPassword2.Clear();
            GetComboTiposDeUsuarios();
            LabelPassword.Text = "Contraseña:";
            LabelPassword2.Text = "Repetir contraseña:";
            TxtPassword.PlaceholderText = "Mínimo 6 caracteres";
            TxtPassword2.PlaceholderText = "Mínimo 6 caracteres";
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            tabControlUsuarios.SelectedTab = tabPageLista;
        }

        private async void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!DataControl()) return;

            Usuario usuarioAGuardar = GetUserDataFromScreen();
            bool responseSuccessfull = false;

            try
            {
                if (_currentUsuario != null)
                {
                    usuarioAGuardar.Id = _currentUsuario.Id;

                    responseSuccessfull = await _usuarioService.UpdateAsync(usuarioAGuardar);

                    if (responseSuccessfull && !string.IsNullOrWhiteSpace(TxtPassword.Text))
                        await UpdatePasswordInFirebase(usuarioAGuardar);
                }
                else
                {
                    var nuevoUsuario = await _usuarioService.AddAsync(usuarioAGuardar);
                    responseSuccessfull = nuevoUsuario != null;

                    if (responseSuccessfull)
                        await CreateUserInFirebase(nuevoUsuario);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en la operación: {ex.Message}", "Error");
                return;
            }

            if (responseSuccessfull)
            {
                _currentUsuario = null;
                LabelStatusMessage.Text = $"Usuario {usuarioAGuardar.Nombre} guardado correctamente";
                TimerStatusBar.Start();
                await GetAllData();
                LimpiarControlesAgregarEditar();
                tabControlUsuarios.SelectedTab = tabPageLista;
            }
            else
            {
                MessageBox.Show("Error al guardar el usuario en la base de datos.", "Error");
            }
        }

        private bool DataControl()
        {
            // Validaciones simples
            if (string.IsNullOrWhiteSpace(TxtNombre.Text))
            {
                MessageBox.Show("El nombre no puede estar vacío", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (string.IsNullOrWhiteSpace(TxtApellido.Text))
            {
                MessageBox.Show("El apellido no puede estar vacío", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (string.IsNullOrWhiteSpace(TxtDni.Text))
            {
                MessageBox.Show("El DNI no puede estar vacío", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (string.IsNullOrWhiteSpace(TxtEmail.Text))
            {
                MessageBox.Show("El email no puede estar vacío", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (_currentUsuario == null && (TxtPassword.Text != TxtPassword2.Text))
            {
                MessageBox.Show("Las contraseñas ingresadas no coinciden", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (_currentUsuario == null && (string.IsNullOrWhiteSpace(TxtPassword.Text) || string.IsNullOrWhiteSpace(TxtPassword2.Text)))
            {
                MessageBox.Show("Debe completar el campo contraseña y verificación", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (_currentUsuario != null && (string.IsNullOrWhiteSpace(TxtPassword.Text) && string.IsNullOrWhiteSpace(TxtPassword2.Text)))//modificación que no cambia la contraseña
            {
                return true;
            }

            if (_currentUsuario != null && (string.IsNullOrWhiteSpace(TxtPassword.Text) || string.IsNullOrWhiteSpace(TxtPassword2.Text)))
            {
                MessageBox.Show("Para modificar la contraseña debe completar la contraseña anterior y nueva", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if ((TxtPassword.Text.Length < 6) || (TxtPassword2.Text.Length < 6))
            {
                MessageBox.Show("Las contraseñas deben tener al menos 6 caracteres", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        private Usuario GetUserDataFromScreen()
        {
            return new Usuario
            {
                Id = _currentUsuario?.Id ?? 0,
                Nombre = TxtNombre.Text,
                Apellido = TxtApellido.Text,
                Dni = TxtDni.Text,
                Email = TxtEmail.Text,
                Password = TxtPassword.Text,
                TipoUsuario = (TipoUsuarioEnum)(ComboTiposDeUsuarios.SelectedItem ?? TipoUsuarioEnum.Cliente)
            };
        }

        private async Task UpdatePasswordInFirebase(Usuario usuarioAGuardar)
        {
            try
            {
                var userCredential = await _firebaseAuthClient.SignInWithEmailAndPasswordAsync(
                    usuarioAGuardar.Email,
                    TxtPassword.Text.Trim()
                );
                await userCredential.User.ChangePasswordAsync(TxtPassword2.Text.Trim());
            }
            catch (FirebaseAuthException ex)
            {
                MessageBox.Show($"Error al actualizar la contraseña en Firebase: {ex.Reason}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task CreateUserInFirebase(Usuario? nuevoUsuario)
        {
            try
            {
                var userCredential = await _firebaseAuthClient.CreateUserWithEmailAndPasswordAsync(
                    nuevoUsuario.Email,
                    TxtPassword.Text.Trim(),
                    nuevoUsuario.Nombre + " " + nuevoUsuario.Apellido// Contraseña por defecto, se recomienda cambiarla luego
                );
                await SendVerificationEmailAsync(userCredential.User.GetIdTokenAsync().Result); // Enviar correo de verificación
            }
            catch (FirebaseAuthException ex)
            {
                MessageBox.Show($"Error al crear el usuario en Firebase: {ex.Reason}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SendVerificationEmailAsync(string idToken)
        {
            var FirebaseApiKey = Service.Properties.Resources.ApiKeyFirebase;
            var RequestUri = "https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key=" + FirebaseApiKey;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var content = new StringContent("{\"requestType\":\"VERIFY_EMAIL\",\"idToken\":\"" + idToken + "\"}");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = await client.PostAsync(RequestUri, content);
                response.EnsureSuccessStatusCode();
            }
        }

        private void btnModificar_Click(object sender, EventArgs e)
        {
            //checheamos que haya una capacitación seleccionada
            if (GridUsuario.RowCount > 0 && GridUsuario.SelectedRows.Count > 0)
            {
                _currentUsuario = (Usuario)GridUsuario.SelectedRows[0].DataBoundItem;
                TxtNombre.Text = _currentUsuario.Nombre;
                TxtApellido.Text = _currentUsuario.Apellido;
                TxtDni.Text = _currentUsuario.Dni;
                TxtEmail.Text = _currentUsuario.Email;
                ComboTiposDeUsuarios.SelectedItem = _currentUsuario.TipoUsuario;

                LabelPassword.Text = "Contraseña anterior:";
                LabelPassword2.Text = "Nueva contraseña:";
                TxtPassword.PlaceholderText = "Dejar en blanco para no modificar";
                TxtPassword2.PlaceholderText = "Dejar en blanco para no modificar";
                tabControlUsuarios.SelectedTab = tabPageAgregarEditar;
            }
            else
            {
                MessageBox.Show("Debe seleccionar un usuario a modificar", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnBuscar_Click(object sender, EventArgs e)
        {
            GridUsuario.DataSource = await _usuarioService.GetAllAsync(txtBuscar.Text);
        }

        private void txtBuscar_TextChanged(object sender, EventArgs e)
        {
            //btnBuscar.PerformClick();
        }

        private void TimerStatusBar_Tick(object sender, EventArgs e)
        {
            LabelStatusMessage.Text = string.Empty;
            TimerStatusBar.Stop(); // Detener el temporizador después de mostrar el mensaje
        }

        private async void checkVerEliminados_CheckedChanged(object sender, EventArgs e)
        {
            await GetAllData();
        }

        private async void btnRestore_Click(object sender, EventArgs e)
        {
            if (!checkVerEliminados.Checked) return;
            //checheamos que haya peliculas seleccionadas
            if (GridUsuario.RowCount > 0 && GridUsuario.SelectedRows.Count > 0)
            {
                Usuario entitySelected = (Usuario)GridUsuario.SelectedRows[0].DataBoundItem;
                var respuesta = MessageBox.Show($"¿Seguro que recuper al usuario {entitySelected.Nombre}?", "Confirmar Restauración", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (respuesta == DialogResult.Yes)
                {
                    if (await _usuarioService.RestoreAsync(entitySelected.Id))
                    {
                        LabelStatusMessage.Text = $"Usuario {entitySelected.Nombre} restaurado correctamente";
                        TimerStatusBar.Start(); // Iniciar el temporizador para mostrar el mensaje en la barra de estado
                        await GetAllData();
                    }
                    else
                    {
                        MessageBox.Show("Error al restaurar el usuario", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Debe seleccionar un usuario a restaurar", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
