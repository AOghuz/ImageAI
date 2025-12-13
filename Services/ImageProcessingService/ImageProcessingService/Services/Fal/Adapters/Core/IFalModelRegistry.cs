namespace ImageProcessingService.Services.Fal.Adapters.Core;

public interface IFalModelRegistry
{
    bool TryGet(string modelKey, out IFalModelAdapter adapter);
}
