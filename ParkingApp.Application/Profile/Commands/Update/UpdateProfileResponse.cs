using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Profile.Commands.Update;

public record UpdateProfileResponse(
    string FullName,
    string PhoneNumber,
    string Email,
    GenderEnum? Gender,
    string? GenderDescription,
    DateOnly? DateOfBirth,
    bool IsProfileComplete,
    string? ProfileImageUrl);