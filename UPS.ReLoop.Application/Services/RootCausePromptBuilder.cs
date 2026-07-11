namespace UPS.ReLoop.Application.Services;

using System.Text;
using UPS.ReLoop.Application.DTOs.RootCauseAgent;

public static class RootCausePromptBuilder
{
    public static string Build(RootCauseRequest request, List<ReasonBreakdown> breakdown)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are a retail analytics expert for UPS ReLoop Nexus.");
        sb.AppendLine("Analyze the following product return data and identify the root cause.");
        sb.AppendLine();
        sb.AppendLine($"Total Returns: {request.Returns.Count}");
        sb.AppendLine();
        sb.AppendLine("Return Reason Breakdown:");
        foreach (var item in breakdown)
        {
            sb.AppendLine($"- \"{item.Reason}\": {item.Count} occurrences ({item.Percentage:F1}%)");
        }

        sb.AppendLine();
        sb.AppendLine("Categories involved:");
        var categories = request.Returns.Select(r => r.Category).Distinct();
        foreach (var cat in categories)
            sb.AppendLine($"- {cat}");

        sb.AppendLine();
        sb.AppendLine("Locations involved:");
        var locations = request.Returns.Select(r => r.Location).Distinct();
        foreach (var loc in locations)
            sb.AppendLine($"- {loc}");

        sb.AppendLine();
        sb.AppendLine("Products involved:");
        var products = request.Returns.Select(r => r.ProductName).Distinct().Take(10);
        foreach (var p in products)
            sb.AppendLine($"- {p}");

        sb.AppendLine();
        sb.AppendLine("Tasks:");
        sb.AppendLine("1. Identify the root cause (group similar reasons into one underlying cause)");
        sb.AppendLine("2. Calculate the frequency percentage of that root cause");
        sb.AppendLine("3. Generate a specific actionable recommendation");
        sb.AppendLine("4. Estimate the operational impact (e.g., reduce returns by X%)");
        sb.AppendLine();
        sb.AppendLine("Respond ONLY with valid JSON (no markdown, no code fences):");
        sb.AppendLine("{");
        sb.AppendLine("  \"rootCause\": \"<underlying root cause>\",");
        sb.AppendLine("  \"frequency\": \"<percentage as string e.g. 72%>\",");
        sb.AppendLine("  \"recommendation\": \"<specific actionable recommendation>\",");
        sb.AppendLine("  \"impact\": \"<estimated operational impact>\"");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
