using ParkingApp.Domain.Common.Base;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Domain;

public class Vehicle : AuditableEntity
{
    public VehicleTypeEnum VehicleType { get; private set; }
    public string Name { get; private set; } = default!;
    public string VehicleNumber { get; private set; } = default!;
    public Guid UserId { get; private set; }

    // Navigation property
    public User? User { get; private set; }

    private Vehicle() { }

    public static Vehicle Create(
        Guid userId,
        VehicleTypeEnum vehicleType,
        string name,
        string vehicleNumber)
    {
        return new Vehicle
        {
            UserId = userId,
            VehicleType = vehicleType,
            Name = name.Trim(),
            VehicleNumber = vehicleNumber.Trim().ToUpperInvariant(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }
}