using FluentValidation;

namespace Wallet.Application.DTOs;

public record EstimateRequest(
    string Operation,            // "RemoveBackground","Upscale","BlurBackground" ...
    int? WidthPx = null,
    int? HeightPx = null,
    IDictionary<string, string>? Attributes = null
);

public record EstimateResponse(long EstimatedAmountInKurus, string Currency = "TRY");

public class EstimateRequestValidator : AbstractValidator<EstimateRequest>
{
    public EstimateRequestValidator()
    {
        RuleFor(x => x.Operation).NotEmpty().MaximumLength(64);
        RuleFor(x => x.WidthPx).GreaterThan(0).When(x => x.WidthPx.HasValue);
        RuleFor(x => x.HeightPx).GreaterThan(0).When(x => x.HeightPx.HasValue);
    }
}
