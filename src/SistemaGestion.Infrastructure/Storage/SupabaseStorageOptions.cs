namespace SistemaGestion.Infrastructure.Storage;

/// <summary>
/// Configuración del almacenamiento de imágenes en Supabase Storage
/// (BR-100). Se lee de variables de entorno planas del `.env` de la raíz
/// (gitignored, AGENTS.md §46); nunca se persiste en el repositorio.
/// </summary>
public sealed class SupabaseStorageOptions
{
    /// <summary>URL base del proyecto (https://xxx.supabase.co).</summary>
    public string? Url { get; init; }

    /// <summary>Bucket de imágenes (ej. productos).</summary>
    public string? Bucket { get; init; }

    /// <summary>Key service_role; sólo del lado del servidor.</summary>
    public string? ServiceRoleKey { get; init; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Url)
        && !string.IsNullOrWhiteSpace(Bucket)
        && !string.IsNullOrWhiteSpace(ServiceRoleKey);
}
