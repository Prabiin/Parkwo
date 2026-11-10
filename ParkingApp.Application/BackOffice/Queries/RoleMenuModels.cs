namespace ParkingApp.Application.BackOffice.Queries;

/// <summary>
/// Shared permission catalog shape for the role add and edit forms. Both
/// <c>roles/init</c> and <c>roles/{id}</c> return this so the client renders the
/// same menu/action tree with a checkbox per action.
/// </summary>
public record RoleActionOption(
    Guid Id,
    string ActionKey,
    string DisplayName,
    bool IsAssigned);

public record RoleMenuOption(
    string MenuKey,
    string Title,
    IReadOnlyList<RoleActionOption> Actions);
