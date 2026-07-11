namespace UPS.ReLoop.Application.Interfaces;

using UPS.ReLoop.Application.DTOs.Decision;

/// <summary>
/// Grounds resale eligibility in retailer policy. A policy block always overrides
/// item condition and yields a citation, preventing the LLM from "inventing"
/// permission to resell restricted goods.
/// </summary>
public interface IRetailerPolicyService
{
    /// <summary>Evaluate whether a category may be resold locally, with a citation.</summary>
    PolicyComplianceResult Evaluate(string category);

    /// <summary>Build a grounded citation for the governing policy of a category.</summary>
    Citation GetCitation(string category);
}
