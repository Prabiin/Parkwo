using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Domain;

public class UserOrganization
{
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public OrganizationRoleEnum Role { get; set; }
    public DateTimeOffset JoinedAtUtc { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public Organization? Organization { get; set; }
}