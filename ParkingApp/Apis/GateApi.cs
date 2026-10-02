using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.Bookings.Commands.ScanEntry;
using ParkingApp.Application.Bookings.Commands.ScanExit;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;

namespace ParkingApp.Api.Apis;

/// <summary>
/// Gate endpoints used by staff phones at the entrance and exit. Every route is
/// facility-scoped: the facility id in the body is not decoration, it is what
/// the handler authorises the caller against, so a pass cannot be presented at
/// the wrong lot.
/// </summary>
public class GateApi : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("gate")
            .RequireAuthorization();

        group.MapPost(ScanEntry, "entry", "");
        group.MapPost(ScanExit, "exit", "");
    }

    private static async Task<IResult> ScanEntry(ISender sender, IServiceProvider serviceProvider,
        ScanEntryCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<ScanEntryCommand, ScanEntryResponse>(sender,
            request, serviceProvider, cancellationToken);

    private static async Task<IResult> ScanExit(ISender sender, IServiceProvider serviceProvider,
        ScanExitCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<ScanExitCommand, ScanExitResponse>(sender,
            request, serviceProvider, cancellationToken);
}
