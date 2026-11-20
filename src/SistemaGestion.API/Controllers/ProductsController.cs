using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGestion.API.Authorization;
using SistemaGestion.Application.Productos;
using SistemaGestion.Application.Productos.Dtos;
using SistemaGestion.Domain.Authorization;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.Controllers;

/// <summary>
/// Gestión de productos (api-spec §12, functional-spec §8). SKU único
/// como identidad de negocio (BR-030); DELETE desactiva sin borrar
/// historial (BR-032).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/products")]
public class ProductsController : ControllerBase
{
    private readonly ProductosService _productosService;

    public ProductsController(ProductosService productosService)
    {
        _productosService = productosService;
    }

    /// <summary>
    /// Lista productos con paginación, búsqueda y filtros (type, category,
    /// active, showroom, lowStock, isCustom).
    /// </summary>
    [HttpGet]
    [RequirePermission(Permisos.ProductosView)]
    [ProducesResponseType(typeof(PagedResult<ProductoListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = ProductosService.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] TipoProducto? type = null,
        [FromQuery] string? category = null,
        [FromQuery] bool? active = null,
        [FromQuery] bool? showroom = null,
        [FromQuery] bool? lowStock = null,
        [FromQuery] bool? isCustom = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _productosService.GetListAsync(
            page, pageSize, search, type, category, active, showroom, lowStock, isCustom, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Devuelve un producto por su identificador.
    /// </summary>
    [HttpGet("{id:int}")]
    [RequirePermission(Permisos.ProductosView)]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _productosService.GetByIdAsync(id, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Crea un producto. El SKU debe ser único (BR-030).
    /// </summary>
    [HttpPost]
    [RequirePermission(Permisos.ProductosCreate)]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductoDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _productosService.CreateAsync(dto, cancellationToken);

        return ToHttpResult(
            result,
            producto => CreatedAtAction(nameof(GetById), new { id = producto.Id }, producto));
    }

    /// <summary>
    /// Actualiza propiedades permitidas del producto (PATCH parcial).
    /// El stock actual no se modifica aquí: se gestiona con inventario.
    /// </summary>
    [HttpPatch("{id:int}")]
    [RequirePermission(Permisos.ProductosUpdate)]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateProductoDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _productosService.UpdateAsync(id, dto, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Desactiva el producto sin eliminar su historial (BR-032).
    /// </summary>
    [HttpDelete("{id:int}")]
    [RequirePermission(Permisos.ProductosDelete)]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var result = await _productosService.DeactivateAsync(id, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    // ------------------------------------------------------------------
    // Imágenes de producto (AGENTS.md §24, BR-100/BR-101)
    // ------------------------------------------------------------------

    /// <summary>
    /// Galería de imágenes del producto (URLs públicas del storage).
    /// </summary>
    [HttpGet("{id:int}/images")]
    [RequirePermission(Permisos.ProductosView)]
    [ProducesResponseType(typeof(IReadOnlyList<ProductoImagenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImages(int id, CancellationToken cancellationToken)
    {
        var result = await _productosService.GetImagesAsync(id, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Sube una imagen al storage y registra su referencia (BR-101).
    /// Valida extensión, MIME type y tamaño (≤ 5 MB) en backend.
    /// </summary>
    [HttpPost("{id:int}/images")]
    [RequestSizeLimit(MaxImageRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxImageRequestBytes)]
    [RequirePermission(Permisos.ProductosUpdate)]
    [ProducesResponseType(typeof(ProductoImagenDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadImage(
        int id,
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation.Error",
                detail: "Debés adjuntar un archivo de imagen.");
        }

        await using var stream = file.OpenReadStream();
        var result = await _productosService.AddImageAsync(
            id, file.FileName, file.ContentType, stream, file.Length, cancellationToken);

        return ToHttpResult(
            result,
            imagen => CreatedAtAction(nameof(GetImages), new { id }, imagen));
    }

    /// <summary>
    /// Elimina la imagen del storage y su referencia (idempotente).
    /// </summary>
    [HttpDelete("{id:int}/images/{imageId:int}")]
    [RequirePermission(Permisos.ProductosUpdate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> DeleteImage(
        int id,
        int imageId,
        CancellationToken cancellationToken)
    {
        var result = await _productosService.DeleteImageAsync(id, imageId, cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return ToHttpProblem(result.Error);
    }

    /// <summary>
    /// Tope del formulario multipart: imagen (5 MB) + margen de metadatos.
    /// </summary>
    private const long MaxImageRequestBytes = ProductosService.MaxImageBytes + 64 * 1024;

    private IActionResult ToHttpResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value);
        }

        return ToHttpProblem(result.Error);
    }

    private IActionResult ToHttpProblem(Error error)
    {
        var problem = new ProblemDetails
        {
            Title = error.Code,
            Detail = error.Description,
            Status = error.Code switch
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
