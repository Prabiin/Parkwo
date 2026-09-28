using FluentValidation;

namespace ParkingApp.Application.BackOffice.Commands.UpdateApproval;

public sealed class UpdateFacilityCapacityApprovalCommandValidator
    : AbstractValidator<UpdateFacilityCapacityApprovalCommand>
{
    public UpdateFacilityCapacityApprovalCommandValidator()
    {
        RuleFor(x => x.FacilityId).NotEmpty();
        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .When(x => !x.Approve)
            .MaximumLength(500);
    }
}
