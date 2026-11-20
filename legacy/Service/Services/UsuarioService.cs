using Microsoft.Extensions.Caching.Memory;
using Cliente.DTOs;
using Cliente.Interfaces;
using Cliente.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace Cliente.Services
{
    public class UsuarioService : GenericService<Usuario>, IUsuarioService
    {
        public UsuarioService(IMemoryCache memoryCache) : base(memoryCache)
        {

        }

        public async Task<Usuario?> GetByEmailAsync(string email)
        {
            SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"{_endpoint}/byemail?email={email}");
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error al obtener los datos: {response.StatusCode}");
            }
            return JsonSerializer.Deserialize<Usuario>(content, _options);
        }
        public async Task<bool> LoginInSystem(string email, string password)
        {
            SetAuthorizationHeader();
            var loginDTO = new LoginDTO
            {
                Username = email,
                Password = password
            };
            var response = await _httpClient.PostAsJsonAsync($"auth/login", loginDTO);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error al iniciar sesión: {response.StatusCode}");
            }
            var result = JsonSerializer.Deserialize<string>(content, _options);
            _memoryCache!.Set("jwt", result);
            return response.IsSuccessStatusCode;
        }

    }
}