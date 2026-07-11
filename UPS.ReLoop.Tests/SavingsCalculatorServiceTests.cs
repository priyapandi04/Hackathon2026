namespace UPS.ReLoop.Tests;

using UPS.ReLoop.Application.Common.Exceptions;
using UPS.ReLoop.Application.DTOs.Savings;
using UPS.ReLoop.Application.Services;
using Xunit;

public class SavingsCalculatorServiceTests
{
    private readonly SavingsCalculatorService _service = new();

    [Fact]
    public void CalculateSavings_ValidInput_ReturnsCorrectDistanceSaved()
    {
        var request = new SavingsRequest(500, 20, 0.026, 0.0037);

        var result = _service.CalculateSavings(request);

        Assert.Equal(480, result.DistanceSavedKm);
    }

    [Fact]
    public void CalculateSavings_ValidInput_ReturnsCorrectCostSaved()
    {
        var request = new SavingsRequest(500, 20, 0.026, 0.0037);

        var result = _service.CalculateSavings(request);

        Assert.Equal(12.48, result.CostSaved);
    }

    [Fact]
    public void CalculateSavings_ValidInput_ReturnsCorrectCo2Saved()
    {
        var request = new SavingsRequest(500, 20, 0.026, 0.0037);

        var result = _service.CalculateSavings(request);

        Assert.Equal(1.776, result.Co2SavedKg);
    }

    [Fact]
    public void CalculateSavings_LocalGreaterThanWarehouse_ReturnsZeroSavings()
    {
        var request = new SavingsRequest(100, 200, 0.026, 0.0037);

        var result = _service.CalculateSavings(request);

        Assert.Equal(0, result.DistanceSavedKm);
        Assert.Equal(0, result.CostSaved);
        Assert.Equal(0, result.Co2SavedKg);
    }

    [Fact]
    public void CalculateSavings_ZeroDistances_ReturnsZero()
    {
        var request = new SavingsRequest(0, 0, 0.026, 0.0037);

        var result = _service.CalculateSavings(request);

        Assert.Equal(0, result.DistanceSavedKm);
        Assert.Equal(0, result.CostSaved);
        Assert.Equal(0, result.Co2SavedKg);
    }

    [Fact]
    public void CalculateSavings_NegativeWarehouseDistance_ThrowsBadRequest()
    {
        var request = new SavingsRequest(-10, 20, 0.026, 0.0037);

        Assert.Throws<BadRequestException>(() => _service.CalculateSavings(request));
    }

    [Fact]
    public void CalculateSavings_NegativeLocalDistance_ThrowsBadRequest()
    {
        var request = new SavingsRequest(500, -5, 0.026, 0.0037);

        Assert.Throws<BadRequestException>(() => _service.CalculateSavings(request));
    }

    [Fact]
    public void CalculateSavings_NegativeCostPerKm_ThrowsBadRequest()
    {
        var request = new SavingsRequest(500, 20, -0.01, 0.0037);

        Assert.Throws<BadRequestException>(() => _service.CalculateSavings(request));
    }

    [Fact]
    public void CalculateSavings_DefaultRates_UsesExpectedValues()
    {
        var request = new SavingsRequest(1000, 50);

        var result = _service.CalculateSavings(request);

        Assert.Equal(950, result.DistanceSavedKm);
        Assert.Equal(24.70, result.CostSaved);
        Assert.Equal(3.515, result.Co2SavedKg);
    }
}
