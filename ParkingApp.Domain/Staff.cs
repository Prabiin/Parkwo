using ParkingApp.Domain.Common.Base;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Domain;

/// <summary>
/// A Parkwo field employee: the person who actually stands at a facility's gate.
/// Staff are not app users — they have no rider account, no vehicles and no
/// bookings of their own. They are a first-class identity created by BackOffice,
/// tied to exactly one <see cref="ParkingFacility"/>, and they log into the staff
/// app with their email and a generated password. The gate scan and the walk-in
/// desk both run as a staff member, so every entry, exit and manual settlement is
/// attributable to a named person rather than to an anonymous shared login.
///
/// <para>
/// PIN and fingerprint unlock are deliberately NOT here. Those are a local
/// convenience on the staff device (they unlock the stored refresh token); the
/// only credential the server ever sees is the password, and the only server
/// session is the refresh token. That is what makes the password a daily login:
/// once the device's refresh token lapses, the staff member must type the
/// password again.
/// </para>
///
/// <para>
/// <see cref="DeviceName"/> and <see cref="DeviceBrand"/> are captured for
/// support and asset tracking only — they are metadata, not a binding. The staff
/// member may log in from any device; nothing is locked to a device id.
/// </para>
/// </summary>
public class Staff : AuditableEntity
{
    /// <summary>Staff member's full name, as it should appear on gate records.</summary>
    public string FullName { get; set; } = default!;

    /// <summary>
    /// Contact number, stored in canonical form. Used for operational contact,
    /// never as a login factor.
    /// </summary>
    public string PhoneNumber { get; set; } = default!;

    /// <summary>
    /// Login identifier. Must be a valid email address: the generated password
    /// is emailed here on creation, and this is what the staff member types to
    /// sign in.
    /// </summary>
    public string Email { get; set; } = default!;

    public string PasswordHash { get; set; } = default!;

    /// <summary>
    /// True from creation until the staff member sets their own password. While
    /// true, operational staff routes are refused; only login, refresh,
    /// change-password and profile are reachable.
    /// </summary>
    public bool MustChangePassword { get; set; } = true;

    public DateOnly? DateOfBirth { get; set; }
    public GenderEnum? Gender { get; set; }

    /// <summary>Device the staff member reports using. Tracking metadata only.</summary>
    public string? DeviceName { get; set; }

    /// <summary>Device manufacturer/brand. Tracking metadata only.</summary>
    public string? DeviceBrand { get; set; }

    /// <summary>The single facility this staff member is assigned to and may scan at.</summary>
    public Guid FacilityId { get; set; }

    /// <summary>BackOffice deactivation toggle. A deactivated staff member cannot sign in or scan.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Soft delete. The row is kept so gate history stays attributable.</summary>
    public bool IsDeleted { get; set; }

    public DateTimeOffset? LastLoginAtUtc { get; set; }

    // Navigation properties
    public ParkingFacility? Facility { get; set; }
    public ICollection<StaffRefreshToken> RefreshTokens { get; set; } = [];

    /// <summary>
    /// Creates an active staff member with the BackOffice-generated password.
    /// <paramref name="mustChangePassword"/> is true for a freshly created (or
    /// password-reset) account so the gate cannot be operated on a shared secret.
    /// </summary>
    public static Staff Create(
        string fullName,
        string phoneNumber,
        string email,
        string passwordHash,
        Guid facilityId,
        DateOnly? dateOfBirth,
        GenderEnum? gender,
        string? deviceName,
        string? deviceBrand)
    {
        return new Staff
        {
            FullName = fullName.Trim(),
            PhoneNumber = phoneNumber.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            MustChangePassword = true,
            FacilityId = facilityId,
            DateOfBirth = dateOfBirth,
            Gender = gender,
            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? null : deviceName.Trim(),
            DeviceBrand = string.IsNullOrWhiteSpace(deviceBrand) ? null : deviceBrand.Trim(),
            IsActive = true,
            IsDeleted = false,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Edits the profile fields BackOffice owns. The password is not touched
    /// here — a reset is a separate action that regenerates and re-emails a
    /// secret, so an edit can never silently change how someone logs in.
    /// </summary>
    public void UpdateDetails(
        string fullName,
        string phoneNumber,
        string email,
        Guid facilityId,
        DateOnly? dateOfBirth,
        GenderEnum? gender,
        string? deviceName,
        string? deviceBrand)
    {
        FullName = fullName.Trim();
        PhoneNumber = phoneNumber.Trim();
        Email = email.Trim().ToLowerInvariant();
        FacilityId = facilityId;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        DeviceName = string.IsNullOrWhiteSpace(deviceName) ? null : deviceName.Trim();
        DeviceBrand = string.IsNullOrWhiteSpace(deviceBrand) ? null : deviceBrand.Trim();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Replaces the credential. Used by both a BackOffice reset (which also
    /// re-forces a change and re-emails the secret) and the staff member's own
    /// change-password, so the two paths cannot diverge.
    /// </summary>
    public void SetPassword(string passwordHash, bool mustChangePassword)
    {
        PasswordHash = passwordHash;
        MustChangePassword = mustChangePassword;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void RecordLogin()
    {
        LastLoginAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        IsActive = false;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
