namespace SistemaGestion.Infrastructure.Authentication.Firebase;

/// <summary>
/// Opciones de Firebase Authentication / Storage cargadas desde la sección
/// "Firebase" de la configuración (variables de entorno FIREBASE_*).
/// No se almacenan secretos reales en el repositorio (AGENTS.md §46-47).
/// </summary>
public sealed class FirebaseOptions
{
    public const string SectionName = "Firebase";

    public string ProjectId { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string ServiceAccountJson { get; set; } = string.Empty;
    public string StorageBucket { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ProjectId)
        || !string.IsNullOrWhiteSpace(ServiceAccountJson);
}