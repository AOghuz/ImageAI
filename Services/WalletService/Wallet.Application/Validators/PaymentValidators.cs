using FluentValidation;
using Wallet.Application.DTOs;

public class TopUpIntentRequestValidator : AbstractValidator<TopUpIntentRequest>
{
    public TopUpIntentRequestValidator()
    {
        RuleFor(x => x.AmountInKurus)
            .GreaterThanOrEqualTo(100)
            .WithMessage("Amount must be at least 100 kuruş (1 TL)")
            .LessThanOrEqualTo(1_000_000) // Max 10,000 TL for safety
            .WithMessage("Amount cannot exceed 1,000,000 kuruş (10,000 TL)");

        RuleFor(x => x.Provider)
            .NotEmpty()
            .WithMessage("Provider is required")
            .MaximumLength(32)
            .WithMessage("Provider name cannot exceed 32 characters")
            .Must(BeValidProvider)
            .WithMessage("Provider must be one of: iyzico, stripe, paypal, test");

        RuleFor(x => x.SuccessReturnUrl)
            .Must(BeValidUrlOrNull)
            .WithMessage("Success return URL must be a valid URL")
            .MaximumLength(512)
            .WithMessage("Success return URL cannot exceed 512 characters");

        RuleFor(x => x.CancelReturnUrl)
            .Must(BeValidUrlOrNull)
            .WithMessage("Cancel return URL must be a valid URL")
            .MaximumLength(512)
            .WithMessage("Cancel return URL cannot exceed 512 characters");

        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(256)
            .WithMessage("Idempotency key cannot exceed 256 characters");
    }

    private static bool BeValidProvider(string provider)
    {
        if (string.IsNullOrEmpty(provider))
            return false;

        var validProviders = new[] { "iyzico", "stripe", "paypal", "test" };
        return validProviders.Contains(provider.ToLower());
    }

    private static bool BeValidUrlOrNull(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

// Additional validation for package payments
public class CreatePackagePaymentRequestValidator : AbstractValidator<CreatePackagePaymentRequest>
{
    public CreatePackagePaymentRequestValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty()
            .WithMessage("Provider is required")
            .MaximumLength(32)
            .WithMessage("Provider name cannot exceed 32 characters")
            .Must(BeValidProvider)
            .WithMessage("Provider must be one of: iyzico, stripe, paypal, test");

        RuleFor(x => x.SuccessReturnUrl)
            .Must(BeValidUrlOrNull)
            .WithMessage("Success return URL must be a valid URL")
            .MaximumLength(512)
            .WithMessage("Success return URL cannot exceed 512 characters");

        RuleFor(x => x.CancelReturnUrl)
            .Must(BeValidUrlOrNull)
            .WithMessage("Cancel return URL must be a valid URL")
            .MaximumLength(512)
            .WithMessage("Cancel return URL cannot exceed 512 characters");

        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(256)
            .WithMessage("Idempotency key cannot exceed 256 characters");
    }

    private static bool BeValidProvider(string provider)
    {
        if (string.IsNullOrEmpty(provider))
            return false;

        var validProviders = new[] { "iyzico", "stripe", "paypal", "test" };
        return validProviders.Contains(provider.ToLower());
    }

    private static bool BeValidUrlOrNull(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

// Request model for package payments (used in controller)
public record CreatePackagePaymentRequest(
    string Provider,
    string? SuccessReturnUrl = null,
    string? CancelReturnUrl = null,
    string? IdempotencyKey = null
);

// Response wrapper for package list
public record PackageListResponse(List<CoinPackageDto> Packages);