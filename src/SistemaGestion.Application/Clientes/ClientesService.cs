using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Clientes.Dtos;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Clientes;

/// <summary>
/// Casos de uso de clientes (functional-spec §6): alta, consulta,
/// búsqueda con paginación, edición parcial y desactivación lógica
/// (BR-011). Las validaciones críticas se ejecutan siempre aquí, no solo
/// en el frontend (AGENTS.md §31).
/// </summary>
public sealed class ClientesService
{
    /// <summary>
    /// Tamaño de página máximo permitido (AGENTS.md §30: sin páginas ilimitadas).
    /// </summary>
    public const int MaxPageSize = 100;

    public const int DefaultPageSize = 20;

    private readonly IClienteRepository _repository;

    public ClientesService(IClienteRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResult<ClienteListDto>>> GetListAsync(
        int page,
        int pageSize,
        string? search,
        bool? activo,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var paged = await _repository.GetPagedAsync(page, pageSize, search, activo, cancellationToken);

        return new PagedResult<ClienteListDto>
        {
            Items = paged.Items.Select(ClienteListDto.From).ToList(),
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalItems = paged.TotalItems,
        };
    }

    public async Task<Result<ClienteDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cliente = await _repository.GetByIdAsync(id, cancellationToken);
        if (cliente is null)
        {
            return Error.NotFound($"El cliente {id} no existe.");
        }

        return ClienteDto.From(cliente);
    }

    public async Task<Result<ClienteDto>> CreateAsync(
        CreateClienteDto dto,
        CancellationToken cancellationToken = default)
    {
        var validationError = Validate(dto.Nombre, dto.Apellido, dto.Email);
        if (validationError is not null)
        {
            return validationError;
        }

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _repository.EmailExistsAsync(email, excludeId: null, cancellationToken))
        {
            return Error.Conflict($"Ya existe un cliente con el email {email}.");
        }

        Cliente cliente;
        try
        {
            cliente = Cliente.Crear(
                dto.Nombre,
                dto.Apellido,
                dto.Email,
                dto.Telefono,
                dto.Direccion,
                dto.Documento);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(ex.Message);
        }

        await _repository.AddAsync(cliente, cancellationToken);

        return ClienteDto.From(cliente);
    }

    public async Task<Result<ClienteDto>> UpdateAsync(
        int id,
        UpdateClienteDto dto,
        CancellationToken cancellationToken = default)
    {
        var cliente = await _repository.GetByIdAsync(id, cancellationToken);
        if (cliente is null)
        {
            return Error.NotFound($"El cliente {id} no existe.");
        }

        if (dto.Email is not null)
        {
            var validationError = Validate(
                dto.Nombre ?? cliente.Nombre,
                dto.Apellido ?? cliente.Apellido,
                dto.Email);
            if (validationError is not null)
            {
                return validationError;
            }

            var email = dto.Email.Trim().ToLowerInvariant();
            if (!string.Equals(email, cliente.Email, StringComparison.Ordinal)
                && await _repository.EmailExistsAsync(email, id, cancellationToken))
            {
                return Error.Conflict($"Ya existe un cliente con el email {email}.");
            }
        }
        else if (dto.Nombre is not null || dto.Apellido is not null)
        {
            var validationError = Validate(
                dto.Nombre ?? cliente.Nombre,
                dto.Apellido ?? cliente.Apellido,
                cliente.Email);
            if (validationError is not null)
            {
                return validationError;
            }
        }

        try
        {
            cliente.Actualizar(
                dto.Nombre,
                dto.Apellido,
                dto.Email,
                dto.Telefono,
                dto.Direccion,
                dto.Documento,
                dto.Notas,
                dto.Activo);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(ex.Message);
        }

        await _repository.UpdateAsync(cliente, cancellationToken);

        return ClienteDto.From(cliente);
    }

    /// <summary>
    /// Desactivación lógica (BR-011): no borra presupuestos, pedidos ni
    /// pagos históricos. Idempotente.
    /// </summary>
    public async Task<Result<ClienteDto>> DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var cliente = await _repository.GetByIdAsync(id, cancellationToken);
        if (cliente is null)
        {
            return Error.NotFound($"El cliente {id} no existe.");
        }

        cliente.Desactivar();
        await _repository.UpdateAsync(cliente, cancellationToken);

        return ClienteDto.From(cliente);
    }

    private static Error? Validate(string? nombre, string? apellido, string? email)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return Error.Validation("El nombre es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(apellido))
        {
            return Error.Validation("El apellido es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return Error.Validation("El email es obligatorio.");
        }

        return null;
    }
}
