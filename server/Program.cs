using System.Text.Json;
using System.Threading.RateLimiting;
using Dapper;
using KotW.Server;
using KotW.Shared;
using Npgsql;

// Account server: sign-up, log-in and one cloud save per account, so a character follows its
// player between computers and phones. The game itself still runs on the client for now.
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = 256 * 1024);
var db = NpgsqlDataSource.Create(builder.Configuration.GetConnectionString("Db")
    ?? throw new InvalidOperationException("Set ConnectionStrings:Db (or the ConnectionStrings__Db environment variable)."));
// Guessing passwords costs ten tries a minute per address.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});
var app = builder.Build();
app.UseRateLimiter();
await Schema.Create(db);

var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
static IResult Fail(int status, string code, string message) => Results.Json(new ApiError { Code = code, Message = message }, statusCode: status);
IResult LoggedOut() => Fail(401, "unauthorized", "Your login has expired. Please log in again.");

app.MapGet("/health", () => "ok");

app.MapPost("/auth/register", async (AuthRequest req) =>
{
    var email = Accounts.NormalizeEmail(req.Email);
    if ((Accounts.EmailProblem(email) ?? Accounts.PasswordProblem(req.Password)) is { } problem) return Fail(400, "invalid", problem);
    await using var conn = await db.OpenConnectionAsync();
    var id = await conn.QuerySingleOrDefaultAsync<long?>(
        "INSERT INTO accounts (email, password_hash) VALUES (@email, @hash) ON CONFLICT (email) DO NOTHING RETURNING id",
        new { email, hash = Passwords.Hash(req.Password) });
    if (id == null) return Fail(409, "email_taken", "There's already an account with that email. Log in instead.");
    return Results.Ok(new AuthReply { Token = await Sessions.Start(conn, id.Value), Email = email });
}).RequireRateLimiting("auth");

app.MapPost("/auth/login", async (AuthRequest req) =>
{
    var email = Accounts.NormalizeEmail(req.Email);
    await using var conn = await db.OpenConnectionAsync();
    var account = await conn.QuerySingleOrDefaultAsync<AccountRow>(
        "SELECT id AS Id, password_hash AS PasswordHash FROM accounts WHERE email = @email", new { email });
    bool ok = Passwords.Verify(req.Password.Length <= Accounts.MaxPassword ? req.Password : "", account?.PasswordHash ?? Passwords.Dummy);
    if (account == null || !ok) return Fail(401, "bad_login", "That email and password don't match an account.");
    return Results.Ok(new AuthReply { Token = await Sessions.Start(conn, account.Id), Email = email });
}).RequireRateLimiting("auth");

app.MapPost("/auth/logout", async (HttpRequest http) =>
{
    await using var conn = await db.OpenConnectionAsync();
    await Sessions.End(conn, http);
    return Results.NoContent();
});

app.MapGet("/save", async (HttpRequest http) =>
{
    await using var conn = await db.OpenConnectionAsync();
    if (await Sessions.Account(conn, http) is not { } accountId) return LoggedOut();
    var row = await conn.QuerySingleOrDefaultAsync<SaveRow>(
        "SELECT data::text AS Data, revision AS Revision FROM characters WHERE account_id = @accountId", new { accountId });
    return Results.Ok(row == null ? new CloudSave() : new CloudSave { Revision = row.Revision, Save = JsonSerializer.Deserialize<SaveData>(row.Data, json) });
});

// Revision 0 creates the account's character; any other must match the stored one, so a device
// that loaded an older copy can't overwrite progress made on another.
app.MapPut("/save", async (HttpRequest http, CloudSave body) =>
{
    if (body.Save is not { } save) return Fail(400, "invalid", "No character was sent.");
    if (Accounts.SaveProblem(save) is { } problem) return Fail(400, "invalid", problem);
    await using var conn = await db.OpenConnectionAsync();
    if (await Sessions.Account(conn, http) is not { } accountId) return LoggedOut();
    var args = new { accountId, name = save.Name, level = save.Level, data = JsonSerializer.Serialize(save, json), revision = body.Revision };
    try
    {
        int? revision = body.Revision == 0
            ? await conn.QuerySingleOrDefaultAsync<int?>("""
                INSERT INTO characters (account_id, name, level, data, revision) VALUES (@accountId, @name, @level, CAST(@data AS jsonb), 1)
                ON CONFLICT (account_id) DO NOTHING RETURNING revision
                """, args)
            : await conn.QuerySingleOrDefaultAsync<int?>("""
                UPDATE characters SET name = @name, level = @level, data = CAST(@data AS jsonb), revision = revision + 1, updated_at = now()
                WHERE account_id = @accountId AND revision = @revision RETURNING revision
                """, args);
        if (revision == null) return Fail(409, "stale", "This character was saved from another device after it was loaded here.");
        return Results.Ok(new CloudSave { Revision = revision.Value });
    }
    catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation && e.ConstraintName == "characters_name")
    {
        return Fail(409, "name_taken", $"Someone else already has a character named {save.Name}.");
    }
});

app.Run();

internal sealed record AccountRow(long Id, string PasswordHash);
internal sealed record SaveRow(string Data, int Revision);
