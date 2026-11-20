namespace SistemaGestion.Shared.Results;

/// <summary>
/// Respuesta paginada estándar para listados de la API (AGENTS.md §30).
/// </summary>
public sealed record PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public int TotalItems { get; init; }

    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalItems / (double)PageSize) : 0;
}