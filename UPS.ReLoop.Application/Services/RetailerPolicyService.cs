namespace UPS.ReLoop.Application.Services;

using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.DTOs.Decision;
using UPS.ReLoop.Application.Interfaces;

/// <summary>
/// Policy-first eligibility grounding. Uses a deterministic retailer-policy
/// catalog (seeded in-memory for the MVP; backed by the RetailerPolicies table
/// in production). A restricted category — hygiene, food, serialized electronics
/// — is refused regardless of physical condition, and every decision returns a
/// citation (policy ref) so it is auditable and cannot be hallucinated.
/// </summary>
public class RetailerPolicyService : IRetailerPolicyService
{
    private sealed record PolicyEntry(string PolicyRef, string PolicyName, bool ResaleAllowed, string Rationale);

    // Seeded retailer-policy catalog. In production this is loaded from the
    // RetailerPolicies table / retailer integration.
    private static readonly IReadOnlyDictionary<string, PolicyEntry> Catalog =
        new Dictionary<string, PolicyEntry>(StringComparer.OrdinalIgnoreCase)
        {
            ["apparel"]     = new("RP-APP-2.1",  "Apparel Resale Policy",            true,  "Apparel in resalable condition may be sold locally."),
            ["general"]     = new("RP-GEN-1.0",  "General Merchandise Policy",       true,  "General merchandise in good condition is eligible for local resale."),
            ["electronics"] = new("RP-ELEC-3.2", "Electronics (Non-Serialized)",     true,  "Non-serialized electronics may be resold after condition grading."),
            ["footwear"]    = new("RP-FTW-1.4",  "Footwear Resale Policy",           true,  "Footwear in like-new condition may be resold locally."),
            ["hygiene"]     = new("RP-HYG-1.0",  "Personal Care & Hygiene Policy",   false, "Opened hygiene / personal-care items cannot be resold for safety reasons."),
            ["food"]        = new("RP-FOOD-1.0", "Perishable / Food Policy",         false, "Food and perishable goods are prohibited from local resale."),
            ["serialized"]  = new("RP-SER-2.0",  "Serialized-Electronics Policy",    false, "Serialized / warranty-tracked electronics must return to the seller."),
            ["medical"]     = new("RP-MED-1.0",  "Medical Devices Policy",           false, "Medical devices are restricted from resale by retailer policy."),
        };

    private static readonly PolicyEntry Default =
        new("RP-DEF-0.0", "Default Return Policy", false, "No matching resale policy found; route to seller pending review.");

    private readonly ILogger<RetailerPolicyService> _logger;

    public RetailerPolicyService(ILogger<RetailerPolicyService> logger)
    {
        _logger = logger;
    }

    public PolicyComplianceResult Evaluate(string category)
    {
        var entry = Resolve(category);
        var restricted = !entry.ResaleAllowed;

        if (restricted)
            _logger.LogInformation("Policy block for category '{Category}' via {PolicyRef}: {Rationale}",
                category, entry.PolicyRef, entry.Rationale);

        return new PolicyComplianceResult
        {
            ResaleAllowed = entry.ResaleAllowed,
            IsRestrictedCategory = restricted,
            PolicyRef = entry.PolicyRef,
            PolicyName = entry.PolicyName,
            Reason = entry.Rationale
        };
    }

    public Citation GetCitation(string category)
    {
        var entry = Resolve(category);
        return new Citation("policy", entry.PolicyRef, entry.Rationale);
    }

    private static PolicyEntry Resolve(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return Default;

        if (Catalog.TryGetValue(category.Trim(), out var exact))
            return exact;

        // Fuzzy contains-match so free-text categories still ground to a policy.
        foreach (var kvp in Catalog)
        {
            if (category.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }

        return Default;
    }
}
