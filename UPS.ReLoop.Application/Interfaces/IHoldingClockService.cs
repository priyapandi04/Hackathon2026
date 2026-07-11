namespace UPS.ReLoop.Application.Interfaces;

using UPS.ReLoop.Application.DTOs.Decision;

/// <summary>
/// Deterministic 10-day holding clock. Computes the holding day, days remaining,
/// and whether an item must be auto-returned to the seller. Pure logic — no LLM.
/// </summary>
public interface IHoldingClockService
{
    /// <summary>
    /// Evaluate the clock from a pickup date. If <paramref name="now"/> is null,
    /// UTC now is used.
    /// </summary>
    HoldingClockResult Evaluate(DateTime pickupDate, DateTime? now = null);

    /// <summary>
    /// Evaluate the clock from an explicit number of holding days already completed
    /// (useful for the provided synthetic dataset).
    /// </summary>
    HoldingClockResult EvaluateFromDays(int holdingDaysCompleted);
}
