using KotW.ArtGen;
using KotW.Shared;

// Generates every sprite sheet, icon and sound the Godot client loads.
// Usage: dotnet run --project tools/ArtGen -- <client folder>
var client = Path.GetFullPath(args.Length > 0 ? args[0] : Path.Combine("..", "..", "client"));
var art = Path.Combine(client, "art");
var audio = Path.Combine(client, "audio");
Console.WriteLine($"Writing assets to {client}");

Tiles.Atlas().Save(Path.Combine(art, "tiles.png"));

People.Sheet(People.Skin).Save(Path.Combine(art, "people", "skin.png"));
People.Sheet(People.Body).Save(Path.Combine(art, "people", "body.png"));
People.Sheet(People.Ghost).Save(Path.Combine(art, "people", "ghost.png"));
foreach (var style in Content.HairStyles)
    People.Sheet((d, f) => People.Hair(style, d, f)).Save(Path.Combine(art, "people", $"hair_{style}.png"));
foreach (var it in Content.Items.Values)
{
    Func<Dir, int, Canvas>? layer = it.Type switch
    {
        ItemType.Armor => (d, f) => People.Armor(it, d, f),
        ItemType.Helmet => (d, f) => People.Helmet(it, d, f),
        ItemType.Weapon => (d, f) => People.Weapon(it, d, f),
        ItemType.Shield => (d, f) => People.Shield(it, d, f),
        _ => null,
    };
    layer?.Invoke(Dir.Down, 0); // fail fast on a bad item before writing anything
    if (layer != null) People.Sheet(layer).Save(Path.Combine(art, "gear", $"{it.Id}.png"));
}

foreach (var id in Monsters.All.Keys) Monsters.Strip(id).Save(Path.Combine(art, "monsters", $"{id}.png"));

Icons.Atlas(out var order).Save(Path.Combine(art, "icons.png"));
File.WriteAllLines(Path.Combine(art, "icons.txt"), order);

Sound.Effects(Path.Combine(audio, "sfx"));
Sound.Music(Path.Combine(audio, "music", "town.wav"), 293.66, 0.24, 0.5, true, 1, 11);
Sound.Music(Path.Combine(audio, "music", "fields.wav"), 329.63, 0.2, 0.55, true, 1, 22);
Sound.Music(Path.Combine(audio, "music", "forest.wav"), 246.94, 0.28, 0.38, true, 0.5, 33);
Sound.Music(Path.Combine(audio, "music", "cave.wav"), 196.0, 0.34, 0.22, false, 0.5, 44, 12);

// A preview sheet of every outfit and monster, handy for judging the art at a glance.
var preview = new Canvas(16 * 34, 3 * 50 + 70);
var looks = Content.Npcs.Values.Select(n => n.Look).Concat(Content.Folk.Select(f => f.Look)).Take(16).ToList();
for (int i = 0; i < looks.Count; i++)
    for (int r = 0; r < 3; r++)
        preview.Blit(Doll(looks[i], new[] { Dir.Down, Dir.Right, Dir.Up }[r]), i * 34, r * 50);
int mx = 0;
foreach (var id in Monsters.All.Keys)
{
    var (w, h, draw) = Monsters.All[id];
    var m = draw(0);
    if (mx + w > preview.W) break;
    preview.Blit(m, mx, 150 + 64 - h - 2);
    mx += w + 2;
}
var big = new Canvas(preview.W * 3, preview.H * 3);
for (int y = 0; y < big.H; y++) for (int x = 0; x < big.W; x++) big.P[y * big.W + x] = preview.Get(x / 3, y / 3).A == 0 ? Pal.Hex("#5f9a44") : preview.Get(x / 3, y / 3);
big.Save(Path.Combine(client, "..", "tools", "ArtGen", "preview.png"));

// The app icon (iPhone needs one; desktops show it on the window): a warrior on a path at dusk.
var icon = new Canvas(1024, 1024);
const int iconScale = 18, horizon = 820;
for (int y = 0; y < icon.H; y++)
    for (int x = 0; x < icon.W; x++)
        icon.P[y * icon.W + x] = y < horizon ? Mix(Pal.Hex("#3e5288"), Pal.Hex("#141726"), y / (double)horizon)
            : (y / 36 + x / 72) % 2 == 0 ? Pal.Hex("#5f9a44") : Pal.Hex("#4f8a3a");
var hero = Doll(new Look("#f2c9a0", "#1a1a1a", "topknot", "lamellar", "iron_sword"), Dir.Down);
int hx = (icon.W - hero.W * iconScale) / 2, hy = horizon + 90 - hero.H * iconScale;
for (int y = 0; y < hero.H; y++)
    for (int x = 0; x < hero.W; x++)
    {
        var px = hero.Get(x, y);
        if (px.A == 0) continue;
        for (int dy = 0; dy < iconScale; dy++)
            for (int dx = 0; dx < iconScale; dx++)
                icon.P[(hy + y * iconScale + dy) * icon.W + hx + x * iconScale + dx] = px with { A = 255 };
    }
icon.Save(Path.Combine(client, "icon.png"));
Console.WriteLine("Done.");

// One standing frame of a paper-doll character, layered the way client/scripts/Actor.cs draws it.
static Canvas Doll(Look l, Dir d)
{
    var frame = new Canvas(32, 48);
    void Layer(Canvas c, string tint) { if (tint != "") c.Map(px => Tint(px, Pal.Hex(tint))); frame.Blit(c, 0, 0); }
    var weapon = l.Weapon != null ? People.Weapon(Content.Items[l.Weapon], d, 0) : null;
    if (d == Dir.Up && weapon != null) frame.Blit(weapon, 0, 0);
    Layer(People.Skin(d, 0), l.Skin);
    frame.Blit(People.Body(d, 0), 0, 0);
    if (l.Armor != null) frame.Blit(People.Armor(Content.Items[l.Armor], d, 0), 0, 0);
    Layer(People.Hair(l.Style, d, 0), l.Hair);
    if (l.Helmet != null) frame.Blit(People.Helmet(Content.Items[l.Helmet], d, 0), 0, 0);
    if (d != Dir.Up && weapon != null) frame.Blit(weapon, 0, 0);
    return frame;
}

static Rgba Mix(Rgba a, Rgba b, double t) =>
    new((byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t));

// Same maths as client/shaders/tint.gdshader, so the preview matches the game.
static Rgba Tint(Rgba px, Rgba baseCol)
{
    double v = px.R / 255.0;
    double[] b = { baseCol.R / 255.0, baseCol.G / 255.0, baseCol.B / 255.0 };
    double[] dark = { b[0] * 0.36, b[1] * 0.32, b[2] * 0.42 };
    double[] light = { b[0] + (1 - b[0]) * 0.45, b[1] + (1 - b[1]) * 0.42, b[2] + (1 - b[2]) * 0.32 };
    double Ch(int k) => v < 0.5 ? dark[k] + (b[k] - dark[k]) * (v * 2) : b[k] + (light[k] - b[k]) * ((v - 0.5) * 2);
    return new Rgba((byte)(Ch(0) * 255), (byte)(Ch(1) * 255), (byte)(Ch(2) * 255), px.A);
}
