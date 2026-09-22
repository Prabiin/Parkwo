using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Common.Models;

/// <summary>
/// Common dropdown option. Code carries the enum numeric value as string
/// (e.g. "1"), Text carries its [Description] (e.g. "Two wheeler").
/// </summary>
public sealed record ListModel<T>(string Code, string Text) where T : struct, Enum
{
    public static IReadOnlyList<ListModel<T>> FromEnum()
        => Enum.GetValues<T>()
            .Select(e => new ListModel<T>(
                Convert.ToInt32((object)e).ToString(),
                ((Enum)(object)e).ToDescription()))
            .ToList();
}
