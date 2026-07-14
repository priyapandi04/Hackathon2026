namespace UPS.ReLoop.Application.DTOs.Buyers;

public record BuyerDto(
    string BuyerId,
    string BuyerName,
    string Hub,
    string Zone,
    double DistanceKm,
    double EstimatedDeliveryHours,
    int DemandScore,
    string PreferredCategory,
    string Recommendation);

public record BuyerListResponse(
    string Hub,
    List<BuyerDto> Buyers);
