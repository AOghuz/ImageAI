using ImageProcessingService.Services.Fal.Adapters.Core;

public class FalModelRegistry : IFalModelRegistry
{
    // Adapter'ları Key-Value olarak tutacağız
    // Key: "fal-ai/flux-pro", Value: FluxAdapter instance
    private readonly Dictionary<string, IFalModelAdapter> _adapters;

    // Constructor'da tüm IFalModelAdapter'ları alıyoruz (Program.cs'de kaydetmiştik)
    public FalModelRegistry(IEnumerable<IFalModelAdapter> adapters)
    {
        _adapters = new Dictionary<string, IFalModelAdapter>(StringComparer.OrdinalIgnoreCase);

        foreach (var adapter in adapters)
        {
            foreach (var modelKey in adapter.SupportedModels)
            {
                // Aynı model key birden fazla adapter'da olmamalı
                if (!_adapters.ContainsKey(modelKey))
                {
                    _adapters[modelKey] = adapter;
                }
            }
        }
    }

    public IFalModelAdapter? GetAdapter(string modelKey)
    {
        _adapters.TryGetValue(modelKey, out var adapter);
        return adapter;
    }

    public bool TryGet(string modelKey, out IFalModelAdapter adapter)
    {
        return _adapters.TryGetValue(modelKey, out adapter!);
    }
}