using FluentValidation;

namespace Wallet.Application.DTOs;

public class CreateReservationRequestValidator : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationRequestValidator()
    {
        RuleFor(x => x.JobId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.AmountInKurus).GreaterThan(0);
        RuleFor(x => x.TtlMinutes).InclusiveBetween(5, 240);
        RuleFor(x => x.IdempotencyKey).MaximumLength(256);
    }
}

public class CommitReservationRequestValidator : AbstractValidator<CommitReservationRequest>
{
    public CommitReservationRequestValidator()
    {
        RuleFor(x => x.ReservationId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).MaximumLength(256);
    }
}

public class ReleaseReservationRequestValidator : AbstractValidator<ReleaseReservationRequest>
{
    public ReleaseReservationRequestValidator()
    {
        RuleFor(x => x.ReservationId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(256);
        RuleFor(x => x.IdempotencyKey).MaximumLength(256);
    }
}
