namespace SistemaGestion.Application.Abstractions;

/// <summary>
/// Contrato para verificar Firebase ID Tokens con Firebase Admin SDK.
/// El backend es el único que valida la identidad (AGENTS.md §8).
/// </summary>
public interface IFirebaseAuthService
{
    /// <summary>
    /// Verifica un ID Token. Devuelve <c>null</c> cuando el token es inválido o vencido.
    /// </summary>
    Task<FirebaseAuthResult?> VerifyTokenAsync(string idToken, CancellationToken cancellationToken = default);
}