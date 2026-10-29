namespace KotW.Shared;

// Requests and replies for the account server, compiled by both the client and the server
// so the two always agree on the shape. Sent as JSON with camelCase names.

public sealed class AuthRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public sealed class AuthReply
{
    public string Token { get; set; } = "";
    public string Email { get; set; } = "";
}

// GET /save returns the account's character and its revision (0 and no save when there is none yet).
// PUT /save sends the revision the client last saw; the server refuses it if another device saved since.
public sealed class CloudSave
{
    public int Revision { get; set; }
    public SaveData? Save { get; set; }
}

public sealed class ApiError
{
    public string Code { get; set; } = ""; // invalid | email_taken | bad_login | unauthorized | stale | name_taken
    public string Message { get; set; } = "";
}

// Checks shared by the login form (instant feedback) and the server (the real gate).
public static class Accounts
{
    public const int MinPassword = 8, MaxPassword = 128, MaxEmail = 254;

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public static string? EmailProblem(string email)
    {
        int at = email.IndexOf('@');
        if (at < 1 || at != email.LastIndexOf('@') || at == email.Length - 1 || email.Length > MaxEmail || email.Any(char.IsWhiteSpace))
            return "Please enter a valid email address.";
        return null;
    }

    public static string? PasswordProblem(string password) =>
        password.Length < MinPassword ? $"Passwords need at least {MinPassword} characters."
        : password.Length > MaxPassword ? $"Passwords can be at most {MaxPassword} characters."
        : null;

    // A save the client can load without crashing, with a name other players will see.
    public static string? SaveProblem(SaveData s)
    {
        if (s.Name.Length == 0 || s.Name.Length > Rules.MaxName || Rules.CleanName(s.Name) != s.Name)
            return $"Character names use letters, digits, spaces, - and _ (up to {Rules.MaxName}).";
        if (s.Level < 1 || s.Level > Rules.MaxLevel) return "That character's level is out of range.";
        if (s.Cls != null && !Content.Classes.ContainsKey(s.Cls)) return "That character's class doesn't exist.";
        if (s.Stats == null || s.Inv == null || s.Equip == null || s.Storage == null || s.Quests == null || s.Bar == null)
            return "That save is missing parts.";
        return null;
    }
}
