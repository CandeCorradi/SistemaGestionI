using System.Net.Mail;

namespace SistemaGestion.Domain.Entities;

/// <summary>
/// Proveedor de la carpintería (functional-spec §7, BR-020: la terminología
/// nueva es "Proveedor", nunca "Mayorista"). Baja lógica con
/// <see cref="Activo"/>: desactivar conserva el historial de productos y
/// pedidos (BR-021).
/// </summary>
public class Proveedor : EntityBase
{
    public string Nombre { get; private set; } = string.Empty;

    /// <summary>Persona de contacto, opcional.</summary>
    public string? Contacto { get; private set; }

    public string? Email { get; private set; }

    public string? Telefono { get; private set; }

    public string? Direccion { get; private set; }

    public string? Notas { get; private set; }

    public bool Activo { get; private set; } = true;

    public DateTime FechaCreacion { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Crea un proveedor validando los datos obligatorios.
    /// El email es opcional; si existe se normaliza a minúsculas.
    /// </summary>
    public static Proveedor Crear(
        string nombre,
        string? contacto = null,
        string? email = null,
        string? telefono = null,
        string? direccion = null)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));
        }

        if (nombre.Trim().Length > 100)
        {
            throw new ArgumentException("El nombre no puede superar 100 caracteres.", nameof(nombre));
        }

        var emailNormalizado = NullIfBlank(email);
        if (emailNormalizado is not null)
        {
            emailNormalizado = NormalizeEmail(emailNormalizado);
            if (!EsEmailValido(emailNormalizado))
            {
                throw new ArgumentException("El email no tiene un formato válido.", nameof(email));
            }

            if (emailNormalizado.Length > 150)
            {
                throw new ArgumentException("El email no puede superar 150 caracteres.", nameof(email));
            }
        }

        return new Proveedor
        {
            Nombre = nombre.Trim(),
            Contacto = NullIfBlank(contacto),
            Email = emailNormalizado,
            Telefono = NullIfBlank(telefono),
            Direccion = NullIfBlank(direccion),
        };
    }

    /// <summary>
    /// Actualiza el proveedor con los valores provistos (mezcla tipo PATCH:
    /// los valores no nulos reemplazan a los actuales). Un string vacío
    /// limpia el campo opcional.
    /// </summary>
    public void Actualizar(
        string? nombre = null,
        string? contacto = null,
        string? email = null,
        string? telefono = null,
        string? direccion = null,
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

        if (contacto is not null)
        {
            Contacto = NullIfBlank(contacto);
        }

        if (email is not null)
        {
            var normalizado = NullIfBlank(email);
            if (normalizado is not null)
            {
                normalizado = NormalizeEmail(normalizado);
                if (!EsEmailValido(normalizado))
                {
                    throw new ArgumentException("El email no tiene un formato válido.", nameof(email));
                }

                if (normalizado.Length > 150)
                {
                    throw new ArgumentException("El email no puede superar 150 caracteres.", nameof(email));
                }
            }

            Email = normalizado;
        }

        if (telefono is not null)
        {
            Telefono = NullIfBlank(telefono);
        }

        if (direccion is not null)
        {
            Direccion = NullIfBlank(direccion);
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
    /// Desactivación lógica (BR-021): conserva el historial relacionado.
    /// </summary>
    public void Desactivar() => Activo = false;

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
