using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Commands.Create;

public record CreateBookingResponse(
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
    long AmountPaisa,
    decimal PlatformFeeNpr,
    decimal ProviderAmountNpr,
    DateTimeOffset HoldExpiresAtUtc);