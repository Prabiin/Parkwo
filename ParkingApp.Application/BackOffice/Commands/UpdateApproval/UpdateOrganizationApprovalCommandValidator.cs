using FluentValidation;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.BackOffice.Commands.UpdateApproval;

public sealed class UpdateOrganizationApprovalCommandValidator
    : AbstractValidator<UpdateOrganizationApprovalCommand>
{
    public UpdateOrganizationApprovalCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.ApprovalStatus).IsInEnum();
        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .When(x => x.ApprovalStatus == ApprovalStatusEnum.Rejected)
            .MaximumLength(500);
    }
}
