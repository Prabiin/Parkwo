namespace ParkingApp.Application.BackOffice.Queries.GetRoleInit;

public record GetRoleInitResponse(
    IReadOnlyList<RoleMenuOption> Menus);
