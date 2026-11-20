namespace SistemaGestion.Domain.Entities;

/// <summary>
/// Permiso granular del sistema (AGENTS.md §7, BR-002). Catálogo fijo
/// definido en <see cref="Authorization.Permisos"/>; no se crean permisos
/// en tiempo de ejecución. Código UPPER_SNAKE (p. ej. CLIENTES_VIEW).
/// </summary>
public class Permiso : EntityBase
{
    public string Codigo { get; private set; } = string.Empty;

    public string Nombre { get; private set; } = string.Empty;

    /// <summary>
    /// Constructor para EF Core / seed.
    /// </summary>
    public Permiso(string codigo, string nombre)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ArgumentException("El código del permiso es obligatorio.", nameof(codigo));
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre del permiso es obligatorio.", nameof(nombre));
        }

        Codigo = codigo.Trim();
        Nombre = nombre.Trim();
    }
}
