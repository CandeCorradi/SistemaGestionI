using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Infrastructure.Storage;

/// <summary>
/// Implementación de <see cref="IStorageService"/> sobre la API REST de
/// Supabase Storage (BR-100: las imágenes viven en el storage, nunca en
/// la base de datos — BR-101). Usa la key service_role del lado del
/// servidor y expone los objetos mediante URLs públicas del bucket.
/// </summary>
public sealed class SupabaseStorageService : IStorageService, IDisposable
{
    private readonly SupabaseStorageOptions _options;
    private readonly ILogger<SupabaseStorageService> _logger;
    private readonly HttpClient _client;

    public SupabaseStorageService(
        SupabaseStorageOptions options,
        ILogger<SupabaseStorageService> logger)
    {
        _options = options;
        _logger = logger;
        _client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public bool IsConfigured => _options.IsConfigured;

    public string GetPublicUrl(string path)
    {
        if (!IsConfigured)
        {
            return string.Empty;
        }

        return $"{_options.Url!.TrimEnd('/')}/storage/v1/object/public/{_options.Bucket}/{path}";
    }

    public async Task<Result> UploadAsync(
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return Result.Failure(NotConfiguredError());
        }

        try
        {
            using var request = BuildRequest(HttpMethod.Post, $"storage/v1/object/{_options.Bucket}/{path}");
            request.Content = new StreamContent(content);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            var response = await _client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "STORAGE_UPLOAD_FAILED path={Path} status={Status} detail={Detail}",
                path, (int)response.StatusCode, Truncate(body));

            return Result.Failure(Error.Storage(
                $"No se pudo subir la imagen al almacenamiento (HTTP {(int)response.StatusCode})."));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "STORAGE_UPLOAD_ERROR path={Path}", path);
            return Result.Failure(Error.Storage("No se pudo conectar con el almacenamiento de imágenes."));
        }
    }

    public async Task<Result> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return Result.Failure(NotConfiguredError());
        }

        try
        {
            using var request = BuildRequest(HttpMethod.Delete, $"storage/v1/object/{_options.Bucket}/{path}");

            var response = await _client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }

            var status = (int)response.StatusCode;
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            // Idempotente: un objeto que ya no existe no debe bloquear
            // la limpieza de la referencia en la base de datos.
            if (status == 404 || (status == 400 && body.Contains("not found", StringComparison.OrdinalIgnoreCase)))
            {
                return Result.Success();
            }

            _logger.LogError(
                "STORAGE_DELETE_FAILED path={Path} status={Status} detail={Detail}",
                path, status, Truncate(body));

            return Result.Failure(Error.Storage(
                $"No se pudo eliminar la imagen del almacenamiento (HTTP {status})."));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "STORAGE_DELETE_ERROR path={Path}", path);
            return Result.Failure(Error.Storage("No se pudo conectar con el almacenamiento de imágenes."));
        }
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string relativePath)
    {
        var request = new HttpRequestMessage(
            method,
            $"{_options.Url!.TrimEnd('/')}/{relativePath}");

        request.Headers.TryAddWithoutValidation("apikey", _options.ServiceRoleKey);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_options.ServiceRoleKey}");

        return request;
    }

    private static Error NotConfiguredError()
        => Error.Storage(
            "El almacenamiento de imágenes no está configurado " +
            "(SUPABASE_URL / SUPABASE_STORAGE_BUCKET / SUPABASE_SERVICE_ROLE_KEY).");

    private static string Truncate(string value)
        => value.Length <= 500 ? value : value[..500];

    public void Dispose() => _client.Dispose();
}
