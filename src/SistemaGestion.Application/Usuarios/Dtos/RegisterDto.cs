namespace SistemaGestion.Application.Usuarios.Dtos;

/// <summary>
/// Solicitud de autorregistro de cuenta (Portal Cliente). El backend
/// asigna siempre el rol Cliente (BR-001: roles fijos, sin elección).
/// </summary>
public sealed record RegisterDto(string Email, string Password, string Nombre, string Apellido);

/// <summary>
/// Resultado del autorregistro. No expone el UID de Firebase.
/// </summary>
public sealed record RegisterResultDto(int Id, string Email, string Nombre, string Apellido, string Rol);
