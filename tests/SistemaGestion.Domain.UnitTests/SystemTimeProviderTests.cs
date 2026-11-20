using Xunit;

namespace SistemaGestion.Domain.UnitTests;

public sealed class SystemTimeProviderTests
{
    private static readonly Domain.Common.SystemTimeProvider Sut = new();

    [Fact]
    public void UtcNow_HasUtcKind()
        => Assert.Equal(DateTimeKind.Utc, Sut.UtcNow.Kind);

    [Fact]
    public void UtcNow_IsCloseToRealTime()
    {
        var before = DateTime.UtcNow;
        var now = Sut.UtcNow;
        var after = DateTime.UtcNow;

        Assert.InRange(now, before, after);
    }
}