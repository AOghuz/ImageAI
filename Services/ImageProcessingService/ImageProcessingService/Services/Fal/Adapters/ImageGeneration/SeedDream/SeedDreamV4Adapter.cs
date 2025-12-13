using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using ImageProcessingService.Services.Fal.Adapters.Core;

namespace ImageProcessingService.Services.Fal.Adapters.ImageGeneration.SeedDream;

public sealed class SeedDreamV4Adapter : FalModelAdapterBase
{
    public override string Key => "seedream-v4-text-to-image";

    public override bool SupportsTextToImage => true;
    public override bool SupportsImageEdit => false;

    // Text-to-Image endpoint
    public override string GetTextToImagePath(FalModelConfig cfg) => cfg.ModelPath!;

    public override object BuildTextToImagePayload(TextToImageRequest req, FalModelConfig cfg)
    {
        var n = ClampNumImages(req.NumImages, cfg);
        var extras = req.Extras ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        var payload = new Dictionary<string, object>
        {
            ["prompt"] = req.Prompt,
            ["num_images"] = n
        };

        // SeedDream v4 specific parameters
        if (TryGetObject(extras, "image_size") is object imageSize)
            payload["image_size"] = imageSize;

        if (TryGetInt(extras, "max_images") is int maxImages)
            payload["max_images"] = Math.Clamp(maxImages, 1, cfg.MaxImages);

        if (TryGetInt(extras, "seed") is int seed)
            payload["seed"] = seed;

        if (TryGetBool(extras, "sync_mode") is bool syncMode)
            payload["sync_mode"] = syncMode;

        if (TryGetBool(extras, "enable_safety_checker") is bool safety)
            payload["enable_safety_checker"] = safety;

        return payload;
    }

    public override object BuildImageEditPayload(ImageEditRequest req, FalModelConfig cfg) =>
        throw new NotSupportedException("SeedDream v4 Text-to-Image sadece text-to-image destekler.");

    // Helper methods
    private static int? TryGetInt(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) ? v switch
        {
            int i => i,
            long l => (int)l,
            string s when int.TryParse(s, out var x) => x,
            _ => null
        } : null;

    private static bool? TryGetBool(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) ? v switch
        {
            bool b => b,
            string s when bool.TryParse(s, out var x) => x,
            _ => null
        } : null;

    private static object? TryGetObject(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) ? v : null;
}