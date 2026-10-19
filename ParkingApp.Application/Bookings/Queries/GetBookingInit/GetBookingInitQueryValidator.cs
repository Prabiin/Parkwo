using FluentValidation;

namespace ParkingApp.Application.Bookings.Queries.GetBookingInit;

public sealed class GetBookingInitQueryValidator : AbstractValidator<GetBookingInitQuery>
{
    public GetBookingInitQueryValidator()
    {
        RuleFor(x => x.FacilityId)
            .NotEmpty();
    }
}
