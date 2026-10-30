using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using KotW.Shared;

namespace KotW;

// Account server messages, generated at compile time like SaveJson (iOS has no reflection-based JSON).
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(AuthRequest))]
[JsonSerializable(typeof(AuthReply))]
[JsonSerializable(typeof(CloudSave))]
[JsonSerializable(typeof(ApiError))]
internal partial class ApiJson : JsonSerializerContext { }

// The player's account: logs in, fetches the character saved to it and uploads new saves.
// Godot's own HTTPRequest does the talking, since it brings its own TLS on every platform.
public partial class Cloud : Node
{
    private const string Unreachable = "Couldn't reach the server. Check your connection and try again.";
    public string? Email { get; private set; }
    private string? _token;
    private int _revision; // of the account's character, as last loaded or saved here

    // Logged in, and not choosing to play offline: saves go to the account instead of this device.
    public bool Active => _token != null && !Offline;
    public bool LoggedIn => _token != null;
    public bool Offline;

    private static string AccountPath => ProjectSettings.GlobalizePath("user://account.json");

    // The address comes from project.godot (kotw/server_url) or `-- --server=<url>` on the command line.
    // 127.0.0.1 rather than localhost: Godot tries IPv6 ::1 first, which Docker doesn't publish, and times out there.
    private static readonly string Server = (OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--server="))?[9..]
        ?? ProjectSettings.GetSetting("kotw/server_url", "http://127.0.0.1:5080").AsString()).TrimEnd('/');

    public override void _Ready()
    {
        try
        {
            if (File.Exists(AccountPath) && JsonSerializer.Deserialize(File.ReadAllText(AccountPath), ApiJson.Default.AuthReply) is { } a)
            {
                _token = a.Token;
                Email = a.Email;
            }
        }
        catch (Exception e) { GD.PushWarning($"Could not read account: {e.Message}"); }
    }

    // Logs in (or creates the account) and remembers it on this device. Returns an error to show, or null.
    public async Task<string?> LogIn(string email, string password, bool create)
    {
        var body = JsonSerializer.Serialize(new AuthRequest { Email = email, Password = password }, ApiJson.Default.AuthRequest);
        var (status, reply) = await Send(Godot.HttpClient.Method.Post, create ? "/auth/register" : "/auth/login", body, false);
        if (status != 200) return Problem(status, reply);
        var auth = JsonSerializer.Deserialize(reply, ApiJson.Default.AuthReply)!;
        _token = auth.Token;
        Email = auth.Email;
        _revision = 0;
        Offline = false;
        try { File.WriteAllText(AccountPath, JsonSerializer.Serialize(auth, ApiJson.Default.AuthReply)); }
        catch (Exception e) { GD.PushWarning($"Could not remember account: {e.Message}"); }
        return null;
    }

    public void LogOut()
    {
        if (_token != null) _ = Send(Godot.HttpClient.Method.Post, "/auth/logout", "", true);
        Forget();
    }

    private void Forget()
    {
        _token = null;
        Email = null;
        _revision = 0;
        try { File.Delete(AccountPath); } catch (Exception e) { GD.PushWarning($"Could not forget account: {e.Message}"); }
    }

    // The account's character (null if it has none yet), or an error to show.
    public async Task<(SaveData? Save, string? Error)> Fetch()
    {
        var (status, reply) = await Send(Godot.HttpClient.Method.Get, "/save", null, true);
        if (status != 200) return (null, Problem(status, reply));
        var cloud = JsonSerializer.Deserialize(reply, ApiJson.Default.CloudSave)!;
        _revision = cloud.Revision;
        return (cloud.Save, null);
    }

    // Saves the character to the account. Returns an error to show, or null.
    public async Task<string?> Upload(SaveData data)
    {
        var body = JsonSerializer.Serialize(new CloudSave { Revision = _revision, Save = data }, ApiJson.Default.CloudSave);
        var (status, reply) = await Send(Godot.HttpClient.Method.Put, "/save", body, true);
        if (status != 200) return Problem(status, reply);
        _revision = JsonSerializer.Deserialize(reply, ApiJson.Default.CloudSave)!.Revision;
        return null;
    }

    private string Problem(int status, string reply)
    {
        if (status == 0) return Unreachable;
        if (status == 429) return "Too many tries. Wait a minute, then try again.";
        ApiError? err = null;
        try { err = JsonSerializer.Deserialize(reply, ApiJson.Default.ApiError); } catch (JsonException) { }
        if (err?.Code == "unauthorized") Forget();
        return err?.Message is { Length: > 0 } m ? m : $"The server had a problem ({status}). Try again later.";
    }

    // Status 0 means the server couldn't be reached at all.
    private async Task<(int Status, string Body)> Send(Godot.HttpClient.Method method, string path, string? body, bool auth)
    {
        var req = new HttpRequest { Timeout = 10 };
        AddChild(req);
        var headers = new List<string> { "Content-Type: application/json" };
        if (auth && _token != null) headers.Add($"Authorization: Bearer {_token}");
        try
        {
            if (req.Request(Server + path, headers.ToArray(), method, body ?? "") != Error.Ok) return (0, "");
            var res = await ToSignal(req, HttpRequest.SignalName.RequestCompleted);
            if ((HttpRequest.Result)res[0].AsInt32() != HttpRequest.Result.Success) return (0, "");
            return (res[1].AsInt32(), Encoding.UTF8.GetString(res[3].AsByteArray()));
        }
        finally { req.QueueFree(); }
    }
}
