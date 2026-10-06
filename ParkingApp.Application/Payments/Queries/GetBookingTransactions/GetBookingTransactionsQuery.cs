using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Payments.Queries.GetBookingTransactions;

/// <summary>
/// Everything ever charged against one booking, prepaid and overstay together,
/// newest first. The two tables are kept separate on purpose; this is the one
/// place that reads both.
/// </summary>
public sealed record GetBookingTransactionsQuery(Guid BookingId)
    : IRequestResult<GetBookingTransactionsQuery, GetBookingTransactionsResponse>;

public sealed class GetBookingTransactionsQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestResultHandler<GetBookingTransactionsQuery, GetBookingTransactionsResponse>
{
    public async Task<Result<GetBookingTransactionsResponse>> Handle(
        GetBookingTransactionsQuery request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<GetBookingTransactionsResponse>.Failure("Authentication required.", 401);

        var booking = await context.Bookings
            .AsNoTracking()
            .Where(b => b.Id == request.BookingId)
            .Select(b => new { b.Id, b.UserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (booking is null)
            return Result<GetBookingTransactionsResponse>.Failure("Booking not found.", 404);

        if (booking.UserId != userId.Value)
            return Result<GetBookingTransactionsResponse>.Failure(
                "You can only view payments for your own booking.", 403);

        var prepaid = await context.Payments
            .AsNoTracking()
            .Where(p => p.BookingId == request.BookingId)
            .Select(p => new Row(
                p.Id,
                PaymentPurposeEnum.Prepaid,
                p.Gateway,
                p.Status,
                p.AmountPaisa,
                p.Currency,
                p.GatewayTransactionId,
                p.FailureReason,
                p.CreatedAtUtc,
                p.PaidAtUtc))
            .ToListAsync(cancellationToken);

        var overstay = await context.OverstayPayments
            .AsNoTracking()
            .Where(p => p.BookingId == request.BookingId)
            .Select(p => new Row(
                p.Id,
                PaymentPurposeEnum.Overstay,
                p.Gateway,
                p.Status,
                p.AmountPaisa,
                p.Currency,
                p.GatewayTransactionId,
                p.FailureReason,
                p.CreatedAtUtc,
                p.PaidAtUtc))
            .ToListAsync(cancellationToken);

        var transactions = prepaid
            .Concat(overstay)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new BookingTransaction(
                t.Id,
                t.Purpose,
                t.Purpose.ToDescription(),
                t.Gateway,
                t.Status,
                t.Status.ToDescription(),
                BookingPricing.ToNpr(t.AmountPaisa),
                t.Currency,
                t.GatewayTransactionId,
                t.FailureReason,
                LocalTime.Format(t.CreatedAtUtc),
                LocalTime.Format(t.PaidAtUtc)))
            .ToList();

        return Result<GetBookingTransactionsResponse>.Success(
            new GetBookingTransactionsResponse(request.BookingId, transactions));
    }

    /// <summary>Both tables expose the same columns, so they merge into one list after this projection.</summary>
    private sealed record Row(
        Guid Id,
        PaymentPurposeEnum Purpose,
        PaymentGatewayEnum Gateway,
        PaymentStatusEnum Status,
        long AmountPaisa,
        string Currency,
        string? GatewayTransactionId,
        string? FailureReason,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? PaidAtUtc);
}