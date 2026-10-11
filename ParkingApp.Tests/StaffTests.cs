using ParkingApp.Application.Common.Helpers;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Tests;

/// <summary>
/// The staff identity shape: a first-class field employee created by BackOffice
/// with a generated secret, forced to change it, and tied to one facility. These
/// pin the pieces the gate authorisation and the forced-change flow depend on.
/// </summary>
public class StaffTests
{
    private static readonly Guid Facility = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static Staff NewStaff() => Staff.Create(
        "  Sita Gurung  ",
        "+977 9812345678",
        "  Sita.Gurung@Example.COM ",
        "hash",
        Facility,
        new DateOnly(1996, 5, 4),
        GenderEnum.Female,
        "  Galaxy A14  ",
        "Samsung");

    [Fact]
    public void Create_normalises_identity_and_forces_password_change()
    {
        var staff = NewStaff();

        Assert.Equal("Sita Gurung", staff.FullName);
        Assert.Equal("+977 9812345678", staff.PhoneNumber);
        // Email is the login key, so it is stored canonical and lower-cased.
        Assert.Equal("sita.gurung@example.com", staff.Email);
        Assert.Equal("Galaxy A14", staff.DeviceName);
        Assert.Equal("Samsung", staff.DeviceBrand);
        Assert.Equal(Facility, staff.FacilityId);

        // A freshly created account can log in but cannot open the gate yet.
        Assert.True(staff.MustChangePassword);
        Assert.True(staff.IsActive);
        Assert.False(staff.IsDeleted);
    }

    [Fact]
    public void Create_treats_blank_device_metadata_as_absent()
    {
        var staff = Staff.Create(
            "Hari", "9800000000", "hari@example.com", "hash", Facility,
            null, null, "   ", null);

        Assert.Null(staff.DeviceName);
        Assert.Null(staff.DeviceBrand);
        Assert.Null(staff.DateOfBirth);
        Assert.Null(staff.Gender);
    }

    [Fact]
    public void SetPassword_can_clear_the_forced_change()
    {
        var staff = NewStaff();

        staff.SetPassword("new-hash", mustChangePassword: false);

        Assert.Equal("new-hash", staff.PasswordHash);
        Assert.False(staff.MustChangePassword);
    }

    [Fact]
    public void UpdateDetails_edits_profile_without_touching_the_credential()
    {
        var staff = NewStaff();

        staff.UpdateDetails(
            "Sita G.", "9811111111", "sita.g@example.com", Facility,
            null, GenderEnum.Female, "iPhone 13", "Apple");

        Assert.Equal("Sita G.", staff.FullName);
        Assert.Equal("sita.g@example.com", staff.Email);
        Assert.Equal("iPhone 13", staff.DeviceName);
        // The password is a separate action; an edit never changes the login secret.
        Assert.Equal("hash", staff.PasswordHash);
        Assert.True(staff.MustChangePassword);
    }

    [Fact]
    public void SetActive_toggles_without_deleting()
    {
        var staff = NewStaff();

        staff.SetActive(false);

        Assert.False(staff.IsActive);
        Assert.False(staff.IsDeleted);
    }

    [Fact]
    public void SoftDelete_removes_from_roster_but_keeps_the_row()
    {
        var staff = NewStaff();

        staff.SoftDelete();

        Assert.True(staff.IsDeleted);
        Assert.False(staff.IsActive);
    }

    [Fact]
    public void RefreshToken_rotates_and_revokes()
    {
        var staff = NewStaff();

        var token = StaffRefreshToken.Create(staff.Id, "token-1", expiresInDays: 30);

        Assert.True(token.IsActive);
        Assert.Equal(staff.Id, token.StaffId);

        token.Revoke("Rotated", replacedByToken: "token-2");

        Assert.True(token.IsRevoked);
        Assert.False(token.IsActive);
    }
}

/// <summary>
/// The generated password is emailed once and replaced on first login, so it only
/// has to be random and policy-compliant — not memorable.
/// </summary>
public class PasswordGeneratorTests
{
    [Fact]
    public void Generated_password_is_long_and_mixes_character_classes()
    {
        for (var i = 0; i < 50; i++)
        {
            var password = PasswordGenerator.Generate();

            Assert.Equal(12, password.Length);
            Assert.Contains(password, char.IsUpper);
            Assert.Contains(password, char.IsLower);
            Assert.Contains(password, char.IsDigit);
            Assert.Contains(password, c => !char.IsLetterOrDigit(c));
        }
    }

    [Fact]
    public void Generated_passwords_are_distinct()
    {
        var passwords = Enumerable.Range(0, 25)
            .Select(_ => PasswordGenerator.Generate())
            .ToHashSet();

        Assert.Equal(25, passwords.Count);
    }
}
