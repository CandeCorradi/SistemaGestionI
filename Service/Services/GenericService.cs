using Service.Interfaces;
using Service.Utils;
using System.Net.Http.Json;
using System.Text.Json;

namespace Service.Services
{
    public class GenericService<T> : IGenericService<T> where T : class
    {
        private readonly HttpClient _httpClient;
        protected readonly JsonSerializerOptions _options;
        protected readonly string _endpoint;


        public GenericService()
        {
            _httpClient = new HttpClient();
            _options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
            _endpoint = Properties.Resources.UrlApiAzure + ApiEndPoints.GetEndpoint(typeof(T).Name);
            //_endpoint = Properties.Resources.UrlApiLocal + ApiEndPoints.GetEndpoint(typeof(T).Name);

        }
        public async Task<T?> AddAsync(T? entity)
        {
            var response = await _httpClient.PostAsJsonAsync(_endpoint, entity);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error al agregar el dato: {response.StatusCode} - {content}");
            }
            return JsonSerializer.Deserialize<T>(content, _options);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{_endpoint}/{id}");
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error al eliminar el dato: {response.StatusCode}");
            }
            return response.IsSuccessStatusCode;

        }

        public async Task<List<T>?> GetAllAsync(string? filtro = "")
        {
            var url = string.IsNullOrEmpty(filtro)
                ? _endpoint
                : $"{_endpoint}?filter={Uri.EscapeDataString(filtro)}";

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                return new List<T>();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error al obtener los datos: {response.StatusCode} - {content}");

            if (string.IsNullOrWhiteSpace(content))
                return new List<T>();

            try
            {
                // Intentar deserializar como lista directa
                var lista = JsonSerializer.Deserialize<List<T>>(content, _options);
                if (lista != null) return lista;

                // Intentar algunos wrappers comunes: { data: [...] }, { items: [...] }, { result: [...] }
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in new[] { "data", "items", "result", "value" })
                    {
                        if (doc.RootElement.TryGetProperty(prop, out var arr) && arr.ValueKind == JsonValueKind.Array)
                        {
                            return JsonSerializer.Deserialize<List<T>>(arr.GetRawText(), _options);
                        }
                    }
                }
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<T>>(content, _options);
                }

                // Fallback: usar GetFromJsonAsync (puede repetir la petición)
                return await _httpClient.GetFromJsonAsync<List<T>>(url, _options);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al deserializar la respuesta: {ex.Message}. Contenido: {content}");
            }
        }

        public async Task<List<T>?> GetAllDeletedsAsync(string? filtro)
        {
            var response = await _httpClient.GetAsync($"{_endpoint}/deleteds");
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error al obtener los datos: {response.StatusCode}");
            }
            return JsonSerializer.Deserialize<List<T>>(content, _options);
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{_endpoint}/{id}");
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error al obtener los datos: {response.StatusCode}");
            }
            return JsonSerializer.Deserialize<T>(content, _options);
        }

        public async Task<bool> RestoreAsync(int id)
        {
            var response = await _httpClient.PutAsync($"{_endpoint}/restore/{id}", null);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error al restaurar el dato: {response.StatusCode}");
            }
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateAsync(T? entity)
        {
            var idValue = entity.GetType().GetProperty("Id").GetValue(entity);
            var response = await _httpClient.PutAsJsonAsync($"{_endpoint}/{idValue}", entity);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Hubo un problema al actualizar");
            }
            else
            {
                return response.IsSuccessStatusCode;
            }

        }
    }
}

