using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;
using ParkingApp.Application.Payments;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Queries.GetExitSummary;

/// <summary>
/// What the rider sees on the exit page. Read-only, and it answers even before
/// staff have scanned (ExitRecorded false) because the button that opens it is
/// on screen from the start. Before the scan there is nothing to reconcile.
/// </summary>
public sealed record GetExitSummaryQuery(Guid BookingId)
    : IRequestResult<GetExitSummaryQuery, GetExitSummaryResponse>;

public sealed class GetExitSummaryQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IEnumerable<IPaymentGateway> gateways,
    IOptions<PassSettings> passSettings)
    : IRequestResultHandler<GetExitSummaryQuery, GetExitSummaryResponse>
{
    public async Task<Result<GetExitSummaryResponse>> Handle(
        GetExitSummaryQuery request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<GetExitSummaryResponse>.Failure("Authentication required.", 401);

        var booking = await context.Bookings
            .AsNoTracking()
            .Where(b => b.Id == request.BookingId)
            .Select(b => new
            {
                b.Id,
                b.UserId,
                b.Status,
                b.StartsAtUtc,
                b.EndsAtUtc,
                b.EnteredAtUtc,
                b.ExitedAtUtc,
                b.ActualStayMinutes,
                b.OverstayMinutes,
                b.OverstayBillableHours,
                b.OverstayAmountPaisa
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (booking is null)
            return Result<GetExitSummaryResponse>.Failure("Booking not found.", 404);

        if (booking.UserId != userId.Value)
            return Result<GetExitSummaryResponse>.Failure("You can only view your own booking.", 403);

        // Entry/exit grace windows come from configuration (Pass:EarlyEntryGraceMinutes,
        // Pass:OverstayGraceMinutes); both numbers are shown on the exit page.
        var earlyEntryGraceMinutes = passSettings.Value.EarlyEntryGraceMinutes;
        var graceMinutes = passSettings.Value.OverstayGraceMinutes;
        var exitRecorded = booking.ExitedAtUtc is not null;

        // Check with the gateway whether an old abandoned payment is actually
        // settled or dead, so the pay button says the right thing even when the
        // callback never came.
        var overstayPaymentStatus = GetPaymentStatus(
            await OverstayPaymentStatus.LoadAsync(context, gateways, booking.Id, cancellationToken));

        // The charge was frozen at the exit scan; the rider sees and pays the
        // same figure. Null means nothing is owed.
        OverstayPricing.Charge? charge = booking.OverstayAmountPaisa is { } amountPaisa
            ? new OverstayPricing.Charge(booking.OverstayBillableHours ?? 0, amountPaisa)
            : null;

        return Result<GetExitSummaryResponse>.Success(
            new GetExitSummaryResponse(
                booking.Id,
                booking.Status,
                exitRecorded,
                LocalTime.Format(booking.StartsAtUtc),
                LocalTime.Format(booking.EndsAtUtc),
                LocalTime.Format(booking.EnteredAtUtc),
                LocalTime.Format(booking.ExitedAtUtc),
                booking.ActualStayMinutes,
                earlyEntryGraceMinutes,
                graceMinutes,
                booking.OverstayMinutes,
                charge is not null,
                charge?.BillableHours,
                charge is null ? null : BookingPricing.ToNpr(charge.Value.AmountPaisa),
                exitRecorded
                    ? OverstayPricing.DescribeExitMessage(booking.OverstayMinutes, graceMinutes, charge)
                    : "Waiting for staff to scan your exit.",
                overstayPaymentStatus));
    }

    /// <summary>
    /// What the pay button should say. A settled attempt wins (never offer what
    /// was already paid); then a live one (do not start a second meanwhile);
    /// otherwise null, meaning a fresh payment can begin.
    /// </summary>
    private static PaymentStatusEnum? GetPaymentStatus(
        IReadOnlyList<OverstayPaymentStatus.Attempt> attempts)
    {
        var statuses = attempts.Select(a => a.Status).ToList();

        var settled = statuses
            .Where(s => s is PaymentStatusEnum.Completed or PaymentStatusEnum.Refunded)
            .Cast<PaymentStatusEnum?>()
            .FirstOrDefault();

        var live = statuses
            .Where(s => s is PaymentStatusEnum.Initiated)
            .Cast<PaymentStatusEnum?>()
            .FirstOrDefault();

        return settled ?? live;
    }
}