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

public enum VehicleCategoryEnum
{
    [Description("Scooter")]
    Scooter = 1,
    [Description("Motorcycle")]
    Motorcycle = 2,
    [Description("Car / Jeep / Van")]
    CarJeepVan = 3
}

public enum LicenseCategoryEnum
{
    [Description("K (Scooter / Moped)")]
    K = 1,
    [Description("A (Motorcycle)")]
    A = 2,
    [Description("A1 (Heavy motorcycle)")]
    A1 = 3,
    [Description("B (Car / Jeep / Van)")]
    B = 4
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

public enum BookingStatusEnum
{
    [Description("Pending payment")]
    PendingPayment = 1,
    [Description("Confirmed")]
    Confirmed = 2,
    [Description("Active")]
    Active = 3,
    [Description("Completed")]
    Completed = 4,
    [Description("Cancelled")]
    Cancelled = 5,
    [Description("Expired")]
    Expired = 6,
    [Description("Refunded")]
    Refunded = 7
}

public enum PaymentGatewayEnum
{
    [Description("Khalti")]
    Khalti = 1
}

/// <summary>
/// Why a payment exists. The two live in different tables, so a rider looking
/// at a booking's history only gets this field as the label that tells them
/// which charge a row was for.
/// </summary>
public enum PaymentPurposeEnum
{
    [Description("Booking payment")]
    Prepaid = 1,
    [Description("Overstay payment")]
    Overstay = 2
}

public enum ScanTypeEnum
{
    [Description("Entry")]
    Entry = 1,
    [Description("Exit")]
    Exit = 2
}

public enum ScanOutcomeEnum
{
    [Description("Accepted")]
    Accepted = 1,
    [Description("Already parked")]
    AlreadyParked = 2,
    [Description("Not checked in")]
    NotCheckedIn = 3,
    [Description("Already closed")]
    AlreadyClosed = 4,
    [Description("Pass is not valid yet")]
    TooEarly = 5,
    [Description("Pass has expired")]
    TooLate = 6,
    [Description("Unknown pass")]
    UnknownPass = 7,
    [Description("Pass does not belong to this facility")]
    WrongFacility = 8,
    [Description("Pass is not valid for this booking state")]
    InvalidState = 9,
    [Description("Payment is not settled")]
    NotPaid = 10,
    [Description("Overstay")]
    Overstay = 11
}

public enum PaymentStatusEnum
{
    [Description("Initiated")]
    Initiated = 1,
    [Description("Completed")]
    Completed = 2,
    [Description("Failed")]
    Failed = 3,
    [Description("Cancelled")]
    Cancelled = 4,
    [Description("Expired")]
    Expired = 5,
    [Description("Refunded")]
    Refunded = 6
}