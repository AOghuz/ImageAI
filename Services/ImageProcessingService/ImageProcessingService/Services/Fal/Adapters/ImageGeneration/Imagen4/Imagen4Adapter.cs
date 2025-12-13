using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using ImageProcessingService.Services.Fal.Adapters.Core;

namespace ImageProcessingService.Services.Fal.Adapters.ImageGeneration.Imagen4;

public sealed class Imagen4Adapter : FalModelAdapterBase
{
    public override string Key => "imagen4";
    public override bool SupportsTextToImage => true;
    public override bool SupportsImageEdit => false;

    // SUBMIT:  {base}/preview  -> ör: fal-ai/imagen4/preview
    // STATUS/RESULT BASE: cfg.ModelPath -> ör: fal-ai/imagen4
    public override string GetTextToImagePath(FalModelConfig cfg)
        => (cfg.ModelPath ?? "").TrimEnd('/') + "/preview";

    public override object BuildTextToImagePayload(TextToImageRequest req, FalModelConfig cfg)
    {
        var extras = req.Extras ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        var n = Math.Clamp(req.NumImages ?? 1, 1, Math.Max(1, cfg.MaxImages)); // 1–4

        var payload = new Dictionary<string, object>
        {
            ["prompt"] = req.Prompt,
            ["num_images"] = n
        };

        if (TryGetString(extras, "negative_prompt") is string neg) payload["negative_prompt"] = neg;
        if (TryGetString(extras, "aspect_ratio") is string ar) payload["aspect_ratio"] = ar;   // "1:1","16:9","9:16","3:4","4:3"
        if (TryGetInt(extras, "seed") is int seed) payload["seed"] = seed;
        if (TryGetString(extras, "resolution") is string res) payload["resolution"] = res;   // "1K" | "2K"

        return payload;
    }

    public override object BuildImageEditPayload(ImageEditRequest req, FalModelConfig cfg)
        => throw new NotSupportedException("imagen4 image edit desteklemez.");

    private static string? TryGetString(IDictionary<string, object> d, string k)
        => d.TryGetValue(k, out var v) ? v?.ToString() : null;

    private static int? TryGetInt(IDictionary<string, object> d, string k)
        => d.TryGetValue(k, out var v) ? v switch
        {
            int i => i,
            long l => (int)l,
            string s when int.TryParse(s, out var x) => x,
            _ => (int?)null
        } : null;
}
