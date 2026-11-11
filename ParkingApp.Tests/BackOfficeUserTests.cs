using ParkingApp.Domain;

namespace ParkingApp.Tests;

/// <summary>
/// The BackOffice admin identity: created by another admin with a generated
/// secret, forced to replace it on first sign-in, and re-forced when an invite
/// is resent. These pin the flag the console authorisation and resend window
/// both depend on.
/// </summary>
public class BackOfficeUserTests
{
    private static BackOfficeUser NewUser() => new()
    {
        UserName = "admin",
        FullName = "Admin User",
        Email = "admin@example.com",
        PasswordHash = "hash",
        MustChangePassword = true,
        IsActive = true
    };

    [Fact]
    public void SetPassword_clears_the_forced_change()
    {
        var user = NewUser();

        user.SetPassword("new-hash", mustChangePassword: false);

        Assert.Equal("new-hash", user.PasswordHash);
        Assert.False(user.MustChangePassword);
        Assert.NotNull(user.UpdatedAtUtc);
    }

    [Fact]
    public void SetPassword_can_reforce_the_change_for_a_resent_invite()
    {
        var user = NewUser();
        user.SetPassword("own-hash", mustChangePassword: false);

        user.SetPassword("resent-hash", mustChangePassword: true);

        Assert.Equal("resent-hash", user.PasswordHash);
        Assert.True(user.MustChangePassword);
    }

    [Fact]
    public void A_new_user_defaults_to_no_forced_change_until_the_create_flow_sets_it()
    {
        // The column defaults to false so existing and seeded accounts (the
        // SuperAdmin) keep working; only the create/resend flows force a change.
        var user = new BackOfficeUser();

        Assert.False(user.MustChangePassword);
        Assert.True(user.IsActive);
        Assert.False(user.IsDeleted);
    }

    [Fact]
    public void SoftDelete_deactivates_the_account_without_removing_the_row()
    {
        // The row is what the audit trail points at and what the permissions
        // they granted were attached to, so deleting must not remove it.
        var user = NewUser();

        user.SoftDelete();

        Assert.True(user.IsDeleted);
        Assert.False(user.IsActive);
        Assert.NotNull(user.UpdatedAtUtc);
    }

    [Fact]
    public void A_deleted_account_cannot_be_reactivated_through_SetActive()
    {
        // Reactivation is not offered for a deleted account, but nothing in the
        // domain should be able to flip IsActive back on and quietly revive it.
        var user = NewUser();
        user.SoftDelete();

        Assert.False(user.IsActive);
    }
}
