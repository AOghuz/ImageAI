// Integrations/Fal/FalAuthHandler.cs
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace ImageProcessingService.Integrations.Fal;

public sealed class FalAuthHandler : DelegatingHandler
{
    private readonly IConfiguration _cfg;

    public FalAuthHandler(IConfiguration cfg)
    {
        _cfg = cfg;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var apiKey = _cfg["Fal:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Fal:ApiKey yapılandırılmamış");

        request.Headers.Authorization = new AuthenticationHeaderValue("Key", apiKey);

        return await base.SendAsync(request, cancellationToken);
    }
}