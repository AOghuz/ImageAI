// Services/Fal/Adapters/ImageEditing/SeedDream/SeedDreamAdapter.cs
using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using ImageProcessingService.Services.Fal.Adapters.Core;

namespace ImageProcessingService.Services.Fal.Adapters.ImageEditing.SeedDream;

public sealed class SeedDreamAdapter : FalModelAdapterBase
{
    public override string Key => "seedream-v4-edit";
    public override bool SupportsTextToImage => false;
    public override bool SupportsImageEdit => true;

    // SeedDream edit tek endpoint (dokümana göre): appsettings'te ModelPath birebir olmalı (örn: "fal-ai/seedream/v4/edit")
    public override string GetImageEditPath(FalModelConfig cfg) => cfg.ModelPath!;

    public override object BuildImageEditPayload(ImageEditRequest req, FalModelConfig cfg)
    {
        if (req.ImageDataUris is null || req.ImageDataUris.Count == 0)
            throw new ArgumentException("SeedDream için en az 1 görüntü gereklidir.");
        if (req.ImageDataUris.Count > 10)
            throw new ArgumentException("SeedDream maksimum 10 görüntü kabul eder.");

        foreach (var uri in req.ImageDataUris)
        {
            if (string.IsNullOrWhiteSpace(uri) || !uri.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Geçersiz URL: {uri}");
        }

        var payload = new Dictionary<string, object?>
        {
            ["prompt"] = req.Prompt,
            ["image_urls"] = req.ImageDataUris.ToArray()
        };

        // Sadece dolu gelenleri ekle
        if (req.NumImages.HasValue)
            payload["num_images"] = ClampNumImages(req.NumImages, cfg);

        // SeedDream çıktıyı PNG veriyor; istersen kullanıcıdan geleni saygıyla geçir
        if (!string.IsNullOrWhiteSpace(req.OutputFormat))
            payload["output_format"] = NormalizeFormat(req.OutputFormat, cfg);

        var extras = req.Extras ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        static T? Get<T>(IDictionary<string, object> d, string k, Func<object, T?> conv)
        {
            if (!d.TryGetValue(k, out var v) || v is null) return default;
            var s = v is string str ? str.Trim() : v.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(s)) return default;
            return conv(v);
        }

        // image_size: string preset veya {width,height} object gelebilir — her ikisini de geçir
        object? imageSize = Get(extras, "image_size", v => v); // olduğu gibi

        int? maxImages = Get(extras, "max_images", v =>
                      v is int i ? i :
                      v is long l ? (int?)l :
                      int.TryParse(v.ToString(), out var x) ? x : (int?)null);

        int? seed = Get(extras, "seed", v =>
                      v is int i ? i :
                      v is long l ? (int?)l :
                      int.TryParse(v.ToString(), out var x) ? x : (int?)null);

        bool? syncMode = Get(extras, "sync_mode", v =>
                      v is bool b ? b :
                      bool.TryParse(v.ToString(), out var bb) ? bb : (bool?)null);

        bool? safety = Get(extras, "enable_safety_checker", v =>
                      v is bool b ? b :
                      bool.TryParse(v.ToString(), out var bb) ? bb : (bool?)null);

        if (imageSize != null) payload["image_size"] = imageSize;
        if (maxImages.HasValue) payload["max_images"] = Math.Clamp(maxImages.Value, 1, cfg.MaxImages);
        if (seed.HasValue) payload["seed"] = seed.Value;
        if (syncMode.HasValue) payload["sync_mode"] = syncMode.Value;
        if (safety.HasValue) payload["enable_safety_checker"] = safety.Value;

        return payload;
    }
}
