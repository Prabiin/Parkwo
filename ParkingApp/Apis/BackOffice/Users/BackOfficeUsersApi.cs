using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.BackOffice.Commands.CreateUser;
using ParkingApp.Application.BackOffice.Commands.DeleteUser;
using ParkingApp.Application.BackOffice.Commands.ResendInvite;
using ParkingApp.Application.BackOffice.Commands.UpdateUser;
using ParkingApp.Application.BackOffice.Queries.GetUserById;
using ParkingApp.Application.BackOffice.Queries.GetUsers;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;

namespace ParkingApp.Api.Apis.BackOffice.Users;

public class BackOfficeUsersApi : BackOfficeGroup
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("backoffice/users").RequireAuthorization(BackOfficePolicy);

        group.MapGet("", GetUsers);
        group.MapGet("{userId:guid}", GetUserById);
        group.MapPost("", CreateUser);
        group.MapPut("{userId:guid}", UpdateUser);
        group.MapPut(ResendInvite, "{userId:guid}/resend-invite", "");
        group.MapDelete("{userId:guid}", DeleteUser);
    }

    private static async Task<IResult> GetUsers(
        ISender sender,
        IServiceProvider serviceProvider,
        string? searchTerm,
        bool? isActive,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
        => await ExecuteQuery<GetUsersQuery, GetUsersResponse>(sender,
            new GetUsersQuery(searchTerm, isActive, page, pageSize), serviceProvider, cancellationToken);

    private static async Task<IResult> GetUserById(
        ISender sender,
        IServiceProvider serviceProvider,
        Guid userId,
        CancellationToken cancellationToken = default)
        => await ExecuteQuery<GetUserByIdQuery, GetUserByIdResponse>(sender,
            new GetUserByIdQuery(userId), serviceProvider, cancellationToken);

    private static async Task<IResult> CreateUser(
        ISender sender,
        IServiceProvider serviceProvider,
        CreateUserCommand request,
        CancellationToken cancellationToken = default)
        => await ExecuteCommand<CreateUserCommand, CreateUserResponse>(sender,
            request, serviceProvider, cancellationToken);

    private static async Task<IResult> UpdateUser(
        ISender sender,
        IServiceProvider serviceProvider,
        Guid userId,
        UpdateUserCommand request,
        CancellationToken cancellationToken = default)
        => await ExecuteCommand<UpdateUserCommand, Unit>(sender,
            request with { UserId = userId }, serviceProvider, cancellationToken);

    private static async Task<IResult> ResendInvite(
        ISender sender,
        IServiceProvider serviceProvider,
        Guid userId,
        CancellationToken cancellationToken = default)
        => await ExecuteCommand<ResendUserInviteCommand, Unit>(sender,
            new ResendUserInviteCommand(userId), serviceProvider, cancellationToken);

    private static async Task<IResult> DeleteUser(
        ISender sender,
        IServiceProvider serviceProvider,
        Guid userId,
        CancellationToken cancellationToken = default)
        => await ExecuteCommand<DeleteUserCommand, Unit>(sender,
            new DeleteUserCommand(userId), serviceProvider, cancellationToken);
}