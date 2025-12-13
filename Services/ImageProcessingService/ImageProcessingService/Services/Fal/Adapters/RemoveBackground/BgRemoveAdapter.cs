using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using ImageProcessingService.Services.Fal.Adapters.Core;

namespace ImageProcessingService.Services.Fal.Adapters.RemoveBackground;

public sealed class BgRemoveAdapter : FalModelAdapterBase
{
    public override string Key => "bg-remove";
    public override bool SupportsTextToImage => false;
    public override bool SupportsImageEdit => true;

    // Submit: POST https://queue.fal.run/fal-ai/bria/background/remove
    // Status/Result: https://queue.fal.run/fal-ai/bria/requests/{id}[ /status ]
    public override string GetImageEditPath(FalModelConfig cfg)
        => cfg.ModelPath!.TrimEnd('/') + "/background/remove";

    public override object BuildImageEditPayload(ImageEditRequest req, FalModelConfig cfg)
    {
        if (req.ImageDataUris is null || req.ImageDataUris.Count == 0)
            throw new ArgumentException("Bg Remove için bir adet image_url zorunludur.");

        var extras = req.Extras ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        var payload = new Dictionary<string, object>
        {
            ["image_url"] = req.ImageDataUris[0]
        };
        if (TryGetBool(extras, "sync_mode") is bool syncMode)
            payload["sync_mode"] = syncMode;
        return payload;
    }

    public override IReadOnlyList<string> ExtractImageUrls(FalResult result)
    {
        if (result.Images is { Count: > 0 })
            return result.Images.Where(i => !string.IsNullOrWhiteSpace(i.Url))
                                .Select(i => i.Url!).ToList();
        if (!string.IsNullOrWhiteSpace(result.Image?.Url))
            return new[] { result.Image!.Url! };
        return Array.Empty<string>();
    }

    private static bool? TryGetBool(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) ? v switch
        {
            bool b => b,
            string s when bool.TryParse(s, out var x) => x,
            _ => (bool?)null
        } : null;
}
