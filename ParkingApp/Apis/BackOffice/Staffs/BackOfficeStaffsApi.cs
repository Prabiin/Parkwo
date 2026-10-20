using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Staffs.Commands.Create;
using ParkingApp.Application.Staffs.Commands.Delete;
using ParkingApp.Application.Staffs.Commands.ResetPassword;
using ParkingApp.Application.Staffs.Commands.ResendInvite;
using ParkingApp.Application.Staffs.Commands.SetActive;
using ParkingApp.Application.Staffs.Commands.Update;
using ParkingApp.Application.Staffs.Queries.GetStaffById;
using ParkingApp.Application.Staffs.Queries.GetStaffInit;
using ParkingApp.Application.Staffs.Queries.GetStaffs;

namespace ParkingApp.Api.Apis.BackOffice.Staffs;

/// <summary>
/// BackOffice CRUD for gate staff. Parkwo field employees are a first-class
/// identity here — created with a facility assignment and a generated password
/// that is emailed to them, then forced to set their own on first login. The
/// list endpoint supports the three states the roster needs: all / active /
/// inactive, with an optional facility and free-text search.
/// </summary>
public class BackOfficeStaffsApi : BackOfficeGroup
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("backoffice");

        group.MapPost(Create, "staff", "").RequireAuthorization(BackOfficePolicy);
        group.MapGet("staff", List).RequireAuthorization(BackOfficePolicy);
        group.MapGet("staff/init", Init).RequireAuthorization(BackOfficePolicy);
        group.MapGet("staff/{id}", Detail).RequireAuthorization(BackOfficePolicy);
        group.MapPut(Update, "staff/{id}", "").RequireAuthorization(BackOfficePolicy);
        group.MapPut(ResetPassword, "staff/{id}/password", "").RequireAuthorization(BackOfficePolicy);
        group.MapPut(ResendInvite, "staff/{id}/resend-invite", "").RequireAuthorization(BackOfficePolicy);
        group.MapPut(SetActive, "staff/{id}/active", "").RequireAuthorization(BackOfficePolicy);
        group.MapDelete(Delete, "staff/{id}", "").RequireAuthorization(BackOfficePolicy);
    }

    private static async Task<IResult> Create(ISender sender, IServiceProvider serviceProvider,
        CreateStaffRequest request, CancellationToken cancellationToken)
        => await ExecuteCommand<CreateStaffCommand, CreateStaffResponse>(sender,
            new CreateStaffCommand(
                request.FullName,
                request.PhoneNumber,
                request.Email,
                request.DateOfBirth,
                request.Gender,
                request.DeviceName,
                request.DeviceBrand,
                request.FacilityId),
            serviceProvider, cancellationToken);

    private static async Task<IResult> Update(ISender sender, IServiceProvider serviceProvider,
        Guid id, UpdateStaffRequest request, CancellationToken cancellationToken)
        => await ExecuteCommand<UpdateStaffCommand, Unit>(sender,
            new UpdateStaffCommand(
                id,
                request.FullName,
                request.PhoneNumber,
                request.Email,
                request.DateOfBirth,
                request.Gender,
                request.DeviceName,
                request.DeviceBrand,
                request.FacilityId),
            serviceProvider, cancellationToken);

    private static async Task<IResult> ResetPassword(ISender sender, IServiceProvider serviceProvider,
        Guid id, CancellationToken cancellationToken)
        => await ExecuteCommand<ResetStaffPasswordCommand, Unit>(sender,
            new ResetStaffPasswordCommand(id), serviceProvider, cancellationToken);

    /// <summary>
    /// "Resend invite" — a distinct route from the reset above even though both
    /// rotate the password. They mean different things to the operator (one is
    /// "they never got it", the other "they lost it"), the client shows them at
    /// different moments, and keeping them separate means the response can be
    /// shaped for its own button without a flag deciding which behaviour ran.
    /// </summary>
    private static async Task<IResult> ResendInvite(ISender sender, IServiceProvider serviceProvider,
        Guid id, CancellationToken cancellationToken)
        => await ExecuteCommand<ResendStaffInviteCommand, Unit>(sender,
            new ResendStaffInviteCommand(id), serviceProvider, cancellationToken);

    private static async Task<IResult> SetActive(ISender sender, IServiceProvider serviceProvider,
        Guid id, SetStaffActiveRequest request, CancellationToken cancellationToken)
        => await ExecuteCommand<SetStaffActiveCommand, Unit>(sender,
            new SetStaffActiveCommand(id, request.IsActive), serviceProvider, cancellationToken);

    private static async Task<IResult> Delete(ISender sender, IServiceProvider serviceProvider,
        Guid id, CancellationToken cancellationToken)
        => await ExecuteCommand<DeleteStaffCommand, Unit>(sender,
            new DeleteStaffCommand(id), serviceProvider, cancellationToken);

    private static async Task<IResult> List(ISender sender, IServiceProvider serviceProvider,
        Guid? facilityId, string? search, string? status, CancellationToken cancellationToken)
    {
        // status is all|active|inactive; absent means everything. IsDefined guards
        // against Enum.TryParse accepting an out-of-range numeric like "5".
        var filter = StaffStatusFilterEnum.All;
        if (!string.IsNullOrWhiteSpace(status)
            && !(Enum.TryParse(status, ignoreCase: true, out filter)
                 && Enum.IsDefined(typeof(StaffStatusFilterEnum), filter)))
            return Results.BadRequest(new { Errors = new[] { "status: Use all, active or inactive." } });

        return await ExecuteQuery<GetStaffsQuery, GetStaffsResponse>(sender,
            new GetStaffsQuery(facilityId, search, filter), serviceProvider, cancellationToken);
    }

    private static async Task<IResult> Init(ISender sender, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        => await ExecuteQuery<GetStaffInitQuery, GetStaffInitResponse>(sender,
            new GetStaffInitQuery(), serviceProvider, cancellationToken);

    private static async Task<IResult> Detail(ISender sender, IServiceProvider serviceProvider,
        Guid id, CancellationToken cancellationToken)
        => await ExecuteQuery<GetStaffByIdQuery, GetStaffByIdResponse>(sender,
            new GetStaffByIdQuery(id), serviceProvider, cancellationToken);
}

public sealed class CreateStaffRequest
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public Domain.Common.Enums.GenderEnum? Gender { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceBrand { get; set; }
    public Guid FacilityId { get; set; }
}

public sealed class UpdateStaffRequest
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public Domain.Common.Enums.GenderEnum? Gender { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceBrand { get; set; }
    public Guid FacilityId { get; set; }
}

public sealed class SetStaffActiveRequest
{
    public bool IsActive { get; set; }
}
