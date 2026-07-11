namespace UPS.ReLoop.Application.Interfaces;

using UPS.ReLoop.Application.DTOs.Savings;

public interface ISavingsCalculatorService
{
    SavingsResponse CalculateSavings(SavingsRequest request);
}
