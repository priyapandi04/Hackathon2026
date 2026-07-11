namespace UPS.ReLoop.Application.Interfaces;

using UPS.ReLoop.Application.DTOs.Decision;

/// <summary>
/// Autonomous Diversion / Dynamic-Pricing agent. Given the holding day, the
/// local demand/match strength and a base price, it decides how to clear the
/// item locally before the 10-day clock forces a costly return — adjusting
/// price, widening the radius, offering to Access Points, or escalating.
/// </summary>
public interface IDiversionAgentService
{
    DiversionDecision Decide(int matchScore, HoldingClockResult clock, decimal basePrice, bool resaleAllowed);
}
