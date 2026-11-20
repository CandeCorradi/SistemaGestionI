using Xunit;

namespace SistemaGestion.Infrastructure.IntegrationTests;

/// <summary>
/// Los tests que usan la misma BD aislada (sistemagestion_test) deben
/// ejecutarse en serie: cada clase hace EnsureDeleted/EnsureCreated en su
/// constructor y en paralelo se pisarían entre sí.
/// </summary>
[CollectionDefinition("postgres-test-db")]
public sealed class PostgresTestDbCollection
{
}
