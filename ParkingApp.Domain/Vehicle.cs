using ParkingApp.Domain.Common.Base;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Domain;

public class Vehicle : AuditableEntity
{
    public VehicleTypeEnum VehicleType { get; private set; }
    /// <summary>
    /// Optional sub-type (scooter, motorcycle, car/jeep/van). No longer asked at
    /// registration: parking only needs <see cref="VehicleType"/>, and the
    /// driving-license check that relied on it is gone. Nullable so vehicles
    /// registered before the change keep their value.
    /// </summary>
    public VehicleCategoryEnum? VehicleCategory { get; private set; }
    public string Name { get; private set; } = default!;
    public string VehicleNumber { get; private set; } = default!;
    public string Brand { get; private set; } = default!;
    public string Model { get; private set; } = default!;
    public string Color { get; private set; } = default!;
    public Guid UserId { get; private set; }

    // Navigation property
    public User? User { get; private set; }

    private Vehicle() { }

    public static Vehicle Create(
        Guid userId,
        VehicleTypeEnum vehicleType,
        VehicleCategoryEnum? vehicleCategory,
        string name,
        string vehicleNumber,
        string brand,
        string model,
        string color)
    {
        return new Vehicle
        {
            UserId = userId,
            VehicleType = vehicleType,
            VehicleCategory = vehicleCategory,
            Name = name.Trim(),
            VehicleNumber = vehicleNumber.Trim().ToUpperInvariant(),
            Brand = brand.Trim(),
            Model = model.Trim(),
            Color = color.Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }
}