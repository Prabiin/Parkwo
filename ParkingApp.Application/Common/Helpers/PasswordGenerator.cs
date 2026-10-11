using System.Security.Cryptography;

namespace ParkingApp.Application.Common.Helpers;

/// <summary>
/// Generates the temporary password handed to a new staff member. Deliberately
/// random and never derived from the person's details: it is emailed in plain
/// text once, and the account is forced to replace it on first login, so the
/// value only has to survive the trip to the inbox, not be memorable.
/// </summary>
public static class PasswordGenerator
{
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";   // no I/O
    private const string Lower = "abcdefghijkmnopqrstuvwxyz";  // no l
    private const string Digits = "23456789";                  // no 0/1
    private const string Symbols = "!@#$%*?";

    private const int Length = 12;

    public static string Generate()
    {
        // Guarantee one of each class, then fill and shuffle, so every generated
        // password satisfies a typical complexity policy without a retry loop.
        var chars = new List<char>(Length)
        {
            Upper[RandomNumberGenerator.GetInt32(Upper.Length)],
            Lower[RandomNumberGenerator.GetInt32(Lower.Length)],
            Digits[RandomNumberGenerator.GetInt32(Digits.Length)],
            Symbols[RandomNumberGenerator.GetInt32(Symbols.Length)]
        };

        const string all = Upper + Lower + Digits + Symbols;
        while (chars.Count < Length)
            chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);

        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars.ToArray());
    }
}
