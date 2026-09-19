using System.ComponentModel;

namespace ParkingApp.Domain.Common.Enums;

public enum OtpTypeEnum
{
    [Description("Registration")]
    Registration = 1,
    [Description("Login")]
    Login = 2,
    [Description("Change phone number")]
    ChangePhoneNumber = 3,
    [Description("Delete account")]
    DeleteAccount = 4
}

public enum OtpStatusEnum
{
    [Description("Pending")]
    Pending = 1,
    [Description("Verified")]
    Verified = 2,
    [Description("Expired")]
    Expired = 3,
    [Description("Cancelled")]
    Cancelled = 4
}

public enum OtpChannelEnum
{
    [Description("SMS")]
    Sms = 1,
    [Description("Email")]
    Email = 2
}

public enum VehicleTypeEnum
{
    [Description("Two wheeler")]
    TwoWheeler = 1,
    [Description("Four wheeler")]
    FourWheeler = 2
}

public enum GenderEnum
{
    [Description("Male")]
    Male = 1,
    [Description("Female")]
    Female = 2,
    [Description("Other")]
    Other = 3
}

public enum ProviderTypeEnum
{
    [Description("Individual")]
    Individual = 1,
    [Description("Company")]
    Company = 2
}

public enum ApprovalStatusEnum
{
    [Description("Pending")]
    Pending = 1,
    [Description("Verified")]
    Verified = 2,
    [Description("Under review")]
    UnderReview = 3,
    [Description("Rejected")]
    Rejected = 4
}

public enum OrganizationRoleEnum
{
    [Description("Owner")]
    Owner = 1,
    [Description("Admin")]
    Admin = 2,
    [Description("Monitor")]
    Monitor = 3
}