using FluentValidation;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.BackOffice.Queries.GetParkingFacilities;

public sealed class GetParkingFacilitiesQueryValidator : AbstractValidator<GetParkingFacilitiesQuery>
{
    public GetParkingFacilitiesQueryValidator()
    {
        RuleFor(x => x.ApprovalStatus)
            .IsInEnum()
            .When(x => x.ApprovalStatus.HasValue);
    }
}