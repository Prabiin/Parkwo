using Microsoft.EntityFrameworkCore;
using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Facilities;
using ParkingApp.Application.Facilities.Commands.Create;
using ParkingApp.Application.Facilities.Commands.CreateReview;
using ParkingApp.Application.Facilities.Commands.CreateSpots;
using ParkingApp.Application.Facilities.Queries.GetMyParkingFacilities;
using ParkingApp.Application.Facilities.Queries.GetParkingFacilityById;
using ParkingApp.Application.Facilities.Queries.GetParkingFacilityReviews;
using ParkingApp.Domain;

namespace ParkingApp.Api.Apis;

public class FacilityApi : EndpointGroupBase
{
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];
    private const long MaxImageBytes = 5 * 1024 * 1024;

    public override void Map(IEndpointRouteBuilder app)
    {
        app.MapGroup("facilities")
            .MapPost(CreateFacility, "", "")
            .MapGet(ListMyFacilities, "", "")
            .MapGet(GetFacilityById, "{facilityId}", "")
            .MapPost(CreateSpots, "{facilityId}/spots", "")
            .MapPost(UploadImages, "{facilityId}/images", "")
            .MapPost(CreateReview, "{facilityId}/reviews", "")
            .MapGet(ListReviews, "{facilityId}/reviews", "")
            .RequireAuthorization();
    }

    private static async Task<IResult> CreateFacility(ISender sender, IServiceProvider serviceProvider,
        CreateParkingFacilityCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<CreateParkingFacilityCommand, CreateParkingFacilityResponse>(sender,
            request, serviceProvider, cancellationToken);

    private static async Task<IResult> ListMyFacilities(ISender sender, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        => await ExecuteQuery<GetMyParkingFacilitiesQuery, GetMyParkingFacilitiesResponse>(sender,
            new GetMyParkingFacilitiesQuery(), serviceProvider, cancellationToken);

    private static async Task<IResult> GetFacilityById(ISender sender, IServiceProvider serviceProvider,
        Guid facilityId, CancellationToken cancellationToken)
        => await ExecuteQuery<GetParkingFacilityByIdQuery, GetParkingFacilityByIdResponse>(sender,
            new GetParkingFacilityByIdQuery(facilityId), serviceProvider, cancellationToken);

    private static async Task<IResult> CreateSpots(ISender sender, IServiceProvider serviceProvider,
        Guid facilityId, CreateParkingSpotsCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<CreateParkingSpotsCommand, CreateParkingSpotsResponse>(sender,
            request with { FacilityId = facilityId }, serviceProvider, cancellationToken);

    private static async Task<IResult> CreateReview(ISender sender, IServiceProvider serviceProvider,
        Guid facilityId, CreateParkingFacilityReviewCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<CreateParkingFacilityReviewCommand, CreateParkingFacilityReviewResponse>(sender,
            request with { FacilityId = facilityId }, serviceProvider, cancellationToken);

    private static async Task<IResult> ListReviews(ISender sender, IServiceProvider serviceProvider,
        Guid facilityId, CancellationToken cancellationToken)
        => await ExecuteQuery<GetParkingFacilityReviewsQuery, GetParkingFacilityReviewsResponse>(sender,
            new GetParkingFacilityReviewsQuery(facilityId), serviceProvider, cancellationToken);

    private static async Task<IResult> UploadImages(Guid facilityId, IFormFileCollection files,
        IApplicationDbContext context, ICurrentUserService currentUser, IFileStorage storage,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Results.Unauthorized();

        var facility = await context.ParkingFacilities
            .AsNoTracking()
            .Select(f => new { f.Id, f.ProviderId })
            .FirstOrDefaultAsync(f => f.Id == facilityId, cancellationToken);

        if (facility is null)
            return Results.NotFound();

        if (!await ProviderOwnership.IsOwnerAsync(context, userId.Value, facility.ProviderId, cancellationToken))
            return Results.Forbid();

        if (files.Count == 0)
            return Results.BadRequest(new { Errors = new[] { "files: At least one image is required." } });

        foreach (var file in files)
        {
            if (!AllowedImageTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
                return Results.BadRequest(new
                {
                    Errors = new[] { $"files: '{file.FileName}' is not a supported image type." }
                });

            if (file.Length > MaxImageBytes || file.Length == 0)
                return Results.BadRequest(new
                {
                    Errors = new[] { $"files: '{file.FileName}' must be between 1 byte and 5 MB." }
                });
        }

        var currentMaxOrder = await context.ParkingFacilityImages
            .Where(i => i.FacilityId == facilityId)
            .Select(i => (int?)i.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var images = new List<ParkingFacilityImage>();

        foreach (var file in files)
        {
            await using var stream = file.OpenReadStream();
            var stored = await storage.SaveAsync(stream, file.FileName, file.ContentType, "facility-images", cancellationToken);

            currentMaxOrder++;

            images.Add(new ParkingFacilityImage
            {
                FacilityId = facilityId,
                FileName = stored.FileName,
                Url = stored.Url,
                ContentType = stored.ContentType,
                SizeInBytes = stored.SizeInBytes,
                SortOrder = currentMaxOrder,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        context.ParkingFacilityImages.AddRange(images);
        await context.SaveChangesAsync(cancellationToken);

        var items = images
            .Select(i => new ParkingFacilityImageResponse(
                i.Id, i.Url, i.FileName, i.ContentType, i.SizeInBytes, i.SortOrder))
            .ToList();

        return Results.Ok(new { Images = items });
    }
}