using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.StaffAuth.Commands.ChangePassword;
using ParkingApp.Application.StaffAuth.Commands.Login;
using ParkingApp.Application.StaffAuth.Commands.Logout;
using ParkingApp.Application.StaffAuth.Commands.RefreshToken;
using ParkingApp.Application.Staffs.Queries.GetStaffProfile;
using ParkingApp.Infrastructure.Auth;

namespace ParkingApp.Api.Apis;

/// <summary>
/// The staff app's own surface. Login and refresh are anonymous (they create the
/// session); change-password and profile require a staff token. Operational
/// routes — the gate scan and the walk-in desk — live on their own endpoints and
/// additionally require that the generated password has been changed.
/// </summary>
public class StaffApi : EndpointGroupBase
{
    public const string StaffPolicy = "StaffOnly";

    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("staff");

        group.MapPost(Login, "auth/login", "");
        group.MapPost(RefreshToken, "auth/refresh-token", "");
        group.MapPost(Logout, "auth/logout", "");
        group.MapPost(ChangePassword, "auth/change-password", "")
            .RequireAuthorization(StaffPolicy);
        group.MapGet(Profile, "profile", "")
            .RequireAuthorization(StaffPolicy);
    }

    private static async Task<IResult> Login(ISender sender, IServiceProvider serviceProvider,
        StaffLoginCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<StaffLoginCommand, StaffLoginResponse>(sender,
            request, serviceProvider, cancellationToken);

    private static async Task<IResult> RefreshToken(ISender sender, IServiceProvider serviceProvider,
        StaffRefreshTokenCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<StaffRefreshTokenCommand, StaffRefreshTokenResponse>(sender,
            request, serviceProvider, cancellationToken);

    private static async Task<IResult> Logout(ISender sender, IServiceProvider serviceProvider,
        StaffLogoutCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<StaffLogoutCommand, Unit>(sender,
            request, serviceProvider, cancellationToken);

    private static async Task<IResult> ChangePassword(ISender sender, IServiceProvider serviceProvider,
        StaffChangePasswordCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<StaffChangePasswordCommand, Unit>(sender,
            request, serviceProvider, cancellationToken);

    private static async Task<IResult> Profile(ISender sender, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        => await ExecuteQuery<GetStaffProfileQuery, GetStaffProfileResponse>(sender,
            new GetStaffProfileQuery(), serviceProvider, cancellationToken);
}
