using ParkingApp.Domain.BackOffice.Role;

namespace ParkingApp.Tests;

/// <summary>
/// The BackOffice role lifecycle: a role can be paused (inactive) or deleted,
/// and the two are deliberately different. Pausing is reversible and keeps the
/// role in the console; deleting removes it from the console for good. Neither
/// one touches the role's permissions, because a pause is about the role, not
/// about what it can do.
/// </summary>
public class BackOfficeRoleTests
{
    private static BackOfficeRole NewRole() => new()
    {
        Name = "Operator",
        Description = "Day-to-day operator",
        IsSystem = false,
        IsActive = true,
        IsDeleted = false
    };

    [Fact]
    public void A_new_role_is_active_and_not_deleted()
    {
        var role = NewRole();

        Assert.True(role.IsActive);
        Assert.False(role.IsDeleted);
        Assert.False(role.IsSystem);
    }

    [Fact]
    public void Deactivating_a_role_keeps_its_permissions()
    {
        // Switching a role off must not strip access, otherwise coming back
        // from a pause would mean re-granting everything by hand.
        var role = NewRole();
        role.RoleMenuActions.Add(new BackOfficeRoleMenuAction { MenuActionId = Guid.NewGuid() });

        role.SetActive(false);

        Assert.False(role.IsActive);
        Assert.False(role.IsDeleted);
        Assert.Single(role.RoleMenuActions);
    }

    [Fact]
    public void A_role_can_be_reactivated()
    {
        var role = NewRole();
        role.SetActive(false);

        role.SetActive(true);

        Assert.True(role.IsActive);
        Assert.False(role.IsDeleted);
    }

    [Fact]
    public void Deleting_a_role_also_deactivates_it_and_keeps_its_permissions()
    {
        var role = NewRole();
        role.RoleMenuActions.Add(new BackOfficeRoleMenuAction { MenuActionId = Guid.NewGuid() });

        role.SoftDelete();

        Assert.True(role.IsDeleted);
        // Inactive too, so nobody holding it can sign in.
        Assert.False(role.IsActive);
        // The permissions stay on the record.
        Assert.Single(role.RoleMenuActions);
    }
}
