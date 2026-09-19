using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Profile.Queries.GetProfile;

public record GetProfileResponse(
    string? FullName,
    string PhoneNumber,
    string? Email,
    GenderEnum? Gender,
    string? GenderDescription,
    DateOnly? DateOfBirth,
    DateTimeOffset MemberSince,
    bool IsProfileComplete,
    bool HasVehicle,
    int BookingsCount,
    decimal AmountSavedInNpr,
    decimal Rating,
    string? ProfileImageUrl);