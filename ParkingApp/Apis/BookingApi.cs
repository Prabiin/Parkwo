using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.Bookings.Commands.Cancel;
using ParkingApp.Application.Bookings.Commands.Create;
using ParkingApp.Application.Bookings.Commands.RotatePass;
using ParkingApp.Application.Bookings.Queries.GetBookingById;
using ParkingApp.Application.Bookings.Queries.GetBookingPass;
using ParkingApp.Application.Bookings.Queries.GetBookingSummary;
using ParkingApp.Application.Bookings.Queries.GetExitSummary;
using ParkingApp.Application.Bookings.Queries.GetOverstaySummary;
using ParkingApp.Application.Payments.Queries.GetBookingTransactions;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Domain.Common.Enums;
using ParkingApp.Infrastructure.Auth;

namespace ParkingApp.Api.Apis;

public class BookingApi : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        app.MapGroup("bookings")
            .MapPost(CreateBooking, "", "")
            .MapGet(GetBookingById, "{bookingId}", "")
            // The rate/amount confirmation shown before the prepaid payment starts.
            .MapGet(GetBookingSummary, "{bookingId}/summary", "")
            .MapPost(CancelBooking, "{bookingId}/cancel", "")
            // The QR the rider shows at the gate, and the invalidate-and-reissue
            // call for when that QR leaks.
            .MapGet(GetBookingPass, "{bookingId}/pass", "")
            .MapPost(RotateBookingPass, "{bookingId}/pass/rotate", "")
            // The rider's exit page. Read-only, and answerable before staff have
            // scanned — the button beside the QR is live from the moment it opens.
            .MapGet(GetExitSummary, "{bookingId}/exit-summary", "")
            // The amount/rate confirmation before an overstay payment starts.
            .MapGet(GetOverstaySummary, "{bookingId}/overstay/summary", "")
            // Payment history: the union of both payment tables, so a receipt
            // screen shows the prepaid charge and any overstay charge together.
            .MapGet(GetBookingTransactions, "{bookingId}/transactions", "")
            .RequireAuthorization(ProfileCompleteRequirement.PolicyName);
    }

    private static async Task<IResult> CreateBooking(ISender sender, IServiceProvider serviceProvider,
        CreateBookingCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<CreateBookingCommand, CreateBookingResponse>(sender,
            request, serviceProvider, cancellationToken);

    private static async Task<IResult> GetBookingById(ISender sender, IServiceProvider serviceProvider,
        Guid bookingId, CancellationToken cancellationToken)
        => await ExecuteQuery<GetBookingByIdQuery, GetBookingByIdResponse>(sender,
            new GetBookingByIdQuery(bookingId), serviceProvider, cancellationToken);

    private static async Task<IResult> GetBookingSummary(ISender sender, IServiceProvider serviceProvider,
        Guid bookingId, CancellationToken cancellationToken)
        => await ExecuteQuery<GetBookingSummaryQuery, GetBookingSummaryResponse>(sender,
            new GetBookingSummaryQuery(bookingId), serviceProvider, cancellationToken);

    private static async Task<IResult> CancelBooking(ISender sender, IServiceProvider serviceProvider,
        Guid bookingId, CancelBookingCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<CancelBookingCommand, Unit>(sender,
            request with { BookingId = bookingId }, serviceProvider, cancellationToken);

    private static async Task<IResult> GetBookingPass(ISender sender, IServiceProvider serviceProvider,
        Guid bookingId, CancellationToken cancellationToken)
        => await ExecuteQuery<GetBookingPassQuery, GetBookingPassResponse>(sender,
            new GetBookingPassQuery(bookingId), serviceProvider, cancellationToken);

    private static async Task<IResult> RotateBookingPass(ISender sender, IServiceProvider serviceProvider,
        Guid bookingId, CancellationToken cancellationToken)
        => await ExecuteCommand<RotatePassCommand, RotatePassResponse>(sender,
            new RotatePassCommand(bookingId), serviceProvider, cancellationToken);

    private static async Task<IResult> GetExitSummary(ISender sender, IServiceProvider serviceProvider,
        Guid bookingId, CancellationToken cancellationToken)
        => await ExecuteQuery<GetExitSummaryQuery, GetExitSummaryResponse>(sender,
            new GetExitSummaryQuery(bookingId), serviceProvider, cancellationToken);

    private static async Task<IResult> GetOverstaySummary(ISender sender, IServiceProvider serviceProvider,
        Guid bookingId, CancellationToken cancellationToken)
        => await ExecuteQuery<GetOverstaySummaryQuery, GetOverstaySummaryResponse>(sender,
            new GetOverstaySummaryQuery(bookingId), serviceProvider, cancellationToken);

    private static async Task<IResult> GetBookingTransactions(ISender sender, IServiceProvider serviceProvider,
        Guid bookingId, CancellationToken cancellationToken)
        => await ExecuteQuery<GetBookingTransactionsQuery, GetBookingTransactionsResponse>(sender,
            new GetBookingTransactionsQuery(bookingId), serviceProvider, cancellationToken);
}
