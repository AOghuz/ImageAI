namespace ImageProcessingService.Services.Fal.Abstract.Requests;

public sealed class ImageEditRequest
{
    public required string Prompt { get; init; }
    public required IReadOnlyList<string> ImageDataUris { get; init; } // controller dönüştürür
    public int? NumImages { get; init; }
    public string? OutputFormat { get; init; }
    public Dictionary<string, object>? Extras { get; init; }
}
