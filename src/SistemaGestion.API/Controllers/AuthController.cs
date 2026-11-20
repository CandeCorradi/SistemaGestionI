using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SistemaGestion.Application.Permisos;
using SistemaGestion.Application.Permisos.Dtos;
using SistemaGestion.Application.Usuarios;
using SistemaGestion.Application.Usuarios.Dtos;

namespace SistemaGestion.API.Controllers;

/// <summary>
/// Endpoints de autenticación. El login/logout/recuperación de contraseña los
/// gestiona Firebase en el frontend; el backend únicamente valida el ID Token
/// y deriva la identidad del mismo (spec §3, functional-spec §4). El
/// autorregistro sí es un endpoint del backend: crea la cuenta en Firebase
/// y el usuario interno con rol Cliente.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly PermisosService _permisosService;

    private readonly UsuariosService _usuariosService;

    public AuthController(PermisosService permisosService, UsuariosService usuariosService)
    {
        _permisosService = permisosService;
        _usuariosService = usuariosService;
    }

    /// <summary>
    /// Devuelve la identidad autenticada del Firebase ID Token.
    /// Requiere: Authorization: Bearer &lt;firebase-id-token&gt;
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var emailVerified = User.FindFirstValue("email_verified") == "true";

        return Ok(new { uid, email, emailVerified });
    }

    /// <summary>
    /// Autorregistro de cuenta (Portal Cliente): crea la cuenta en
    /// Firebase Authentication y el usuario interno con rol Cliente.
    /// Endpoint público con rate limit por IP (AGENTS.md §45).
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(RegisterResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register(RegisterDto dto, CancellationToken cancellationToken)
    {
        var result = await _usuariosService.RegisterAsync(dto, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var status = result.Error.Code switch
        {
            "Validation.Error" => StatusCodes.Status400BadRequest,
            "Conflict.Error" => StatusCodes.Status409Conflict,
            "NotFound.Error" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError,
        };

        return StatusCode(status, new ProblemDetails
        {
            Title = result.Error.Code,
            Detail = result.Error.Description,
            Status = status,
            Instance = HttpContext.Request.Path,
        });
    }

    /// <summary>
    /// Aprovisiona la cuenta interna de una identidad externa ya autenticada
    /// (Google). El cliente obtiene el Firebase ID Token con el SDK web y
    /// llama a este endpoint; el backend deriva el UID y el email del token
    /// verificado (nunca de datos enviados por el cliente) y crea o vincula
    /// el usuario interno con rol Cliente. Es idempotente.
    /// </summary>
    [HttpPost("google")]
    [Authorize]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ProvisionGoogle(
        CancellationToken cancellationToken)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var displayName = User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue("name");

        var result = await _usuariosService.ProvisionExternalAsync(
            uid, email, displayName, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var status = result.Error.Code switch
        {
            "Validation.Error" => StatusCodes.Status400BadRequest,
            "Conflict.Error" => StatusCodes.Status409Conflict,
            "NotFound.Error" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError,
        };

        return StatusCode(status, new ProblemDetails
        {
            Title = result.Error.Code,
            Detail = result.Error.Description,
            Status = status,
            Instance = HttpContext.Request.Path,
        });
    }

    /// <summary>
    /// Devuelve los permisos efectivos del usuario autenticado (unión de
    /// permisos del rol y permisos explícitos). El frontend los usa para
    /// ocultar funcionalidades; la seguridad real sigue siendo del backend
    /// (BR-003).
    /// </summary>
    [HttpGet("permissions")]
    [Authorize]
    [ProducesResponseType(typeof(PermisosActualesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyPermissions(CancellationToken cancellationToken)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _permisosService.GetEffectivePermisosAsync(uid, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return NotFound(new ProblemDetails
        {
            Title = result.Error.Code,
            Detail = result.Error.Description,
            Status = StatusCodes.Status404NotFound,
            Instance = HttpContext.Request.Path,
        });
    }

    /// <summary>
    /// Devuelve el perfil del usuario interno vinculado al token actual
    /// (roadmap §18 — Profile): email interno, nombre, apellido y rol.
    /// 404 si el uid no tiene usuario interno activo.
    /// </summary>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(AuthProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _usuariosService.GetProfileAsync(uid, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return NotFound(new ProblemDetails
        {
            Title = result.Error.Code,
            Detail = result.Error.Description,
            Status = StatusCodes.Status404NotFound,
            Instance = HttpContext.Request.Path,
        });
    }
}
