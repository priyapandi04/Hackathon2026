namespace UPS.ReLoop.Application.Services;

using UPS.ReLoop.Application.Common.Exceptions;
using UPS.ReLoop.Application.DTOs.Savings;
using UPS.ReLoop.Application.Interfaces;

public class SavingsCalculatorService : ISavingsCalculatorService
{
    public SavingsResponse CalculateSavings(SavingsRequest request)
    {
        if (request.WarehouseDistanceKm < 0)
            throw new BadRequestException("WarehouseDistanceKm must be non-negative.");
        if (request.LocalDeliveryDistanceKm < 0)
            throw new BadRequestException("LocalDeliveryDistanceKm must be non-negative.");
        if (request.CostPerKm < 0)
            throw new BadRequestException("CostPerKm must be non-negative.");
        if (request.Co2PerKm < 0)
            throw new BadRequestException("Co2PerKm must be non-negative.");

        var distanceSaved = request.WarehouseDistanceKm - request.LocalDeliveryDistanceKm;
        if (distanceSaved < 0) distanceSaved = 0;

        var costSaved = Math.Round(distanceSaved * request.CostPerKm, 2);
        var co2Saved = Math.Round(distanceSaved * request.Co2PerKm, 4);

        return new SavingsResponse(distanceSaved, costSaved, co2Saved);
    }
}
