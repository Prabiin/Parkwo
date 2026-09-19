using ParkingApp.Domain.Common.Base;

namespace ParkingApp.Domain;

public class ParkingFacilityReview : AuditableEntity
{
    public Guid FacilityId { get; set; }
    public Guid AuthorId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }

    // Navigation properties
    public ParkingFacility? Facility { get; set; }
    public User? Author { get; set; }
}