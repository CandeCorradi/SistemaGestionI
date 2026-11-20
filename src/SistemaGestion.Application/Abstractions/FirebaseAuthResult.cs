namespace SistemaGestion.Application.Abstractions;

/// <summary>
/// Resultado de la verificación de un Firebase ID Token.
/// </summary>
public sealed record FirebaseAuthResult(
    string Uid,
    string Email,
    bool EmailVerified,
    string? Name = null);