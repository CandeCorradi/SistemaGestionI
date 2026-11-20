using System.Globalization;

namespace SistemaGestion.Domain.ValueObjects;

/// <summary>
/// Representa dinero de forma segura usando <see cref="decimal"/>.
/// Nunca se usa float/double para importes (AGENTS.md §16).
/// Los importes no pueden ser negativos.
/// </summary>
public readonly record struct Money
{
    public static readonly Money Zero = new(decimal.Zero);

    public decimal Amount { get; }

    public Money(decimal amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "El importe no puede ser negativo.");
        }

        Amount = amount;
    }

    public Money Add(Money other) => new(Amount + other.Amount);

    public Money Subtract(Money other)
    {
        var result = Amount - other.Amount;
        if (result < 0)
        {
            throw new InvalidOperationException("El resultado de la resta no puede ser negativo.");
        }

        return new Money(result);
    }

    public Money Multiply(decimal factor)
    {
        if (factor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(factor), "El factor no puede ser negativo.");
        }

        return new Money(Amount * factor);
    }

    public static Money operator +(Money left, Money right) => left.Add(right);

    public static Money operator -(Money left, Money right) => left.Subtract(right);

    public static Money operator *(Money left, decimal factor) => left.Multiply(factor);

    public static bool operator <(Money left, Money right) => left.Amount < right.Amount;

    public static bool operator >(Money left, Money right) => left.Amount > right.Amount;

    public static bool operator <=(Money left, Money right) => left.Amount <= right.Amount;

    public static bool operator >=(Money left, Money right) => left.Amount >= right.Amount;

    public override string ToString() => Amount.ToString("0.00", CultureInfo.InvariantCulture);
}