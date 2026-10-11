using ParkingApp.Domain.Common.Base;

namespace ParkingApp.Domain;

/// <summary>
/// An optional photo of a <see cref="FreeParkingFacility"/>, uploaded by
/// BackOffice (free parkings have no owner). Mirrors ParkingFacilityImage so the
/// two image pipelines stay identical.
/// </summary>
public class FreeParkingFacilityImage : AuditableEntity
{
    public Guid FreeParkingFacilityId { get; set; }
    public string FileName { get; set; } = default!;
    public string Url { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long SizeInBytes { get; set; }
    public int SortOrder { get; set; }

    public FreeParkingFacility? Facility { get; set; }
}
