namespace ParkingApp.Application.BackOffice.Queries.GetRoleInit;

public record RoleActionOption(
    Guid Id,
    string ActionKey,
    string DisplayName);

public record RoleMenuOption(
    string MenuKey,
    string Title,
    IReadOnlyList<RoleActionOption> Actions);

public record GetRoleInitResponse(
    IReadOnlyList<RoleMenuOption> Menus);
