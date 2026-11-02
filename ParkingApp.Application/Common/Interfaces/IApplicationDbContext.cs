using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ParkingApp.Domain;
using ParkingApp.Domain.BackOffice.Menu;
using ParkingApp.Domain.BackOffice.Role;

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
    DbSet<FreeParkingFacility> FreeParkingFacilities { get; }
    DbSet<FreeParkingFacilityImage> FreeParkingFacilityImages { get; }
    DbSet<BackOfficeUser> BackOfficeUsers { get; }
    DbSet<Staff> Staffs { get; }
    DbSet<StaffRefreshToken> StaffRefreshTokens { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<Payment> Payments { get; }
    DbSet<OverstayPayment> OverstayPayments { get; }

    // BackOffice RBAC
    DbSet<BackOfficeMenu> BackOfficeMenus { get; }
    DbSet<BackOfficeMenuAction> BackOfficeMenuActions { get; }
    DbSet<BackOfficeRole> BackOfficeRoles { get; }
    DbSet<BackOfficeUserRole> BackOfficeUserRoles { get; }
    DbSet<BackOfficeRoleMenuAction> BackOfficeRoleMenuActions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts an explicit transaction. Booking creation needs SERIALIZABLE
    /// isolation so its availability count and insert cannot interleave with a
    /// competing booking for the same lot (last-space double-sell).
    /// </summary>
    Task<IDbContextTransaction> BeginSerializableTransactionAsync(CancellationToken cancellationToken = default);
}