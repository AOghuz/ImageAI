using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract.Requests;

namespace ImageProcessingService.Services.Fal.Adapters.Core;

public abstract class FalModelAdapterBase : IFalModelAdapter
{
    public abstract string Key { get; }
    public virtual bool SupportsTextToImage => true;
    public virtual bool SupportsImageEdit => true;

    public virtual int ClampNumImages(int? n, FalModelConfig cfg)
        => Math.Clamp(n ?? 1, 1, Math.Max(1, cfg.MaxImages));

    public virtual string NormalizeFormat(string? fmt, FalModelConfig cfg)
    {
        var f = (fmt ?? cfg.DefaultOutputFormat ?? "jpeg").ToLowerInvariant();
        return f == "png" ? "png" : "jpeg";
    }

    public virtual string GetTextToImagePath(FalModelConfig cfg) => cfg.ModelPath!;
    public virtual string GetImageEditPath(FalModelConfig cfg) => cfg.ModelPath! + "/edit";

    public virtual object BuildTextToImagePayload(TextToImageRequest req, FalModelConfig cfg)
        => new
        {
            prompt = req.Prompt,
            num_images = ClampNumImages(req.NumImages, cfg),
            output_format = NormalizeFormat(req.OutputFormat, cfg)
            // Extras gerekiyorsa alt sınıfta override edilir
        };

    public virtual object BuildImageEditPayload(ImageEditRequest req, FalModelConfig cfg)
        => new
        {
            prompt = req.Prompt,
            image_urls = req.ImageDataUris,
            num_images = ClampNumImages(req.NumImages, cfg),
            output_format = NormalizeFormat(req.OutputFormat, cfg)
        };

    public virtual IReadOnlyList<string> ExtractImageUrls(FalResult result)
        => result.Images?.Where(i => !string.IsNullOrWhiteSpace(i.Url)).Select(i => i.Url!).ToList()
           ?? new List<string>();
}
