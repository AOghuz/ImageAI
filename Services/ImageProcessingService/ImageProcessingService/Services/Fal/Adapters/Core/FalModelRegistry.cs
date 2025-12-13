namespace ImageProcessingService.Services.Fal.Adapters.Core;

public sealed class FalModelRegistry : IFalModelRegistry
{
    private readonly Dictionary<string, IFalModelAdapter> _map;
    public FalModelRegistry(IEnumerable<IFalModelAdapter> adapters)
        => _map = adapters.ToDictionary(a => a.Key, StringComparer.OrdinalIgnoreCase);

    public bool TryGet(string modelKey, out IFalModelAdapter adapter)
        => _map.TryGetValue(modelKey, out adapter!);
}
