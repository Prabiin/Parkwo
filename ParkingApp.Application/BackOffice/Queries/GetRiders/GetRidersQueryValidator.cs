using FluentValidation;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.BackOffice.Queries.GetRiders;

public sealed class GetRidersQueryValidator : AbstractValidator<GetRidersQuery>
{
    public GetRidersQueryValidator()
    {
        RuleFor(x => x.VehicleType)
            .IsInEnum()
            .When(x => x.VehicleType.HasValue);
    }
}