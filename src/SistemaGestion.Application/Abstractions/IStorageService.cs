using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Abstractions;

/// <summary>
/// Almacenamiento de archivos externo (Supabase Storage, BR-100/
/// functional-spec §25). La implementación vive en Infrastructure; el
/// dominio sólo conoce rutas (path) dentro del bucket. Los binarios
/// nunca se guardan en PostgreSQL (BR-101).
/// </summary>
public interface IStorageService
{
    /// <summary>false cuando faltan URL/bucket/key en el entorno.</summary>
    bool IsConfigured { get; }

    /// <summary>Sube un objeto al bucket. La ruta debe ser única.</summary>
    Task<Result> UploadAsync(
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina un objeto. Un objeto inexistente se considera éxito
    /// (idempotente) para no bloquear la limpieza de metadatos.
    /// </summary>
    Task<Result> DeleteAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>URL pública de lectura del objeto (bucket público).</summary>
    string GetPublicUrl(string path);
}
