namespace SistemaGestion.Domain.Common;

/// <summary>
/// Implementación real del reloj. Devuelve siempre instantes en UTC.
/// </summary>
public sealed class SystemTimeProvider : ITimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}