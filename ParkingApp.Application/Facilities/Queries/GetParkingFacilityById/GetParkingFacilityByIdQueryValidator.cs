using FluentValidation;

namespace ParkingApp.Application.Facilities.Queries.GetParkingFacilityById;

public sealed class GetParkingFacilityByIdQueryValidator : AbstractValidator<GetParkingFacilityByIdQuery>
{
    public GetParkingFacilityByIdQueryValidator()
    {
        RuleFor(x => x.FacilityId)
            .NotEmpty();
    }
}