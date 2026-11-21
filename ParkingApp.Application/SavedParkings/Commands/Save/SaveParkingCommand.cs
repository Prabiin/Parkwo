using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.SavedParkings.Commands.Save;

/// <summary>
/// Bookmarks a paid lot or a free parking. Idempotent: saving a place that is
/// already saved returns the existing bookmark's id instead of failing, so the
/// app's bookmark button can be tapped without first checking its state.
/// </summary>
public sealed record SaveParkingCommand(Guid? FacilityId, Guid? FreeParkingId)
    : IRequestResult<SaveParkingCommand, Guid>;

public sealed class SaveParkingCommandValidator : AbstractValidator<SaveParkingCommand>
{
    public SaveParkingCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => x.FacilityId.HasValue ^ x.FreeParkingId.HasValue)
            .WithMessage("Send exactly one of facilityId or freeParkingId.")
            .OverridePropertyName("Target");
    }
}

public sealed class SaveParkingCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestResultHandler<SaveParkingCommand, Guid>
{
    public async Task<Result<Guid>> Handle(SaveParkingCommand request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<Guid>.Failure("Authentication required.", 401);

        if (request.FacilityId is { } facilityId)
        {
            // Only lots riders can actually see may be saved.
            var visible = await context.ParkingFacilities
                .AnyAsync(f => f.Id == facilityId && f.ApprovalStatus == ApprovalStatusEnum.Verified, cancellationToken);
            if (!visible)
                return Result<Guid>.Failure("Parking facility not found.", 404);

            var existing = await context.SavedParkings
                .Where(s => s.UserId == userId && s.FacilityId == facilityId)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (existing is { } id)
                return Result<Guid>.Success(id);

            var saved = SavedParking.ForFacility(userId.Value, facilityId);
            context.SavedParkings.Add(saved);
            await context.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(saved.Id);
        }

        var freeId = request.FreeParkingId!.Value;

        var freeVisible = await context.FreeParkingFacilities
            .AnyAsync(f => f.Id == freeId && f.IsActive && !f.IsDeleted, cancellationToken);
        if (!freeVisible)
            return Result<Guid>.Failure("Free parking not found.", 404);

        var existingFree = await context.SavedParkings
            .Where(s => s.UserId == userId && s.FreeParkingFacilityId == freeId)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (existingFree is { } freeSavedId)
            return Result<Guid>.Success(freeSavedId);

        var savedFree = SavedParking.ForFreeParking(userId.Value, freeId);
        context.SavedParkings.Add(savedFree);
        await context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(savedFree.Id);
    }
}
