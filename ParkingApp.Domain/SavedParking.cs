using ParkingApp.Domain.Common.Base;

namespace ParkingApp.Domain;

/// <summary>
/// A rider's bookmark of a parking place, shown on the Saved tab. Exactly one
/// of <see cref="FacilityId"/> (a paid, bookable lot) or
/// <see cref="FreeParkingFacilityId"/> (a Parkwo free parking) is set; the
/// database enforces that with a check constraint, and a rider can save the
/// same place only once.
/// </summary>
public class SavedParking : AuditableEntity
{
    public Guid UserId { get; private set; }
    public Guid? FacilityId { get; private set; }
    public Guid? FreeParkingFacilityId { get; private set; }

    public User? User { get; private set; }
    public ParkingFacility? Facility { get; private set; }
    public FreeParkingFacility? FreeParkingFacility { get; private set; }

    private SavedParking() { }

    public static SavedParking ForFacility(Guid userId, Guid facilityId) => new()
    {
        UserId = userId,
        FacilityId = facilityId,
        CreatedAtUtc = DateTimeOffset.UtcNow
    };

    public static SavedParking ForFreeParking(Guid userId, Guid freeParkingFacilityId) => new()
    {
        UserId = userId,
        FreeParkingFacilityId = freeParkingFacilityId,
        CreatedAtUtc = DateTimeOffset.UtcNow
    };
}
