using Microsoft.EntityFrameworkCore;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Infrastructure.Persistence;
using Xunit;

namespace SistemaGestion.Infrastructure.IntegrationTests;

/// <summary>
/// Validación básica del contexto EF Core contra el proveedor PostgreSQL.
/// NOTA: los tests que requieren una base real (queries, constraints,
/// migraciones) se habilitan en la Fase 4 con PostgreSQL aislado
/// (AGENTS.md §39, §42).
/// </summary>
public sealed class SistemaGestionDbContextTests
{
    [Fact]
    public void CanBuild_WithNpgsqlProvider_WithoutConnecting()
    {
        var options = new DbContextOptionsBuilder<SistemaGestionDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=sistemagestion;Username=postgres;Password=1234")
            .Options;

        using var context = new SistemaGestionDbContext(options);

        Assert.NotNull(context);
        Assert.Contains("Npgsql", context.Database.ProviderName);
    }

    [Fact]
    public void Model_DiscoversAllDomainEntities()
    {
        var options = new DbContextOptionsBuilder<SistemaGestionDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=sistemagestion;Username=postgres;Password=1234")
            .Options;

        using var context = new SistemaGestionDbContext(options);
        var model = context.Model;

        Assert.NotNull(model.FindEntityType(typeof(Cliente)));
        Assert.NotNull(model.FindEntityType(typeof(Producto)));
        Assert.NotNull(model.FindEntityType(typeof(Presupuesto)));
        Assert.NotNull(model.FindEntityType(typeof(PresupuestoDetalle)));
        Assert.NotNull(model.FindEntityType(typeof(Pedido)));
        Assert.NotNull(model.FindEntityType(typeof(Usuario)));
        Assert.NotNull(model.FindEntityType(typeof(Rol)));
    }
}
