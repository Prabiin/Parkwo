using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.Bookings.Commands.CompleteWalkIn;
using ParkingApp.Application.Bookings.Commands.CreateWalkIn;
using ParkingApp.Application.Bookings.Queries.FindWalkInByPhone;
using ParkingApp.Application.Bookings.Queries.GetBookingPass;
using ParkingApp.Application.Bookings.Queries.GetWalkInPass;
using ParkingApp.Application.Bookings.Queries.GetWalkIns;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Payments.Commands.Create;
using ParkingApp.Application.Payments.Commands.CreateOverstay;
using ParkingApp.Application.Payments.Commands.CreateWalkIn;
using ParkingApp.Application.Payments.Commands.CreateWalkInOverstay;
using ParkingApp.Application.Payments.Commands.VerifyWalkIn;
using ParkingApp.Application.Payments.Commands.VerifyWalkInOverstay;
using ParkingApp.Domain.Common.Enums;
using ParkingApp.Infrastructure.Auth;

namespace ParkingApp.Api.Apis;

/// <summary>
/// Walk-in desk, used by gate staff phones. Every route is facility-scoped and
/// the handlers authorise the caller against that facility, so a walk-in opened
/// at one lot cannot be paid for, passed, or closed at another. The flow is:
/// create -> pay (dynamic QR) -> verify -> issue the one shared pass -> scan in
/// / resolve by phone and scan out. Riders never call these routes directly —
/// they have no account, and the staff device is the system of record at the gate.
/// </summary>
public class WalkInApi : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        app.MapGroup("walkins")
            .MapPost(Create, "", "")
            // The staff board: unpaid holds to chase, and vehicles still parked.
            .MapGet(List, "", "")
            // Exit fallback: the vehicles still parked under a phone number.
            .MapGet(LookupByPhone, "lookup", "")
            // The one shared gate pass, issued to the staff device to show the rider.
            .MapGet(GetPass, "{bookingId}/pass", "")
            .MapPost(InitiatePayment, "{bookingId}/pay", "")
            // The "rider paid, webhook is slow" override.
            .MapPost(VerifyPayment, "{bookingId}/verify-payment", "")
            .MapPost(CompleteExit, "{bookingId}/exit", "")
            // After an overstayed exit: restarted settlement on the staff device.
            .MapPost(InitiateOverstayPayment, "{bookingId}/overstay-pay", "")
            .MapPost(VerifyOverstayPayment, "{bookingId}/verify-overstay", "")
            // Staff-only, and only after the generated password has been changed.
            .RequireAuthorization(StaffPasswordChangedRequirement.PolicyName);
    }

    private static async Task<IResult> Create(ISender sender, IServiceProvider serviceProvider,
        CreateWalkInBookingCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<CreateWalkInBookingCommand, CreateWalkInBookingResponse>(sender,
            request, serviceProvider, cancellationToken);

    private static async Task<IResult> List(ISender sender, IServiceProvider serviceProvider,
        Guid facilityId, string? status, CancellationToken cancellationToken)
    {
        BookingStatusEnum? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!int.TryParse(status, out var code)
                || !Enum.IsDefined(typeof(BookingStatusEnum), code))
                return Results.BadRequest(new { Errors = new[] { "status: Invalid booking status." } });

            parsedStatus = (BookingStatusEnum)code;
        }

        return await ExecuteQuery<GetWalkInsQuery, GetWalkInsResponse>(sender,
            new GetWalkInsQuery(facilityId, parsedStatus), serviceProvider, cancellationToken);
    }

    private static async Task<IResult> LookupByPhone(ISender sender, IServiceProvider serviceProvider,
        Guid facilityId, string phone, CancellationToken cancellationToken)
        => await ExecuteQuery<FindWalkInByPhoneQuery, FindWalkInByPhoneResponse>(sender,
            new FindWalkInByPhoneQuery(facilityId, phone), serviceProvider, cancellationToken);

    private static async Task<IResult> GetPass(ISender sender, IServiceProvider serviceProvider,
        Guid bookingId, CancellationToken cancellationToken)
        => await ExecuteQuery<GetWalkInPassQuery, GetBookingPassResponse>(sender,
            new GetWalkInPassQuery(bookingId), serviceProvider, cancellationToken);

    private static async Task<IResult> InitiatePayment(ISender sender, IServiceProvider serviceProvider,
        Guid bookingId, CreateWalkInPaymentCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<CreateWalkInPaymentCommand, CreatePaymentResponse>(sender,
            request with { BookingId = bookingId }, serviceProvider, cancellationToken);

    private static async Task<IResult> VerifyPayment(ISender sender, IServiceProvider serviceProvider,
        Guid bookingId, VerifyWalkInPaymentCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<VerifyWalkInPaymentCommand, VerifyWalkInPaymentResponse>(sender,
            request with { BookingId = bookingId }, serviceProvider, cancellationToken);

    private static async Task<IResult> CompleteExit(ISender sender, IServiceProvider serviceProvider,
        Guid bookingId, CompleteWalkInExitCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<CompleteWalkInExitCommand, CompleteWalkInExitResponse>(sender,
            request with { BookingId = bookingId }, serviceProvider, cancellationToken);

    private static async Task<IResult> InitiateOverstayPayment(ISender sender, IServiceProvider serviceProvider,
        Guid bookingId, CreateWalkInOverstayPaymentCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<CreateWalkInOverstayPaymentCommand, CreateOverstayPaymentResponse>(sender,
            request with { BookingId = bookingId }, serviceProvider, cancellationToken);

    private static async Task<IResult> VerifyOverstayPayment(ISender sender, IServiceProvider serviceProvider,
        Guid bookingId, VerifyWalkInOverstayPaymentCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<VerifyWalkInOverstayPaymentCommand, VerifyWalkInOverstayPaymentResponse>(sender,
            request with { BookingId = bookingId }, serviceProvider, cancellationToken);
}
