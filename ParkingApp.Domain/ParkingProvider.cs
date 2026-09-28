using ParkingApp.Domain.Common.Base;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Domain;

public class ParkingProvider : AuditableEntity
{
    public ProviderTypeEnum ProviderType { get; set; }
    public Guid? OwnerUserId { get; set; }
    public Guid? OwnerOrganizationId { get; set; }

    // Navigation properties
    public User? OwnerUser { get; set; }
    public Organization? OwnerOrganization { get; set; }
    public ICollection<ParkingFacility> Facilities { get; set; } = [];
}