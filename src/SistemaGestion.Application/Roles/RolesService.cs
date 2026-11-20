using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Roles.Dtos;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Roles;

/// <summary>
/// Consulta de los roles fijos del sistema. No existe operación de
/// creación/edición: los roles son exactamente cinco (BR-001).
/// </summary>
public sealed class RolesService
{
    private readonly IRolRepository _repository;

    public RolesService(IRolRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<RolDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = await _repository.GetAllAsync(cancellationToken);

        IReadOnlyList<RolDto> dtos = roles
            .OrderBy(r => r.Id)
            .Select(RolDto.From)
            .ToList();

        return Result.Success(dtos);
    }
}
