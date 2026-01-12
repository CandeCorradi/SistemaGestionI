using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebase.Auth.Providers;
using MovilApp.Views;
using Service.Enums;
using Service.Models;
using Service.Services;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Diagnostics;

namespace MovilApp.ViewModels.Login
{
    public partial class SignInViewModel : ObservableObject
    {
        private readonly FirebaseAuthClient _clientAuth;
        GenericService<Usuario> _usuarioService = new();
        private readonly string FirebaseApiKey;
        private readonly string RequestUri;

        public IRelayCommand RegistrarseCommand { get; }
        public IRelayCommand VolverCommand { get; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegistrarseCommand))]
        private string name;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegistrarseCommand))]
        private string lastname;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegistrarseCommand))]
        private string dni;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegistrarseCommand))]
        private string email;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegistrarseCommand))]
        private string password;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegistrarseCommand))]
        private string verifyPassword;

        public SignInViewModel()
        {
            FirebaseApiKey = Service.Properties.Resources.ApiKeyFirebase;
            RequestUri = "https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key=" + FirebaseApiKey;

            RegistrarseCommand = new AsyncRelayCommand(Registrarse, PermitirRegistrarse);
            VolverCommand = new AsyncRelayCommand(Volver);

            _clientAuth = new FirebaseAuthClient(new FirebaseAuthConfig()
            {
                ApiKey = FirebaseApiKey,
                AuthDomain = Service.Properties.Resources.AuthDomainFirebase,
                Providers = new Firebase.Auth.Providers.FirebaseAuthProvider[]
                {
                    new EmailProvider()
                }
            });
        }

        private bool PermitirRegistrarse()
        {
            return !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(email)
                && !string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(verifyPassword)
                && !string.IsNullOrEmpty(dni) && !string.IsNullOrEmpty(lastname);
        }

        private async Task Volver()
        {
            if (Application.Current?.MainPage is SistemaDeGestionShell shell)
            {
                await shell.GoToAsync("//Login");
            }
        }

        private async Task Registrarse()
        {
            if (password != verifyPassword)
            {
                await Application.Current.MainPage.DisplayAlert("Registrarse", "Las contraseñas ingresadas no coinciden", "Ok");
                return;
            }

            try
            {
                // 1. Crear usuario en Firebase
                var userCredential = await _clientAuth.CreateUserWithEmailAndPasswordAsync(email, password, name);

                // 2. Guardar el usuario en tu base de datos local
                var nuevoUsuario = new Usuario
                {
                    Apellido = lastname,
                    Nombre = name,
                    Dni = dni,
                    Email = email,
                    Password = password,
                    TipoUsuario = TipoUsuarioEnum.Administrador,
                    IsDeleted = false
                };
                await _usuarioService.AddAsync(nuevoUsuario);

                // 3. Enviar correo de verificación (SIN .Result)
                // Obtenemos el token de forma asíncrona correctamente
                var token = await userCredential.User.GetIdTokenAsync();
                await SendVerificationEmailAsync(token);

                await Application.Current.MainPage.DisplayAlert("Éxito", "Cuenta creada. Por favor, revisa tu correo para verificar la cuenta (revisa Spam).", "Ok");

                // 4. Navegar
                if (Application.Current?.MainPage is SistemaDeGestionShell shell)
                {
                    await shell.GoToAsync("//Login");
                }
            }
            catch (FirebaseAuthException ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error de Autenticación", $"No se pudo crear el usuario: {ex.Reason}", "Ok");
            }
            catch (Exception ex)
            {
                // Esto atrapará cualquier otro error (como el de red o HttpClient)
                await Application.Current.MainPage.DisplayAlert("Error", $"Ocurrió algo inesperado: {ex.Message}", "Ok");
            }
        }

        public async Task SendVerificationEmailAsync(string idToken)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var content = new StringContent("{\"requestType\":\"VERIFY_EMAIL\",\"idToken\":\"" + idToken + "\"}");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = await client.PostAsync(RequestUri, content);
                response.EnsureSuccessStatusCode();
            }
        }
    }
}

