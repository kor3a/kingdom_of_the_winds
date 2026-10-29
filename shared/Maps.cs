namespace KotW.Shared;

// The four zones of the first playable world, built from code with fixed seeds
// so every load (and the future server) produces the same layout.
// Town -> south gate -> rice fields -> east -> pine forest -> north -> mountain cave.

public sealed record Portal(int X, int Y, string To, int Tx, int Ty, Dir Facing);
public sealed record SpawnRule(string Monster, int Count, int X0, int Y0, int X1, int Y1, bool Fixed = false);
public sealed record Area(int X0, int Y0, int X1, int Y1);

public sealed class GameMap
{
    public required string Id, Name, Levels;
    public int W, H;
    public required char[] T;
    public Dictionary<int, Portal> Portals = new();
    public SpawnRule[] Spawns = Array.Empty<SpawnRule>();
    public Area? FolkArea;
    public double Dark;        // 0..1 darkness for caves
    public string Tint = "";   // optional colour wash

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;
    public char Tile(int x, int y) => InBounds(x, y) ? T[y * W + x] : '#';
    public bool Solid(int x, int y) => Content.Tiles.TryGetValue(Tile(x, y), out var t) && t.Solid;
    public bool Walkable(int x, int y) => InBounds(x, y) && !Solid(x, y);
    public Portal? PortalAt(int x, int y) => Portals.TryGetValue(y * W + x, out var p) ? p : null;
}

internal sealed class Grid
{
    public readonly int W, H;
    public char[] T;
    public Grid(int w, int h, char fill) { W = w; H = h; T = Enumerable.Repeat(fill, w * h).ToArray(); }
    public bool In(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;
    public char? Get(int x, int y) => In(x, y) ? T[y * W + x] : null;
    public void Set(int x, int y, char c) { if (In(x, y)) T[y * W + x] = c; }
    public void Rect(int x0, int y0, int x1, int y1, char c)
    {
        for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) Set(x, y, c);
    }
    public void Border(char c)
    {
        for (int x = 0; x < W; x++) { Set(x, 0, c); Set(x, H - 1, c); }
        for (int y = 0; y < H; y++) { Set(0, y, c); Set(W - 1, y, c); }
    }
    public void Each(Action<int, int, char> fn)
    {
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) fn(x, y, T[y * W + x]);
    }
    public bool IsSolid(int x, int y) { var c = Get(x, y); return c == null || Content.Tiles[c.Value].Solid; }

    // True when all 8 neighbours are open ground, so a lone obstacle here can never cut a path.
    public bool OpenAround(int x, int y, string floor)
    {
        for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
        {
            var c = Get(x + dx, y + dy);
            if (c == null || !floor.Contains(c.Value)) return false;
        }
        return true;
    }

    // Scatter decorations where they cannot block anything.
    public void Scatter(Mulberry32 rng, string floor, params (char Ch, double Chance)[] items)
    {
        Each((x, y, c) =>
        {
            if (c != floor[0]) return;
            foreach (var (ch, chance) in items)
                if (rng.Next() < chance && OpenAround(x, y, floor)) { Set(x, y, ch); return; }
        });
    }

    // Turn every walkable tile not reachable from (sx, sy) into fillWith.
    public void SealUnreachable(int sx, int sy, char fillWith)
    {
        var seen = new bool[W * H];
        var stack = new Stack<(int, int)>();
        stack.Push((sx, sy));
        seen[sy * W + sx] = true;
        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                int nx = x + dx, ny = y + dy;
                if (!In(nx, ny) || seen[ny * W + nx] || IsSolid(nx, ny)) continue;
                seen[ny * W + nx] = true;
                stack.Push((nx, ny));
            }
        }
        Each((x, y, _) => { if (!IsSolid(x, y) && !seen[y * W + x]) Set(x, y, fillWith); });
    }
}

public static class Maps
{
    public static Dictionary<string, GameMap> BuildAll() => new()
    {
        ["town"] = Town(), ["fields"] = Fields(), ["forest"] = Forest(), ["cave"] = Cave(),
    };

    private static GameMap Make(string id, string name, string levels, Grid g, Portal[] portals)
    {
        var m = new GameMap { Id = id, Name = name, Levels = levels, W = g.W, H = g.H, T = g.T };
        foreach (var p in portals) m.Portals[p.Y * g.W + p.X] = p;
        return m;
    }

    private static void Building(Grid g, int x0, int y0, int x1, int y1, int[] doors, char roof = 'R')
    {
        g.Rect(x0, y0, x1, y1 - 1, roof);
        g.Rect(x0, y1, x1, y1, 'H');
        foreach (var dx in doors) g.Set(dx, y1, 'D');
    }

    private static GameMap Town()
    {
        int W = 40, H = 30;
        var g = new Grid(W, H, '.');
        var rng = new Mulberry32(101);
        g.Border('#');
        g.Rect(19, 8, 20, 29, ',');
        g.Rect(2, 14, 37, 15, ',');
        g.Rect(2, 22, 37, 23, ',');
        g.Rect(13, 8, 26, 13, '_');
        Building(g, 3, 3, 10, 6, new[] { 6 });
        g.Rect(6, 7, 8, 8, ','); g.Rect(6, 9, 7, 13, ',');
        Building(g, 15, 2, 24, 5, new[] { 19, 20 }, 'Q');
        g.Rect(18, 6, 21, 7, 'S');
        g.Set(16, 6, 'L'); g.Set(23, 7, '.'); g.Set(23, 6, '.');
        Building(g, 29, 3, 36, 6, new[] { 32 });
        g.Rect(32, 7, 34, 8, ','); g.Rect(32, 9, 33, 13, ',');
        Building(g, 2, 17, 8, 21, new[] { 5 });
        Building(g, 10, 17, 17, 21, new[] { 13, 14 });
        Building(g, 30, 17, 36, 21, new[] { 33 });
        g.Set(16, 10, 'W');
        g.Rect(4, 25, 10, 27, '~');
        g.Rect(24, 17, 27, 19, '*');
        for (int x = 24; x <= 34; x += 2) g.Set(x, 26, 'F');
        g.Rect(16, 27, 18, 28, ','); // the gate guard's post
        g.Each((x, y, c) =>
        {
            if (c != '.') return;
            int edge = Math.Min(Math.Min(x, y), Math.Min(W - 1 - x, H - 1 - y));
            if (edge == 1 && rng.Next() < 0.45 && g.OpenAround(x, y, ".#T*")) g.Set(x, y, 'T');
        });
        g.Scatter(rng, ".*,", ('T', 0.05), ('*', 0.08));
        g.Set(19, 29, 'G'); g.Set(20, 29, 'G');
        g.SealUnreachable(6, 8, 'T');
        var m = Make("town", "Pungseong, the Capital", "Town", g, new[]
        {
            new Portal(19, 29, "fields", 21, 1, Dir.Down), new Portal(20, 29, "fields", 22, 1, Dir.Down),
        });
        m.FolkArea = new Area(13, 8, 26, 15);
        return m;
    }

    private static GameMap Fields()
    {
        int W = 44, H = 34;
        var g = new Grid(W, H, '.');
        var rng = new Mulberry32(202);
        g.Each((x, y, _) =>
        {
            int edge = Math.Min(Math.Min(x, y), Math.Min(W - 1 - x, H - 1 - y));
            if (edge == 0 || (edge == 1 && rng.Next() < 0.55)) g.Set(x, y, 'T');
        });
        for (int py = 3; py + 3 < H - 2; py += 6)
            for (int px = 3; px + 6 < W - 2; px += 8)
            {
                if (rng.Next() < 0.22) continue;
                g.Rect(px, py, px + 5, py + 2, 'p');
                if (rng.Next() < 0.35) for (int x = px; x <= px + 5; x++) g.Set(x, py + 3, 'F');
            }
        g.Rect(33, 24, 37, 26, '~');
        g.Rect(21, 0, 22, 17, ',');
        g.Rect(2, 16, 43, 17, ',');
        g.Rect(9, 18, 10, 31, ',');
        g.Scatter(rng, ".,*", ('B', 0.03), ('T', 0.02), ('*', 0.05));
        g.SealUnreachable(21, 1, 'T');
        var m = Make("fields", "Gate Paddies", "Lv 1-5", g, new[]
        {
            new Portal(21, 0, "town", 19, 28, Dir.Up), new Portal(22, 0, "town", 20, 28, Dir.Up),
            new Portal(43, 16, "forest", 1, 17, Dir.Right), new Portal(43, 17, "forest", 1, 18, Dir.Right),
        });
        m.Spawns = new[]
        {
            new SpawnRule("rabbit", 9, 2, 2, 24, 31), new SpawnRule("rat", 6, 2, 2, 41, 31),
            new SpawnRule("squirrel", 6, 2, 2, 41, 31), new SpawnRule("paddy_snake", 6, 27, 2, 41, 31),
        };
        return m;
    }

    private static GameMap Forest()
    {
        int W = 46, H = 36;
        var g = new Grid(W, H, 'g');
        var rng = new Mulberry32(303);
        g.Border('P');
        g.Each((x, y, c) =>
        {
            if (c != 'g') return;
            double r = rng.Next();
            if (r < 0.17) g.Set(x, y, 'P');
            else if (r < 0.2) g.Set(x, y, 'r');
            else if (r < 0.22) g.Set(x, y, 'B');
        });
        for (int i = 0; i < 9; i++)
        {
            int cx = 4 + (int)(rng.Next() * (W - 8)), cy = 4 + (int)(rng.Next() * (H - 8)), r = 2 + (int)(rng.Next() * 2);
            for (int y = cy - r; y <= cy + r; y++) for (int x = cx - r; x <= cx + r; x++)
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r + 1 && x > 0 && y > 0 && x < W - 1 && y < H - 1) g.Set(x, y, 'g');
        }
        int sx = 12;
        for (int y = 1; y < H - 1; y++)
        {
            g.Set(sx, y, '~'); g.Set(sx + 1, y, '~');
            double r = rng.Next();
            if (r < 0.25 && sx > 8) sx--; else if (r > 0.75 && sx < 15) sx++;
            g.Set(sx, y, '~'); g.Set(sx + 1, y, '~');
        }
        for (int x = 0; x <= 23; x++) foreach (var y in new[] { 17, 18 }) g.Set(x, y, g.Get(x, y) == '~' ? '=' : ',');
        g.Rect(22, 0, 23, 18, ',');
        g.Rect(25, 1, 31, 6, 'g');
        g.Set(27, 4, 'S'); g.Set(28, 4, 'S');
        g.Set(26, 2, 'L'); g.Set(30, 5, 'L');
        g.SealUnreachable(1, 17, 'P');
        var m = Make("forest", "Whispering Pinewood", "Lv 5-12", g, new[]
        {
            new Portal(0, 17, "fields", 42, 16, Dir.Left), new Portal(0, 18, "fields", 42, 17, Dir.Left),
            new Portal(22, 0, "cave", 19, 36, Dir.Up), new Portal(23, 0, "cave", 20, 36, Dir.Up),
        });
        m.Tint = "#08281810";
        m.Spawns = new[]
        {
            new SpawnRule("fox", 7, 2, 2, 24, 34), new SpawnRule("wolf", 7, 18, 2, 44, 20), new SpawnRule("boar", 6, 8, 19, 44, 34),
        };
        return m;
    }

    private static GameMap Cave()
    {
        int W = 40, H = 38;
        var rng = new Mulberry32(404);
        var g = new Grid(W, H, 'c');
        g.Each((x, y, _) =>
        {
            int edge = Math.Min(Math.Min(x, y), Math.Min(W - 1 - x, H - 1 - y));
            g.Set(x, y, edge == 0 || rng.Next() < 0.44 ? 'X' : 'c');
        });
        for (int it = 0; it < 4; it++)
        {
            var next = new Grid(W, H, 'c');
            g.Each((x, y, c) =>
            {
                int n = 0;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                    if ((dx != 0 || dy != 0) && (g.Get(x + dx, y + dy) ?? 'X') == 'X') n++;
                int edge = Math.Min(Math.Min(x, y), Math.Min(W - 1 - x, H - 1 - y));
                next.Set(x, y, edge == 0 || n >= 5 ? 'X' : n <= 3 ? 'c' : c);
            });
            g = next;
        }
        int bx = 20, by = 8;
        for (int y = 1; y < 16; y++) for (int x = 1; x < W - 1; x++)
            if ((x - bx) * (x - bx) + Math.Pow((y - by) * 1.1, 2) <= 38) g.Set(x, y, 'c');
        int tx = 19, ty = 36;
        while (ty > by + 4)
        {
            g.Rect(tx - 1, ty - 1, tx + 1, ty + 1, 'c');
            double r = rng.Next();
            if (r < 0.6) ty--;
            else if (r < 0.8) tx = Math.Max(5, tx - 1);
            else tx = Math.Min(W - 6, tx + 1);
            if (ty < 22) tx += Math.Sign(bx - tx) * (rng.Next() < 0.5 ? 1 : 0);
        }
        g.Rect(18, 35, 21, 36, 'c');
        g.Border('X');
        g.Set(19, 37, 'c'); g.Set(20, 37, 'c');
        g.SealUnreachable(19, 36, 'X');
        g.Set(19, 3, 'a'); g.Set(20, 3, 'a'); g.Set(21, 3, 'a');
        g.Scatter(rng, "c", ('m', 0.05));
        g.Each((x, y, c) => { if (c == 'X' && g.Get(x, y + 1) == 'c' && rng.Next() < 0.07) g.Set(x, y, 't'); });
        var m = Make("cave", "Hollow of the Mountain Lord", "Lv 12-20", g, new[]
        {
            new Portal(19, 37, "forest", 22, 1, Dir.Down), new Portal(20, 37, "forest", 23, 1, Dir.Down),
        });
        m.Dark = 0.9;
        m.Spawns = new[]
        {
            new SpawnRule("cave_bat", 7, 2, 17, 37, 34), new SpawnRule("dokkaebi", 6, 2, 16, 37, 32),
            new SpawnRule("stone_spirit", 4, 2, 16, 37, 30), new SpawnRule("mountain_lord", 1, bx, by, bx, by, true),
        };
        return m;
    }
}
