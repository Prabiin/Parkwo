using FluentValidation;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.BackOffice.Queries.GetOrganizations;

public sealed class GetOrganizationsQueryValidator : AbstractValidator<GetOrganizationsQuery>
{
    public GetOrganizationsQueryValidator()
    {
        RuleFor(x => x.ApprovalStatus)
            .IsInEnum()
            .When(x => x.ApprovalStatus.HasValue);
    }
}