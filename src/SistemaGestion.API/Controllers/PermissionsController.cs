using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGestion.API.Authorization;
using SistemaGestion.Application.Permisos;
using SistemaGestion.Application.Permisos.Dtos;
using SistemaGestion.Domain.Authorization;

namespace SistemaGestion.API.Controllers;

/// <summary>
/// Catálogo de permisos granulares (AGENTS.md §7 / BR-002).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/permissions")]
public class PermissionsController : ControllerBase
{
    private readonly PermisosService _permisosService;

    public PermissionsController(PermisosService permisosService)
    {
        _permisosService = permisosService;
    }

    /// <summary>
    /// Lista el catálogo completo de permisos (códigos UPPER_SNAKE).
    /// </summary>
    [HttpGet]
    [RequirePermission(Permisos.RolesView)]
    [ProducesResponseType(typeof(IReadOnlyList<PermisoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCatalog(CancellationToken cancellationToken)
    {
        var result = await _permisosService.GetCatalogAsync(cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : StatusCode(StatusCodes.Status500InternalServerError);
    }
}
