using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;

namespace ParkingApp.Application.SavedParkings.Commands.Remove;

/// <summary>
/// Removes a bookmark, addressed either by its own id (Saved tab) or by the
/// place it points at (the bookmark toggle on a lot's detail screen). Removing
/// something that is not saved succeeds, so the toggle is idempotent too.
/// </summary>
public sealed record RemoveSavedParkingCommand(Guid SavedId)
    : IRequestResult<RemoveSavedParkingCommand, Unit>;

public sealed class RemoveSavedParkingCommandValidator : AbstractValidator<RemoveSavedParkingCommand>
{
    public RemoveSavedParkingCommandValidator()
    {
        RuleFor(x => x.SavedId)
            .NotEmpty()
            .WithMessage("SavedId is required.");
    }
}

public sealed class RemoveSavedParkingCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestResultHandler<RemoveSavedParkingCommand, Unit>
{
    public async Task<Result<Unit>> Handle(RemoveSavedParkingCommand request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<Unit>.Failure("Authentication required.", 401);

        var saved = await context.SavedParkings
            .FirstOrDefaultAsync(
                s => s.Id == request.SavedId && s.UserId == userId,
                cancellationToken);

        if (saved is null)
            return Result<Unit>.Failure("Saved parking not found.", 404);

        context.SavedParkings.Remove(saved);

        await context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
