namespace SistemaGestion.Shared.Results;

/// <summary>
/// Resultado tipado de una operación que puede fallar.
/// </summary>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    public Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value
    {
        get
        {
            if (IsFailure)
            {
                throw new InvalidOperationException("No se puede acceder al valor de un resultado fallido.");
            }

            return _value!;
        }
    }

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
}