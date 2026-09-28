namespace ParkingApp.Application.Configuration;

public class ParkingStandards
{
    public const string SectionName = "ParkingStandards";

    public decimal TwoWheelerAreaSqM { get; set; } = 2;
    public decimal FourWheelerAreaSqM { get; set; } = 12.5m;
}
