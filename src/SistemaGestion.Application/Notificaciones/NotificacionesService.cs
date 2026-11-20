using Microsoft.Extensions.Logging;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Notificaciones.Dtos;
using SistemaGestion.Domain.Common;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Notificaciones;

/// <summary>
/// Casos de uso de notificaciones (functional-spec §18, BR-090..BR-092):
/// listado propio paginado, conteo de no leídas, marcar como leída y
/// publicación best-effort de eventos de negocio. Cada usuario sólo
/// accede a sus propias notificaciones (gate de la roadmap Phase 13).
/// </summary>
public sealed class NotificacionesService : INotificacionPublisher
{
    /// <summary>Tamaño de página máximo permitido (AGENTS.md §30).</summary>
    public const int MaxPageSize = 100;

    public const int DefaultPageSize = 20;

    private readonly INotificacionRepository _notificaciones;

    private readonly IClienteRepository _clientes;

    private readonly IUsuarioRepository _usuarios;

    private readonly ITimeProvider _clock;

    private readonly ILogger<NotificacionesService> _logger;

    public NotificacionesService(
        INotificacionRepository notificaciones,
        IClienteRepository clientes,
        IUsuarioRepository usuarios,
        ITimeProvider clock,
        ILogger<NotificacionesService> logger)
    {
        _notificaciones = notificaciones;
        _clientes = clientes;
        _usuarios = usuarios;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>Listado paginado de las notificaciones del usuario.</summary>
    public async Task<Result<PagedResult<NotificationDto>>> GetListAsync(
        string firebaseUid,
        int page,
        int pageSize,
        bool unreadOnly,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var usuario = await ResolverUsuarioAsync(firebaseUid, cancellationToken);
        if (usuario is null)
        {
            return Error.NotFound("No hay usuario interno activo asociado al token.");
        }

        var paged = await _notificaciones.GetPagedByUsuarioAsync(
            usuario.Id, page, pageSize, unreadOnly, cancellationToken);

        return new PagedResult<NotificationDto>
        {
            Items = paged.Items.Select(NotificationDto.From).ToList(),
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalItems = paged.TotalItems,
        };
    }

    /// <summary>Conteo de notificaciones no leídas del usuario.</summary>
    public async Task<Result<int>> CountUnreadAsync(
        string firebaseUid,
        CancellationToken cancellationToken = default)
    {
        var usuario = await ResolverUsuarioAsync(firebaseUid, cancellationToken);
        if (usuario is null)
        {
            return Error.NotFound("No hay usuario interno activo asociado al token.");
        }

        return await _notificaciones.CountUnreadAsync(usuario.Id, cancellationToken);
    }

    /// <summary>
    /// Marca una notificación como leída (BR-091). Sólo la propia: una
    /// notificación ajena responde 404 para no filtrar su existencia.
    /// </summary>
    public async Task<Result> MarkReadAsync(
        string firebaseUid,
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Result.Failure(Error.Validation("La notificación indicada no es válida."));
        }

        var usuario = await ResolverUsuarioAsync(firebaseUid, cancellationToken);
        if (usuario is null)
        {
            return Result.Failure(Error.NotFound("No hay usuario interno activo asociado al token."));
        }

        var notificacion = await _notificaciones.GetByIdAsync(id, cancellationToken);
        if (notificacion is null || notificacion.UsuarioId != usuario.Id)
        {
            return Result.Failure(Error.NotFound($"La notificación {id} no existe."));
        }

        notificacion.MarcarLeida();
        await _notificaciones.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>Marca todas las notificaciones del usuario como leídas.</summary>
    public async Task<Result<int>> MarkAllReadAsync(
        string firebaseUid,
        CancellationToken cancellationToken = default)
    {
        var usuario = await ResolverUsuarioAsync(firebaseUid, cancellationToken);
        if (usuario is null)
        {
            return Error.NotFound("No hay usuario interno activo asociado al token.");
        }

        return await _notificaciones.MarkAllReadAsync(usuario.Id, cancellationToken);
    }

    /// <summary>
    /// Publica una notificación de negocio de forma best-effort (BR-092):
    /// resuelve los destinatarios, persiste y nunca propaga excepciones
    /// para no romper la operación que la origina.
    /// </summary>
    public async Task PublicarAsync(
        NotificacionDraft draft,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var destinatarios = await ResolverDestinatariosAsync(draft, cancellationToken);
            if (destinatarios.Count == 0)
            {
                _logger.LogWarning(
                    "NOTIFY_SKIPPED: tipo={Tipo} sin destinatarios resolubles " +
                    "(clienteId={ClienteId}, equipo={ParaEquipo}).",
                    draft.Tipo, draft.ClienteId, draft.ParaEquipo);
                return;
            }

            var fecha = _clock.UtcNow;
            var filas = destinatarios
                .Select(usuarioId => Notificacion.Crear(
                    usuarioId: usuarioId,
                    tipo: draft.Tipo,
                    titulo: draft.Titulo,
                    mensaje: draft.Mensaje,
                    entidadTipo: draft.EntidadTipo,
                    entidadId: draft.EntidadId,
                    fechaCreacionUtc: fecha))
                .ToList();

            await _notificaciones.AddRangeAsync(filas, cancellationToken);

            _logger.LogInformation(
                "NOTIFY_PUBLISHED: tipo={Tipo} destinatarios={Cantidad} " +
                "(entidad={EntidadTipo}:{EntidadId}).",
                draft.Tipo, filas.Count, draft.EntidadTipo, draft.EntidadId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "NOTIFY_FAILED: no se pudo publicar la notificación tipo={Tipo} " +
                "(clienteId={ClienteId}, equipo={ParaEquipo}).",
                draft.Tipo, draft.ClienteId, draft.ParaEquipo);
        }
    }

    private async Task<Usuario?> ResolverUsuarioAsync(
        string firebaseUid,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(firebaseUid))
        {
            return null;
        }

        var usuario = await _usuarios.GetByFirebaseUidAsync(firebaseUid, cancellationToken);
        return usuario is { Activo: true } ? usuario : null;
    }

    private async Task<IReadOnlyList<int>> ResolverDestinatariosAsync(
        NotificacionDraft draft,
        CancellationToken cancellationToken)
    {
        var destinatarios = new HashSet<int>();

        if (draft.ClienteId is > 0)
        {
            var cliente = await _clientes.GetByIdAsync(draft.ClienteId.Value, cancellationToken);
            if (!string.IsNullOrWhiteSpace(cliente?.FirebaseUid))
            {
                var usuario = await _usuarios.GetByFirebaseUidAsync(
                    cliente.FirebaseUid!, cancellationToken);
                if (usuario is { Activo: true })
                {
                    destinatarios.Add(usuario.Id);
                }
            }
        }

        if (draft.ParaEquipo)
        {
            foreach (var usuarioId in await _usuarios.GetActiveStaffUserIdsAsync(cancellationToken))
            {
                destinatarios.Add(usuarioId);
            }
        }

        return destinatarios.ToList();
    }
}
