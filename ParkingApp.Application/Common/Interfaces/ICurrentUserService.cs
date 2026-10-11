namespace ParkingApp.Application.Common.Interfaces;

/// <summary>
/// The authenticated caller behind the current request. Parkwo has three token
/// realms — riders, BackOffice operators and gate staff — and only one of them
/// is ever populated for a given request: a rider token yields <see cref="UserId"/>,
/// a staff token yields <see cref="StaffId"/>. Reading the wrong one is a null,
/// not the other identity, so a handler cannot accidentally attribute an action
/// to the wrong realm.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>The rider account behind the request, or null for a staff/BackOffice caller.</summary>
    Guid? UserId { get; }

    /// <summary>The gate staff member behind the request, or null for a rider/BackOffice caller.</summary>
    Guid? StaffId { get; }

    /// <summary>True when the request carries a staff token.</summary>
    bool IsStaff { get; }
}
