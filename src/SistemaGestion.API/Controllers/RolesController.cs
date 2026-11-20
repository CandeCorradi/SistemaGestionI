using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGestion.API.Authorization;
using SistemaGestion.Application.Roles;
using SistemaGestion.Application.Roles.Dtos;
using SistemaGestion.Domain.Authorization;

namespace SistemaGestion.API.Controllers;

/// <summary>
/// Lectura de los roles fijos del sistema (BR-001: no existen operaciones
/// de creación o edición de roles).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/roles")]
public class RolesController : ControllerBase
{
    private readonly RolesService _rolesService;

    public RolesController(RolesService rolesService)
    {
        _rolesService = rolesService;
    }

    /// <summary>
    /// Lista los cinco roles fijos: SuperAdmin, Owner, Admin, Empleado, Cliente.
    /// </summary>
    [HttpGet]
    [RequirePermission(Permisos.RolesView)]
    [ProducesResponseType(typeof(IReadOnlyList<RolDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _rolesService.GetAllAsync(cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : StatusCode(StatusCodes.Status500InternalServerError);
    }
}
