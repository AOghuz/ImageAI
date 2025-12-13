using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using ImageProcessingService.Services.Fal.Adapters.Core;

namespace ImageProcessingService.Services.Fal.Adapters.ImageEditing.NanoBanana;

public sealed class NanoBananaAdapter : FalModelAdapterBase
{
    public override string Key => "nano-banana";

    public override string GetImageEditPath(FalModelConfig cfg) => cfg.ModelPath! + "/edit";

    // FAL dokümantasyonuna göre exact payload
    // Services/Fal/Adapters/ImageEditing/NanoBanana/NanoBananaAdapter.cs
    public override object BuildImageEditPayload(ImageEditRequest req, FalModelConfig cfg)
    {
        if (req.ImageDataUris is null || req.ImageDataUris.Count == 0)
            throw new ArgumentException("Nano-banana için en az 1 görüntü gereklidir.");

        foreach (var url in req.ImageDataUris)
        {
            if (string.IsNullOrWhiteSpace(url) ||
               !(url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                 url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"Geçersiz URL: {url}");
        }

        var n = ClampNumImages(req.NumImages, cfg);
        var payload = new Dictionary<string, object>
        {
            ["prompt"] = req.Prompt,
            ["image_urls"] = req.ImageDataUris.ToArray(),
            ["num_images"] = n
        };

        if (!string.IsNullOrWhiteSpace(req.OutputFormat))
            payload["output_format"] = req.OutputFormat; // "jpeg" | "png"

        if (req.Extras is { Count: > 0 } && req.Extras.TryGetValue("sync_mode", out var sm))
            payload["sync_mode"] = sm;

        return payload;
    }


}