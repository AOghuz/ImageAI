namespace ImageProcessingService.Services.Fal.Abstract.Requests;

public sealed class TextToImageRequest
{
    public required string Prompt { get; init; }
    public int? NumImages { get; init; }          // adapter model limitiyle clamp edilir
    public string? OutputFormat { get; init; }    // "jpeg" | "png" (adapter default uygular)
    public Dictionary<string, object>? Extras { get; init; } // modele özel parametreler
}
