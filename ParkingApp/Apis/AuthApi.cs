using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Features.Logout.Command;
using ParkingApp.Application.Features.RefreshToken.Command;
using ParkingApp.Application.Features.SendOtp.Command;
using ParkingApp.Application.Features.VerifyOtp.Command;

namespace ParkingApp.Api.Apis;

public class AuthApi : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        app.MapGroup("auth")
            //.RequireAuthorization()
            .MapPost(SendOtp, "/send-otp", "")
            .MapPost(VerifyOtp, "/verify-otp", "")
            .MapPost(RefreshToken, "/refresh-token", "")
            .MapPost(Logout, "/logout", "");
    }

    private static async Task<IResult> SendOtp(ISender sender, IServiceProvider serviceProvider,
        SendOtpCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<SendOtpCommand, SendOtpResponse>(sender,
            request, serviceProvider, cancellationToken);

    private static async Task<IResult> VerifyOtp(ISender sender, IServiceProvider serviceProvider,
        VerifyOtpCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<VerifyOtpCommand, VerifyOtpResponse>(sender,
            request, serviceProvider, cancellationToken);

    private static async Task<IResult> RefreshToken(ISender sender, IServiceProvider serviceProvider,
        RefreshTokenCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<RefreshTokenCommand, RefreshTokenResponse>(sender,
            request, serviceProvider, cancellationToken);

    private static async Task<IResult> Logout(ISender sender, IServiceProvider serviceProvider,
        LogoutCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<LogoutCommand, LogoutResponse>(sender,
            request, serviceProvider, cancellationToken);
}