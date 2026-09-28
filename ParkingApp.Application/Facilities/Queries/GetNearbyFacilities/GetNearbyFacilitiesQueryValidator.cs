using FluentValidation;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Facilities.Queries.GetNearbyFacilities;

public sealed class GetNearbyFacilitiesQueryValidator : AbstractValidator<GetNearbyFacilitiesQuery>
{
    public const double DefaultRadiusKm = 5;
    public const double MaxRadiusKm = 20;

    public GetNearbyFacilitiesQueryValidator()
    {
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90d, 90d);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180d, 180d);

        RuleFor(x => x.RadiusKm)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxRadiusKm)
            .When(x => x.RadiusKm.HasValue);

        RuleFor(x => x.VehicleType)
            .IsInEnum()
            .When(x => x.VehicleType.HasValue);
    }
}
