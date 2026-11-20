namespace SistemaGestion.Application.Usuarios.Dtos;

/// <summary>
/// Datos de creación de un usuario interno. El rol es obligatorio:
/// cada usuario tiene exactamente un rol fijo (BR-001, roadmap §10.2).
/// </summary>
public sealed record CreateUsuarioDto(
    string Email,
    string Nombre,
    string Apellido,
    int RolId,
    string? FirebaseUid = null);

/// <summary>
/// Actualización parcial (PATCH): solo se aplican los campos no nulos.
/// </summary>
public sealed record UpdateUsuarioDto(
    string? Nombre = null,
    string? Apellido = null,
    string? Email = null,
    bool? Activo = null,
    int? RolId = null);
