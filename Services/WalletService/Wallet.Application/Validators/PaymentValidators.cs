using FluentValidation;
using Wallet.Application.DTOs;

namespace Wallet.Application.Validators;

public class PaymentInitiateValidator : AbstractValidator<PaymentInitiateDto>
{
    public PaymentInitiateValidator()
    {
        RuleFor(x => x.PackageId)
            .NotEmpty().WithMessage("Paket seçimi zorunludur.");
    }
}