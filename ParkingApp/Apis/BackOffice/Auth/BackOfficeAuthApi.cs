using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.BackOffice.Commands.Login;
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
        group.MapGet("init", Init).RequireAuthorization(BackOfficePolicy);
        // Future: auth/logout, auth/change-password.
    }

    private static async Task<IResult> Login(ISender sender, IServiceProvider serviceProvider,
        BackOfficeLoginCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<BackOfficeLoginCommand, BackOfficeLoginResponse>(sender,
            request, serviceProvider, cancellationToken);

    // Shared dropdown codes for the admin console (kept here until a dedicated init group needs it).
    private static IResult Init()
        => Results.Ok(new
        {
            VehicleTypes = ListModel<VehicleTypeEnum>.FromEnum(),
            ApprovalStatuses = ListModel<ApprovalStatusEnum>.FromEnum()
        });
}
