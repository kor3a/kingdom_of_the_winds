using KotW.ArtGen;
using KotW.Shared;

// Generates every sprite sheet, icon and sound the Godot client loads.
// Usage: dotnet run --project tools/ArtGen -- <client folder>
var client = Path.GetFullPath(args.Length > 0 ? args[0] : Path.Combine("..", "..", "client"));
var art = Path.Combine(client, "art");
var audio = Path.Combine(client, "audio");
var problems = Check.Run();
foreach (var problem in problems) Console.WriteLine($"Content problem: {problem}");
if (problems.Count > 0) return 1;
Console.WriteLine($"Writing assets to {client}");

var atlas = Tiles.Atlas();
atlas.Save(Path.Combine(art, "tiles.png"));

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
var skillAtlas = SkillIcons.Atlas();
skillAtlas.Save(Path.Combine(art, "skills.png"));

Sound.Effects(Path.Combine(audio, "sfx"));
Sound.Music(Path.Combine(audio, "music", "town.wav"), 293.66, 0.24, 0.5, true, 1, 11);
Sound.Music(Path.Combine(audio, "music", "fields.wav"), 329.63, 0.2, 0.55, true, 1, 22);
Sound.Music(Path.Combine(audio, "music", "forest.wav"), 246.94, 0.28, 0.38, true, 0.5, 33);
Sound.Music(Path.Combine(audio, "music", "cave.wav"), 196.0, 0.34, 0.22, false, 0.5, 44, 12);
Sound.Music(Path.Combine(audio, "music", "marsh.wav"), 220.0, 0.3, 0.35, false, 0.5, 55);
Sound.Music(Path.Combine(audio, "music", "harbor.wav"), 349.23, 0.22, 0.5, true, 1, 66);
Sound.Music(Path.Combine(audio, "music", "sea.wav"), 261.63, 0.26, 0.4, false, 1, 77);
Sound.Music(Path.Combine(audio, "music", "ember.wav"), 185.0, 0.2, 0.55, true, 0.5, 88);
Sound.Music(Path.Combine(audio, "music", "temple.wav"), 293.66, 0.36, 0.3, false, 1, 99);
Sound.Music(Path.Combine(audio, "music", "snow.wav"), 392.0, 0.32, 0.3, false, 1, 111);
Sound.Music(Path.Combine(audio, "music", "moon.wav"), 311.13, 0.3, 0.35, false, 1, 122);
Sound.Music(Path.Combine(audio, "music", "spirit.wav"), 174.61, 0.34, 0.28, false, 0.5, 133, 12);
Sound.Music(Path.Combine(audio, "music", "sky.wav"), 440.0, 0.24, 0.45, false, 1, 144);
Sound.Music(Path.Combine(audio, "music", "storm.wav"), 164.81, 0.18, 0.6, true, 0.5, 155);

// A preview sheet of every outfit and monster, handy for judging the art at a glance.
var preview = new Canvas(16 * 34, 3 * 50 + 70);
var looks = Content.Npcs.Values.Select(n => n.Look).Concat(Content.Folk.Select(f => f.Look)).Take(16).ToList();
for (int i = 0; i < looks.Count; i++)
    for (int r = 0; r < 3; r++)
        preview.Blit(People.Doll(looks[i], new[] { Dir.Down, Dir.Right, Dir.Up }[r]), i * 34, r * 50);
int mx = 0;
foreach (var id in Monsters.All.Keys)
{
    var (w, h, draw) = Monsters.All[id];
    var m = draw(0);
    if (mx + w > preview.W) break;
    preview.Blit(m, mx, 150 + 64 - h - 2);
    mx += w + 2;
}

// Every monster, packed into rows, standing on a shared floor line.
var bestiary = new Canvas(640, 760);
int bx = 0, by = 0, rowH = 0;
foreach (var (w, h, draw) in Monsters.All.Values)
{
    if (bx + w > bestiary.W) { bx = 0; by += rowH + 4; rowH = 0; }
    rowH = Math.Max(rowH, h);
    bestiary.Blit(draw(0), bx, by);
    bx += w + 4;
}
Upscale(bestiary, 2, Pal.Hex("#5f9a44"), Path.Combine(client, "..", "tools", "ArtGen", "preview_monsters.png"));
Upscale(preview, 3, Pal.Hex("#5f9a44"), Path.Combine(client, "..", "tools", "ArtGen", "preview.png"));

// A close-up of every weapon through the whole animation (idle, walk x3, wind-up, strike) in three facings.
var weapons = Content.Items.Values.Where(it => it.Type == ItemType.Weapon).ToList();
var gear = new Canvas(weapons.Count * 3 * 33, 6 * 49);
for (int i = 0; i < weapons.Count; i++)
{
    var w = weapons[i];
    var l = new Look("#f2c9a0", "#2a1a1a", "topknot", w.Look is "staff" ? "silk_robe" : w.Look is "ringstaff" ? "npc_monk" : "leather_armor",
        w.Id, null, w.Look is "sword" ? "wooden_shield" : null);
    for (int r = 0; r < 3; r++)
        for (int f = 0; f < 6; f++)
            gear.Blit(People.Doll(l, new[] { Dir.Down, Dir.Right, Dir.Up }[r], f), (i * 3 + r) * 33, f * 49);
}
Upscale(gear, 4, Pal.Hex("#5f9a44"), Path.Combine(client, "..", "tools", "ArtGen", "preview_gear.png"));
Upscale(skillAtlas, 3, Pal.Hex("#1a1410"), Path.Combine(client, "..", "tools", "ArtGen", "preview_skills.png"));

// Every zone at a quarter of a tile per pixel-block, with NPCs as gold dots and fixed bosses as red ones.
const int mapPx = 6;
var zones = Maps.BuildAll().Values.ToList();
var sheet = new Canvas(4 * 50 * mapPx, 4 * 42 * mapPx);
for (int i = 0; i < zones.Count; i++)
{
    var z = zones[i];
    int ox = i % 4 * 50 * mapPx, oy = i / 4 * 42 * mapPx;
    Rgba Sample(char ch, int px, int py)
    {
        int row = Content.TileOrder.IndexOf(ch);
        return atlas.Get(px * 32 / mapPx, row * 32 + py * 32 / mapPx);
    }
    for (int y = 0; y < z.H; y++)
        for (int x = 0; x < z.W; x++)
            for (int py = 0; py < mapPx; py++)
                for (int px = 0; px < mapPx; px++)
                {
                    var col = Sample(z.Tile(x, y), px, py);
                    if (col.A < 200) col = Sample(z.Ground, px, py);
                    sheet.P[(oy + y * mapPx + py) * sheet.W + ox + x * mapPx + px] = col with { A = 255 };
                }
    foreach (var n in Content.Npcs.Values.Where(n => n.Map == z.Id)) sheet.Rect(ox + n.X * mapPx + 1, oy + n.Y * mapPx + 1, mapPx - 2, mapPx - 2, Pal.Hex("#ffd040"));
    foreach (var s in z.Spawns.Where(s => s.Fixed)) sheet.Rect(ox + s.X0 * mapPx, oy + s.Y0 * mapPx, mapPx, mapPx, Pal.Hex("#ff2020"));
    foreach (var pt in z.Portals.Values) sheet.Rect(ox + pt.X * mapPx + 1, oy + pt.Y * mapPx + 1, mapPx - 2, mapPx - 2, Pal.Hex("#40e0ff"));
}
sheet.Save(Path.Combine(client, "..", "tools", "ArtGen", "preview_maps.png"));

// The app icon (iPhone needs one; desktops show it on the window): a warrior on a path at dusk.
var icon = new Canvas(1024, 1024);
const int iconScale = 18, horizon = 820;
for (int y = 0; y < icon.H; y++)
    for (int x = 0; x < icon.W; x++)
        icon.P[y * icon.W + x] = y < horizon ? Mix(Pal.Hex("#3e5288"), Pal.Hex("#141726"), y / (double)horizon)
            : (y / 36 + x / 72) % 2 == 0 ? Pal.Hex("#5f9a44") : Pal.Hex("#4f8a3a");
var hero = People.Doll(new Look("#f2c9a0", "#1a1a1a", "topknot", "lamellar", "iron_sword"), Dir.Down);
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
return 0;

static void Upscale(Canvas src, int scale, Rgba bg, string path)
{
    var big = new Canvas(src.W * scale, src.H * scale);
    for (int y = 0; y < big.H; y++) for (int x = 0; x < big.W; x++)
    {
        var px = src.Get(x / scale, y / scale);
        big.P[y * big.W + x] = Mix(bg, px with { A = 255 }, px.A / 255.0);
    }
    big.Save(path);
}

static Rgba Mix(Rgba a, Rgba b, double t) =>
    new((byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t));

