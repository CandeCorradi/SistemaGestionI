using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGestion.API.Authorization;
using SistemaGestion.Application.Permisos;
using SistemaGestion.Application.Permisos.Dtos;
using SistemaGestion.Application.Usuarios;
using SistemaGestion.Application.Usuarios.Dtos;
using SistemaGestion.Domain.Authorization;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.Controllers;

/// <summary>
/// Gestión de usuarios internos y sus permisos explícitos (api-spec §9).
/// Cada operación declara su permiso granular (BR-003: la autorización
/// siempre ocurre en backend).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly UsuariosService _usuariosService;

    private readonly PermisosService _permisosService;

    public UsersController(UsuariosService usuariosService, PermisosService permisosService)
    {
        _usuariosService = usuariosService;
        _permisosService = permisosService;
    }

    /// <summary>
    /// Lista usuarios con paginación y búsqueda.
    /// </summary>
    [HttpGet]
    [RequirePermission(Permisos.UsuariosView)]
    [ProducesResponseType(typeof(PagedResult<UsuarioListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = UsuariosService.DefaultPageSize,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _usuariosService.GetListAsync(page, pageSize, search, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Devuelve un usuario por su identificador.
    /// </summary>
    [HttpGet("{id:int}")]
    [RequirePermission(Permisos.UsuariosView)]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _usuariosService.GetByIdAsync(id, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Crea un usuario interno.
    /// </summary>
    [HttpPost]
    [RequirePermission(Permisos.UsuariosCreate)]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUsuarioDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _usuariosService.CreateAsync(dto, cancellationToken);

        return ToHttpResult(
            result,
            usuario => CreatedAtAction(nameof(GetById), new { id = usuario.Id }, usuario));
    }

    /// <summary>
    /// Actualiza propiedades permitidas del usuario (PATCH parcial),
    /// incluida la activación/desactivación de la cuenta.
    /// </summary>
    [HttpPatch("{id:int}")]
    [RequirePermission(Permisos.UsuariosUpdate)]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateUsuarioDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _usuariosService.UpdateAsync(id, dto, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Devuelve los permisos explícitos del usuario (adicionales a los de
    /// su rol).
    /// </summary>
    [HttpGet("{id:int}/permissions")]
    [RequirePermission(Permisos.UsuariosView)]
    [ProducesResponseType(typeof(PermisosUsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPermissions(int id, CancellationToken cancellationToken)
    {
        var result = await _permisosService.GetUsuarioPermisosAsync(id, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Reemplaza los permisos explícitos del usuario (api-spec §9).
    /// Los códigos deben pertenecer al catálogo (BR-002).
    /// </summary>
    [HttpPut("{id:int}/permissions")]
    [RequirePermission(Permisos.UsuariosUpdate)]
    [ProducesResponseType(typeof(PermisosUsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePermissions(
        int id,
        [FromBody] UpdatePermisosUsuarioDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _permisosService.UpdateUsuarioPermisosAsync(id, dto, cancellationToken);

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
