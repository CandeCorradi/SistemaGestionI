using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Application.Usuarios.Dtos;

/// <summary>
/// Contrato público de la API para usuarios (AGENTS.md §28: nunca exponer
/// entidades de persistencia).
/// </summary>
public sealed record UsuarioDto
{
    public int Id { get; init; }

    public string Email { get; init; } = string.Empty;

    public string Nombre { get; init; } = string.Empty;

    public string Apellido { get; init; } = string.Empty;

    public bool Activo { get; init; }

    public int RolId { get; init; }

    public string? FirebaseUid { get; init; }

    public DateTime FechaCreacion { get; init; }

    public static UsuarioDto From(Usuario usuario) => new()
    {
        Id = usuario.Id,
        Email = usuario.Email,
        Nombre = usuario.Nombre,
        Apellido = usuario.Apellido,
        Activo = usuario.Activo,
        RolId = usuario.RolId,
        FirebaseUid = usuario.FirebaseUid,
        FechaCreacion = usuario.FechaCreacion,
    };
}

/// <summary>
/// Perfil del usuario interno autenticado (roadmap §18 — Profile). Se
/// devuelve junto con la identidad de Firebase (<c>/auth/me</c>).
/// </summary>
public sealed record AuthProfileDto
{
    public string Email { get; init; } = string.Empty;

    public string Nombre { get; init; } = string.Empty;

    public string Apellido { get; init; } = string.Empty;

    /// <summary>Nombre visible del rol (BR-001).</summary>
    public string Rol { get; init; } = string.Empty;

    public bool Activo { get; init; }
}

/// <summary>
/// Fila de listado de usuarios (sin FirebaseUid: dato sensible para listados).
/// </summary>
public sealed record UsuarioListDto
{
    public int Id { get; init; }

    public string Email { get; init; } = string.Empty;

    public string Nombre { get; init; } = string.Empty;

    public string Apellido { get; init; } = string.Empty;

    public bool Activo { get; init; }

    public int RolId { get; init; }

    public static UsuarioListDto From(Usuario usuario) => new()
    {
        Id = usuario.Id,
        Email = usuario.Email,
        Nombre = usuario.Nombre,
        Apellido = usuario.Apellido,
        Activo = usuario.Activo,
        RolId = usuario.RolId,
    };
}
