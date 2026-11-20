using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Microsoft.Extensions.Logging;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Infrastructure.Authentication.Firebase;

/// <summary>
/// Administra cuentas de usuarios con Firebase Admin SDK (autorregistro).
/// Requiere que FirebaseApp haya sido inicializado por FirebaseConfigurator.
/// </summary>
public sealed class FirebaseUserService : IFirebaseUserService
{
    private readonly ILogger<FirebaseUserService> _logger;

    public FirebaseUserService(ILogger<FirebaseUserService> logger)
    {
        _logger = logger;
    }

    public async Task<Result<string>> CreateAuthUserAsync(
        string email,
        string password,
        string nombre,
        string apellido,
        CancellationToken cancellationToken = default)
    {
        var app = FirebaseApp.DefaultInstance;
        if (app is null)
        {
            throw new InvalidOperationException("Firebase no está inicializado.");
        }

        try
        {
            var record = await FirebaseAuth.GetAuth(app).CreateUserAsync(
                new UserRecordArgs
                {
                    Email = email,
                    Password = password,
                    DisplayName = $"{nombre} {apellido}".Trim(),
                },
                cancellationToken);

            return record.Uid;
        }
        catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.EmailAlreadyExists)
        {
            return Error.Conflict($"Ya existe una cuenta con el email {email}.");
        }
        catch (FirebaseAuthException ex)
        {
            _logger.LogError(ex, "FIREBASE_USER_CREATE_FAILED: {ErrorCode}", ex.ErrorCode);
            throw new InvalidOperationException(
                "No se pudo crear la cuenta en Firebase Authentication.", ex);
        }
    }

    public async Task DeleteAuthUserAsync(string firebaseUid, CancellationToken cancellationToken = default)
    {
        var app = FirebaseApp.DefaultInstance;
        if (app is null)
        {
            throw new InvalidOperationException("Firebase no está inicializado.");
        }

        await FirebaseAuth.GetAuth(app).DeleteUserAsync(firebaseUid, cancellationToken);
    }
}
