using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Payments.Commands.CreateOverstay;

/// <summary>
/// Same handoff shape as the prepaid response, but the amount is in rupees
/// (what the rider reads). Paisa is only ever sent to the gateway by the
/// adapter.
/// </summary>
public record CreateOverstayPaymentResponse(
    Guid OverstayPaymentId,
    Guid BookingId,
    PaymentGatewayEnum Gateway,
    PaymentStatusEnum Status,
    string StatusDescription,
    PaymentHandoffModeEnum Mode,
    string ModeDescription,
    string Payload,
    IReadOnlyDictionary<string, string>? FormFields,
    decimal AmountNpr,
    string Currency,
    int BillableHours);