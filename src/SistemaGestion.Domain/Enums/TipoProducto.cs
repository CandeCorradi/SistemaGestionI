namespace SistemaGestion.Domain.Enums;

/// <summary>
/// Tipo comercial de un producto (AGENTS.md §14): los productos pueden
/// representar muebles, melaminas, accesorios, insumos u otros materiales
/// definidos por el dominio. El carácter estándar/medida de un mueble no
/// pertenece a este enum (se modela con la entidad de muebles).
/// </summary>
public enum TipoProducto
{
    Mueble = 0,
    Melamina = 1,
    Accesorio = 2,
    Insumo = 3,
    Otro = 4,
}
