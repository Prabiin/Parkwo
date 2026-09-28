using ParkingApp.Domain.Common.Base;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Domain;

public class User : AuditableEntity
{
    public string? FullName { get; set; }
    public string PhoneNumber { get; set; } = default!;
    public string? Email { get; set; }
    public GenderEnum? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public bool IsPhoneVerified { get; set; }
    public string? ProfileImageUrl { get; set; }

    // Navigation properties
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<Vehicle> Vehicles { get; set; } = [];
    public DrivingLicense? DrivingLicense { get; set; }
    public ParkingProvider? ParkingProvider { get; set; }
    public ICollection<UserOrganization> OrganizationMemberships { get; set; } = [];
}