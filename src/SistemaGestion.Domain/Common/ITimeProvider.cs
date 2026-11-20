namespace SistemaGestion.Domain.Common;

/// <summary>
/// Abstracción del reloj para mantener la lógica de negocio desacoplada de
/// <c>DateTime.Now</c> y poder testearla (AGENTS.md §17).
/// </summary>
public interface ITimeProvider
{
    DateTime UtcNow { get; }
}