using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Application.Clientes.Dtos;

/// <summary>
/// Contrato de creación de clientes (functional-spec §6).
/// </summary>
public sealed record CreateClienteDto(
    string Nombre,
    string Apellido,
    string Email,
    string? Telefono = null,
    string? Direccion = null,
    string? Documento = null);

/// <summary>
/// Actualización parcial de un cliente: los campos no nulos reemplazan a los
/// actuales (PATCH). <c>Activo=false</c> desactiva sin borrar historial (BR-011).
/// </summary>
public sealed record UpdateClienteDto(
    string? Nombre = null,
    string? Apellido = null,
    string? Email = null,
    string? Telefono = null,
    string? Direccion = null,
    string? Documento = null,
    string? Notas = null,
    bool? Activo = null);

/// <summary>
/// Contrato público de la API para clientes (AGENTS.md §28: nunca exponer
/// entidades de persistencia).
/// </summary>
public sealed record ClienteDto
{
    public int Id { get; init; }

    public string Nombre { get; init; } = string.Empty;

    public string Apellido { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? Telefono { get; init; }

    public string? Direccion { get; init; }

    public string? Documento { get; init; }

    public string? Notas { get; init; }

    public string? FirebaseUid { get; init; }

    public bool Activo { get; init; }

    public DateTime FechaCreacion { get; init; }

    public static ClienteDto From(Cliente cliente) => new()
    {
        Id = cliente.Id,
        Nombre = cliente.Nombre,
        Apellido = cliente.Apellido,
        Email = cliente.Email,
        Telefono = cliente.Telefono,
        Direccion = cliente.Direccion,
        Documento = cliente.Documento,
        Notas = cliente.Notas,
        FirebaseUid = cliente.FirebaseUid,
        Activo = cliente.Activo,
        FechaCreacion = cliente.FechaCreacion,
    };
}

/// <summary>
/// Fila de listado de clientes (sin notas ni FirebaseUid: datos de detalle).
/// </summary>
public sealed record ClienteListDto
{
    public int Id { get; init; }

    public string Nombre { get; init; } = string.Empty;

    public string Apellido { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? Telefono { get; init; }

    public string? Documento { get; init; }

    public bool Activo { get; init; }

    public static ClienteListDto From(Cliente cliente) => new()
    {
        Id = cliente.Id,
        Nombre = cliente.Nombre,
        Apellido = cliente.Apellido,
        Email = cliente.Email,
        Telefono = cliente.Telefono,
        Documento = cliente.Documento,
        Activo = cliente.Activo,
    };
}
