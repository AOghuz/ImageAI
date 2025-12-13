using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using ImageProcessingService.Services.Fal.Adapters.Core;

namespace ImageProcessingService.Services.Fal.Adapters.ImageGeneration.QwenImage;

public sealed class QwenImageAdapter : FalModelAdapterBase
{
    public override string Key => "qwen-image";

    public override bool SupportsTextToImage => true;
    public override bool SupportsImageEdit => false;

    // Text-to-Image endpoint
    public override string GetTextToImagePath(FalModelConfig cfg) => cfg.ModelPath!;

    public override object BuildTextToImagePayload(TextToImageRequest req, FalModelConfig cfg)
    {
        var n = ClampNumImages(req.NumImages, cfg);
        var fmt = NormalizeFormat(req.OutputFormat, cfg);
        var extras = req.Extras ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        var payload = new Dictionary<string, object>
        {
            ["prompt"] = req.Prompt,
            ["num_images"] = n,
            ["output_format"] = fmt
        };

        // Qwen Image specific parameters
        if (TryGetObject(extras, "image_size") is object imageSize)
            payload["image_size"] = imageSize;

        if (TryGetInt(extras, "num_inference_steps") is int steps)
            payload["num_inference_steps"] = steps;

        if (TryGetInt(extras, "seed") is int seed)
            payload["seed"] = seed;

        if (TryGetDouble(extras, "guidance_scale") is double guidance)
            payload["guidance_scale"] = guidance;

        if (TryGetBool(extras, "sync_mode") is bool syncMode)
            payload["sync_mode"] = syncMode;

        if (TryGetBool(extras, "enable_safety_checker") is bool safety)
            payload["enable_safety_checker"] = safety;

        if (TryGetString(extras, "negative_prompt") is string negativePrompt)
            payload["negative_prompt"] = negativePrompt;

        if (TryGetString(extras, "acceleration") is string acceleration)
            payload["acceleration"] = acceleration;

        if (TryGetArray(extras, "loras") is object[] loras)
        {
            // Qwen Image maksimum 3 LoRA kabul eder
            if (loras.Length > 3)
                throw new ArgumentException("Qwen Image maksimum 3 LoRA destekler.");

            payload["loras"] = loras;
        }

        return payload;
    }

    public override object BuildImageEditPayload(ImageEditRequest req, FalModelConfig cfg) =>
        throw new NotSupportedException("Qwen Image sadece text-to-image destekler.");

    // Helper methods
    private static int? TryGetInt(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) ? v switch
        {
            int i => i,
            long l => (int)l,
            string s when int.TryParse(s, out var x) => x,
            _ => null
        } : null;

    private static double? TryGetDouble(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) ? v switch
        {
            double db => db,
            float f => (double)f,
            int i => (double)i,
            string s when double.TryParse(s, out var x) => x,
            _ => null
        } : null;

    private static bool? TryGetBool(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) ? v switch
        {
            bool b => b,
            string s when bool.TryParse(s, out var x) => x,
            _ => null
        } : null;

    private static string? TryGetString(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) ? v?.ToString() : null;

    private static object? TryGetObject(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) ? v : null;

    private static object[]? TryGetArray(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) ? v as object[] : null;
}