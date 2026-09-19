using FluentValidation;

namespace ParkingApp.Application.BackOffice.Queries.GetParkingFacilityDetail;

public sealed class GetParkingFacilityDetailQueryValidator : AbstractValidator<GetParkingFacilityDetailQuery>
{
    public GetParkingFacilityDetailQueryValidator()
    {
        RuleFor(x => x.FacilityId)
            .NotEmpty();
    }
}