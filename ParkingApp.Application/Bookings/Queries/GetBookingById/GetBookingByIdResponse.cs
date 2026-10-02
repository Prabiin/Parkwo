using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Queries.GetBookingById;

public record GetBookingByIdResponse(
    Guid BookingId,
    BookingStatusEnum Status,
    string StatusDescription,
    Guid FacilityId,
    string FacilityName,
    Guid VehicleId,
    string VehicleNumber,
    VehicleTypeEnum VehicleType,
    string VehicleTypeDescription,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    decimal PricePerHourNpr,
    int BillableHours,
    decimal TotalAmountNpr,
    decimal PlatformFeeNpr,
    decimal ProviderAmountNpr,
    DateTimeOffset HoldExpiresAtUtc,
    DateTimeOffset? ConfirmedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string? CancellationReason);