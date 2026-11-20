using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGestion.Application.Dashboard;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.Controllers;

/// <summary>
/// Dashboard administrativo (api-spec §19): indicadores de negocio
/// filtrados por los permisos efectivos del usuario autenticado. El
/// bloque de notificaciones son datos propios del token (BR-090).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;

    public DashboardController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>Indicadores agregados permitidos para el usuario actual.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var firebaseUid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _dashboardService.GetAsync(
            firebaseUid ?? string.Empty, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: result.Error.Code,
            detail: result.Error.Description);
    }
}
