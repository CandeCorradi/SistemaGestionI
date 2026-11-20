using DotNetEnv;
using Microsoft.Extensions.Caching.Memory;
using Cliente.DTOs;
using Cliente.Interfaces;
using Cliente.Utils;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Service.Interfaces;

namespace Cliente.Services
{
    public class AuthService : IAuthService
    {
        IMemoryCache _memoryCache;
        HttpClient _httpClient;
        JsonSerializerOptions _options;

        protected void SetAuthorizationHeader()
        {
            // Si ya está configurado (por un DelegatingHandler), no hacer nada
            if (_httpClient.DefaultRequestHeaders.Authorization is not null)
                return;

            // 1) Intentar leer desde IMemoryCache (configurado por FirebaseAuthService)
            if (_memoryCache is not null && _memoryCache.TryGetValue("jwt", out string? cachedToken) && !string.IsNullOrWhiteSpace(cachedToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cachedToken);
                return;
            }

            throw new InvalidOperationException("El token JWT no está disponible para la autorización.");

        }

        public AuthService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
            SettingHttpClient();

        }

        private void SettingHttpClient()
        {
            Env.Load();
            var apiUrl = Environment.GetEnvironmentVariable("API_URL");
            if (apiUrl == null)
            {
                throw new InvalidOperationException("La variable de entorno 'API_URL' no está definida. Asegúrate de que esté configurada correctamente.");
            }
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(apiUrl);
            _options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
        }

        public async Task<bool> CreateUserWithEmailAndPasswordAsync(string email, string password, string nombre)
        {
            SetAuthorizationHeader();
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(nombre))
            {
                throw new ArgumentException("Email, password o nombre no pueden ser nulos o vacíos.");
            }
            try
            {
                var endpointAuth = ApiEndPoints.GetEndpoint("Login");
                var newUser = new RegisterDTO { Email = email, Password = password, Nombre = nombre };
                var response = await _httpClient.PostAsJsonAsync($"{endpointAuth}/register/", newUser);
                if (response.IsSuccessStatusCode)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al crear usuario" + ex.Message);
            }

        }

        //si no recibo el objeto IConfiguration en el constructor, creo un constructor vacio que instancie uno y lea el archivo appsettings.json
        public async Task<string?> Login(LoginDTO? login)
        {
            if (login == null)
            {
                throw new ArgumentException("El objeto login no llego.");
            }
            try
            {
                var endpointAuth = ApiEndPoints.GetEndpoint("Login");
                var response = await _httpClient.PostAsJsonAsync($"{endpointAuth}/login/", login);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    _memoryCache.Set("jwt", result);
                    return null;
                }
                else
                {
                    //si no es exitoso, devuelvo el mensaje de error
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return errorContent;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al loguearse->: " + ex.Message);
            }




        }

        public async Task<bool> ResetPassword(LoginDTO? login) //acordarse de cambiar, solo necesito que me manden el mail o sea sting mail)
        {
            SetAuthorizationHeader();
            if (login == null)
            {
                throw new ArgumentException("El objeto login no llego.");
            }
            try
            {
                var endpointAuth = ApiEndPoints.GetEndpoint("Login");
                var response = await _httpClient.PostAsJsonAsync($"{endpointAuth}/resetpassword/", login);
                if (response.IsSuccessStatusCode)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al resetear el password->: " + ex.Message);
            }
        }

        public async Task<bool> DeleteUser(LoginDTO? login)
        {
            SetAuthorizationHeader();
            if (login == null)
            {
                throw new ArgumentException("El objeto login no llego.");
            }
            try
            {
                var endpointAuth = ApiEndPoints.GetEndpoint("Login");
                var response = await _httpClient.PostAsJsonAsync($"{endpointAuth}/deleteuser/", login);
                if (response.IsSuccessStatusCode)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al eliminar el usuario en firebase->: " + ex.Message);
            }
        }

        public async Task<bool> SendVerificationEmail(string email)
        {
            SetAuthorizationHeader();
            if (email == null)
            {
                throw new ArgumentException("El objeto email no llego.");
            }
            try
            {
                var endpointAuth = ApiEndPoints.GetEndpoint("Login");
                var response = await _httpClient.PostAsJsonAsync($"{endpointAuth}/sendverification/", email);
                if (response.IsSuccessStatusCode)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al enviar la verificación: " + ex.Message);
            }
        }
    }
}
