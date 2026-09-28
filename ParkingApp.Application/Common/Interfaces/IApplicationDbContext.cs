using Microsoft.EntityFrameworkCore;
using ParkingApp.Domain;

namespace ParkingApp.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Otp> Otps { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Vehicle> Vehicles { get; }
    DbSet<DrivingLicense> DrivingLicenses { get; }
    DbSet<Organization> Organizations { get; }
    DbSet<UserOrganization> UserOrganizations { get; }
    DbSet<ParkingProvider> ParkingProviders { get; }
    DbSet<ParkingFacility> ParkingFacilities { get; }
    DbSet<ParkingFacilityImage> ParkingFacilityImages { get; }
    DbSet<ParkingFacilityReview> ParkingFacilityReviews { get; }
    DbSet<BackOfficeUser> BackOfficeUsers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}