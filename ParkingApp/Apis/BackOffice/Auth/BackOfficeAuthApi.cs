using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.BackOffice.Commands.ChangePassword;
using ParkingApp.Application.BackOffice.Commands.Login;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Models;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Api.Apis.BackOffice.Auth;

public class BackOfficeAuthApi : BackOfficeGroup
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("backoffice");

        group.MapPost(Login, "auth/login", "");
        // Deliberately the narrow policy: a user still on the generated password
        // must be able to change it before any other console route opens.
        group.MapPost(ChangePassword, "auth/change-password", "")
            .RequireAuthorization(BackOfficeAuthenticatedPolicy);
        group.MapGet("init", Init).RequireAuthorization(BackOfficePolicy);
        // Future: auth/logout.
    }

    private static async Task<IResult> Login(ISender sender, IServiceProvider serviceProvider,
        BackOfficeLoginCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<BackOfficeLoginCommand, BackOfficeLoginResponse>(sender,
            request, serviceProvider, cancellationToken);

    private static async Task<IResult> ChangePassword(ISender sender, IServiceProvider serviceProvider,
        BackOfficeChangePasswordCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<BackOfficeChangePasswordCommand, Unit>(sender,
            request, serviceProvider, cancellationToken);

    // Shared dropdown codes for the admin console (kept here until a dedicated init group needs it).
    private static IResult Init()
        => Results.Ok(new
        {
            VehicleTypes = ListModel<VehicleTypeEnum>.FromEnum(),
            ApprovalStatuses = ListModel<ApprovalStatusEnum>.FromEnum()
        });
}
