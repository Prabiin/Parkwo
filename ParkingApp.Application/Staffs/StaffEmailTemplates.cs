namespace ParkingApp.Application.Staffs;

/// <summary>
/// The plain-text staff emails. Centralised so the wording that carries a
/// one-time password reads the same whether the account was created or reset,
/// and so the "you must change it" instruction is never accidentally dropped.
/// </summary>
public static class StaffEmailTemplates
{
    public static string Welcome(string fullName, string email, string password)
        => $"""
            Hello {fullName},

            A Parkwo staff account has been created for you.

            Email (username): {email}
            Temporary password: {password}

            Sign in to the Parkwo Staff app with the details above. For security,
            you must set your own password the first time you log in before you
            can use the gate.

            If you were not expecting this, please contact your Parkwo
            administrator.

            — Parkwo
            """;

    public static string PasswordReset(string fullName, string email, string password)
        => $"""
            Hello {fullName},

            Your Parkwo staff password has been reset.

            Email (username): {email}
            Temporary password: {password}

            Sign in with the temporary password above. You must set your own
            password before you can use the gate again.

            If you did not request this, please contact your Parkwo administrator
            immediately.

            — Parkwo
            """;
}
