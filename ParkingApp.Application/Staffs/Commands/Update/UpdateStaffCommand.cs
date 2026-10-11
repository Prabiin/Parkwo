using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Staffs.Commands.Update;

/// <summary>
/// BackOffice edits a staff member's profile and facility assignment. The email
/// moves the login with it (it is the username), so uniqueness is re-checked;
/// the password is untouched — a reset is the separate action that rotates it.
/// </summary>
public sealed record UpdateStaffCommand(
    Guid StaffId,
    string FullName,
    string PhoneNumber,
    string Email,
    DateOnly? DateOfBirth,
    GenderEnum? Gender,
    string? DeviceName,
    string? DeviceBrand,
    Guid FacilityId)
    : IRequestResult<UpdateStaffCommand, Unit>;

public sealed class UpdateStaffCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestResultHandler<UpdateStaffCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        UpdateStaffCommand request,
        CancellationToken cancellationToken = default)
    {
        var staff = await context.Staffs
            .FirstOrDefaultAsync(s => s.Id == request.StaffId && !s.IsDeleted, cancellationToken);

        if (staff is null)
            return Result<Unit>.Failure("Staff member not found.", 404);

        var email = request.Email.Trim().ToLowerInvariant();

        if (await context.Staffs.AnyAsync(s => s.Email == email && s.Id != staff.Id, cancellationToken))
            return Result<Unit>.Failure("A staff member with this email already exists.", 409);

        if (!await context.ParkingFacilities.AnyAsync(f => f.Id == request.FacilityId, cancellationToken))
            return Result<Unit>.Failure("Parking facility not found.", 404);

        staff.UpdateDetails(
            request.FullName,
            PhoneNumbers.Normalize(request.PhoneNumber),
            email,
            request.FacilityId,
            request.DateOfBirth,
            request.Gender,
            request.DeviceName,
            request.DeviceBrand);

        staff.UpdatedBy = currentUser.UserId;

        await context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
