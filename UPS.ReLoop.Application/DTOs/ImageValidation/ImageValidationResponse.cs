namespace UPS.ReLoop.Application.DTOs.ImageValidation;

using System.Text.Json.Serialization;

public class ImageValidationResponse
{
    [JsonPropertyName("condition")]
    public string Condition { get; set; } = string.Empty;

    [JsonPropertyName("damageScore")]
    public int DamageScore { get; set; }

    [JsonPropertyName("missingTags")]
    public bool MissingTags { get; set; }

    [JsonPropertyName("eligible")]
    public bool Eligible { get; set; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("remarks")]
    public string Remarks { get; set; } = string.Empty;
}
