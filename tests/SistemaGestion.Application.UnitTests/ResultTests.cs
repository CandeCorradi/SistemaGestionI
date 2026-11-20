using SistemaGestion.Shared.Results;
using Xunit;

namespace SistemaGestion.Application.UnitTests;

public sealed class ResultTests
{
    [Fact]
    public void Success_HasIsSuccessTrue()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void Failure_CarriesError()
    {
        var error = Error.NotFound("El recurso no existe.");

        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void SuccessGeneric_RetrievesValue()
    {
        var result = Result.Success(42);

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void FailureGeneric_AccessValue_Throws()
    {
        var result = Result.Failure<int>(Error.Conflict("Recurso en conflicto."));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromValue_CreatesSuccess()
    {
        Result<int> result = 10;

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromError_CreatesFailure()
    {
        Error error = Error.Validation("Dato inválido.");

        Result<int> result = error;

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void SuccessWithError_Throws()
        => Assert.Throws<InvalidOperationException>(() => Result.Success(Error.Conflict("No")));

    [Fact]
    public void FailureWithoutError_Throws()
        => Assert.Throws<InvalidOperationException>(() => Result.Failure(Error.None));
}