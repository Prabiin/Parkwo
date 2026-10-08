using NetTopologySuite.Geometries;
using ParkingApp.Domain.Common.Base;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Domain;

public class ParkingFacility : AuditableEntity
{
    public Guid ProviderId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Address { get; set; } = default!;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public Point? Location { get; set; }
    public bool HasMarkedParkingLot { get; set; }
    public int TwoWheelerOccupancy { get; set; }
    public int FourWheelerOccupancy { get; set; }
    public decimal? LandAreaSqM { get; set; }
    public int? PendingTwoWheelerOccupancy { get; set; }
    public int? PendingFourWheelerOccupancy { get; set; }
    public decimal? PendingLandAreaSqM { get; set; }
    public ApprovalStatusEnum ApprovalStatus { get; set; }
    public string? RejectionReason { get; set; }
    public double? AverageRating { get; set; }
    public int RatingCount { get; set; }

    // Navigation properties
    public ParkingProvider? Provider { get; set; }
    public ICollection<ParkingFacilityImage> Images { get; set; } = [];
    public ICollection<ParkingFacilityReview> Reviews { get; set; } = [];
}