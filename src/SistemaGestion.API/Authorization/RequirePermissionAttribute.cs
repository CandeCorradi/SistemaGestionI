using Microsoft.AspNetCore.Authorization;
using SistemaGestion.Domain.Authorization;

namespace SistemaGestion.API.Authorization;

/// <summary>
/// Declara el permiso granular requerido por un endpoint.
/// Equivalente a [Authorize] + política "Permission:{codigo}".
/// El frontend puede ocultar botones según permisos, pero la seguridad real
/// es siempre esta validación en backend (BR-003).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            throw new ArgumentException("El código del permiso es obligatorio.", nameof(permissionCode));
        }

        var normalizado = permissionCode.Trim().ToUpperInvariant();
        if (!Permisos.Todos.Contains(normalizado, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"El permiso '{normalizado}' no existe en el catálogo (AGENTS.md §7).",
                nameof(permissionCode));
        }

        Policy = PermissionRequirement.GetPolicyName(normalizado);
    }
}
