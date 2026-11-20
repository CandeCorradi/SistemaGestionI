using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Proveedores.Dtos;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Proveedores;

/// <summary>
/// Casos de uso de proveedores (functional-spec §7): alta, consulta,
/// búsqueda con paginación, edición parcial y desactivación lógica
/// (BR-021). Terminología obligatoria: "Proveedor", nunca "Mayorista"
/// (BR-020). Las validaciones críticas se ejecutan siempre aquí, no solo
/// en el frontend (AGENTS.md §31).
/// </summary>
public sealed class ProveedoresService
{
    /// <summary>
    /// Tamaño de página máximo permitido (AGENTS.md §30: sin páginas ilimitadas).
    /// </summary>
    public const int MaxPageSize = 100;

    public const int DefaultPageSize = 20;

    private readonly IProveedorRepository _repository;

    public ProveedoresService(IProveedorRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResult<ProveedorListDto>>> GetListAsync(
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

        return new PagedResult<ProveedorListDto>
        {
            Items = paged.Items.Select(ProveedorListDto.From).ToList(),
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalItems = paged.TotalItems,
        };
    }

    public async Task<Result<ProveedorDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var proveedor = await _repository.GetByIdAsync(id, cancellationToken);
        if (proveedor is null)
        {
            return Error.NotFound($"El proveedor {id} no existe.");
        }

        return ProveedorDto.From(proveedor);
    }

    public async Task<Result<ProveedorDto>> CreateAsync(
        CreateProveedorDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return Error.Validation("El nombre es obligatorio.");
        }

        var conflict = await ValidateUniqueEmailAsync(dto.Email, excludeId: null, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        Proveedor proveedor;
        try
        {
            proveedor = Proveedor.Crear(
                dto.Nombre,
                dto.Contacto,
                dto.Email,
                dto.Telefono,
                dto.Direccion);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(ex.Message);
        }

        await _repository.AddAsync(proveedor, cancellationToken);

        return ProveedorDto.From(proveedor);
    }

    public async Task<Result<ProveedorDto>> UpdateAsync(
        int id,
        UpdateProveedorDto dto,
        CancellationToken cancellationToken = default)
    {
        var proveedor = await _repository.GetByIdAsync(id, cancellationToken);
        if (proveedor is null)
        {
            return Error.NotFound($"El proveedor {id} no existe.");
        }

        if (string.IsNullOrWhiteSpace(dto.Nombre ?? proveedor.Nombre))
        {
            return Error.Validation("El nombre es obligatorio.");
        }

        if (dto.Email is not null)
        {
            var conflict = await ValidateUniqueEmailAsync(dto.Email, id, cancellationToken);
            if (conflict is not null)
            {
                return conflict;
            }
        }

        try
        {
            proveedor.Actualizar(
                dto.Nombre,
                dto.Contacto,
                dto.Email,
                dto.Telefono,
                dto.Direccion,
                dto.Notas,
                dto.Activo);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(ex.Message);
        }

        await _repository.UpdateAsync(proveedor, cancellationToken);

        return ProveedorDto.From(proveedor);
    }

    /// <summary>
    /// Desactivación lógica (BR-021): no borra productos ni pedidos
    /// relacionados. Idempotente.
    /// </summary>
    public async Task<Result<ProveedorDto>> DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var proveedor = await _repository.GetByIdAsync(id, cancellationToken);
        if (proveedor is null)
        {
            return Error.NotFound($"El proveedor {id} no existe.");
        }

        proveedor.Desactivar();
        await _repository.UpdateAsync(proveedor, cancellationToken);

        return ProveedorDto.From(proveedor);
    }

    /// <summary>
    /// El email es opcional en proveedores; la unicidad solo aplica cuando
    /// se informa (índice único admite múltiples NULL en PostgreSQL).
    /// </summary>
    private async Task<Error?> ValidateUniqueEmailAsync(
        string? email,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalized = email.Trim().ToLowerInvariant();
        if (await _repository.EmailExistsAsync(normalized, excludeId, cancellationToken))
        {
            return Error.Conflict($"Ya existe un proveedor con el email {normalized}.");
        }

        return null;
    }
}
