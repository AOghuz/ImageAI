using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using ImageProcessingService.Services.Fal.Adapters.Core;

namespace ImageProcessingService.Services.Fal.Adapters.Upscale.AuraSr;

public sealed class AuraSrAdapter : FalModelAdapterBase
{
    public override string Key => "aura-sr";
    public override bool SupportsTextToImage => false;
    public override bool SupportsImageEdit => true;

    // POST https://queue.fal.run/fal-ai/aura-sr
    public override string GetImageEditPath(FalModelConfig cfg) => cfg.ModelPath!;

    public override object BuildImageEditPayload(ImageEditRequest req, FalModelConfig cfg)
    {
        // tek URL bekliyor
        var url = req.ImageDataUris?.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Aura-SR için image_url (tek) zorunludur.");

        var extras = req.Extras ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        var payload = new Dictionary<string, object>
        {
            ["image_url"] = url
        };

        // upscaling_factor (sadece 4), overlapping_tiles (bool), checkpoint ("v1"|"v2")
        if (TryGetInt(extras, "upscaling_factor") is int f) payload["upscaling_factor"] = f;  // default 4
        if (TryGetBool(extras, "overlapping_tiles") is bool ov) payload["overlapping_tiles"] = ov;
        if (TryGetString(extras, "checkpoint") is string ckpt) payload["checkpoint"] = ckpt;

        return payload;
    }

    // Tek görselliyi çıkar
    public override IReadOnlyList<string> ExtractImageUrls(FalResult result)
        => result.Image?.Url is { Length: > 0 } u ? new[] { u } : Array.Empty<string>();

    // helpers
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

    private static string? TryGetString(IDictionary<string, object> d, string k)
        => d.TryGetValue(k, out var v) ? v?.ToString() : null;
}
