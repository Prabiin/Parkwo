namespace ParkingApp.Application.Common;

/// <summary>
/// Void success value for commands that create no single entity to return
/// (e.g. batch spot creation — the client re-fetches the parent detail).
/// </summary>
public readonly record struct Unit
{
    public static readonly Unit Value = new();
}
