using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGestion.Application.Productos;
using SistemaGestion.Application.Productos.Dtos;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.Controllers;

/// <summary>
/// Catálogo público (functional-spec §9, roadmap §13.4): listado y
/// detalle de productos activos con datos de exhibición e imágenes
/// (Supabase Storage, BR-100). Accesible sin autenticación — no expone
/// costo, stock numérico, proveedor ni estado interno (BR-004).
/// </summary>
[ApiController]
[Route("api/v1/catalog/products")]
public class CatalogController : ControllerBase
{
    private readonly ProductosService _productosService;

    public CatalogController(ProductosService productosService)
    {
        _productosService = productosService;
    }

    /// <summary>
    /// Listado paginado del catálogo con búsqueda, tipo y categoría.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<CatalogoItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = ProductosService.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] TipoProducto? type = null,
        [FromQuery] string? category = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _productosService.GetCatalogPagedAsync(
            page, pageSize, search, type, category, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Detalle público de un producto con galería de imágenes.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CatalogoProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _productosService.GetCatalogByIdAsync(id, cancellationToken);

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
                "Storage.Error" => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status500InternalServerError,
            },
            Instance = HttpContext.Request.Path,
        };

        return StatusCode(problem.Status.Value, problem);
    }
}
