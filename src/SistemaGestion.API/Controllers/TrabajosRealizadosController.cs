using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGestion.API.Authorization;
using SistemaGestion.Application.TrabajosRealizados;
using SistemaGestion.Application.TrabajosRealizados.Dtos;
using SistemaGestion.Domain.Authorization;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.Controllers;

/// <summary>
/// Gestión administrativa de trabajos realizados / vidriera (AGENTS.md
/// §23): CRUD, publicación y galería de imágenes en Supabase Storage.
/// La exhibición pública vive en <see cref="ShowroomController"/>.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/trabajos-realizados")]
public class TrabajosRealizadosController : ControllerBase
{
    private readonly TrabajosRealizadosService _service;

    public TrabajosRealizadosController(TrabajosRealizadosService service)
    {
        _service = service;
    }

    /// <summary>
    /// Lista trabajos con paginación, búsqueda y filtros (published,
    /// featured, category).
    /// </summary>
    [HttpGet]
    [RequirePermission(Permisos.TrabajosView)]
    [ProducesResponseType(typeof(PagedResult<TrabajoRealizadoListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = TrabajosRealizadosService.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] bool? published = null,
        [FromQuery] bool? featured = null,
        [FromQuery] string? category = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetListAsync(
            page, pageSize, search, published, featured, category, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>Devuelve un trabajo con su galería de imágenes.</summary>
    [HttpGet("{id:int}")]
    [RequirePermission(Permisos.TrabajosView)]
    [ProducesResponseType(typeof(TrabajoRealizadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>Crea un trabajo realizado.</summary>
    [HttpPost]
    [RequirePermission(Permisos.TrabajosCreate)]
    [ProducesResponseType(typeof(TrabajoRealizadoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTrabajoRealizadoDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(dto, cancellationToken);

        return ToHttpResult(
            result,
            trabajo => CreatedAtAction(nameof(GetById), new { id = trabajo.Id }, trabajo));
    }

    /// <summary>
    /// Actualiza propiedades permitidas del trabajo (PATCH parcial),
    /// incluida la publicación / ocultamiento de la vidriera.
    /// </summary>
    [HttpPatch("{id:int}")]
    [RequirePermission(Permisos.TrabajosUpdate)]
    [ProducesResponseType(typeof(TrabajoRealizadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateTrabajoRealizadoDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(id, dto, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Elimina el trabajo y sus imágenes (la vidriera no es historial
    /// crítico, BR-161; los objetos del storage se limpian best-effort).
    /// </summary>
    [HttpDelete("{id:int}")]
    [RequirePermission(Permisos.TrabajosDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(id, cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return ToHttpProblem(result.Error);
    }

    // ------------------------------------------------------------------
    // Imágenes (AGENTS.md §24, BR-100/BR-101)
    // ------------------------------------------------------------------

    /// <summary>Galería de imágenes del trabajo (URLs públicas).</summary>
    [HttpGet("{id:int}/images")]
    [RequirePermission(Permisos.TrabajosView)]
    [ProducesResponseType(typeof(IReadOnlyList<TrabajoRealizadoImagenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImages(int id, CancellationToken cancellationToken)
    {
        var result = await _service.GetImagesAsync(id, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Sube una imagen al storage y registra su referencia (BR-101).
    /// Valida extensión, MIME type y tamaño (≤ 5 MB) en backend.
    /// </summary>
    [HttpPost("{id:int}/images")]
    [RequestSizeLimit(MaxImageRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxImageRequestBytes)]
    [RequirePermission(Permisos.TrabajosUpdate)]
    [ProducesResponseType(typeof(TrabajoRealizadoImagenDto), StatusCodes.Status201Created)]
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
        var result = await _service.AddImageAsync(
            id, file.FileName, file.ContentType, stream, file.Length, cancellationToken);

        return ToHttpResult(
            result,
            imagen => CreatedAtAction(nameof(GetImages), new { id }, imagen));
    }

    /// <summary>Elimina la imagen del storage y su referencia.</summary>
    [HttpDelete("{id:int}/images/{imageId:int}")]
    [RequirePermission(Permisos.TrabajosUpdate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> DeleteImage(
        int id,
        int imageId,
        CancellationToken cancellationToken)
    {
        var result = await _service.DeleteImageAsync(id, imageId, cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return ToHttpProblem(result.Error);
    }

    /// <summary>
    /// Tope del formulario multipart: imagen (5 MB) + margen de metadatos.
    /// </summary>
    private const long MaxImageRequestBytes = TrabajosRealizadosService.MaxImageBytes + 64 * 1024;

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
