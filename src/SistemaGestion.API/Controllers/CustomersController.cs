using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGestion.API.Authorization;
using SistemaGestion.Application.Clientes;
using SistemaGestion.Application.Clientes.Dtos;
using SistemaGestion.Domain.Authorization;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.Controllers;

/// <summary>
/// Gestión de clientes de la carpintería (api-spec §10, functional-spec §6).
/// DELETE desactiva sin borrar historial (BR-011). Cada operación declara su
/// permiso granular (BR-003: la autorización siempre ocurre en backend).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/customers")]
public class CustomersController : ControllerBase
{
    private readonly ClientesService _clientesService;

    public CustomersController(ClientesService clientesService)
    {
        _clientesService = clientesService;
    }

    /// <summary>
    /// Lista clientes con paginación, búsqueda y filtro de activos.
    /// </summary>
    [HttpGet]
    [RequirePermission(Permisos.ClientesView)]
    [ProducesResponseType(typeof(PagedResult<ClienteListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = ClientesService.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] bool? active = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _clientesService.GetListAsync(page, pageSize, search, active, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Devuelve un cliente por su identificador.
    /// </summary>
    [HttpGet("{id:int}")]
    [RequirePermission(Permisos.ClientesView)]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _clientesService.GetByIdAsync(id, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Crea un cliente.
    /// </summary>
    [HttpPost]
    [RequirePermission(Permisos.ClientesCreate)]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateClienteDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _clientesService.CreateAsync(dto, cancellationToken);

        return ToHttpResult(
            result,
            cliente => CreatedAtAction(nameof(GetById), new { id = cliente.Id }, cliente));
    }

    /// <summary>
    /// Actualiza propiedades permitidas del cliente (PATCH parcial).
    /// </summary>
    [HttpPatch("{id:int}")]
    [RequirePermission(Permisos.ClientesUpdate)]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateClienteDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _clientesService.UpdateAsync(id, dto, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Desactiva el cliente sin eliminar su historial (BR-011).
    /// </summary>
    [HttpDelete("{id:int}")]
    [RequirePermission(Permisos.ClientesDelete)]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var result = await _clientesService.DeactivateAsync(id, cancellationToken);

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
