using System.Net.Mail;

namespace SistemaGestion.Domain.Entities;

/// <summary>
/// Usuario interno del sistema. La identidad externa se asocia mediante
/// <see cref="FirebaseUid"/> (único); roles y permisos se gestionan en
/// fases siguientes (AGENTS.md §6-9). No se elimina físicamente: se da de
/// baja lógica con <see cref="Activo"/> (functional-spec §5).
/// </summary>
public class Usuario : EntityBase
{
    public string? FirebaseUid { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string Nombre { get; private set; } = string.Empty;

    public string Apellido { get; private set; } = string.Empty;

    /// <summary>
    /// Rol fijo asignado (BR-001). Cada usuario tiene exactamente un rol
    /// (roadmap §10.2: Firebase UID → Usuario → Rol → Permisos).
    /// </summary>
    public int RolId { get; private set; }

    public bool Activo { get; private set; } = true;

    public DateTime FechaCreacion { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Crea un usuario validando los datos obligatorios.
    /// El email se normaliza a minúsculas para garantizar unicidad.
    /// </summary>
    public static Usuario Crear(
        string email,
        string nombre,
        string apellido,
        int rolId,
        string? firebaseUid = null)
    {
        email = NormalizeEmail(email);

        if (!EsEmailValido(email))
        {
            throw new ArgumentException("El email no tiene un formato válido.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));
        }

        if (string.IsNullOrWhiteSpace(apellido))
        {
            throw new ArgumentException("El apellido es obligatorio.", nameof(apellido));
        }

        if (rolId <= 0)
        {
            throw new ArgumentException("El rol es obligatorio.", nameof(rolId));
        }

        return new Usuario
        {
            Email = email,
            Nombre = nombre.Trim(),
            Apellido = apellido.Trim(),
            RolId = rolId,
            FirebaseUid = string.IsNullOrWhiteSpace(firebaseUid) ? null : firebaseUid.Trim(),
        };
    }

    /// <summary>
    /// Actualiza el perfil con los valores provistos (mezcla tipo PATCH:
    /// los valores no nulos reemplazan a los actuales).
    /// </summary>
    public void Actualizar(
        string? nombre = null,
        string? apellido = null,
        string? email = null,
        bool? activo = null,
        int? rolId = null)
    {
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            Nombre = nombre.Trim();
        }

        if (!string.IsNullOrWhiteSpace(apellido))
        {
            Apellido = apellido.Trim();
        }

        if (email is not null)
        {
            var normalized = NormalizeEmail(email);
            if (!EsEmailValido(normalized))
            {
                throw new ArgumentException("El email no tiene un formato válido.", nameof(email));
            }

            Email = normalized;
        }

        if (activo.HasValue)
        {
            Activo = activo.Value;
        }

        if (rolId.HasValue)
        {
            if (rolId.Value <= 0)
            {
                throw new ArgumentException("El rol es obligatorio.", nameof(rolId));
            }

            RolId = rolId.Value;
        }
    }

    /// <summary>
    /// Asocia el Firebase UID de la cuenta de autenticación externa.
    /// </summary>
    public void VincularFirebaseUid(string firebaseUid)
    {
        if (string.IsNullOrWhiteSpace(firebaseUid))
        {
            throw new ArgumentException("El Firebase UID es obligatorio.", nameof(firebaseUid));
        }

        FirebaseUid = firebaseUid.Trim();
    }

    private static string NormalizeEmail(string? email)
        => (email ?? string.Empty).Trim().ToLowerInvariant();

    private static bool EsEmailValido(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
