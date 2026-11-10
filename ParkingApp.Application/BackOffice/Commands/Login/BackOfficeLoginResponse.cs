using System.Collections.Generic;

namespace ParkingApp.Application.BackOffice.Commands.Login;

/// <summary>
/// Response for BackOffice login.
/// </summary>
public record BackOfficeLoginResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string FullName,
    string UserName,
    string Email,
    bool MustChangePassword,
    IReadOnlyList<BackOfficeMenuDto> Menus,
    IReadOnlyList<string> Permissions);

/// <summary>
/// DTO for BackOffice menu (hierarchical tree).
/// </summary>
public record BackOfficeMenuDto(
    string Key,
    string Title,
    string? Route,
    string? Icon,
    IReadOnlyList<BackOfficeMenuDto> Children,
    IReadOnlyList<string> Actions);