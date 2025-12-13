using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using ImageProcessingService.Services.Fal.Adapters.Core;

namespace ImageProcessingService.Services.Fal.Adapters.ImageEditing.QwenImageEdit;

public sealed class QwenImageEditAdapter : FalModelAdapterBase
{
    public override string Key => "qwen-image-edit";

    // Qwen Image Edit sadece image editing destekler
    public override bool SupportsTextToImage => false;
    public override bool SupportsImageEdit => true;

    // Qwen Image Edit'te direkt endpoint var, /edit suffix'i yok
    public override string GetImageEditPath(FalModelConfig cfg) => cfg.ModelPath!; // "fal-ai/qwen-image-edit"

    public override object BuildTextToImagePayload(TextToImageRequest req, FalModelConfig cfg)
        => throw new NotSupportedException("qwen-image-edit text-to-image desteklemez.");

    // Qwen Image Edit input şeması:
    // prompt (string), image_url (string), image_size (string|object),
    // num_inference_steps (int, default 30), seed (int), guidance_scale (float, default 4),
    // sync_mode (bool), num_images (int, default 1), enable_safety_checker (bool, default true),
    // output_format ("jpeg"/"png", default "png"), negative_prompt (string), 
    // acceleration ("none"/"regular"/"high", default "regular")
    public override object BuildImageEditPayload(ImageEditRequest req, FalModelConfig cfg)
    {
        if (req.ImageDataUris is null || req.ImageDataUris.Count == 0)
            throw new ArgumentException("Qwen Image Edit için en az 1 görüntü gereklidir.");

        var n = ClampNumImages(req.NumImages, cfg);
        var fmt = NormalizeFormat(req.OutputFormat, cfg);

        var extras = req.Extras ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // Qwen Image Edit parametreleri
        object? imageSize = TryGetImageSize(extras, "image_size");
        int? steps = TryGetInt(extras, "num_inference_steps");
        int? seed = TryGetInt(extras, "seed");
        double? guidance = TryGetDouble(extras, "guidance_scale");
        bool? syncMode = TryGetBool(extras, "sync_mode");
        bool? safety = TryGetBool(extras, "enable_safety_checker");
        string? negativePrompt = TryGetString(extras, "negative_prompt");
        string? acceleration = TryGetString(extras, "acceleration") ?? "regular";

        // Payload oluştur - null değerleri dahil etme
        var payload = new Dictionary<string, object>
        {
            ["prompt"] = req.Prompt,
            ["image_url"] = req.ImageDataUris[0], // Qwen tek görüntü alıyor
            ["num_images"] = n,
            ["output_format"] = fmt
        };

        if (imageSize != null) payload["image_size"] = imageSize;
        if (steps.HasValue) payload["num_inference_steps"] = steps.Value;
        if (seed.HasValue) payload["seed"] = seed.Value;
        if (guidance.HasValue) payload["guidance_scale"] = guidance.Value;
        if (syncMode.HasValue) payload["sync_mode"] = syncMode.Value;
        if (safety.HasValue) payload["enable_safety_checker"] = safety.Value;
        if (!string.IsNullOrWhiteSpace(negativePrompt)) payload["negative_prompt"] = negativePrompt;
        if (!string.IsNullOrWhiteSpace(acceleration)) payload["acceleration"] = acceleration;

        return payload;
    }

    // Image size özel handling - string veya object olabilir
    private static object? TryGetImageSize(IDictionary<string, object> d, string k)
    {
        if (!d.TryGetValue(k, out var v)) return null;

        return v switch
        {
            string s => s, // Predefined size like "square_hd", "landscape_16_9", etc.
            IDictionary<string, object> dict when dict.ContainsKey("width") && dict.ContainsKey("height") =>
                new { width = TryGetInt(dict, "width"), height = TryGetInt(dict, "height") },
            _ => null
        };
    }

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
}