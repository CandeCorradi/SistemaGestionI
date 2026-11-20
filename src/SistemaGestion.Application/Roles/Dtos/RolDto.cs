using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Application.Roles.Dtos;

/// <summary>
/// Contrato público de un rol fijo (BR-001).
/// </summary>
public sealed record RolDto
{
    public int Id { get; init; }

    public string Codigo { get; init; } = string.Empty;

    public string Nombre { get; init; } = string.Empty;

    public static RolDto From(Rol rol) => new()
    {
        Id = rol.Id,
        Codigo = rol.Codigo,
        Nombre = rol.Nombre,
    };
}
