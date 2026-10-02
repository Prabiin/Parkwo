using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
    DbSet<Booking> Bookings { get; }
    DbSet<Payment> Payments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts an explicit transaction. Booking creation needs SERIALIZABLE
    /// isolation so its availability count and insert cannot interleave with a
    /// competing booking for the same lot (last-space double-sell).
    /// </summary>
    Task<IDbContextTransaction> BeginSerializableTransactionAsync(CancellationToken cancellationToken = default);
}