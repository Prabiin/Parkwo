using ParkingApp.Domain.Common.Base;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Domain;

public class Organization : AuditableEntity
{
    public string Name { get; set; } = default!;
    public string RegistrationNumber { get; set; } = default!;
    public string ContactNumber { get; set; } = default!;
    public string Address { get; set; } = default!;
    public Guid OwnerUserId { get; set; }
    public ApprovalStatusEnum ApprovalStatus { get; set; }
    public string? RejectionReason { get; set; }

    // Navigation properties
    public User? OwnerUser { get; set; }
    public ICollection<UserOrganization> Members { get; set; } = [];
    public ParkingProvider? ParkingProvider { get; set; }
}