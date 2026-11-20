using SistemaGestion.Application.Facturacion;
using Xunit;

namespace SistemaGestion.Application.UnitTests;

/// <summary>
/// Numeración simulada (BR-130/BR-131): siempre simulada y con prefijo
/// SIM- para que el documento nunca parezca un comprobante fiscal
/// autorizado.
/// </summary>
public sealed class SimulatedBillingServiceTests
{
    private readonly SimulatedBillingService _service = new();

    [Fact]
    public void IsSimulated_IsTrue()
        => Assert.True(_service.IsSimulated);

    [Fact]
    public void GenerateNumber_FormatsSixDigitPrefixedNumber()
    {
        Assert.Equal("SIM-000042", _service.GenerateNumber(42));
        Assert.Equal("SIM-000001", _service.GenerateNumber(1));
    }

    [Fact]
    public void GenerateNumber_InvalidInvoiceId_Throws()
        => Assert.Throws<ArgumentException>(() => _service.GenerateNumber(0));
}
