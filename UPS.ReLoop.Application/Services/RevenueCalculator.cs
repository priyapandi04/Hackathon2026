namespace UPS.ReLoop.Application.Services;

using UPS.ReLoop.Application.DTOs.Decision;

/// <summary>
/// Triple-value + new-revenue economics for one diverted item. Turns the
/// reverse-logistics cost center into two engines: cost avoided (reverse
/// freight) and revenue earned (resale margin + resale-as-a-service fee),
/// plus a quantified CO2 value. All figures are illustrative and configurable.
/// </summary>
public static class RevenueCalculator
{
    // Illustrative commercial assumptions in Indian Rupees (documented in the business case).
    private const decimal ResaleServiceFeeRate = 0.12m; // ~12% success fee to UPS
    private const decimal Co2PriceInrPerKg = 4m;         // illustrative carbon value (INR/kg)
    private const decimal AiCostPerItem = 0.5m;          // GPT-4o-mini + infra per item (INR)
    private const decimal DefaultMarginRate = 0.20m;     // margin as share of price if none supplied

    public static RevenueOpportunity Calculate(
        decimal freightAvoided,
        decimal salePrice,
        double co2SavedKg,
        decimal? resaleMargin = null)
    {
        var margin = resaleMargin ?? Math.Round(salePrice * DefaultMarginRate, 2);
        var serviceFee = Math.Round(salePrice * ResaleServiceFeeRate, 2);
        var co2Value = Math.Round((decimal)co2SavedKg * Co2PriceInrPerKg, 2);

        var total = Math.Round(freightAvoided + margin + serviceFee + co2Value - AiCostPerItem, 2);

        return new RevenueOpportunity
        {
            FreightAvoided = Math.Round(freightAvoided, 2),
            ResaleMargin = margin,
            ResaleServiceFee = serviceFee,
            Co2ValueInr = co2Value,
            AiCost = AiCostPerItem,
            TotalNetValue = total
        };
    }
}
