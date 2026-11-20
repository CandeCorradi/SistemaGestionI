using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Abstractions;

/// <summary>
/// Contrato para administrar cuentas de usuarios en Firebase
/// Authentication con el Admin SDK. Lo usa el autorregistro: si la
/// persistencia interna falla, se invoca el rollback de la cuenta.
/// </summary>
public interface IFirebaseUserService
{
    /// <summary>
    /// Crea la cuenta en Firebase Authentication y devuelve el UID.
    /// <c>Conflict.Error</c> cuando el email ya está registrado;
    /// <c>Validation.Error</c> cuando Firebase rechaza los datos de
    /// entrada; cualquier otro error lanza excepción (500).
    /// </summary>
    Task<Result<string>> CreateAuthUserAsync(
        string email,
        string password,
        string nombre,
        string apellido,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina la cuenta (rollback cuando no se pudo persistir el
    /// usuario interno).
    /// </summary>
    Task DeleteAuthUserAsync(string firebaseUid, CancellationToken cancellationToken = default);
}
