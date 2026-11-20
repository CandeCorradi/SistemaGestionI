using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Application.Proveedores.Dtos;

/// <summary>
/// Contrato de creación de proveedores (functional-spec §7).
/// </summary>
public sealed record CreateProveedorDto(
    string Nombre,
    string? Contacto = null,
    string? Email = null,
    string? Telefono = null,
    string? Direccion = null);

/// <summary>
/// Actualización parcial de un proveedor: los campos no nulos reemplazan a
/// los actuales (PATCH). <c>Activo=false</c> desactiva sin borrar
/// historial (BR-021).
/// </summary>
public sealed record UpdateProveedorDto(
    string? Nombre = null,
    string? Contacto = null,
    string? Email = null,
    string? Telefono = null,
    string? Direccion = null,
    string? Notas = null,
    bool? Activo = null);

/// <summary>
/// Contrato público de la API para proveedores (AGENTS.md §28: nunca
/// exponer entidades de persistencia; BR-020: término "Proveedor").
/// </summary>
public sealed record ProveedorDto
{
    public int Id { get; init; }

    public string Nombre { get; init; } = string.Empty;

    public string? Contacto { get; init; }

    public string? Email { get; init; }

    public string? Telefono { get; init; }

    public string? Direccion { get; init; }

    public string? Notas { get; init; }

    public bool Activo { get; init; }

    public DateTime FechaCreacion { get; init; }

    public static ProveedorDto From(Proveedor proveedor) => new()
    {
        Id = proveedor.Id,
        Nombre = proveedor.Nombre,
        Contacto = proveedor.Contacto,
        Email = proveedor.Email,
        Telefono = proveedor.Telefono,
        Direccion = proveedor.Direccion,
        Notas = proveedor.Notas,
        Activo = proveedor.Activo,
        FechaCreacion = proveedor.FechaCreacion,
    };
}

/// <summary>
/// Fila de listado de proveedores (sin notas: dato de detalle).
/// </summary>
public sealed record ProveedorListDto
{
    public int Id { get; init; }

    public string Nombre { get; init; } = string.Empty;

    public string? Contacto { get; init; }

    public string? Email { get; init; }

    public string? Telefono { get; init; }

    public bool Activo { get; init; }

    public static ProveedorListDto From(Proveedor proveedor) => new()
    {
        Id = proveedor.Id,
        Nombre = proveedor.Nombre,
        Contacto = proveedor.Contacto,
        Email = proveedor.Email,
        Telefono = proveedor.Telefono,
        Activo = proveedor.Activo,
    };
}
