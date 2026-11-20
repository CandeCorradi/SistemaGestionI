using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using SistemaGestion.Application.Abstractions;

namespace SistemaGestion.Infrastructure.Authentication.Firebase;

/// <summary>
/// Verifica Firebase ID Tokens con Firebase Admin SDK. Requiere que FirebaseApp
/// haya sido inicializado por FirebaseConfigurator.
/// </summary>
public sealed class FirebaseAuthService : IFirebaseAuthService
{
    public async Task<FirebaseAuthResult?> VerifyTokenAsync(string idToken, CancellationToken cancellationToken = default)
    {
        var app = FirebaseApp.DefaultInstance;
        if (app is null)
        {
            throw new InvalidOperationException("Firebase no está inicializado.");
        }

        FirebaseToken decoded;
        try
        {
            decoded = await FirebaseAuth.GetAuth(app).VerifyIdTokenAsync(idToken, cancellationToken);
        }
        catch (Exception)
        {
            return null;
        }

        var email = decoded.Claims.TryGetValue("email", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        var name = decoded.Claims.TryGetValue("name", out var nameValue)
            ? nameValue?.ToString()
            : null;
        var verified = decoded.Claims.TryGetValue("email_verified", out var verifiedValue)
            && bool.TryParse(verifiedValue?.ToString(), out var isVerified)
            && isVerified;

        return new FirebaseAuthResult(decoded.Uid, email, verified, name);
    }
}