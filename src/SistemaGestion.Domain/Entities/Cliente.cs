using System.Net.Mail;

namespace SistemaGestion.Domain.Entities;

/// <summary>
/// Cliente de la carpintería (functional-spec §6). Baja lógica con
/// <see cref="Activo"/>: desactivar nunca borra presupuestos, pedidos ni
/// pagos históricos (BR-011). Puede vincularse a una cuenta del portal con
/// <see cref="FirebaseUid"/> (BR-010).
/// </summary>
public class Cliente : EntityBase
{
    public string Nombre { get; private set; } = string.Empty;

    public string Apellido { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string? Telefono { get; private set; }

    public string? Direccion { get; private set; }

    /// <summary>Número de documento (DNI/CUIT), opcional.</summary>
    public string? Documento { get; private set; }

    public string? Notas { get; private set; }

    /// <summary>UID de Firebase del portal cliente, único cuando existe (BR-010).</summary>
    public string? FirebaseUid { get; private set; }

    public bool Activo { get; private set; } = true;

    public DateTime FechaCreacion { get; private set; } = DateTime.UtcNow;

    public ICollection<Presupuesto> Presupuestos { get; private set; } = new List<Presupuesto>();

    /// <summary>
    /// Crea un cliente validando los datos obligatorios.
    /// El email se normaliza a minúsculas para garantizar unicidad.
    /// </summary>
    public static Cliente Crear(
        string nombre,
        string apellido,
        string email,
        string? telefono = null,
        string? direccion = null,
        string? documento = null,
        string? firebaseUid = null)
    {
        email = NormalizeEmail(email);

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));
        }

        if (string.IsNullOrWhiteSpace(apellido))
        {
            throw new ArgumentException("El apellido es obligatorio.", nameof(apellido));
        }

        if (!EsEmailValido(email))
        {
            throw new ArgumentException("El email no tiene un formato válido.", nameof(email));
        }

        if (nombre.Trim().Length > 100)
        {
            throw new ArgumentException("El nombre no puede superar 100 caracteres.", nameof(nombre));
        }

        if (apellido.Trim().Length > 100)
        {
            throw new ArgumentException("El apellido no puede superar 100 caracteres.", nameof(apellido));
        }

        return new Cliente
        {
            Nombre = nombre.Trim(),
            Apellido = apellido.Trim(),
            Email = email,
            Telefono = NullIfBlank(telefono),
            Direccion = NullIfBlank(direccion),
            Documento = NullIfBlank(documento),
            FirebaseUid = NullIfBlank(firebaseUid),
        };
    }

    /// <summary>
    /// Actualiza el cliente con los valores provistos (mezcla tipo PATCH:
    /// los valores no nulos reemplazan a los actuales). <paramref name="email"/>
    /// vacío no modifica el email actual.
    /// </summary>
    public void Actualizar(
        string? nombre = null,
        string? apellido = null,
        string? email = null,
        string? telefono = null,
        string? direccion = null,
        string? documento = null,
        string? notas = null,
        bool? activo = null)
    {
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            if (nombre.Trim().Length > 100)
            {
                throw new ArgumentException("El nombre no puede superar 100 caracteres.", nameof(nombre));
            }

            Nombre = nombre.Trim();
        }

        if (!string.IsNullOrWhiteSpace(apellido))
        {
            if (apellido.Trim().Length > 100)
            {
                throw new ArgumentException("El apellido no puede superar 100 caracteres.", nameof(apellido));
            }

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

        if (telefono is not null)
        {
            Telefono = NullIfBlank(telefono);
        }

        if (direccion is not null)
        {
            Direccion = NullIfBlank(direccion);
        }

        if (documento is not null)
        {
            Documento = NullIfBlank(documento);
        }

        if (notas is not null)
        {
            Notas = NullIfBlank(notas);
        }

        if (activo.HasValue)
        {
            Activo = activo.Value;
        }
    }

    /// <summary>
    /// Desactivación lógica (BR-011 / BR-160): conserva todo el historial.
    /// </summary>
    public void Desactivar() => Activo = false;

    /// <summary>
    /// Vincula la cuenta del portal cliente (BR-010: UID único).
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

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
