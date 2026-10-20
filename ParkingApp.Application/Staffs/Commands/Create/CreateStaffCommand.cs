using Microsoft.EntityFrameworkCore;
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
    bool MustChangePassword);

public sealed class CreateStaffCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
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

        // Deliver the secret before the row exists. The generated password is
        // kept in no form other than its hash, so a delivery failure here has
        // nothing to undo — the admin simply retries. Persisting first and
        // emailing second would strand a staff account whose password nobody
        // knows (only a reset would recover it), and this has no mail outbox to
        // retry from: "created" has to mean "the password was delivered".
        try
        {
            await emailSender.SendAsync(
                staff.Email,
                "Your Parkwo staff account",
                StaffEmailTemplates.Welcome(staff.FullName, staff.Email, password),
                cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<CreateStaffResponse>.Failure(
                $"The password email could not be sent, so no staff account was created. {ex.Message}",
                502);
        }

        context.Staffs.Add(staff);
        await context.SaveChangesAsync(cancellationToken);

        return Result<CreateStaffResponse>.Success(
            new CreateStaffResponse(
                staff.Id,
                staff.FullName,
                staff.Email,
                staff.FacilityId,
                staff.MustChangePassword));
    }
}
