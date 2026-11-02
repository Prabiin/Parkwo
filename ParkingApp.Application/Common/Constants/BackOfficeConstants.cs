namespace ParkingApp.Application.Common.Constants;

/// <summary>
/// Constants for BackOffice RBAC.
/// </summary>
public static class BackOfficeConstants
{
    /// <summary>
    /// The SuperAdmin user ID.
    /// Updated via migration 20261101142141_UpdateSuperAdminUserId.
    /// </summary>
    public static readonly Guid SuperAdminUserId = new("3f2504e0-4f89-11d3-9a0c-0305e82c3301");

    /// <summary>
    /// Claim type for SuperAdmin flag in JWT.
    /// </summary>
    public const string IsSuperAdminClaim = "is_super_admin";

    /// <summary>
    /// Claim type for BackOffice permissions in JWT.
    /// Format: "backoffice_perm" with values like "role:list", "user:add", etc.
    /// </summary>
    public const string BackOfficePermissionClaim = "backoffice_perm";

    /// <summary>
    /// SuperAdmin role name.
    /// </summary>
    public const string SuperAdminRoleName = "SuperAdmin";
}