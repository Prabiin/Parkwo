using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;
using ParkingApp.Application.Payments;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Queries.GetOverstaySummary;

/// <summary>
/// The "you are about to pay NPR X for the overstay" screen. Read-only and
/// safe to reopen — the actual payment is initiated separately.
/// </summary>
public sealed record GetOverstaySummaryQuery(Guid BookingId)
    : IRequestResult<GetOverstaySummaryQuery, GetOverstaySummaryResponse>;

public sealed class GetOverstaySummaryQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IEnumerable<IPaymentGateway> gateways,
    IOptions<PassSettings> passSettings)
    : IRequestResultHandler<GetOverstaySummaryQuery, GetOverstaySummaryResponse>
{
    public async Task<Result<GetOverstaySummaryResponse>> Handle(
        GetOverstaySummaryQuery request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<GetOverstaySummaryResponse>.Failure("Authentication required.", 401);

        var booking = await context.Bookings
            .AsNoTracking()
            .Where(b => b.Id == request.BookingId)
            .Select(b => new
            {
                b.Id,
                b.UserId,
                b.StartsAtUtc,
                b.EndsAtUtc,
                b.EnteredAtUtc,
                b.ExitedAtUtc,
                b.PricePerHourNpr,
                b.OverstayMinutes,
                b.OverstayBillableHours,
                b.OverstayAmountPaisa,
                FacilityName = b.Facility != null ? b.Facility.Name : ""
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (booking is null)
            return Result<GetOverstaySummaryResponse>.Failure("Booking not found.", 404);

        if (booking.UserId != userId.Value)
            return Result<GetOverstaySummaryResponse>.Failure(
                "You can only view your own booking.", 403);

        if (booking.OverstayAmountPaisa is not { } amountPaisa)
            return Result<GetOverstaySummaryResponse>.Failure(
                "This booking has no overstay charge.", 409);

        // Reconcile with the gateway so the screen never offers a payment that
        // already settled through an earlier callback that went missing.
        var overstayPaymentStatus = GetPaymentStatus(
            await OverstayPaymentStatus.LoadAsync(context, gateways, booking.Id, cancellationToken));

        // Only the overstay past this configured grace is billable
        // (Pass:OverstayGraceMinutes); the summary shows the rider the minutes
        // and the grace that was applied before them.
        var graceMinutes = passSettings.Value.OverstayGraceMinutes;

        return Result<GetOverstaySummaryResponse>.Success(
            new GetOverstaySummaryResponse(
                booking.Id,
                booking.FacilityName,
                LocalTime.Format(booking.StartsAtUtc)!,
                LocalTime.Format(booking.EndsAtUtc)!,
                LocalTime.Format(booking.EnteredAtUtc),
                LocalTime.Format(booking.ExitedAtUtc),
                booking.OverstayMinutes,
                graceMinutes,
                booking.OverstayBillableHours ?? 0,
                booking.PricePerHourNpr,
                BookingPricing.ToNpr(amountPaisa),
                "NPR",
                overstayPaymentStatus));
    }

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