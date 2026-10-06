using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.Bookings.Commands.ScanGate;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Infrastructure.Auth;

namespace ParkingApp.Api.Apis;

/// <summary>
/// Single-gate scan endpoints used by staff phones. Every route is
/// facility-scoped: the facility id in the body is not decoration, it is what
/// the handler authorises the caller against, so a pass cannot be presented at
/// the wrong lot.
/// </summary>
public class GateApi : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("gate")
            .RequireAuthorization(ProfileCompleteRequirement.PolicyName);

        group.MapPost(ScanGate, "entry-exit", "");
    }

    private static async Task<IResult> ScanGate(ISender sender, IServiceProvider serviceProvider,
        ScanGateCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<ScanGateCommand, ScanGateResponse>(sender,
            request, serviceProvider, cancellationToken);
}
