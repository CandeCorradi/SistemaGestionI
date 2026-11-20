using SistemaGestion.Domain.Common;
using SistemaGestion.Domain.Enums;

namespace SistemaGestion.Domain.Entities;

/// <summary>
/// Movimiento de inventario (functional-spec §11, AGENTS.md §15).
/// Conserva el stock anterior y posterior para reconstruir el historial
/// (BR-051) y quién/ cuándlo registró (auditoría §18). La creación valida
/// BR-052 (sin stock negativo) y las reglas de cantidad por tipo; la
/// aplicación atómica sobre el producto es responsabilidad del repositorio
/// (BR-050/BR-053).
/// </summary>
public sealed class MovimientoStock : EntityBase
{
    public int ProductoId { get; private set; }

    public TipoMovimientoStock Tipo { get; private set; }

    public int Cantidad { get; private set; }

    public int StockAnterior { get; private set; }

    public int StockPosterior { get; private set; }

    public string? Motivo { get; private set; }

    public string? Referencia { get; private set; }

    public int? UsuarioId { get; private set; }

    /// <summary>Instante UTC del movimiento (AGENTS.md §17).</summary>
    public DateTime Fecha { get; private set; }

    public Producto? Producto { get; private set; }

    public Usuario? Usuario { get; private set; }

    private MovimientoStock()
    {
    }

    /// <summary>
    /// Asocia las entidades de referencia (producto y usuario) tras
    /// consultarlas. EF Core las pobla mediante Include; los repositorios
    /// en memoria (tests) usan este método para cumplir el mismo contrato.
    /// No altera ninguna regla de negocio del movimiento.
    /// </summary>
    public void AsociarReferencias(Producto? producto, Usuario? usuario)
    {
        Producto = producto;
        Usuario = usuario;
    }

    /// <summary>
    /// Crea un movimiento validando dominio. Lanza <see cref="ArgumentException"/>
    /// si los datos violan una regla (tipo inválido, cantidad inválida o
    /// stock resultante negativo — BR-052).
    /// </summary>
    /// <param name="stockAnterior">Stock leído del producto; con este valor se calcula el posterior.</param>
    public static MovimientoStock Crear(
        int productoId,
        TipoMovimientoStock tipo,
        int cantidad,
        int stockAnterior,
        int? usuarioId,
        DateTime fechaUtc,
        string? motivo = null,
        string? referencia = null)
    {
        if (productoId <= 0)
        {
            throw new ArgumentException("El producto es obligatorio.", nameof(productoId));
        }

        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentException(
                $"El tipo {tipo} no es un tipo de movimiento de stock válido.",
                nameof(tipo));
        }

        if (stockAnterior < 0)
        {
            throw new ArgumentException(
                "El stock anterior no puede ser negativo.",
                nameof(stockAnterior));
        }

        if (fechaUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("La fecha debe estar en UTC (AGENTS.md §17).", nameof(fechaUtc));
        }

        var efecto = CalcularEfecto(tipo, cantidad);
        var stockPosterior = stockAnterior + efecto;

        if (stockPosterior < 0)
        {
            throw new ArgumentException(
                $"La operación dejaría el stock en {stockPosterior}; no se permite stock negativo (BR-052).",
                nameof(cantidad));
        }

        return new MovimientoStock
        {
            ProductoId = productoId,
            Tipo = tipo,
            Cantidad = cantidad,
            StockAnterior = stockAnterior,
            StockPosterior = stockPosterior,
            Motivo = Normalizar(motivo, 500),
            Referencia = Normalizar(referencia, 100),
            UsuarioId = usuarioId,
            Fecha = fechaUtc,
        };
    }

    /// <summary>Efecto (con signo) del movimiento sobre el stock según su tipo.</summary>
    private static int CalcularEfecto(TipoMovimientoStock tipo, int cantidad) => tipo switch
    {
        TipoMovimientoStock.Ingreso
            or TipoMovimientoStock.Devolucion
            or TipoMovimientoStock.Liberacion
            => cantidad > 0
                ? cantidad
                : throw new ArgumentException(
                    "La cantidad debe ser mayor que cero para este tipo de movimiento.",
                    nameof(cantidad)),

        TipoMovimientoStock.Egreso
            or TipoMovimientoStock.Reserva
            => cantidad > 0
                ? -cantidad
                : throw new ArgumentException(
                    "La cantidad debe ser mayor que cero para este tipo de movimiento.",
                    nameof(cantidad)),

        // El ajuste es una corrección: admite signo para subir o bajar el stock.
        TipoMovimientoStock.Ajuste
            => cantidad != 0
                ? cantidad
                : throw new ArgumentException(
                    "La cantidad del ajuste no puede ser cero.",
                    nameof(cantidad)),

        _ => throw new ArgumentException(
            $"El tipo {tipo} no es un tipo de movimiento de stock válido.",
            nameof(tipo)),
    };

    private static string? Normalizar(string? valor, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var normalizado = valor.Trim();
        return normalizado.Length > maxLength
            ? normalizado[..maxLength]
            : normalizado;
    }
}
