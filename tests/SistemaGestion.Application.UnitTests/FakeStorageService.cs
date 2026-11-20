using SistemaGestion.Application.Abstractions;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.UnitTests;

/// <summary>
/// Storage en memoria para tests de unidad (AGENTS.md §39): no hace
/// llamadas de red y permite inyectar fallos configurados.
/// </summary>
public sealed class FakeStorageService : IStorageService
{
    public bool IsConfigured { get; set; } = true;

    public string BaseUrl { get; set; } = "https://storage.test/object/public";

    public Error? UploadError { get; set; }

    public Error? DeleteError { get; set; }

    public List<string> UploadedPaths { get; } = [];

    public List<string> DeletedPaths { get; } = [];

    public string GetPublicUrl(string path) => $"{BaseUrl}/{path}";

    public Task<Result> UploadAsync(
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (UploadError is not null)
        {
            return Task.FromResult(Result.Failure(UploadError));
        }

        UploadedPaths.Add(path);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        if (DeleteError is not null)
        {
            return Task.FromResult(Result.Failure(DeleteError));
        }

        DeletedPaths.Add(path);
        return Task.FromResult(Result.Success());
    }
}
