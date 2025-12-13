using Wallet.Application.Abstractions;
using Wallet.Application.DTOs;

namespace Wallet.Business.Services;

public class PricingService : IPricingService
{
    public Task<EstimateResponse> EstimateAsync(Guid userId, EstimateRequest request, CancellationToken ct = default)
    {
        // Basit kural seti (örnek): TRY kuruş
        long price = request.Operation.ToLowerInvariant() switch
        {
            "removebackground" => 100,  // 1.00 TL
            "blurbackground" => 75,   // 0.75 TL
            "upscale" => CalcUpscalePrice(request),
            _ => 100   // varsayılan
        };

        return Task.FromResult(new EstimateResponse(price));
    }

    private static long CalcUpscalePrice(EstimateRequest req)
    {
        // 200 kuruş taban + (genişlik*yükseklik / 1MP) * 50 kuruş
        var w = req.WidthPx ?? 1024;
        var h = req.HeightPx ?? 1024;
        var megapixels = (w * h) / 1_000_000.0;
        var variable = (long)Math.Ceiling(megapixels * 50);
        return 200 + Math.Max(0, variable);
    }
}
