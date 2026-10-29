using Godot;
using KotW.Shared;

namespace KotW;

// Loads and caches the generated art. Paths match what tools/ArtGen writes.
public static class Assets
{
    public const int T = Content.Tile;
    private static readonly Dictionary<string, Texture2D?> Cache = new();
    private static Dictionary<string, int>? _iconIndex;
    private static Dictionary<char, int>? _tileRow;
    private static Shader? _tint;

    public static Texture2D? Tex(string path)
    {
        if (!Cache.TryGetValue(path, out var t))
        {
            t = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
            Cache[path] = t;
        }
        return t;
    }

    public static Texture2D TileAtlas => Tex("res://art/tiles.png")!;
    public static Texture2D? Gear(string? id) => id == null ? null : Tex($"res://art/gear/{id}.png");
    public static Texture2D Person(string layer) => Tex($"res://art/people/{layer}.png")!;
    public static Texture2D Monster(string sprite) => Tex($"res://art/monsters/{sprite}.png")!;

    public static int TileRow(char c)
    {
        _tileRow ??= Content.TileOrder.Select((ch, i) => (ch, i)).ToDictionary(p => p.ch, p => p.i);
        return _tileRow.TryGetValue(c, out var r) ? r : 0;
    }

    // Icons are packed in item order, skipping NPC-only outfits (same order ArtGen uses).
    public static AtlasTexture? Icon(string? itemId)
    {
        if (itemId == null) return null;
        _iconIndex ??= Content.Items.Values.Where(i => !i.Hidden).Select((it, i) => (it.Id, i)).ToDictionary(p => p.Id, p => p.i);
        if (!_iconIndex.TryGetValue(itemId, out var idx)) return null;
        return new AtlasTexture { Atlas = Tex("res://art/icons.png"), Region = new Rect2(idx % 8 * 32, idx / 8 * 32, 32, 32) };
    }

    public static ShaderMaterial Tint(string hex)
    {
        _tint ??= GD.Load<Shader>("res://shaders/tint.gdshader");
        var m = new ShaderMaterial { Shader = _tint };
        m.SetShaderParameter("base_color", Color.FromHtml(hex));
        return m;
    }

    public static Color C(string hex) => Color.FromHtml(hex);
}
