using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.BackOffice.Role;

namespace ParkingApp.Application.BackOffice.Commands.UpdateUser;

public sealed record UpdateUserCommand(
    Guid UserId,
    string? UserName,
    string? FullName,
    string? Email,
    bool? IsActive,
    IReadOnlyList<Guid>? RoleIds)
    : IRequestResult<UpdateUserCommand, Unit>;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.UserName)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.UserName));

        RuleFor(x => x.FullName)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.FullName));

        RuleFor(x => x.Email)
            .MaximumLength(500)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.RoleIds)
            .NotNull()
            .When(x => x.RoleIds != null);
    }
}

public sealed class UpdateUserCommandHandler(IApplicationDbContext context)
    : IRequestResultHandler<UpdateUserCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken = default)
    {
        var user = await context.BackOfficeUsers
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result<Unit>.Failure("User not found.", 404);

        // Update username if provided
        if (!string.IsNullOrWhiteSpace(request.UserName))
        {
            var normalizedUserName = request.UserName.Trim().ToLower();
            var existingUser = await context.BackOfficeUsers
                .FirstOrDefaultAsync(u => u.UserName.ToLower() == normalizedUserName && u.Id != request.UserId, cancellationToken);

            if (existingUser is not null)
                return Result<Unit>.Failure("Username already exists.", 400);

            user.UserName = normalizedUserName;
        }

        // Update full name if provided
        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            user.FullName = request.FullName.Trim();
        }

        // Update email if provided
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var normalizedEmail = request.Email.Trim().ToLower();
            var existingEmail = await context.BackOfficeUsers
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail && u.Id != request.UserId, cancellationToken);

            if (existingEmail is not null)
                return Result<Unit>.Failure("Email already exists.", 400);

            user.Email = normalizedEmail;
        }

        // Update IsActive if provided
        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        // Update roles if provided
        if (request.RoleIds != null)
        {
            // Validate role IDs exist
            if (request.RoleIds.Count > 0)
            {
                var validRoleIds = await context.BackOfficeRoles
                    .Where(r => request.RoleIds.Contains(r.Id))
                    .Select(r => r.Id)
                    .ToListAsync(cancellationToken);

                var invalidIds = request.RoleIds.Except(validRoleIds).ToList();
                if (invalidIds.Count > 0)
                    return Result<Unit>.Failure($"Invalid role IDs: {string.Join(", ", invalidIds)}", 400);
            }

            // Replace roles
            user.UserRoles.Clear();
            if (request.RoleIds.Count > 0)
            {
                foreach (var roleId in request.RoleIds)
                {
                    user.UserRoles.Add(new BackOfficeUserRole
                    {
                        RoleId = roleId,
                        CreatedAtUtc = DateTimeOffset.UtcNow
                    });
                }
            }
        }

        user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}