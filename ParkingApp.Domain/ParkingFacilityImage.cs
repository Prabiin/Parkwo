using ParkingApp.Domain.Common.Base;

namespace ParkingApp.Domain;

public class ParkingFacilityImage : AuditableEntity
{
    public Guid FacilityId { get; set; }
    public string FileName { get; set; } = default!;
    public string Url { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long SizeInBytes { get; set; }
    public int SortOrder { get; set; }

    // Navigation property
    public ParkingFacility? Facility { get; set; }
}