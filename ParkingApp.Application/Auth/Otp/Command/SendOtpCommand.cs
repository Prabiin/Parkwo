using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Auth.Interfaces;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;
using ParkingApp.Application.Features.Shared;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Features.SendOtp.Command;

public sealed record SendOtpCommand(string PhoneNumber)
    : IRequestResult<SendOtpCommand, SendOtpResponse>;

public sealed class SendOtpCommandHandler(
    IApplicationDbContext context,
    IOtpSender otpSender,
    OtpSettings otpSettings,
    RateLimitingSettings rateLimitingSettings)
    : IRequestResultHandler<SendOtpCommand, SendOtpResponse>
{
    // Application has no ASP.NET dependency, so StatusCodes is not available here.
    private const int TooManyRequests = 429;

    public async Task<Result<SendOtpResponse>> Handle(SendOtpCommand request, CancellationToken cancellationToken = default)
    {
        // Per-number throttle. The IP limiter cannot stop a botnet from SMS
        // bombing one victim, so this counts issued codes for this number over
        // the recent window. Cancelled and expired codes are retained in the
        // table, so they keep counting, which is what we want here.
        var windowStart = DateTimeOffset.UtcNow
            .AddMinutes(-rateLimitingSettings.OtpSendPerPhoneWindowMinutes);

        var recentOtpCount = await context.Otps
            .CountAsync(o => o.PhoneNumber == request.PhoneNumber
                             && o.CreatedAtUtc >= windowStart,
                cancellationToken);

        if (recentOtpCount >= rateLimitingSettings.OtpSendPerPhoneLimit)
        {
            return Result<SendOtpResponse>.Failure(
                "Too many verification codes requested for this number. Please try again later.",
                TooManyRequests);
        }

        var userExists = await context.Users
            .AnyAsync(u => u.PhoneNumber == request.PhoneNumber, cancellationToken);

        var purpose = userExists ? OtpTypeEnum.Login : OtpTypeEnum.Registration;

        await AuthDbHelper.CancelPendingOtpsAsync(
            context, request.PhoneNumber, purpose, cancellationToken);

        var code = AuthDbHelper.GenerateOtpCode();
        var otp = Otp.Create(request.PhoneNumber, code, purpose, OtpChannelEnum.Sms);
        context.Otps.Add(otp);

        await context.SaveChangesAsync(cancellationToken);

        await otpSender.SendAsync(request.PhoneNumber, code, cancellationToken);

        return Result<SendOtpResponse>.Success(
            new SendOtpResponse(
                "OTP sent to your phone number.",
                otp.ExpiresAtUtc,
                otpSettings.ExposeDevCode ? code : null));
    }
}