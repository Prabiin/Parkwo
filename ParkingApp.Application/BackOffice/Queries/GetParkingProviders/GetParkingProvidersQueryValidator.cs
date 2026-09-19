using FluentValidation;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.BackOffice.Queries.GetParkingProviders;

public sealed class GetParkingProvidersQueryValidator : AbstractValidator<GetParkingProvidersQuery>
{
    public GetParkingProvidersQueryValidator()
    {
        RuleFor(x => x.ApprovalStatus)
            .IsInEnum()
            .When(x => x.ApprovalStatus.HasValue);
    }
}