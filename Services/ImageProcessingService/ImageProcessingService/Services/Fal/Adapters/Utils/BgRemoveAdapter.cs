using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using ImageProcessingService.Services.Fal.Adapters.Core;
using ImageProcessingService.Services.Fal.Generic.Storage;
using System.Text.Json;

namespace ImageProcessingService.Services.Fal.Adapters.Utils;

public class BgRemoveAdapter : FalModelAdapterBase, IFalModelAdapter
{
    public override IEnumerable<string> SupportedModels => new[] { "fal-ai/bria/background/remove" };
    public bool SupportsTextToImage => false;
    public bool SupportsImageEdit => true;

    public object BuildTextToImagePayload(TextToImageRequest req, FalModelConfig config) => throw new NotSupportedException();
    public string GetTextToImagePath(FalModelConfig config) => throw new NotSupportedException();

    public object BuildImageEditPayload(ImageEditRequest req, FalModelConfig config)
    {
        return new Dictionary<string, object>
        {
            ["image_url"] = req.ImageUrl
        };
    }
    public string GetImageEditPath(FalModelConfig config) => config.ModelPath!;

    public async Task<ProcessingResult> MapToResultAsync(dynamic jobResult, IGeneratedFileStore fileStore)
    {
        // FIX: DownloadAndSave kaldırıldı, Base metod kullanıldı.
        return await base.MapToResultDefaultAsync((object)jobResult, fileStore);
    }
}