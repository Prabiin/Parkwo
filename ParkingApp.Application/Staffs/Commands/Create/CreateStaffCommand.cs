using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Staffs.Commands.Create;

/// <summary>
/// BackOffice creates a gate staff member. The account is created with a
/// BackOffice-set facility and a generated password that is emailed to the
/// staff member's address; <see cref="Staff.MustChangePassword"/> is left true,
/// so the generated secret only ever opens the change-password screen.
/// <para>
/// The account is created whether or not the email goes out. A failed send is
/// not an error to undo — it is the case the BackOffice "resend" button exists
/// for, and the response says so in <see cref="CreateStaffResponse.PasswordEmailSent"/>.
/// </para>
/// </summary>
public sealed record CreateStaffCommand(
    string FullName,
    string PhoneNumber,
    string Email,
    DateOnly? DateOfBirth,
    GenderEnum? Gender,
    string? DeviceName,
    string? DeviceBrand,
    Guid FacilityId)
    : IRequestResult<CreateStaffCommand, CreateStaffResponse>;

public sealed record CreateStaffResponse(
    Guid StaffId,
    string FullName,
    string Email,
    Guid FacilityId,
    bool MustChangePassword,
    /// <summary>
    /// False when the generated password could not be emailed. The account is
    /// still created and usable — resend issues a fresh password. The client
    /// shows the resend action on the strength of this plus
    /// <see cref="MustChangePassword"/>.
    /// </summary>
    bool PasswordEmailSent);

public sealed class CreateStaffCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    ILogger<CreateStaffCommandHandler> logger,
    ICurrentUserService currentUser)
    : IRequestResultHandler<CreateStaffCommand, CreateStaffResponse>
{
    public async Task<Result<CreateStaffResponse>> Handle(
        CreateStaffCommand request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var phone = PhoneNumbers.Normalize(request.PhoneNumber);

        var facility = await context.ParkingFacilities
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == request.FacilityId, cancellationToken);

        if (facility is null)
            return Result<CreateStaffResponse>.Failure("Parking facility not found.", 404);

        if (await context.Staffs.AnyAsync(s => s.Email == email, cancellationToken))
            return Result<CreateStaffResponse>.Failure(
                "A staff member with this email already exists.", 409);

        var password = PasswordGenerator.Generate();

        var staff = Staff.Create(
            request.FullName,
            phone,
            email,
            passwordHasher.Hash(password),
            facility.Id,
            request.DateOfBirth,
            request.Gender,
            request.DeviceName,
            request.DeviceBrand);

        staff.CreatedBy = currentUser.UserId;

        // Persist first, then try to deliver. An undelivered password is NOT a
        // reason to throw the account away: the row is what the BackOffice
        // "resend" acts on, and recreating the same person from scratch would
        // be refused as a duplicate email. So the account is created either way
        // and a failed send is recovered by resending, which issues a fresh
        // password rather than trying to recall the one that was lost (it is
        // only ever held as a hash, so there is nothing to recall).
        context.Staffs.Add(staff);
        await context.SaveChangesAsync(cancellationToken);

        var deliveryStatus = await StaffPasswordDelivery.SendAsync(
            emailSender,
            logger,
            staff,
            "Your Parkwo staff account",
            StaffEmailTemplates.Welcome(staff.FullName, staff.Email, password),
            cancellationToken);

        return Result<CreateStaffResponse>.Success(
            new CreateStaffResponse(
                staff.Id,
                staff.FullName,
                staff.Email,
                staff.FacilityId,
                StaffPasswordDelivery.IsPasswordOutstanding(staff),
                !StaffPasswordDelivery.IsFailed(deliveryStatus)));
    }
}
