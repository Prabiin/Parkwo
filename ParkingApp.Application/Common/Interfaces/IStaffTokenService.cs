using ParkingApp.Domain;

namespace ParkingApp.Application.Common.Interfaces;

/// <summary>
/// Mints the two credentials a staff session needs: a short-lived access token
/// (what the staff app sends on every operational call) and an opaque refresh
/// token (the day-session credential). Kept parallel to the rider and BackOffice
/// token services so a staff token can never be confused with, or minted by, the
/// rider path.
/// </summary>
public interface IStaffTokenService
{
    string GenerateAccessToken(Staff staff);
    string GenerateRefreshToken();
}
