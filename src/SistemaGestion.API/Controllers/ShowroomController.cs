using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGestion.Application.TrabajosRealizados;
using SistemaGestion.Application.TrabajosRealizados.Dtos;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.Controllers;

/// <summary>
/// Vidriera pública de trabajos realizados (AGENTS.md §23): listado y
/// detalle de obras publicadas, accesible sin autenticación. Sólo se
/// muestran trabajos publicados (BR-190) y el nombre del cliente se
/// oculta salvo consentimiento (BR-191).
/// </summary>
[ApiController]
[Route("api/v1/showroom/works")]
public class ShowroomController : ControllerBase
{
    private readonly TrabajosRealizadosService _service;

    public ShowroomController(TrabajosRealizadosService service)
    {
        _service = service;
    }

    /// <summary>
    /// Listado paginado de la vidriera con búsqueda, categoría y tipo
    /// de mueble. Destacados primero.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<ShowroomItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = TrabajosRealizadosService.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] string? type = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetShowroomPagedAsync(
            page, pageSize, search, category, type, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>Detalle público de un trabajo con su galería.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ShowroomTrabajoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _service.GetShowroomByIdAsync(id, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    private IActionResult ToHttpResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value);
        }

        var problem = new ProblemDetails
        {
            Title = result.Error.Code,
            Detail = result.Error.Description,
            Status = result.Error.Code switch
            {
                "Validation.Error" => StatusCodes.Status400BadRequest,
                "NotFound.Error" => StatusCodes.Status404NotFound,
                "Storage.Error" => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status500InternalServerError,
            },
            Instance = HttpContext.Request.Path,
        };

        return StatusCode(problem.Status.Value, problem);
    }
}
