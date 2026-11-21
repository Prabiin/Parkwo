using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.SavedParkings.Commands.Remove;
using ParkingApp.Application.SavedParkings.Commands.Save;
using ParkingApp.Application.SavedParkings.Queries.GetSavedParkings;
using ParkingApp.Infrastructure.Auth;

namespace ParkingApp.Api.Apis;

/// <summary>
/// The rider's saved (bookmarked) parking places — the Saved tab under
/// Bookings. Both writes are idempotent so the bookmark icon can be toggled
/// without checking state first.
/// </summary>
public class SavedParkingApi : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        app.MapGroup("saved-parkings")
            .MapGet(GetSavedParkingsList, "list", "")
            // Body: { "facilityId": "…" } or { "freeParkingId": "…" }.
            .MapPost(SaveParking, "", "")
            // From the Saved tab.
            .MapDelete(RemoveParkingById, "{savedId:guid}", "")
            .RequireAuthorization(ProfileCompleteRequirement.PolicyName);
    }

    private static async Task<IResult> GetSavedParkingsList(ISender sender, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        => await ExecuteQuery<GetSavedParkingsQuery, GetSavedParkingsResponse>(sender,
            new GetSavedParkingsQuery(), serviceProvider, cancellationToken);

    private static async Task<IResult> SaveParking(ISender sender, IServiceProvider serviceProvider,
        SaveParkingCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<SaveParkingCommand, Guid>(sender,
            request, serviceProvider, cancellationToken);

    private static async Task<IResult> RemoveParkingById(ISender sender, IServiceProvider serviceProvider,
        Guid savedId, CancellationToken cancellationToken)
        => await ExecuteCommand<RemoveSavedParkingCommand, Unit>(sender,
            new RemoveSavedParkingCommand(savedId), serviceProvider, cancellationToken);
}
