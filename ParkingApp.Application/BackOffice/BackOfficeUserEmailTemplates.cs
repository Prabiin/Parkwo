namespace ParkingApp.Application.BackOffice;

/// <summary>
/// The plain-text BackOffice user emails. Centralised so the wording that
/// carries a one-time password reads the same whether the account was created
/// or re-invited, and so the "you must change it" instruction is never
/// accidentally dropped.
/// </summary>
public static class BackOfficeUserEmailTemplates
{
    public static string Welcome(string fullName, string userName, string email, string password)
        => $"""
            Hello {fullName},

            A Parkwo admin account has been created for you.

            Username: {userName}
            Email: {email}
            Temporary password: {password}

            Sign in to the Parkwo admin console with the details above. For
            security, you must set your own password the first time you log in
            before you can use the console.

            If you were not expecting this, please contact your Parkwo
            administrator.

            — Parkwo
            """;

    public static string PasswordReset(string fullName, string userName, string email, string password)
        => $"""
            Hello {fullName},

            Your Parkwo admin password has been reset.

            Username: {userName}
            Email: {email}
            Temporary password: {password}

            Sign in with the temporary password above. You must set your own
            password before you can use the console again.

            If you did not request this, please contact your Parkwo administrator
            immediately.

            — Parkwo
            """;
}
