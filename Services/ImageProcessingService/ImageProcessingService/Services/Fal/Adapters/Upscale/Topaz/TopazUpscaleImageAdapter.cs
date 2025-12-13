using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using ImageProcessingService.Services.Fal.Adapters.Core;

namespace ImageProcessingService.Services.Fal.Adapters.Upscale.Topaz;

public sealed class TopazUpscaleImageAdapter : FalModelAdapterBase
{
    // Bu key Controller ve appsettings ile birebir aynı olmalı
    public override string Key => "topaz-upscale-image";

    public override bool SupportsTextToImage => false;
    public override bool SupportsImageEdit => true;

    // basePath = "fal-ai/topaz" → submit = "fal-ai/topaz/upscale/image"
    public override string GetImageEditPath(FalModelConfig cfg)
    {
        var basePath = cfg.ModelPath?.TrimEnd('/') ?? "fal-ai/topaz";
        return $"{basePath}/upscale/image";
    }

    public override object BuildImageEditPayload(ImageEditRequest req, FalModelConfig cfg)
    {
        var url = req.ImageDataUris?.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Topaz Upscale için image_url (tek) zorunludur.");

        var extras = req.Extras ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        var payload = new Dictionary<string, object>
        {
            ["image_url"] = url
        };

        if (TryGetString(extras, "model") is string model) payload["model"] = model; // "Standard V2" (default) vb.
        if (TryGetDouble(extras, "upscale_factor") is double up) payload["upscale_factor"] = up; // default 2
        if (TryGetBool(extras, "crop_to_fill") is bool crop) payload["crop_to_fill"] = crop;

        // output_format: "jpeg" | "png"
        if (!string.IsNullOrWhiteSpace(req.OutputFormat))
            payload["output_format"] = req.OutputFormat!.ToLowerInvariant();

        if (TryGetString(extras, "subject_detection") is string subj) payload["subject_detection"] = subj; // All|Foreground|Background
        if (TryGetBool(extras, "face_enhancement") is bool face) payload["face_enhancement"] = face;
        if (TryGetDouble(extras, "face_enhancement_creativity") is double cr) payload["face_enhancement_creativity"] = cr;
        if (TryGetDouble(extras, "face_enhancement_strength") is double st) payload["face_enhancement_strength"] = st;

        return payload;
    }

    // Bu model tekil "image" döndürüyor
    public override IReadOnlyList<string> ExtractImageUrls(FalResult result)
        => result.Image?.Url is { Length: > 0 } u ? new[] { u } : Array.Empty<string>();

    private static string? TryGetString(IDictionary<string, object> d, string k)
        => d.TryGetValue(k, out var v) ? v?.ToString() : null;

    private static bool? TryGetBool(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) ? v switch
        {
            bool b => b,
            string s when bool.TryParse(s, out var x) => x,
            _ => (bool?)null
        } : null;

    private static double? TryGetDouble(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) ? v switch
        {
            double db => db,
            float f => (double)f,
            int i => (double)i,
            long l => (double)l,
            string s when double.TryParse(s, out var x) => x,
            _ => (double?)null
        } : null;
}
