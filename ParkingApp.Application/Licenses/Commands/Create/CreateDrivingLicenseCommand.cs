using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;using ParkingApp.Application.Configuration;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Licenses.Commands.Create;

/// <summary>
/// A file attached to the multipart license form. The API layer maps
/// each IFormFile to this record; the handler validates, uploads, and disposes it.
/// </summary>
public sealed record LicenseFileUpload(
    string FileName,
    string ContentType,
    long Length,
    Stream Content);

/// <summary>
/// Submits (or resubmits after rejection) the rider's driving license.
/// Carries the raw multipart form: parsing, file validation, MinIO upload
/// and persistence all happen in the handler, keeping the API tier thin.
/// </summary>
public sealed record CreateDrivingLicenseCommand(
    string LicenseNumber,
    string CategoriesRaw,
    string ExpiryDateRaw,
    LicenseFileUpload? Front,
    LicenseFileUpload? Back)
    : IRequestResult<CreateDrivingLicenseCommand, Guid>;

public sealed class CreateDrivingLicenseCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IFileStorage storage,
    UploadSettings upload)
    : IRequestResultHandler<CreateDrivingLicenseCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateDrivingLicenseCommand request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<Guid>.Failure("Authentication required.", 401);

        var categories = LicenseCategoryParser.Parse(request.CategoriesRaw);
        if (categories is null)
            return Result<Guid>.Failure("License category codes required (e.g. \"2,4\").", 400);

        if (!DateOnly.TryParseExact(request.ExpiryDateRaw.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiryDate))
            return Result<Guid>.Failure("Expiry date is required as \"yyyy-MM-dd\".", 400);

        if (expiryDate <= DateOnly.FromDateTime(DateTime.UtcNow.Date))
            return Result<Guid>.Failure("Expiry date must be in the future.", 400);

        if (request.Front is null)
            return Result<Guid>.Failure("License front photo is required.", 400);

        if (request.Back is null)
            return Result<Guid>.Failure("License back photo is required.", 400);

        var frontError = UploadValidation.ValidateImage(request.Front.FileName, request.Front.Length, upload);
        if (frontError is not null)
            return Result<Guid>.Failure(frontError, 400);

        var backError = UploadValidation.ValidateImage(request.Back.FileName, request.Back.Length, upload);
        if (backError is not null)
            return Result<Guid>.Failure(backError, 400);

        var normalizedNumber = request.LicenseNumber.Trim().ToUpperInvariant();

        var existing = await context.DrivingLicenses.FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

        if (existing is not null && existing.ApprovalStatus != ApprovalStatusEnum.Rejected)
            return Result<Guid>.Failure("A driving license is already submitted on your account.", 409);

        var numberTaken = await context.DrivingLicenses
            .AnyAsync(d => d.LicenseNumber == normalizedNumber
                           && d.UserId != userId, cancellationToken);

        if (numberTaken)
            return Result<Guid>.Failure("This license number is already registered to another account.", 409);

        await using var frontStream = request.Front.Content;
        var storedFront = await storage.SaveAsync(
            frontStream, request.Front.FileName, request.Front.ContentType,
            "license-images", cancellationToken);

        await using var backStream = request.Back.Content;
        var storedBack = await storage.SaveAsync(
            backStream, request.Back.FileName, request.Back.ContentType,
            "license-images", cancellationToken);

        if (existing is not null)
        {
            var oldFront = existing.FrontImageUrl;
            var oldBack = existing.BackImageUrl;

            existing.Resubmit(
                normalizedNumber,
                categories,
                storedFront.Url,
                storedBack.Url,
                expiryDate);

            await context.SaveChangesAsync(cancellationToken);

            await storage.DeleteAsync(oldFront, cancellationToken);
            await storage.DeleteAsync(oldBack, cancellationToken);

            return Result<Guid>.Success(existing.Id);
        }

        var license = DrivingLicense.Create(
            userId.Value,
            normalizedNumber,
            categories,
            storedFront.Url,
            storedBack.Url,
            expiryDate);

        context.DrivingLicenses.Add(license);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(license.Id);
    }
}
