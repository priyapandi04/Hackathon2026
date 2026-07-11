namespace UPS.ReLoop.Application.Services;

using System.Text;
using UPS.ReLoop.Application.DTOs.BusinessExplanation;

public static class BusinessExplanationPromptBuilder
{
    public static string Build(BusinessExplanationRequest request)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are a supply chain and logistics business analyst for UPS ReLoop Nexus.");
        sb.AppendLine("Generate a concise business explanation for the following returned product decision.");
        sb.AppendLine();
        sb.AppendLine("Context:");
        sb.AppendLine($"- Product: {request.ProductName}");

        if (!string.IsNullOrWhiteSpace(request.Category))
            sb.AppendLine($"- Category: {request.Category}");
        if (request.DemandScore.HasValue)
            sb.AppendLine($"- Local Demand Score: {request.DemandScore:F0}/100");
        if (request.DistanceSavedKm.HasValue)
            sb.AppendLine($"- Distance Saved: {request.DistanceSavedKm:F0} km");
        if (request.CostSaved.HasValue)
            sb.AppendLine($"- Cost Saved: ${request.CostSaved:F2}");
        if (request.Co2Saved.HasValue)
            sb.AppendLine($"- CO2 Saved: {request.Co2Saved:F2} kg");
        if (request.MatchScore.HasValue)
            sb.AppendLine($"- Match Score: {request.MatchScore}/100");
        if (!string.IsNullOrWhiteSpace(request.Recommendation))
            sb.AppendLine($"- Recommendation: {request.Recommendation}");

        sb.AppendLine();
        sb.AppendLine("Respond ONLY with valid JSON (no markdown, no code fences):");
        sb.AppendLine("{");
        sb.AppendLine("  \"explanation\": \"<2-3 sentence business explanation focusing on logistics efficiency, sustainability, and cost savings>\",");
        sb.AppendLine("  \"summary\": \"<one-line summary>\",");
        sb.AppendLine("  \"keyBenefits\": [\"<benefit 1>\", \"<benefit 2>\", \"<benefit 3>\"]");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
