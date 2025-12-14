using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using ImageProcessingService.Services.Fal.Adapters.Core;
using ImageProcessingService.Services.Fal.Generic.Storage;

namespace ImageProcessingService.Services.Fal.Adapters.ImageEditing;

public class NanoQwenAdapter : FalModelAdapterBase, IFalModelAdapter
{
    public override IEnumerable<string> SupportedModels => new[]
    {
        "fal-ai/nano-banana",       // T2I
        "fal-ai/nano-banana/edit",  // Edit
        "fal-ai/qwen-image-edit",   // Edit
        "fal-ai/qwen-image"         // T2I
    };

    public bool SupportsTextToImage => true;
    public bool SupportsImageEdit => true;

    // --- T2I ---
    public object BuildTextToImagePayload(TextToImageRequest req, FalModelConfig config)
    {
        var payload = new Dictionary<string, object>
        {
            ["prompt"] = req.Prompt,
            ["output_format"] = req.OutputFormat ?? "png",
            ["num_images"] = 1
        };

        // Nano Banana için "aspect_ratio" (enum), Qwen için "image_size" (enum)
        // Kullanıcı doğru key'i additional params ile göndermeli veya burada maplemeliyiz.
        // Basitlik için ImageSize'ı her iki key'e de atıyoruz, API sadece tanıdığını alır.
        if (req.ImageSize != null)
        {
            payload["aspect_ratio"] = req.ImageSize; // Nano Banana
            payload["image_size"] = req.ImageSize;   // Qwen
        }

        MergeAdditionalParams(payload, req.AdditionalParams);
        if (req.Seed.HasValue) payload["seed"] = req.Seed.Value;

        return payload;
    }
    public string GetTextToImagePath(FalModelConfig config) => config.ModelPath!;

    // --- EDIT ---
    public object BuildImageEditPayload(ImageEditRequest req, FalModelConfig config)
    {
        var payload = new Dictionary<string, object>
        {
            ["prompt"] = req.Prompt ?? "",
            ["image_url"] = req.ImageUrl,
            ["output_format"] = req.OutputFormat ?? "png"
        };

        // Qwen: "image_url" ister. Nano Banana Edit: "image_urls" (liste) isteyebilir dökümana göre.
        // Dökümanda Nano Banana Edit için: "image_urls": [ "url1" ] yazıyor.
        if (config.ModelPath.Contains("nano-banana"))
        {
            payload.Remove("image_url");
            payload["image_urls"] = new[] { req.ImageUrl };
        }

        MergeAdditionalParams(payload, req.AdditionalParams);
        return payload;
    }
    public string GetImageEditPath(FalModelConfig config) => config.ModelPath!;

    public async Task<ProcessingResult> MapToResultAsync(dynamic jobResult, IGeneratedFileStore fileStore)
    {
        // FIX: (object) cast işlemi ekledik
        return await base.MapToResultDefaultAsync((object)jobResult, fileStore);
    }
}