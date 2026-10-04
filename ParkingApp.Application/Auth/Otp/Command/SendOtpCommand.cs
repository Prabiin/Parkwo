using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Auth.Interfaces;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Features.Shared;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Features.SendOtp.Command;

public sealed record SendOtpCommand(string PhoneNumber)
    : IRequestResult<SendOtpCommand, SendOtpResponse>;

public sealed class SendOtpCommandHandler(IApplicationDbContext context, IOtpSender otpSender)
    : IRequestResultHandler<SendOtpCommand, SendOtpResponse>
{
    public async Task<Result<SendOtpResponse>> Handle(SendOtpCommand request, CancellationToken cancellationToken = default)
    {
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
            new SendOtpResponse("OTP sent to your phone number.", otp.ExpiresAtUtc, code));
    }
}