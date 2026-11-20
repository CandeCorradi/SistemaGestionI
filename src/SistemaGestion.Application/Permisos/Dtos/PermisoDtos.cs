namespace SistemaGestion.Application.Permisos.Dtos;

/// <summary>
/// Contrato público de un permiso del catálogo (AGENTS.md §7, BR-002).
/// </summary>
public sealed record PermisoDto
{
    public int Id { get; init; }

    public string Codigo { get; init; } = string.Empty;

    public string Nombre { get; init; } = string.Empty;
}

/// <summary>
/// Permisos explícitos de un usuario (adicionales a los de su rol).
/// </summary>
public sealed record PermisosUsuarioDto
{
    public int UsuarioId { get; init; }

    /// <summary>
    /// Códigos explícitos asignados directamente al usuario.
    /// </summary>
    public IReadOnlyList<string> Codigos { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Permisos efectivos del usuario autenticado (unión rol ∪ explícitos).
/// </summary>
public sealed record PermisosActualesDto
{
    /// <summary>
    /// Códigos efectivos usados por el frontend para ocultar
    /// funcionalidades (la seguridad real sigue siendo del backend).
    /// </summary>
    public IReadOnlyList<string> Codigos { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Reemplazo del conjunto de permisos explícitos de un usuario (PUT).
/// </summary>
public sealed record UpdatePermisosUsuarioDto
{
    /// <summary>
    /// Nuevos códigos explícitos. La lista reemplaza por completo la
    /// anterior (vacía = sin permisos explícitos).
    /// </summary>
    public IReadOnlyList<string>? Codigos { get; init; }
}
