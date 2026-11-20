using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Permisos.Dtos;
using SistemaGestion.Domain.Authorization;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Permisos;

/// <summary>
/// Casos de uso de permisos granulares (AGENTS.md §7, BR-002):
/// catálogo, permisos explícitos de un usuario y permisos efectivos del
/// usuario autenticado. Las validaciones críticas se ejecutan siempre
/// aquí, no solo en el frontend (AGENTS.md §31).
/// </summary>
public sealed class PermisosService
{
    private readonly IPermisoRepository _repository;

    private readonly IUsuarioRepository _usuarioRepository;

    public PermisosService(IPermisoRepository repository, IUsuarioRepository usuarioRepository)
    {
        _repository = repository;
        _usuarioRepository = usuarioRepository;
    }

    /// <summary>
    /// Catálogo completo de permisos (fuente de verdad AGENTS.md §7).
    /// </summary>
    public async Task<Result<IReadOnlyList<PermisoDto>>> GetCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        var permisos = await _repository.GetAllAsync(cancellationToken);

        IReadOnlyList<PermisoDto> dtos = permisos
            .OrderBy(p => p.Id)
            .Select(p => new PermisoDto
            {
                Id = p.Id,
                Codigo = p.Codigo,
                Nombre = p.Nombre,
            })
            .ToList();

        return Result.Success(dtos);
    }

    /// <summary>
    /// Permisos explícitos de un usuario (para administración).
    /// </summary>
    public async Task<Result<PermisosUsuarioDto>> GetUsuarioPermisosAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId, cancellationToken);
        if (usuario is null)
        {
            return Error.NotFound($"El usuario {usuarioId} no existe.");
        }

        var codigos = await _repository.GetExplicitCodesAsync(usuarioId, cancellationToken);

        return new PermisosUsuarioDto
        {
            UsuarioId = usuarioId,
            Codigos = codigos,
        };
    }

    /// <summary>
    /// Permisos efectivos del usuario autenticado identificado por su
    /// Firebase UID (unión rol ∪ explícitos). <c>null</c> en el repositorio
    /// significa que no hay usuario interno activo asociado (404).
    /// </summary>
    public async Task<Result<PermisosActualesDto>> GetEffectivePermisosAsync(
        string? firebaseUid,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(firebaseUid))
        {
            return Error.NotFound("No existe un usuario interno asociado a la identidad autenticada.");
        }

        var codigos = await _repository.GetEffectiveCodesByFirebaseUidAsync(
            firebaseUid.Trim(),
            cancellationToken);

        if (codigos is null)
        {
            return Error.NotFound("No existe un usuario interno activo asociado a la identidad autenticada.");
        }

        return new PermisosActualesDto { Codigos = codigos };
    }

    /// <summary>
    /// Reemplaza por completo los permisos explícitos de un usuario.
    /// Los códigos deben pertenecer al catálogo (BR-002).
    /// </summary>
    public async Task<Result<PermisosUsuarioDto>> UpdateUsuarioPermisosAsync(
        int usuarioId,
        UpdatePermisosUsuarioDto dto,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId, cancellationToken);
        if (usuario is null)
        {
            return Error.NotFound($"El usuario {usuarioId} no existe.");
        }

        if (dto.Codigos is null)
        {
            return Error.Validation("La lista de códigos es obligatoria.");
        }

        var catalogo = await _repository.GetAllAsync(cancellationToken);
        var validos = catalogo.ToDictionary(p => p.Codigo, p => p.Id, StringComparer.Ordinal);
        var posicion = catalogo
            .Select((p, index) => (p.Id, index))
            .ToDictionary(x => x.Id, x => x.index);
        var porId = catalogo.ToDictionary(p => p.Id);

        var ids = new List<int>();
        var vistos = new HashSet<int>();

        foreach (var codigo in dto.Codigos)
        {
            var limpio = codigo?.Trim();
            if (string.IsNullOrEmpty(limpio))
            {
                return Error.Validation("Los códigos de permiso no pueden estar vacíos.");
            }

            if (!validos.TryGetValue(limpio, out var permisoId))
            {
                return Error.Validation($"El permiso {limpio} no existe en el catálogo.");
            }

            if (vistos.Add(permisoId))
            {
                ids.Add(permisoId);
            }
        }

        await _repository.ReplaceExplicitAsync(usuarioId, ids, cancellationToken);

        var codigosFinales = ids
            .OrderBy(id => posicion[id])
            .Select(id => porId[id].Codigo)
            .ToList();

        return new PermisosUsuarioDto
        {
            UsuarioId = usuarioId,
            Codigos = codigosFinales,
        };
    }
}
