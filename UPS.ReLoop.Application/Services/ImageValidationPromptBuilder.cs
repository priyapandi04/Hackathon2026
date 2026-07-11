namespace UPS.ReLoop.Application.Services;

using System.Text;

public static class ImageValidationPromptBuilder
{
    public static string Build(string? productCategory = null, string? additionalContext = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are an expert product return validation agent for UPS ReLoop Nexus.");
        sb.AppendLine("Analyze the provided image of a returned product and evaluate the following criteria:");
        sb.AppendLine();
        sb.AppendLine("1. **Product Condition** - Assess overall condition (New, Like New, Good, Fair, Poor, Damaged)");
        sb.AppendLine("2. **Visible Damage** - Score damage from 0 (no damage) to 10 (severely damaged)");
        sb.AppendLine("3. **Missing Tags** - Determine if original tags/labels are missing (true/false)");
        sb.AppendLine("4. **Packaging Quality** - Consider packaging state in your assessment");
        sb.AppendLine("5. **Resale Eligibility** - Determine if the item is eligible for resale (true/false)");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(productCategory))
        {
            sb.AppendLine($"Product Category: {productCategory}");
        }

        if (!string.IsNullOrWhiteSpace(additionalContext))
        {
            sb.AppendLine($"Additional Context: {additionalContext}");
        }

        sb.AppendLine();
        sb.AppendLine("Respond ONLY with a valid JSON object in this exact format (no markdown, no code fences):");
        sb.AppendLine("{");
        sb.AppendLine("  \"condition\": \"<New|Like New|Good|Fair|Poor|Damaged>\",");
        sb.AppendLine("  \"damageScore\": <0-10>,");
        sb.AppendLine("  \"missingTags\": <true|false>,");
        sb.AppendLine("  \"eligible\": <true|false>,");
        sb.AppendLine("  \"confidence\": <0.0-1.0>,");
        sb.AppendLine("  \"remarks\": \"<brief explanation>\"");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
