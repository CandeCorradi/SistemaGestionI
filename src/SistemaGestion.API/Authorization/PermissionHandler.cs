using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using SistemaGestion.Application.Abstractions;

namespace SistemaGestion.API.Authorization;

/// <summary>
/// Evalúa <see cref="PermissionRequirement"/> contra los permisos efectivos
/// del usuario interno identificado por el Firebase UID del token
/// (AGENTS.md §8: la identidad y permisos reales los determina el backend).
/// Si no hay usuario interno activo asociado, la autorización se deniega.
/// </summary>
public sealed class PermissionHandler
    : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermisoRepository _permisoRepository;

    private readonly ILogger<PermissionHandler> _logger;

    public PermissionHandler(IPermisoRepository permisoRepository, ILogger<PermissionHandler> logger)
    {
        _permisoRepository = permisoRepository;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var firebaseUid = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(firebaseUid))
        {
            // Sin identidad verificada no hay permisos posibles.
            return;
        }

        var permisos = await _permisoRepository.GetEffectiveCodesByFirebaseUidAsync(firebaseUid);

        if (permisos is not null
            && permisos.Contains(requirement.PermissionCode, StringComparer.Ordinal))
        {
            context.Succeed(requirement);
            return;
        }

        _logger.LogWarning(
            "AUTHORIZATION_DENIED: uid={Uid} no tiene el permiso {Permission}",
            firebaseUid,
            requirement.PermissionCode);
    }
}
