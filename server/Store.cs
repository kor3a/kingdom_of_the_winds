using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.WebUtilities;
using Npgsql;

namespace KotW.Server;

// Tables are created on start-up. The plan's split character tables (stats, inventory, skills)
// arrive with the game server; until then a character is stored whole as the client's save.
public static class Schema
{
    public static async Task Create(NpgsqlDataSource db)
    {
        await using var conn = await db.OpenConnectionAsync();
        await conn.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS accounts (
                id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                email text NOT NULL UNIQUE,
                password_hash text NOT NULL,
                created_at timestamptz NOT NULL DEFAULT now()
            );
            CREATE TABLE IF NOT EXISTS sessions (
                token_hash bytea PRIMARY KEY,
                account_id bigint NOT NULL REFERENCES accounts ON DELETE CASCADE,
                created_at timestamptz NOT NULL DEFAULT now(),
                expires_at timestamptz NOT NULL
            );
            CREATE INDEX IF NOT EXISTS sessions_account ON sessions (account_id);
            CREATE TABLE IF NOT EXISTS characters (
                account_id bigint PRIMARY KEY REFERENCES accounts ON DELETE CASCADE,
                name text NOT NULL,
                level int NOT NULL,
                data jsonb NOT NULL,
                revision int NOT NULL,
                updated_at timestamptz NOT NULL DEFAULT now()
            );
            CREATE UNIQUE INDEX IF NOT EXISTS characters_name ON characters (lower(name));
            """);
    }
}

// PBKDF2-SHA256 at OWASP's recommended work factor, stored as "pbkdf2-sha256$iterations$salt$hash".
public static class Passwords
{
    private const int Iterations = 600_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"pbkdf2-sha256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2-sha256") return false;
        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, int.Parse(parts[1]), HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    // Checked against when the email is unknown, so a wrong email takes as long as a wrong password.
    public static readonly string Dummy = Hash(Guid.NewGuid().ToString());
}

// Login tokens: random, handed to the client once, stored only as a SHA-256 hash.
// Each use pushes the expiry back, so a player who keeps playing stays logged in.
public static class Sessions
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(90);

    private static byte[] HashToken(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));

    public static async Task<string> Start(NpgsqlConnection conn, long accountId)
    {
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        await conn.ExecuteAsync("""
            DELETE FROM sessions WHERE account_id = @accountId AND expires_at < now();
            INSERT INTO sessions (token_hash, account_id, expires_at) VALUES (@hash, @accountId, now() + @life);
            """,
            new { hash = HashToken(token), accountId, life = Lifetime });
        return token;
    }

    // The account behind a request's "Authorization: Bearer <token>" header, or null.
    public static Task<long?> Account(NpgsqlConnection conn, HttpRequest req)
    {
        var header = req.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.Ordinal)) return Task.FromResult<long?>(null);
        return conn.QuerySingleOrDefaultAsync<long?>(
            "UPDATE sessions SET expires_at = now() + @life WHERE token_hash = @hash AND expires_at > now() RETURNING account_id",
            new { hash = HashToken(header[7..]), life = Lifetime });
    }

    public static Task End(NpgsqlConnection conn, HttpRequest req)
    {
        var header = req.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.Ordinal)) return Task.CompletedTask;
        return conn.ExecuteAsync("DELETE FROM sessions WHERE token_hash = @hash", new { hash = HashToken(header[7..]) });
    }
}
