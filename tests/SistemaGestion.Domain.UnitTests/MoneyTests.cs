using SistemaGestion.Domain.ValueObjects;
using Xunit;

namespace SistemaGestion.Domain.UnitTests;

public sealed class MoneyTests
{
    [Fact]
    public void Zero_IsZero()
        => Assert.Equal(decimal.Zero, Money.Zero.Amount);

    [Theory]
    [InlineData(0)]
    [InlineData(1.5)]
    [InlineData(999999.99)]
    public void Create_WithNonNegativeAmount_IsValid(decimal amount)
    {
        var money = new Money(amount);

        Assert.Equal(amount, money.Amount);
    }

    [Fact]
    public void Create_WithNegativeAmount_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-0.01m));

    [Fact]
    public void Add_ReturnsSum()
        => Assert.Equal(12.50m, (new Money(10m) + new Money(2.5m)).Amount);

    [Fact]
    public void Subtract_WhenResultIsNonNegative_ReturnsDifference()
        => Assert.Equal(7m, (new Money(10m) - new Money(3m)).Amount);

    [Fact]
    public void Subtract_WhenResultIsNegative_Throws()
        => Assert.Throws<InvalidOperationException>(() => new Money(2m) - new Money(5m));

    [Fact]
    public void Multiply_ByFactor_ReturnsProduct()
        => Assert.Equal(30m, (new Money(10m) * 3).Amount);

    [Theory]
    [InlineData(1, 2, true)]
    [InlineData(2, 2, false)]
    [InlineData(3, 2, false)]
    public void LessThan_ComparesAmounts(decimal left, decimal right, bool expected)
        => Assert.Equal(expected, new Money(left) < new Money(right));
}