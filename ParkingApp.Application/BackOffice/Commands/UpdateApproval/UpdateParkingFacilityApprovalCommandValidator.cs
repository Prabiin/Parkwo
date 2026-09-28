using FluentValidation;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.BackOffice.Commands.UpdateApproval;

public sealed class UpdateParkingFacilityApprovalCommandValidator
    : AbstractValidator<UpdateParkingFacilityApprovalCommand>
{
    public UpdateParkingFacilityApprovalCommandValidator()
    {
        RuleFor(x => x.FacilityId).NotEmpty();
        RuleFor(x => x.ApprovalStatus).IsInEnum();
        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .When(x => x.ApprovalStatus == ApprovalStatusEnum.Rejected)
            .MaximumLength(500);
    }
}
