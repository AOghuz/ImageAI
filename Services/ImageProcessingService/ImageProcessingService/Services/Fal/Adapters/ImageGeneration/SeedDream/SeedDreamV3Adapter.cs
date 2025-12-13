// Services/Fal/Adapters/ImageGeneration/SeedDream/SeedDreamV3Adapter.cs
using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using ImageProcessingService.Services.Fal.Adapters.Core;

namespace ImageProcessingService.Services.Fal.Adapters.ImageGeneration.SeedDream;

public sealed class SeedDreamV3Adapter : FalModelAdapterBase
{
    public override string Key => "seedream-v3";
    public override bool SupportsTextToImage => true;
    public override bool SupportsImageEdit => false;

    public override string GetTextToImagePath(FalModelConfig cfg)
    {
        var basePath = (cfg.ModelPath ?? "").TrimEnd('/');
        return $"{basePath}/text-to-image";
    }

    public override object BuildTextToImagePayload(TextToImageRequest req, FalModelConfig cfg)
    {
        var payload = new Dictionary<string, object>
        {
            ["prompt"] = req.Prompt
        };

        // num_images - sadece değer varsa
        if (req.NumImages.HasValue && req.NumImages.Value > 0)
        {
            payload["num_images"] = ClampNumImages(req.NumImages, cfg);
        }

        // Extras'tan parametreleri al
        if (req.Extras is { Count: > 0 })
        {
            foreach (var kv in req.Extras)
            {
                var key = kv.Key.ToLowerInvariant();
                var value = kv.Value;

                if (value is null) continue;

                // FAL API'ye olduğu gibi gönder (controller zaten doğru format verdi)
                payload[key] = value;
            }
        }

        return payload;
    }

    public override object BuildImageEditPayload(ImageEditRequest req, FalModelConfig cfg)
        => throw new NotSupportedException("SeedDream v3 sadece text-to-image destekler.");
}