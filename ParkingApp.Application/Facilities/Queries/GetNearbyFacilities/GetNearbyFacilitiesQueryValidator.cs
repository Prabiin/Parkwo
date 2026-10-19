using FluentValidation;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Facilities.Queries.GetNearbyFacilities;

public sealed class GetNearbyFacilitiesQueryValidator : AbstractValidator<GetNearbyFacilitiesQuery>
{
    public GetNearbyFacilitiesQueryValidator()
    {
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90d, 90d);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180d, 180d);

        // A blank query is the "near me" mode, not an error. A typed one must be
        // long enough to be selective and short enough to be a search term.
        RuleFor(x => x.Query)
            .Must(BeValidSearchTerm)
            .WithMessage($"Enter a search term of {FacilityTextSearch.MinQueryLength} to "
                         + $"{FacilityTextSearch.MaxQueryLength} characters.");

        RuleFor(x => x.VehicleType)
            .IsInEnum()
            .When(x => x.VehicleType.HasValue);
    }

    private static bool BeValidSearchTerm(string? query)
    {
        if (!FacilityTextSearch.HasQuery(query))
            return true;

        var length = query!.Trim().Length;
        return length >= FacilityTextSearch.MinQueryLength
               && length <= FacilityTextSearch.MaxQueryLength;
    }
}
