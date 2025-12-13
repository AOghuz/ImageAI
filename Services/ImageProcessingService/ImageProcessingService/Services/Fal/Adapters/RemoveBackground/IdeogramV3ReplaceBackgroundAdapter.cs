using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using ImageProcessingService.Services.Fal.Adapters.Core;

namespace ImageProcessingService.Services.Fal.Adapters.RemoveBackground;

public sealed class IdeogramV3ReplaceBackgroundAdapter : FalModelAdapterBase
{
    // appsettings: FalModels altındaki key ile aynı olmalı
    public override string Key => "ideogram-v3-replace-background";

    public override bool SupportsTextToImage => false;
    public override bool SupportsImageEdit => true;

    // Bu modelde tek endpoint var: "fal-ai/ideogram/v3/replace-background"
    public override string GetImageEditPath(FalModelConfig cfg)
    {
        // basePath: fal-ai/ideogram  -> submit: fal-ai/ideogram/v3/replace-background
        var basePath = cfg.ModelPath?.TrimEnd('/') ?? "fal-ai/ideogram";
        return $"{basePath}/v3/replace-background";
    }

    public override object BuildImageEditPayload(ImageEditRequest req, FalModelConfig cfg)
    {
        if (req.ImageDataUris is null || req.ImageDataUris.Count == 0)
            throw new ArgumentException("Ideogram V3 Replace Background için bir adet image_url zorunludur.");

        var n = ClampNumImages(req.NumImages, cfg);
        var extras = req.Extras ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // Zorunlu alanlar
        var payload = new Dictionary<string, object>
        {
            ["prompt"] = req.Prompt,
            ["image_url"] = req.ImageDataUris[0],
            ["num_images"] = n
        };

        // Şema: rendering_speed, color_palette, style_codes, style, expand_prompt,
        // seed, sync_mode, style_preset, image_size, negative_prompt, image_urls (style refs)
        if (TryGetString(extras, "rendering_speed") is string speed) payload["rendering_speed"] = speed;
        if (TryGetObject(extras, "color_palette") is object palette) payload["color_palette"] = palette;
        if (TryGetArray<string>(extras, "style_codes") is string[] codes) payload["style_codes"] = codes;
        if (TryGetString(extras, "style") is string style) payload["style"] = style;
        if (TryGetBool(extras, "expand_prompt") is bool expand) payload["expand_prompt"] = expand;
        if (TryGetInt(extras, "seed") is int seed) payload["seed"] = seed;
        if (TryGetBool(extras, "sync_mode") is bool sync) payload["sync_mode"] = sync;
        if (TryGetString(extras, "style_preset") is string preset) payload["style_preset"] = preset;
        if (TryGetObject(extras, "image_size") is object imageSize) payload["image_size"] = imageSize;
        if (TryGetString(extras, "negative_prompt") is string neg) payload["negative_prompt"] = neg;
        if (TryGetArray<string>(extras, "image_urls") is string[] styleRefs) payload["image_urls"] = styleRefs; // style ref images (opsiyonel)

        // NOT: output_format bu model dokümanında yok → eklemiyoruz (FAL default)
        return payload;
    }

    // --- küçük yardımcılar ---
    private static int? TryGetInt(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) ? v switch
        {
            int i => i,
            long l => (int)l,
            string s when int.TryParse(s, out var x) => x,
            _ => (int?)null
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

    private static T[]? TryGetArray<T>(IDictionary<string, object> d, string k)
    {
        if (!d.TryGetValue(k, out var v) || v is null) return null;
        if (v is IEnumerable<object> objEnum) return objEnum.Select(x => (T)Convert.ChangeType(x, typeof(T))).ToArray();
        if (v is IEnumerable<T> tEnum) return tEnum.ToArray();
        return null;
    }
}
