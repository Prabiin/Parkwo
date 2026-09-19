using ParkingApp.Domain.Common.Base;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Domain;

public class ParkingSpot : AuditableEntity
{
    public Guid FacilityId { get; set; }
    public string SpotNumber { get; set; } = default!;
    public VehicleTypeEnum VehicleType { get; set; }
    public decimal PricePerHourNpr { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ParkingFacility? Facility { get; set; }
}