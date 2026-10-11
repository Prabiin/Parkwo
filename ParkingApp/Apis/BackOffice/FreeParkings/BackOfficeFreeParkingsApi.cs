using Microsoft.AspNetCore.Mvc;
using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.FreeParkings;
using ParkingApp.Application.FreeParkings.Commands.Create;
using ParkingApp.Application.FreeParkings.Commands.Delete;
using ParkingApp.Application.FreeParkings.Commands.Update;
using ParkingApp.Application.FreeParkings.Queries.GetFreeParkingById;
using ParkingApp.Application.FreeParkings.Queries.GetFreeParkings;

namespace ParkingApp.Api.Apis.BackOffice.FreeParkings;

/// <summary>
/// BackOffice CRUD for Parkwo's own public free parkings. These are the lots
/// Parkwo lists itself (no provider, no approval, never bookable), shown to
/// riders as map markers. Create and update are single multipart requests that
/// carry the fields and the photos together — the edit page GETs everything,
/// then PUTs the whole new state back in one call.
/// </summary>
public class BackOfficeFreeParkingsApi : BackOfficeGroup
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("backoffice");

        group.MapPost(Create, "free-parkings", "", disableAntiforgery: true)
            .RequireAuthorization(BackOfficePolicy);
        group.MapGet("free-parkings", List).RequireAuthorization(BackOfficePolicy);
        group.MapGet("free-parkings/{id}", Detail).RequireAuthorization(BackOfficePolicy);
        group.MapPut(Update, "free-parkings/{id}", "", disableAntiforgery: true)
            .RequireAuthorization(BackOfficePolicy);
        group.MapDelete("free-parkings/{id}", Delete).RequireAuthorization(BackOfficePolicy);
    }

    private static async Task<IResult> Create([FromForm] CreateFreeParkingRequest request,
        ISender sender, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        => await ExecuteCommand<CreateFreeParkingCommand, Guid>(sender,
            new CreateFreeParkingCommand(
                request.Name,
                request.Description,
                request.Address,
                request.Latitude,
                request.Longitude,
                request.TwoWheelerOccupancy,
                request.FourWheelerOccupancy,
                request.IsActive,
                ToUploads(request.Files)),
            serviceProvider, cancellationToken);

    private static async Task<IResult> Update([FromForm] UpdateFreeParkingRequest request, Guid id,
        ISender sender, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        => await ExecuteCommand<UpdateFreeParkingCommand, Unit>(sender,
            new UpdateFreeParkingCommand(
                id,
                request.Name,
                request.Description,
                request.Address,
                request.Latitude,
                request.Longitude,
                request.TwoWheelerOccupancy,
                request.FourWheelerOccupancy,
                request.IsActive,
                request.KeepImageIds ?? [],
                ToUploads(request.Files)),
            serviceProvider, cancellationToken);

    private static List<FreeParkingImageUpload> ToUploads(List<IFormFile>? files)
        => files is null
            ? []
            : files.Select(f => new FreeParkingImageUpload(
                f.FileName, f.ContentType, f.Length, f.OpenReadStream())).ToList();

    private static async Task<IResult> List(ISender sender, IServiceProvider serviceProvider,
        string? name, bool? isActive, CancellationToken cancellationToken)
        => await ExecuteQuery<GetFreeParkingsQuery, GetFreeParkingsResponse>(sender,
            new GetFreeParkingsQuery(name, isActive), serviceProvider, cancellationToken);

    private static async Task<IResult> Detail(ISender sender, IServiceProvider serviceProvider,
        Guid id, CancellationToken cancellationToken)
        => await ExecuteQuery<GetFreeParkingByIdQuery, GetFreeParkingByIdResponse>(sender,
            new GetFreeParkingByIdQuery(id), serviceProvider, cancellationToken);

    private static async Task<IResult> Delete(ISender sender, IServiceProvider serviceProvider,
        Guid id, CancellationToken cancellationToken)
        => await ExecuteCommand<DeleteFreeParkingCommand, Unit>(sender,
            new DeleteFreeParkingCommand(id), serviceProvider, cancellationToken);
}

/// <summary>
/// Wire shape of the multipart "create free parking" form. Bound with
/// [FromForm] so the OpenAPI document describes every field and the image
/// picker — one request carries the lot's details and its photos together.
/// The field name for the files is <c>files</c> (repeatable).
/// </summary>
public sealed class CreateFreeParkingRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Address { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public int? TwoWheelerOccupancy { get; set; }

    public int? FourWheelerOccupancy { get; set; }

    public bool IsActive { get; set; } = true;

    public List<IFormFile>? Files { get; set; }
}

/// <summary>
/// Wire shape of the multipart "update free parking" form. <c>keepImageIds</c>
/// carries the ids of the existing photos to retain (from the detail GET);
/// any existing photo not listed is deleted. <c>files</c> (repeatable) adds new
/// photos, appended after the kept ones.
/// </summary>
public sealed class UpdateFreeParkingRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Address { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public int? TwoWheelerOccupancy { get; set; }

    public int? FourWheelerOccupancy { get; set; }

    public bool IsActive { get; set; } = true;

    public List<Guid>? KeepImageIds { get; set; }

    public List<IFormFile>? Files { get; set; }
}
