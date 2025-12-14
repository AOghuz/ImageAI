using System.Text.Json.Serialization;

namespace ImageProcessingService.Services.Fal.Abstract.Requests;

public class TextToImageRequest
{
    public string Prompt { get; set; } = default!;
    public string? ImageSize { get; set; } // "square_hd", "landscape_4_3" vb.
    public int? Seed { get; set; }
    public int? NumImages { get; set; } = 1;
    public string? OutputFormat { get; set; }

    // Dökümanlardaki özel parametreler (guidance_scale, safety_checker, aspect_ratio, resolution vb.) buraya gelecek.
    [JsonExtensionData]
    public Dictionary<string, object> AdditionalParams { get; set; } = new();
}