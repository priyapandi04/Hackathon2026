namespace UPS.ReLoop.Application.DTOs.Savings;

public record SavingsRequest(
    double WarehouseDistanceKm,
    double LocalDeliveryDistanceKm,
    double CostPerKm = 0.026,
    double Co2PerKm = 0.0037);

public record SavingsResponse(
    double DistanceSavedKm,
    double CostSaved,
    double Co2SavedKg);
