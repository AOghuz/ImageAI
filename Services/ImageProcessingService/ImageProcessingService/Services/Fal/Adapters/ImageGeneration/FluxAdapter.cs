using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using ImageProcessingService.Services.Fal.Adapters.Core;
using ImageProcessingService.Services.Fal.Generic.Storage;

namespace ImageProcessingService.Services.Fal.Adapters.ImageGeneration;

public class FluxAdapter : FalModelAdapterBase, IFalModelAdapter
{
    public override IEnumerable<string> SupportedModels => new[]
    {
        "fal-ai/flux-kontext/dev",      // Edit & Gen
        "fal-ai/flux-kontext-lora",     // Gen
        "fal-ai/flux/srpo"              // Gen
    };

    public bool SupportsTextToImage => true;
    public bool SupportsImageEdit => true; // Özellikle Kontext/Dev için

    // --- TEXT TO IMAGE ---
    public object BuildTextToImagePayload(TextToImageRequest req, FalModelConfig config)
    {
        // Varsayılan değerler dökümandan alındı
        var payload = new Dictionary<string, object>
        {
            ["prompt"] = req.Prompt,
            ["image_size"] = req.ImageSize ?? "landscape_4_3",
            ["num_inference_steps"] = 28, // Flux SRPO default
            ["guidance_scale"] = 2.5,     // Flux default
            ["enable_safety_checker"] = true,
            ["output_format"] = req.OutputFormat ?? "jpeg"
        };

        // Ekstra parametreleri ekle (num_images, sync_mode, seed vs.)
        MergeAdditionalParams(payload, req.AdditionalParams);

        if (req.Seed.HasValue) payload["seed"] = req.Seed.Value;

        return payload;
    }

    public string GetTextToImagePath(FalModelConfig config)
    {
        // Flux Kontext Lora için özel path, diğerleri için config'den gelen
        if (config.ModelPath.Contains("flux-kontext-lora"))
            return "fal-ai/flux-kontext-lora/text-to-image";

        return config.ModelPath!;
    }

    // --- IMAGE EDIT (Flux Kontext Dev) ---
    public object BuildImageEditPayload(ImageEditRequest req, FalModelConfig config)
    {
        var payload = new Dictionary<string, object>
        {
            ["prompt"] = req.Prompt ?? "",
            ["image_url"] = req.ImageUrl,
            ["num_inference_steps"] = 28,
            ["guidance_scale"] = 2.5,
            ["output_format"] = req.OutputFormat ?? "jpeg"
        };

        // resolution_mode, acceleration gibi parametreler AdditionalParams'dan gelir
        MergeAdditionalParams(payload, req.AdditionalParams);

        return payload;
    }

    public string GetImageEditPath(FalModelConfig config) => config.ModelPath!; // fal-ai/flux-kontext/dev

    public async Task<ProcessingResult> MapToResultAsync(dynamic jobResult, IGeneratedFileStore fileStore)
    {
        // FIX: (object) cast işlemi
        return await base.MapToResultDefaultAsync((object)jobResult, fileStore);
    }
}