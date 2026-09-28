using FluentValidation;

namespace ParkingApp.Application.BackOffice.Queries.GetLicenses;

public sealed class GetLicensesQueryValidator : AbstractValidator<GetLicensesQuery>
{
    public GetLicensesQueryValidator()
    {
        RuleFor(x => x.ApprovalStatus)
            .IsInEnum()
            .When(x => x.ApprovalStatus.HasValue);
    }
}
