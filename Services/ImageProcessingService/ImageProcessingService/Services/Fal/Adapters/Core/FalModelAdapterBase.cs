using ImageProcessingService.Services.Fal.Generic.Storage;
using System.Text.Json;

namespace ImageProcessingService.Services.Fal.Adapters.Core;

public abstract class FalModelAdapterBase
{
    public abstract IEnumerable<string> SupportedModels { get; }

    protected void MergeAdditionalParams(Dictionary<string, object> payload, Dictionary<string, object> additionalParams)
    {
        if (additionalParams == null) return;
        foreach (var kvp in additionalParams)
        {
            if (kvp.Value == null) continue;
            payload[kvp.Key] = kvp.Value;
        }
    }

    // DÜZELTME: 'dynamic' yerine 'object' yaptık.
    protected Task<ProcessingResult> MapToResultDefaultAsync(object jobResult, IGeneratedFileStore fileStore)
    {
        try
        {
            // URL kontrolü yapıyoruz sadece, indirme işlemini FalJobsService yapıyor.
            var urls = ExtractImageUrls(jobResult);
            if (urls == null || urls.Count == 0)
            {
                return Task.FromResult(new ProcessingResult(false, null, "Fal.AI sonucunda resim URL'i bulunamadı."));
            }

            // Başarılı olduğunu belirtmek için boş bir success dönüyoruz.
            // Asıl dosya yolu FalJobsService tarafından doldurulacak.
            return Task.FromResult(new ProcessingResult(true, null, null, null));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ProcessingResult(false, null, $"Mapping Hatası: {ex.Message}"));
        }
    }

    // DÜZELTME: 'dynamic' yerine 'object' yaptık.
    public virtual List<string> ExtractImageUrls(object jobResult)
    {
        var list = new List<string>();

        // Gelen object aslında bir JsonElement (FalQueueClient'tan geliyor)
        if (jobResult is not JsonElement json) return list;

        // 1. Format: { "images": [ { "url": "..." } ] }
        if (json.TryGetProperty("images", out var imagesElem) && imagesElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in imagesElem.EnumerateArray())
            {
                if (item.TryGetProperty("url", out var urlProp)) list.Add(urlProp.GetString()!);
            }
        }
        // 2. Format: { "image": { "url": "..." } }
        else if (json.TryGetProperty("image", out var imageElem) && imageElem.TryGetProperty("url", out var singleUrl))
        {
            list.Add(singleUrl.GetString()!);
        }

        return list;
    }
}