using System.Text.Json.Serialization;

namespace ImageProcessingService.Services.Fal.Abstract.Requests;

public class ImageEditRequest
{
    public string ImageUrl { get; set; } = default!;
    public string? Prompt { get; set; } // Bazı modellerde opsiyonel
    public string? MaskUrl { get; set; }
    public string? OutputFormat { get; set; }

    // Dökümanlardaki: strength, guidance_scale, subject_detection vb. buraya gelecek.
    [JsonExtensionData]
    public Dictionary<string, object> AdditionalParams { get; set; } = new();
}