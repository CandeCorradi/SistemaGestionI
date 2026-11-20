using System.Text;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;

namespace SistemaGestion.Infrastructure.Authentication.Firebase;

/// <summary>
/// Inicializa FirebaseApp en el arranque si hay credenciales configuradas.
/// La ausencia de configuración no impide que la aplicación se ejecute:
/// los endpoints protegidos rechazarán la autenticación.
/// </summary>
public static class FirebaseConfigurator
{
    public static bool TryInitialize(FirebaseOptions options, ILogger logger)
    {
        if (!options.IsConfigured)
        {
            logger.LogWarning(
                "Firebase no está configurado (FIREBASE_PROJECT_ID / FIREBASE_SERVICE_ACCOUNT_JSON). " +
                "El arranque continúa pero la autenticación y el storage no estarán disponibles.");
            return false;
        }

        if (FirebaseApp.DefaultInstance is not null)
        {
            logger.LogInformation("Firebase ya estaba inicializado.");
            return true;
        }

        try
        {
            var credential = BuildCredential(options);
            FirebaseApp.Create(new AppOptions
            {
                Credential = credential,
                ProjectId = string.IsNullOrWhiteSpace(options.ProjectId) ? null : options.ProjectId,
            });

            logger.LogInformation("Firebase inicializado correctamente (proyecto {ProjectId}).", options.ProjectId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo inicializar Firebase. La autenticación quedará inhabilitada.");
            return false;
        }
    }

    private static GoogleCredential BuildCredential(FirebaseOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ServiceAccountJson))
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(options.ServiceAccountJson));
            return CredentialFactory.FromStream<ServiceAccountCredential>(stream).ToGoogleCredential();
        }

        using var rsa = System.Security.Cryptography.RSA.Create();
        rsa.ImportFromPem(options.PrivateKey);

        return GoogleCredential.FromServiceAccountCredential(
            new ServiceAccountCredential(
                new ServiceAccountCredential.Initializer(options.ClientEmail) { Key = rsa }));
    }
}