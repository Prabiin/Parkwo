using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;
using ParkingApp.Domain;

namespace ParkingApp.Application.FreeParkings.Commands.Update;

/// <summary>
/// BackOffice-only: replaces a free parking's editable fields and its photos in
/// one multipart request (what the edit page submits after populating itself
/// from the detail GET). The incoming image set IS the final state: photos whose
/// ids are in <see cref="KeepImageIds"/> survive, everything else is deleted, and
/// <see cref="NewImages"/> are appended. <see cref="IsActive"/> is the publish
/// toggle; deleting the whole listing is a separate command.
/// </summary>
public sealed record UpdateFreeParkingCommand(
    Guid FreeParkingFacilityId,
    string Name,
    string? Description,
    string Address,
    double Latitude,
    double Longitude,
    int? TwoWheelerOccupancy,
    int? FourWheelerOccupancy,
    bool IsActive,
    IReadOnlyList<Guid> KeepImageIds,
    IReadOnlyList<FreeParkingImageUpload> NewImages)
    : IRequestResult<UpdateFreeParkingCommand, Unit>;

public sealed class UpdateFreeParkingCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IFileStorage storage,
    UploadSettings upload)
    : IRequestResultHandler<UpdateFreeParkingCommand, Unit>
{
    public async Task<Result<Unit>> Handle(UpdateFreeParkingCommand request, CancellationToken cancellationToken = default)
    {
        var facility = await context.FreeParkingFacilities
            .Include(f => f.Images)
            .FirstOrDefaultAsync(f => f.Id == request.FreeParkingFacilityId && !f.IsDeleted, cancellationToken);

        if (facility is null)
            return Result<Unit>.Failure("Free parking not found.", 404);

        // Reject a bad new photo before we touch anything.
        foreach (var image in request.NewImages)
        {
            var error = UploadValidation.ValidateImage(image.FileName, image.Length, upload);
            if (error is not null)
                return Result<Unit>.Failure(error, 400);
        }

        facility.Name = request.Name.Trim();
        facility.Description = request.Description?.Trim();
        facility.Address = request.Address.Trim();
        facility.TwoWheelerOccupancy = request.TwoWheelerOccupancy;
        facility.FourWheelerOccupancy = request.FourWheelerOccupancy;
        facility.IsActive = request.IsActive;
        facility.SetCoordinates(request.Latitude, request.Longitude);
        facility.UpdatedAtUtc = DateTimeOffset.UtcNow;
        facility.UpdatedBy = currentUser.UserId;

        // The request's image set is the final state: keep the listed ids, drop
        // the rest (ids that are not ours are simply ignored).
        var keep = request.KeepImageIds.ToHashSet();
        var removed = facility.Images.Where(i => !keep.Contains(i.Id)).ToList();

        foreach (var image in removed)
        {
            facility.Images.Remove(image);
            context.FreeParkingFacilityImages.Remove(image);
        }

        // Re-number the survivors so appends line up, then add the new photos.
        var sortOrder = 0;
        foreach (var image in facility.Images.OrderBy(i => i.SortOrder).ToList())
            image.SortOrder = sortOrder++;

        foreach (var newImage in request.NewImages)
        {
            await using var stream = newImage.Content;
            var stored = await storage.SaveAsync(
                stream, newImage.FileName, newImage.ContentType, "free-parking-images", cancellationToken);

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

        await context.SaveChangesAsync(cancellationToken);

        // Storage cleanup is best-effort: the rows are already gone, so a
        // storage hiccup must not fail the request.
        foreach (var image in removed)
            await storage.DeleteAsync(image.Url, cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
