using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Common;
using SistemaGestion.Shared.Results;
using DomainPermisos = SistemaGestion.Domain.Authorization.Permisos;

namespace SistemaGestion.Application.Dashboard;

/// <summary>
/// Dashboard administrativo (api-spec Â§19, functional-spec Â§22): devuelve
/// Ãºnicamente los bloques de indicadores para los que el usuario tiene
/// permiso (PEDIDOS_VIEW, PRESUPUESTOS_VIEW, FACTURAS_VIEW o
/// REPORTES_VIEW, PAGOS_VIEW, STOCK_VIEW). El bloque de notificaciones es
/// propio del token (BR-090) y siempre se incluye para usuarios internos
/// activos.
/// </summary>
public sealed class DashboardService
{
    private readonly IDashboardRepository _repository;
    private readonly IPermisoRepository _permisoRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly INotificacionRepository _notificacionRepository;
    private readonly ITimeProvider _timeProvider;

    public DashboardService(
        IDashboardRepository repository,
        IPermisoRepository permisoRepository,
        IUsuarioRepository usuarioRepository,
        INotificacionRepository notificacionRepository,
        ITimeProvider timeProvider)
    {
        _repository = repository;
        _permisoRepository = permisoRepository;
        _usuarioRepository = usuarioRepository;
        _notificacionRepository = notificacionRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<DashboardDto>> GetAsync(
        string firebaseUid,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(firebaseUid))
        {
            return Result.Failure<DashboardDto>(Error.NotFound(
                "El token no identifica un usuario interno activo."));
        }

        var permisos = await _permisoRepository
            .GetEffectiveCodesByFirebaseUidAsync(firebaseUid, cancellationToken);
        if (permisos is null)
        {
            return Result.Failure<DashboardDto>(Error.NotFound(
                "El token no identifica un usuario interno activo."));
        }

        var ahora = _timeProvider.UtcNow;
        var inicioMes = new DateTime(ahora.Year, ahora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var finMes = inicioMes.AddMonths(1);
        bool Tiene(string codigo) => permisos.Contains(codigo, StringComparer.Ordinal);

        DashboardOrdersBlock? orders = null;
        if (Tiene(DomainPermisos.PedidosView))
        {
            var porEstado = await _repository.CountPedidosPorEstadoAsync(cancellationToken);
            orders = new DashboardOrdersBlock(
                Conteo(porEstado, Domain.Enums.EstadoPedido.Pendiente),
                Conteo(porEstado, Domain.Enums.EstadoPedido.Confirmado),
                Conteo(porEstado, Domain.Enums.EstadoPedido.EnProduccion),
                Conteo(porEstado, Domain.Enums.EstadoPedido.Listo),
                await _repository.CountPedidosEntregadosAsync(inicioMes, finMes, cancellationToken),
                await _repository.CountPedidosCanceladosAsync(inicioMes, finMes, cancellationToken));
        }

        DashboardQuotesBlock? quotes = null;
        if (Tiene(DomainPermisos.PresupuestosView))
        {
            var porEstado = await _repository.CountPresupuestosPorEstadoAsync(cancellationToken);
            quotes = new DashboardQuotesBlock(
                Conteo(porEstado, Domain.Enums.EstadoPresupuesto.Borrador),
                Conteo(porEstado, Domain.Enums.EstadoPresupuesto.Enviado),
                await _repository.CountPresupuestosEnRangoAsync(
                    Domain.Enums.EstadoPresupuesto.Aceptado, inicioMes, finMes, cancellationToken),
                await _repository.CountPresupuestosEnRangoAsync(
                    Domain.Enums.EstadoPresupuesto.Rechazado, inicioMes, finMes, cancellationToken));
        }

        DashboardSalesBlock? sales = null;
        if (Tiene(DomainPermisos.FacturasView) || Tiene(DomainPermisos.ReportesView))
        {
            sales = new DashboardSalesBlock(
                await _repository.SumFacturasAsync(inicioMes, finMes, cancellationToken),
                await _repository.CountFacturasAsync(inicioMes, finMes, cancellationToken));
        }

        DashboardPaymentsBlock? payments = null;
        if (Tiene(DomainPermisos.PagosView))
        {
            payments = new DashboardPaymentsBlock(
                await _repository.SumPagosAsync(inicioMes, finMes, cancellationToken),
                await _repository.CountPagosAsync(inicioMes, finMes, cancellationToken));
        }

        DashboardInventoryBlock? inventory = null;
        if (Tiene(DomainPermisos.StockView))
        {
            var counts = await _repository.CountInventarioAsync(cancellationToken);
            inventory = new DashboardInventoryBlock(
                counts.ProductosActivos, counts.StockBajo, counts.EnShowroom);
        }

        var usuario = await _usuarioRepository.GetByFirebaseUidAsync(firebaseUid, cancellationToken);
        if (usuario is null)
        {
            return Result.Failure<DashboardDto>(Error.NotFound(
                "El token no identifica un usuario interno activo."));
        }

        var sinLeer = await _notificacionRepository
            .CountUnreadAsync(usuario.Id, cancellationToken);

        return Result.Success(new DashboardDto(
            orders,
            quotes,
            sales,
            payments,
            inventory,
            new DashboardNotificationsBlock(sinLeer),
            ahora));
    }

    private static int Conteo<T>(IReadOnlyDictionary<T, int> porEstado, T estado)
        where T : notnull
        => porEstado.TryGetValue(estado, out var valor) ? valor : 0;
}
