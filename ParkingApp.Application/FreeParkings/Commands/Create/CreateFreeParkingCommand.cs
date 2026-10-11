using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;
using ParkingApp.Domain;

namespace ParkingApp.Application.FreeParkings.Commands.Create;

/// <summary>
/// BackOffice-only: lists a public free parking on the rider map — fields and
/// photos in a single multipart request, mirroring the license submission. No
/// provider, no approval: these are Parkwo's own listings, so they go live
/// immediately (unless created inactive). File validation, MinIO upload and
/// persistence all happen here, so the API tier stays thin.
/// </summary>
public sealed record CreateFreeParkingCommand(
    string Name,
    string? Description,
    string Address,
    double Latitude,
    double Longitude,
    int? TwoWheelerOccupancy,
    int? FourWheelerOccupancy,
    bool IsActive,
    IReadOnlyList<FreeParkingImageUpload> Images)
    : IRequestResult<CreateFreeParkingCommand, Guid>;

public sealed class CreateFreeParkingCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IFileStorage storage,
    UploadSettings upload)
    : IRequestResultHandler<CreateFreeParkingCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateFreeParkingCommand request, CancellationToken cancellationToken = default)
    {
        // Reject a bad photo before we create the row, so a failed upload never
        // leaves a half-formed listing behind.
        foreach (var image in request.Images)
        {
            var error = UploadValidation.ValidateImage(image.FileName, image.Length, upload);
            if (error is not null)
                return Result<Guid>.Failure(error, 400);
        }

        var facility = new FreeParkingFacility
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Address = request.Address.Trim(),
            TwoWheelerOccupancy = request.TwoWheelerOccupancy,
            FourWheelerOccupancy = request.FourWheelerOccupancy,
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = currentUser.UserId
        };

        facility.SetCoordinates(request.Latitude, request.Longitude);

        var sortOrder = 0;
        foreach (var image in request.Images)
        {
            await using var stream = image.Content;
            var stored = await storage.SaveAsync(
                stream, image.FileName, image.ContentType, "free-parking-images", cancellationToken);

            facility.Images.Add(new FreeParkingFacilityImage
            {
                FileName = stored.FileName,
                Url = stored.Url,
                ContentType = stored.ContentType,
                SizeInBytes = stored.SizeInBytes,
                SortOrder = sortOrder++,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        context.FreeParkingFacilities.Add(facility);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(facility.Id);
    }
}
