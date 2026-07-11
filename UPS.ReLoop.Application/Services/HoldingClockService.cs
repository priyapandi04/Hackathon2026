namespace UPS.ReLoop.Application.Services;

using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.DTOs.Decision;
using UPS.ReLoop.Application.Interfaces;

/// <summary>
/// Deterministic implementation of the 10-day holding clock — the central UPS
/// problem-statement constraint. All arithmetic runs here, never in the LLM, so
/// the countdown and the auto-return-to-seller trigger are fully auditable.
/// </summary>
public class HoldingClockService : IHoldingClockService
{
    private const int ClosingWindowThreshold = 8; // day 8+ = window closing
    private readonly ILogger<HoldingClockService> _logger;

    public HoldingClockService(ILogger<HoldingClockService> logger)
    {
        _logger = logger;
    }

    public HoldingClockResult Evaluate(DateTime pickupDate, DateTime? now = null)
    {
        var reference = now ?? DateTime.UtcNow;
        var elapsed = (int)Math.Floor((reference.Date - pickupDate.Date).TotalDays);
        return EvaluateFromDays(elapsed);
    }

    public HoldingClockResult EvaluateFromDays(int holdingDaysCompleted)
    {
        var day = Math.Max(0, holdingDaysCompleted);
        var remaining = HoldingClockResult.MaxHoldingDays - day;
        var expired = day >= HoldingClockResult.MaxHoldingDays;

        var result = new HoldingClockResult
        {
            HoldingDay = day,
            DaysRemaining = Math.Max(0, remaining),
            IsExpired = expired,
            AutoReturnTriggered = expired,
            ClockStatus = expired ? "Expired" : day >= ClosingWindowThreshold ? "ClosingWindow" : "OnTrack"
        };

        result.Message = expired
            ? $"Day {day}/{HoldingClockResult.MaxHoldingDays}: holding window elapsed — item auto-returned to seller."
            : day >= ClosingWindowThreshold
                ? $"Day {day}/{HoldingClockResult.MaxHoldingDays}: {result.DaysRemaining} day(s) left — divert now or it returns to seller."
                : $"Day {day}/{HoldingClockResult.MaxHoldingDays}: {result.DaysRemaining} day(s) left in local holding window.";

        if (expired)
            _logger.LogInformation("Holding clock EXPIRED at day {Day} — auto return-to-seller triggered.", day);

        return result;
    }
}
