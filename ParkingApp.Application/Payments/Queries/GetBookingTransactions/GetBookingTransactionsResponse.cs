using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Payments.Queries.GetBookingTransactions;

/// <summary>
/// One row of a booking's payment history. Amount is in rupees and times are
/// already local, so the receipt screen can render them directly. Purpose and
/// status each come with their display text alongside the enum.
/// </summary>
public record BookingTransaction(
    Guid PaymentId,
    PaymentPurposeEnum Purpose,
    string PurposeDescription,
    PaymentGatewayEnum Gateway,
    PaymentStatusEnum Status,
    string StatusDescription,
    decimal AmountNpr,
    string Currency,
    string? GatewayTransactionId,
    string? FailureReason,
    string? CreatedAtLocal,
    string? PaidAtLocal);

public record GetBookingTransactionsResponse(
    Guid BookingId,
    IReadOnlyList<BookingTransaction> Transactions);
