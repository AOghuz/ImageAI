namespace ImageProcessingService.Services.Fal.Adapters.Core;

public interface IFalModelRegistry
{
    IFalModelAdapter? GetAdapter(string modelKey);
    bool TryGet(string modelKey, out IFalModelAdapter adapter);
}
