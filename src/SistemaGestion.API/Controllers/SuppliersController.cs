using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGestion.API.Authorization;
using SistemaGestion.Application.Proveedores;
using SistemaGestion.Application.Proveedores.Dtos;
using SistemaGestion.Domain.Authorization;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.Controllers;

/// <summary>
/// Gestión de proveedores (api-spec §11, functional-spec §7). BR-020: el
/// término de dominio es "Proveedor"; la API expone la ruta canónica
/// <c>suppliers</c> definida en api-spec §11 y nunca "mayoristas".
/// DELETE desactiva sin borrar historial (BR-021).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/suppliers")]
public class SuppliersController : ControllerBase
{
    private readonly ProveedoresService _proveedoresService;

    public SuppliersController(ProveedoresService proveedoresService)
    {
        _proveedoresService = proveedoresService;
    }

    /// <summary>
    /// Lista proveedores con paginación, búsqueda y filtro de activos.
    /// </summary>
    [HttpGet]
    [RequirePermission(Permisos.ProveedoresView)]
    [ProducesResponseType(typeof(PagedResult<ProveedorListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = ProveedoresService.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] bool? active = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _proveedoresService.GetListAsync(page, pageSize, search, active, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Devuelve un proveedor por su identificador.
    /// </summary>
    [HttpGet("{id:int}")]
    [RequirePermission(Permisos.ProveedoresView)]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _proveedoresService.GetByIdAsync(id, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Crea un proveedor.
    /// </summary>
    [HttpPost]
    [RequirePermission(Permisos.ProveedoresCreate)]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProveedorDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _proveedoresService.CreateAsync(dto, cancellationToken);

        return ToHttpResult(
            result,
            proveedor => CreatedAtAction(nameof(GetById), new { id = proveedor.Id }, proveedor));
    }

    /// <summary>
    /// Actualiza propiedades permitidas del proveedor (PATCH parcial).
    /// </summary>
    [HttpPatch("{id:int}")]
    [RequirePermission(Permisos.ProveedoresUpdate)]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateProveedorDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _proveedoresService.UpdateAsync(id, dto, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Desactiva el proveedor sin eliminar su historial (BR-021).
    /// </summary>
    [HttpDelete("{id:int}")]
    [RequirePermission(Permisos.ProveedoresDelete)]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var result = await _proveedoresService.DeactivateAsync(id, cancellationToken);

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
                "Conflict.Error" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError,
            },
            Instance = HttpContext.Request.Path,
        };

        return StatusCode(problem.Status.Value, problem);
    }
}
