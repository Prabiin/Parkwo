using ParkingApp.Domain.Common.Base;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Domain;

public class Otp : AuditableEntity
{
    public string PhoneNumber { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public OtpTypeEnum Purpose { get; private set; }
    public OtpStatusEnum Status { get; private set; }
    public OtpChannelEnum Channel { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? VerifiedAtUtc { get; private set; }
    public int AttemptCount { get; private set; }

    private Otp() { }

    public static Otp Create(
        string phoneNumber,
        string code,
        OtpTypeEnum purpose,
        OtpChannelEnum channel = OtpChannelEnum.Sms,
        int expiresInMinutes = 5)
    {
        return new Otp
        {
            PhoneNumber = phoneNumber,
            Code = code,
            Purpose = purpose,
            Channel = channel,
            Status = OtpStatusEnum.Pending,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(expiresInMinutes),
            AttemptCount = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAtUtc;

    public bool Verify(string code)
    {
        if (Status != OtpStatusEnum.Pending)
            return false;

        if (IsExpired)
        {
            Status = OtpStatusEnum.Expired;
            return false;
        }

        AttemptCount++;

        if (Code != code)
            return false;

        Status = OtpStatusEnum.Verified;
        VerifiedAtUtc = DateTimeOffset.UtcNow;
        return true;
    }

    public bool HasExceededMaxAttempts(int maxAttempts = 5)
        => AttemptCount >= maxAttempts;

    public void Cancel()
    {
        if (Status == OtpStatusEnum.Pending)
            Status = OtpStatusEnum.Cancelled;
    }
}
