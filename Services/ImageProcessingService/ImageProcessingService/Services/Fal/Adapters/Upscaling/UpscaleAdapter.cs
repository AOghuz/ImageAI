using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using ImageProcessingService.Services.Fal.Adapters.Core;
using ImageProcessingService.Services.Fal.Generic.Storage;
using System.Text.Json;

namespace ImageProcessingService.Services.Fal.Adapters.Upscaling;

public class UpscaleAdapter : FalModelAdapterBase, IFalModelAdapter
{
    public override IEnumerable<string> SupportedModels => new[]
    {
        "fal-ai/topaz/upscale/image",
        "fal-ai/aura-sr"
    };

    public bool SupportsTextToImage => false;
    public bool SupportsImageEdit => true; // Upscale'i edit olarak kabul ediyoruz

    public object BuildTextToImagePayload(TextToImageRequest req, FalModelConfig config) => throw new NotSupportedException();
    public string GetTextToImagePath(FalModelConfig config) => throw new NotSupportedException();

    public object BuildImageEditPayload(ImageEditRequest req, FalModelConfig config)
    {
        var payload = new Dictionary<string, object>
        {
            ["image_url"] = req.ImageUrl
        };

        // Topaz için varsayılanlar
        if (config.ModelPath.Contains("topaz"))
        {
            payload["model"] = "Standard V2";
            payload["upscale_factor"] = 2;
            payload["output_format"] = req.OutputFormat ?? "jpeg";
            payload["face_enhancement"] = true;
        }
        // Aura için
        else if (config.ModelPath.Contains("aura"))
        {
            payload["upscaling_factor"] = 4; // Aura default
        }

        MergeAdditionalParams(payload, req.AdditionalParams);
        return payload;
    }

    public string GetImageEditPath(FalModelConfig config) => config.ModelPath!;

    public async Task<ProcessingResult> MapToResultAsync(dynamic jobResult, IGeneratedFileStore fileStore)
    {
        // FIX: DownloadAndSave yerine Base metod çağırıyoruz.
        // Base metod URL'leri kontrol edip OK dönecek, indirmeyi Service yapacak.
        return await base.MapToResultDefaultAsync((object)jobResult, fileStore);
    }
}