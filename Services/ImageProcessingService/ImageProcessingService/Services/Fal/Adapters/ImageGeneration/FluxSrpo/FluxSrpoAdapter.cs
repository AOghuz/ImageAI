// Services/Fal/Adapters/ImageGeneration/FluxSrpo/FluxSrpoAdapter.cs
using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using ImageProcessingService.Services.Fal.Adapters.Core;

namespace ImageProcessingService.Services.Fal.Adapters.ImageGeneration.FluxSrpo;

public sealed class FluxSrpoAdapter : FalModelAdapterBase
{
    public override string Key => "flux-srpo";

    public override bool SupportsTextToImage => true;
    public override bool SupportsImageEdit => false;

    // SUBMIT:  {base}/srpo     (örn: fal-ai/flux/srpo)
    // STATUS/RESULT BASE: cfg.ModelPath (örn: fal-ai/flux)
    public override string GetTextToImagePath(FalModelConfig cfg)
    {
        var basePath = (cfg.ModelPath ?? "").TrimEnd('/');
        return $"{basePath}/srpo";
    }

    public override object BuildTextToImagePayload(TextToImageRequest req, FalModelConfig cfg)
    {
        var n = ClampNumImages(req.NumImages, cfg);
        var fmt = NormalizeFormat(req.OutputFormat, cfg);
        var ex = req.Extras ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        var payload = new Dictionary<string, object>
        {
            ["prompt"] = req.Prompt,
            ["num_images"] = n,
            ["output_format"] = fmt
        };

        if (TryGetObject(ex, "image_size") is object imageSize) payload["image_size"] = imageSize;
        if (TryGetInt(ex, "num_inference_steps") is int steps) payload["num_inference_steps"] = steps;
        if (TryGetInt(ex, "seed") is int seed) payload["seed"] = seed;
        if (TryGetDouble(ex, "guidance_scale") is double guidance) payload["guidance_scale"] = guidance;
        if (TryGetBool(ex, "sync_mode") is bool syncMode) payload["sync_mode"] = syncMode;
        if (TryGetBool(ex, "enable_safety_checker") is bool safety) payload["enable_safety_checker"] = safety;

        // boş/gelmediyse hiç koyma → FAL default’u "none"
        if (TryGetString(ex, "acceleration") is string accel && !string.IsNullOrWhiteSpace(accel))
            payload["acceleration"] = accel;

        return payload;
    }

    public override object BuildImageEditPayload(ImageEditRequest req, FalModelConfig cfg)
        => throw new NotSupportedException("flux-srpo image-edit desteklemez.");

    // helpers
    private static int? TryGetInt(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) ? v switch
        {
            int i => i,
            long l => (int)l,
            string s when int.TryParse(s, out var x) => x,
            _ => (int?)null
        } : null;

    private static double? TryGetDouble(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) ? v switch
        {
            double db => db,
            float f => (double)f,
            int i => (double)i,
            string s when double.TryParse(s, out var x) => x,
            _ => (double?)null
        } : null;

    private static bool? TryGetBool(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) ? v switch
        {
            bool b => b,
            string s when bool.TryParse(s, out var x) => x,
            _ => (bool?)null
        } : null;

    private static string? TryGetString(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) ? v?.ToString() : null;

    private static object? TryGetObject(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) ? v : null;
}
