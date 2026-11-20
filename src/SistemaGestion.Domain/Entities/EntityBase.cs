namespace SistemaGestion.Domain.Entities;

/// <summary>
/// Clase base de todas las entidades del dominio.
/// PK entera con generación automática (AGENTS.md §27: no UUID como PK principal).
/// </summary>
public abstract class EntityBase
{
    public int Id { get; set; }
}
