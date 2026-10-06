using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain;

namespace ParkingApp.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Otp> Otps => Set<Otp>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<DrivingLicense> DrivingLicenses => Set<DrivingLicense>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<UserOrganization> UserOrganizations => Set<UserOrganization>();
    public DbSet<ParkingProvider> ParkingProviders => Set<ParkingProvider>();
    public DbSet<ParkingFacility> ParkingFacilities => Set<ParkingFacility>();
    public DbSet<ParkingFacilityImage> ParkingFacilityImages => Set<ParkingFacilityImage>();
    public DbSet<ParkingFacilityReview> ParkingFacilityReviews => Set<ParkingFacilityReview>();
    public DbSet<BackOfficeUser> BackOfficeUsers => Set<BackOfficeUser>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<OverstayPayment> OverstayPayments => Set<OverstayPayment>();

    public async Task<IDbContextTransaction> BeginSerializableTransactionAsync(
        CancellationToken cancellationToken = default)
        => await Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}