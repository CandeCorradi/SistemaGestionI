namespace SistemaGestion.Shared.Results;

/// <summary>
/// Define un error de dominio/aplicación con un código estable (para la API)
/// y una descripción legible.
/// </summary>
public sealed record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "El valor solicitado no existe.");

    public static Error Validation(string description) => new("Validation.Error", description);

    public static Error Conflict(string description) => new("Conflict.Error", description);

    public static Error NotFound(string description) => new("NotFound.Error", description);

    /// <summary>
    /// Falla o falta de configuración del almacenamiento de archivos
    /// externo (Supabase Storage, BR-100): se mapea a 503 en la API.
    /// </summary>
    public static Error Storage(string description) => new("Storage.Error", description);
}