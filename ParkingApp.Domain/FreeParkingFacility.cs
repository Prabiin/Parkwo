using NetTopologySuite.Geometries;
using ParkingApp.Domain.Common.Base;

namespace ParkingApp.Domain;

/// <summary>
/// A public, free parking place listed by Parkwo itself — not by a provider, not
/// bookable, not approved. It exists to put a marker on the rider map; the paid,
/// bookable lots come from <see cref="ParkingFacility"/> and its providers.
/// Because it is display-only it carries no provider, no price, no capacity
/// gating and no bookings, and riders only ever see active, non-deleted rows.
/// </summary>
public class FreeParkingFacility : AuditableEntity
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Address { get; set; } = default!;

    /// <summary>
    /// Map coordinates. Required — a marker with no point is not useful — and
    /// the source of <see cref="Location"/>.
    /// </summary>
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>Query-only PostGIS point, GiST-indexed for the nearby search.</summary>
    public Point? Location { get; set; }

    /// <summary>
    /// How many spaces of each type the place roughly holds. Informational only
    /// (this place is never booked, so nothing is gated on it); null means the
    /// count is unknown.
    /// </summary>
    public int? TwoWheelerOccupancy { get; set; }
    public int? FourWheelerOccupancy { get; set; }

    /// <summary>BackOffice publish toggle. Inactive rows are hidden from riders.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Soft delete. Riders never see deleted rows; BackOffice still can.</summary>
    public bool IsDeleted { get; set; }

    public ICollection<FreeParkingFacilityImage> Images { get; set; } = [];

    /// <summary>
    /// Keeps <see cref="Latitude"/>, <see cref="Longitude"/> and the derived
    /// query point in lockstep — the point is never set by hand, so it can never
    /// drift from the coordinates the API exposes.
    /// </summary>
    public void SetCoordinates(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
        Location = new Point(longitude, latitude) { SRID = 4326 };
    }
}
