using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using ImageProcessingService.Services.Fal.Adapters.Core;

namespace ImageProcessingService.Services.Fal.Adapters.ImageEditing.FluxKontext;

public sealed class FluxKontextAdapter : FalModelAdapterBase
{
    public override string Key => "flux-kontext-dev";

    public override bool SupportsTextToImage => false;
    public override bool SupportsImageEdit => true;

    // SUBMIT path: ModelPath + "/dev" (fal-ai/flux-kontext + /dev)
    public override string GetImageEditPath(FalModelConfig cfg) => cfg.ModelPath! + "/dev";

    public override object BuildTextToImagePayload(TextToImageRequest req, FalModelConfig cfg)
        => throw new NotSupportedException("flux-kontext-dev text-to-image desteklemez.");

    public override object BuildImageEditPayload(ImageEditRequest req, FalModelConfig cfg)
    {
        if (req.ImageDataUris is null || req.ImageDataUris.Count == 0)
            throw new ArgumentException("FluxKontext için en az 1 görüntü gereklidir.");

        // URL validation
        var imageUrl = req.ImageDataUris[0];
        if (string.IsNullOrWhiteSpace(imageUrl) ||
           !(imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
             imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException($"Geçersiz image URL: {imageUrl}");
        }

        var numImages = ClampNumImages(req.NumImages, cfg);
        var outputFormat = NormalizeFormat(req.OutputFormat, cfg);
        var extras = req.Extras ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // FAL API parametrelerine göre payload
        var payload = new Dictionary<string, object>
        {
            ["prompt"] = req.Prompt,
            ["image_url"] = imageUrl, // Tek URL - array değil
            ["num_images"] = numImages,
            ["output_format"] = outputFormat
        };

        // Opsiyonel parametreler - sadece değer varsa ekle
        if (TryGetInt(extras, "num_inference_steps") is int steps)
            payload["num_inference_steps"] = steps;

        if (TryGetDouble(extras, "guidance_scale") is double guidance)
            payload["guidance_scale"] = guidance;

        if (TryGetInt(extras, "seed") is int seed)
            payload["seed"] = seed;

        if (TryGetBool(extras, "enable_safety_checker") is bool safety)
            payload["enable_safety_checker"] = safety;

        var acceleration = TryGetString(extras, "acceleration");
        if (!string.IsNullOrEmpty(acceleration) &&
            (acceleration == "none" || acceleration == "regular" || acceleration == "high"))
        {
            payload["acceleration"] = acceleration;
        }

        var resolutionMode = TryGetString(extras, "resolution_mode");
        if (!string.IsNullOrEmpty(resolutionMode))
        {
            payload["resolution_mode"] = resolutionMode;
        }

        // enhance_prompt boolean parametresi
        if (TryGetBool(extras, "enhance_prompt") is bool enhance)
            payload["enhance_prompt"] = enhance;

        return payload;
    }

    // Helper methods
    private static int? TryGetInt(IDictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value)) return null;

        return value switch
        {
            int i => i,
            long l when l >= int.MinValue && l <= int.MaxValue => (int)l,
            string s when int.TryParse(s, out var result) => result,
            _ => null
        };
    }

    private static double? TryGetDouble(IDictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value)) return null;

        return value switch
        {
            double d => d,
            float f => (double)f,
            int i => (double)i,
            string s when double.TryParse(s, out var result) => result,
            _ => null
        };
    }

    private static bool? TryGetBool(IDictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value)) return null;

        return value switch
        {
            bool b => b,
            string s when bool.TryParse(s, out var result) => result,
            _ => null
        };
    }

    private static string? TryGetString(IDictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value) || value is null) return null;

        var str = value.ToString();
        return string.IsNullOrWhiteSpace(str) ? null : str;
    }
}