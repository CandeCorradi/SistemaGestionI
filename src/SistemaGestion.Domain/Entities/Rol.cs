namespace SistemaGestion.Domain.Entities;

/// <summary>
/// Rol fijo del sistema. Exactamente cinco roles, no se crean dinámicamente
/// (BR-001 / AGENTS.md §6). Una rol es una colección de permisos (BR-002);
/// los permisos se asocian en la fase de autorización.
/// </summary>
public class Rol : EntityBase
{
    public const string SuperAdminCodigo = "SuperAdmin";
    public const string OwnerCodigo = "Owner";
    public const string AdminCodigo = "Admin";
    public const string EmpleadoCodigo = "Empleado";
    public const string ClienteCodigo = "Cliente";

    public string Codigo { get; private set; } = string.Empty;

    public string Nombre { get; private set; } = string.Empty;

    /// <summary>
    /// Constructor para EF Core / seed. El sistema no expone creación de
    /// roles (BR-001).
    /// </summary>
    public Rol(string codigo, string nombre)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ArgumentException("El código del rol es obligatorio.", nameof(codigo));
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre del rol es obligatorio.", nameof(nombre));
        }

        Codigo = codigo.Trim();
        Nombre = nombre.Trim();
    }
}
