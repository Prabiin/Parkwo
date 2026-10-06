using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Payments;

/// <summary>
/// Reads the overstay payment attempts for a booking, and if the newest one is
/// still "started" but older than 10 minutes, asks the gateway what actually
/// happened to it and updates our row to match.
///
/// Why: an overstay has no hold to expire. Prepaid releases a rider when the
/// booking hold lapses; an overstay payment a rider walked away from stays
/// "started" forever, which blocks paying it again AND booking again. Asking
/// the gateway on read fixes this without the callback ever having to arrive.
///
/// If we cannot get a trustworthy answer (gateway not configured, down, slow,
/// or the amount does not match what we charged) the row is left untouched.
/// </summary>
public static class OverstayPaymentStatus
{
    /// <summary>Same as the prepaid hold: this long with no callback means the rider is no longer mid-checkout.</summary>
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(10);

    /// <summary>A short cap so a slow gateway never stalls the page this runs behind.</summary>
    private static readonly TimeSpan LookupTimeout = TimeSpan.FromSeconds(5);

    /// <summary>One row in the overstay payment table, with only the fields callers need.</summary>
    public sealed record Attempt(
        Guid Id,
        PaymentGatewayEnum Gateway,
        PaymentStatusEnum Status,
        string? GatewayPaymentId,
        DateTimeOffset CreatedAtUtc,
        long AmountPaisa);

    /// <summary>
    /// Loads a booking's overstay attempts. If a stale "started" attempt exists,
    /// checks it with the gateway and saves whatever the gateway says.
    /// </summary>
    public static async Task<IReadOnlyList<Attempt>> LoadAsync(
        IApplicationDbContext context,
        IEnumerable<IPaymentGateway> gateways,
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var attempts = await context.OverstayPayments
            .AsNoTracking()
            .Where(p => p.BookingId == bookingId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new Attempt(
                p.Id,
                p.Gateway,
                p.Status,
                p.GatewayPaymentId,
                p.CreatedAtUtc,
                p.AmountPaisa))
            .ToListAsync(cancellationToken);

        var stale = attempts.FirstOrDefault(a =>
            a.Status == PaymentStatusEnum.Initiated
            && a.GatewayPaymentId is not null
            && a.CreatedAtUtc < DateTimeOffset.UtcNow - StaleAfter);

        if (stale is null)
            return attempts;

        IPaymentGateway gateway;
        try
        {
            gateway = PaymentGateways.Find(gateways, stale.Gateway);
        }
        catch (PaymentGatewayException)
        {
            // No credentials for this gateway anymore; leave the row as-is.
            return attempts;
        }

        GatewayPaymentStatus status;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(LookupTimeout);

            status = await gateway.LookupAsync(stale.GatewayPaymentId!, timeout.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is PaymentGatewayException
                                   or HttpRequestException
                                   or JsonException)
        {
            // Gateway unreachable or gave us nothing usable. Leave the row alone.
            return attempts;
        }

        // Still held at the gateway: do nothing. And never settle on a total
        // that differs from what we charged.
        if (status.Status == PaymentStatusEnum.Initiated)
            return attempts;

        if (status.Status == PaymentStatusEnum.Completed
            && status.AmountPaisa is not null
            && status.AmountPaisa.Value != stale.AmountPaisa)
            return attempts;

        var row = await context.OverstayPayments
            .FirstOrDefaultAsync(p => p.Id == stale.Id, cancellationToken);

        if (row is null)
            return attempts;

        UpdateRow(row, status);
        await context.SaveChangesAsync(cancellationToken);

        return attempts
            .Select(a => a.Id == row.Id ? a with { Status = row.Status } : a)
            .ToList();
    }

    /// <summary>
    /// Applies what the gateway reported to an overstay payment row. Used by
    /// the return-url callback and the read check above, so both paths record
    /// the same outcome the same way.
    /// </summary>
    public static void UpdateRow(OverstayPayment payment, GatewayPaymentStatus status)
    {
        switch (status.Status)
        {
            case PaymentStatusEnum.Completed:
                payment.MarkCompleted(status.TransactionId);
                break;
            case PaymentStatusEnum.Cancelled:
                payment.MarkCancelled();
                break;
            case PaymentStatusEnum.Expired:
                payment.MarkExpired();
                break;
            case PaymentStatusEnum.Refunded:
                payment.MarkRefunded();
                break;
            case PaymentStatusEnum.Failed:
                payment.MarkFailed("The gateway reported the payment as failed.");
                break;
            case PaymentStatusEnum.Initiated:
                break;
        }
    }
}