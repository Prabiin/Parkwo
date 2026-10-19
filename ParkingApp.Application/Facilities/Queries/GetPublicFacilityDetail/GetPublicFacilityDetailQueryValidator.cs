using FluentValidation;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Facilities.Queries.GetPublicFacilityDetail;

public sealed class GetPublicFacilityDetailQueryValidator : AbstractValidator<GetPublicFacilityDetailQuery>
{
    public GetPublicFacilityDetailQueryValidator()
    {
        RuleFor(x => x.FacilityId)
            .NotEmpty();

        RuleFor(x => x.VehicleType)
            .IsInEnum()
            .When(x => x.VehicleType.HasValue);
    }
}
