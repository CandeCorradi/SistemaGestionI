using Microsoft.AspNetCore.Authorization;

namespace SistemaGestion.API.Authorization;

/// <summary>
/// Requisito de autorización que exige un permiso granular concreto
/// (p. ej. CLIENTES_VIEW). Las políticas se registran por cada código del
/// catálogo con el prefijo "Permission:" (AGENTS.md §7-8, BR-002/BR-003).
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public const string PolicyPrefix = "Permission:";

    public PermissionRequirement(string permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            throw new ArgumentException("El código del permiso es obligatorio.", nameof(permissionCode));
        }

        PermissionCode = permissionCode.Trim().ToUpperInvariant();
    }

    public string PermissionCode { get; }

    public static string GetPolicyName(string permissionCode)
        => PolicyPrefix + permissionCode.Trim().ToUpperInvariant();
}
