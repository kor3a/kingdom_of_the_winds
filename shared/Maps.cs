namespace KotW.Shared;

// Every zone, built from code with fixed seeds so every load (and the future server)
// produces the same layout. The road runs:
// Pungseong -> paddies -> pinewood (north: the Hollow) -> marsh -> Haeseong Harbor
// (south: coast -> drowned shrine; north: Cinder Highlands) -> Cloudgate Monastery
// (east: Frostfang Pass; west: bamboo grove -> Yellow Springs; north: Sky Palace -> Eye of the Winds).

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
    public string Tint = "";   // optional colour the whole zone is multiplied by
    public char Ground = '.';  // drawn under trees, rocks and other object tiles
    public string Music = "fields";
    public (int X, int Y)? Spawn; // towns: where teleport scrolls land

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
    public static Dictionary<string, GameMap> BuildAll()
    {
        var maps = new[]
        {
            Town(), Fields(), Forest(), Cave(), Marsh(), Harbor(), Coast(), Drowned(), Highlands(), Monastery(),
            Frostpeak(), Bamboo(), Underworld(), SkyPalace(), Eye(),
        }.ToDictionary(m => m.Id);
        // Nobody stands inside a tree: clear the tile under every NPC.
        foreach (var n in Content.Npcs.Values)
            if (maps.TryGetValue(n.Map, out var m) && m.Solid(n.X, n.Y)) m.T[n.Y * m.W + n.X] = m.Ground;
        return maps;
    }

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
        m.Music = "town";
        m.Spawn = (Rules.TownSpawnX, Rules.TownSpawnY);
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
        g.Rect(23, 17, W - 1, 18, ','); // the east road to the marsh
        g.SealUnreachable(1, 17, 'P');
        var m = Make("forest", "Whispering Pinewood", "Lv 5-12", g, new[]
        {
            new Portal(0, 17, "fields", 42, 16, Dir.Left), new Portal(0, 18, "fields", 42, 17, Dir.Left),
            new Portal(22, 0, "cave", 19, 36, Dir.Up), new Portal(23, 0, "cave", 20, 36, Dir.Up),
            new Portal(W - 1, 17, "marsh", 1, 17, Dir.Right), new Portal(W - 1, 18, "marsh", 1, 18, Dir.Right),
        });
        m.Tint = "#dbf2e0";
        m.Ground = 'g';
        m.Music = "forest";
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
        m.Ground = 'c';
        m.Music = "cave";
        m.Spawns = new[]
        {
            new SpawnRule("cave_bat", 7, 2, 17, 37, 34), new SpawnRule("dokkaebi", 6, 2, 16, 37, 32),
            new SpawnRule("stone_spirit", 4, 2, 16, 37, 30), new SpawnRule("mountain_lord", 1, bx, by, bx, by, true),
        };
        return m;
    }

    // ---- shared helpers for the outer zones ----------------------------------------

    private static int Edge(Grid g, int x, int y) => Math.Min(Math.Min(x, y), Math.Min(g.W - 1 - x, g.H - 1 - y));

    private static void Blob(Grid g, double cx, double cy, double r, char c)
    {
        for (int y = (int)(cy - r) - 1; y <= cy + r + 1; y++)
            for (int x = (int)(cx - r) - 1; x <= cx + r + 1; x++)
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r + 0.5 && Edge(g, x, y) > 0) g.Set(x, y, c);
    }

    // A wobbling path of the given width from one point to another.
    private static void Trail(Grid g, Mulberry32 rng, int x, int y, int tx, int ty, char c, int width = 2)
    {
        for (int guard = 0; guard < 2000 && (x != tx || y != ty); guard++)
        {
            g.Rect(x, y, x + width - 1, y + width - 1, c);
            bool horiz = Math.Abs(tx - x) > Math.Abs(ty - y) ? rng.Next() < 0.8 : rng.Next() < 0.2;
            if (horiz && x != tx) x += Math.Sign(tx - x);
            else if (y != ty) y += Math.Sign(ty - y);
            else x += Math.Sign(tx - x);
        }
        g.Rect(x, y, x + width - 1, y + width - 1, c);
    }

    private static (int X, int Y) RandIn(Mulberry32 rng, int x0, int y0, int x1, int y1) =>
        (x0 + (int)(rng.Next() * (x1 - x0 + 1)), y0 + (int)(rng.Next() * (y1 - y0 + 1)));

    // A resting place in the wild: cleared ground, a shrine circle for ghosts and two lanterns.
    private static void Camp(Grid g, int x0, int y0, int x1, int y1, int sx, int sy, char floor)
    {
        g.Rect(x0, y0, x1, y1, floor);
        g.Rect(sx, sy, sx + 1, sy, 'S');
        g.Set(x0 + 1, y0 + 1, 'L'); g.Set(x1 - 1, y0 + 1, 'L');
    }

    // Lone obstacles scattered where they can never seal a path.
    private static void Sprinkle(Grid g, Mulberry32 rng, char floor, params (char Ch, double Chance)[] items) =>
        g.Scatter(rng, floor.ToString(), items);

    private static GameMap Marsh()
    {
        int W = 46, H = 36;
        var g = new Grid(W, H, 'z');
        var rng = new Mulberry32(505);
        g.Each((x, y, _) => { int e = Edge(g, x, y); if (e == 0) g.Set(x, y, 'j'); else if (e == 1 && rng.Next() < 0.5) g.Set(x, y, 'v'); });
        for (int i = 0; i < 16; i++) { var (cx, cy) = RandIn(rng, 3, 3, W - 4, H - 4); Blob(g, cx, cy, 1.5 + rng.Next() * 2.5, '~'); }
        g.Each((x, y, c) =>
        {
            if (c != 'z') return;
            bool shore = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }.Any(d => g.Get(x + d.Item1, y + d.Item2) == '~');
            if (shore && rng.Next() < 0.3) g.Set(x, y, 'v');
        });
        Sprinkle(g, rng, 'z', ('j', 0.025), ('v', 0.04));
        for (int x = 0; x < W; x++) foreach (var y in new[] { 17, 18 }) g.Set(x, y, g.Get(x, y) == '~' ? '=' : ',');
        Trail(g, rng, 23, 17, 23, 7, ',');
        Camp(g, 18, 2, 28, 8, 23, 4, 'z');
        g.Rect(31, 24, 41, 33, 'z');
        for (int x = 31; x <= 41; x++) { if (x < 35 || x > 37) g.Set(x, 24, 'F'); }
        for (int y = 25; y <= 33; y++) { g.Set(31, y, 'F'); g.Set(41, y, 'F'); }
        Trail(g, rng, 36, 19, 36, 24, ',');
        g.SealUnreachable(1, 17, 'v');
        var m = Make("marsh", "Weeping Reed Marsh", "Lv 18-28", g, new[]
        {
            new Portal(0, 17, "forest", 44, 17, Dir.Left), new Portal(0, 18, "forest", 44, 18, Dir.Left),
            new Portal(W - 1, 17, "harbor", 1, 15, Dir.Right), new Portal(W - 1, 18, "harbor", 1, 16, Dir.Right),
        });
        m.Ground = 'z';
        m.Music = "marsh";
        m.Tint = "#dde4c4";
        m.Spawns = new[]
        {
            new SpawnRule("marsh_toad", 8, 2, 2, 43, 33), new SpawnRule("reed_viper", 7, 2, 19, 30, 33),
            new SpawnRule("marsh_bandit", 7, 24, 19, 43, 33), new SpawnRule("marsh_wisp", 6, 2, 2, 43, 15),
            new SpawnRule("bandit_chief", 1, 36, 29, 36, 29, true),
        };
        return m;
    }

    private static GameMap Harbor()
    {
        int W = 42, H = 32;
        var g = new Grid(W, H, '.');
        var rng = new Mulberry32(606);
        g.Border('#');
        g.Rect(31, 1, 33, H - 2, 's');
        g.Rect(34, 0, W - 1, H - 1, '~');
        g.Rect(30, 6, 40, 7, 'd'); g.Rect(30, 22, 40, 23, 'd');
        g.Rect(1, 15, 33, 16, ',');
        g.Rect(19, 1, 20, 30, ',');
        g.Rect(21, 18, 29, 24, '_');
        Building(g, 3, 3, 9, 6, new[] { 6 }); g.Rect(6, 7, 6, 14, ',');
        Building(g, 11, 3, 16, 6, new[] { 13 }); g.Rect(13, 7, 13, 14, ',');
        Building(g, 22, 2, 29, 5, new[] { 25, 26 }, 'Q');
        g.Rect(25, 6, 26, 7, 'S'); g.Set(23, 7, 'L');
        Building(g, 3, 19, 9, 22, new[] { 6 }); g.Rect(6, 23, 6, 24, ',');
        Building(g, 11, 19, 16, 22, new[] { 13 }); g.Rect(13, 23, 13, 24, ',');
        Building(g, 3, 26, 9, 29, new[] { 6 });
        Building(g, 22, 26, 28, 29, new[] { 25 });
        g.Set(16, 11, 'W');
        g.Each((x, y, c) => { if (c == '.' && Edge(g, x, y) == 1 && rng.Next() < 0.4 && g.OpenAround(x, y, ".#T*")) g.Set(x, y, 'T'); });
        Sprinkle(g, rng, '.', ('T', 0.03), ('*', 0.06));
        foreach (var (gx, gy) in new[] { (19, H - 1), (20, H - 1), (19, 0), (20, 0), (0, 15), (0, 16) }) g.Set(gx, gy, 'G');
        g.SealUnreachable(19, 12, 'T');
        var m = Make("harbor", "Haeseong, the Salt Harbor", "Town", g, new[]
        {
            new Portal(0, 15, "marsh", 44, 17, Dir.Left), new Portal(0, 16, "marsh", 44, 18, Dir.Left),
            new Portal(19, H - 1, "coast", 23, 1, Dir.Down), new Portal(20, H - 1, "coast", 24, 1, Dir.Down),
            new Portal(19, 0, "highlands", 23, 36, Dir.Up), new Portal(20, 0, "highlands", 24, 36, Dir.Up),
        });
        m.FolkArea = new Area(21, 17, 29, 24);
        m.Music = "harbor";
        m.Spawn = (19, 12);
        return m;
    }

    private static GameMap Coast()
    {
        int W = 48, H = 34;
        var g = new Grid(W, H, 's');
        var rng = new Mulberry32(707);
        g.Each((x, y, _) =>
        {
            int shore = 35 + (int)Math.Round(3 * Math.Sin(y / 4.0));
            if (x >= shore || y >= H - 3) g.Set(x, y, '~');
            else if (x == 0 || y == 0 || (x == 1 && rng.Next() < 0.6)) g.Set(x, y, 'T');
        });
        g.Each((x, y, c) =>
        {
            if (c != 's') return;
            bool wet = new[] { (1, 0), (0, 1), (1, 1) }.Any(d => g.Get(x + d.Item1, y + d.Item2) == '~');
            if (wet && rng.Next() < 0.12 && g.OpenAround(x, y, "s~")) g.Set(x, y, 'o');
        });
        Sprinkle(g, rng, 's', ('r', 0.03), ('T', 0.02), ('o', 0.01));
        g.Rect(3, 25, 16, 26, 'd'); // the pirates' boardwalk
        g.Rect(36, H - 5, W - 3, H - 1, 'X');
        g.Rect(39, H - 5, 42, H - 1, 'c');
        Trail(g, rng, 30, 27, 39, H - 5, 's');
        g.Rect(23, 0, 24, 1, 's');
        g.SealUnreachable(23, 1, 'T');
        var m = Make("coast", "Saltwind Coast", "Lv 28-36", g, new[]
        {
            new Portal(23, 0, "harbor", 19, 30, Dir.Up), new Portal(24, 0, "harbor", 20, 30, Dir.Up),
            new Portal(40, H - 1, "drowned", 19, 38, Dir.Up), new Portal(41, H - 1, "drowned", 20, 38, Dir.Up),
        });
        m.Ground = 's';
        m.Music = "sea";
        m.Spawns = new[]
        {
            new SpawnRule("shore_crab", 8, 2, 2, 38, 29), new SpawnRule("sea_snake", 7, 18, 2, 38, 29),
            new SpawnRule("pirate", 7, 2, 18, 28, 30), new SpawnRule("drowned_sailor", 8, 2, 2, 38, 29),
        };
        return m;
    }

    // Cellular-automaton caverns with a domed chamber at the top and a trail up from the entrance.
    private static Grid Caverns(int W, int H, uint seed, char floor, char wall, int cx, int cy)
    {
        var rng = new Mulberry32(seed);
        var g = new Grid(W, H, floor);
        g.Each((x, y, _) => g.Set(x, y, Edge(g, x, y) == 0 || rng.Next() < 0.44 ? wall : floor));
        for (int it = 0; it < 4; it++)
        {
            var next = new Grid(W, H, floor);
            g.Each((x, y, c) =>
            {
                int n = 0;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                    if ((dx != 0 || dy != 0) && (g.Get(x + dx, y + dy) ?? wall) == wall) n++;
                next.Set(x, y, Edge(g, x, y) == 0 || n >= 5 ? wall : n <= 3 ? floor : c);
            });
            g = next;
        }
        Blob(g, cx, cy, 6, floor);
        Trail(g, rng, W / 2 - 1, H - 2, cx, cy, floor, 3);
        g.Border(wall);
        g.Rect(W / 2 - 1, H - 2, W / 2, H - 1, floor);
        g.SealUnreachable(W / 2 - 1, H - 2, wall);
        return g;
    }

    private static GameMap Drowned()
    {
        int W = 40, H = 40;
        var g = Caverns(W, H, 808, 'M', 'X', 20, 8);
        var rng = new Mulberry32(809);
        for (int i = 0; i < 6; i++) { var (cx, cy) = RandIn(rng, 4, 16, W - 5, H - 8); Blob(g, cx, cy, 1.2 + rng.Next(), '~'); }
        g.Scatter(rng, "M", ('o', 0.05), ('I', 0.03));
        g.Set(18, 4, 'a'); g.Set(19, 4, 'a'); g.Set(20, 4, 'a');
        g.SealUnreachable(W / 2 - 1, H - 2, 'X');
        var m = Make("drowned", "Drowned Shrine of the Dragon King", "Lv 36-45", g, new[]
        {
            new Portal(W / 2 - 1, H - 1, "coast", 40, 32, Dir.Down), new Portal(W / 2, H - 1, "coast", 41, 32, Dir.Down),
        });
        m.Ground = 'M';
        m.Music = "cave";
        m.Dark = 0.75;
        m.Tint = "#b0e0f0";
        m.Spawns = new[]
        {
            new SpawnRule("coral_golem", 5, 2, 16, 37, 34), new SpawnRule("abyss_eel", 7, 2, 14, 37, 34),
            new SpawnRule("water_ghost", 7, 2, 14, 37, 34), new SpawnRule("dragon_guard", 5, 2, 12, 37, 30),
            new SpawnRule("imugi", 1, 20, 8, 20, 8, true),
        };
        return m;
    }

    private static GameMap Highlands()
    {
        int W = 48, H = 38;
        var g = new Grid(W, H, 'h');
        var rng = new Mulberry32(909);
        g.Each((x, y, _) => { int e = Edge(g, x, y); if (e == 0 || (e == 1 && rng.Next() < 0.5)) g.Set(x, y, 'O'); });
        for (int i = 0; i < 9; i++) { var (cx, cy) = RandIn(rng, 4, 4, W - 5, H - 5); Blob(g, cx, cy, 1.5 + rng.Next() * 1.8, 'l'); }
        Sprinkle(g, rng, 'h', ('O', 0.06), ('j', 0.03), ('r', 0.02));
        Trail(g, rng, 23, H - 2, 23, 1, ',');
        Camp(g, 5, 28, 16, 35, 12, 33, 'h');
        Trail(g, rng, 16, 31, 23, 31, ',');
        Blob(g, 38, 9, 6, 'h');
        for (int k = 0; k < 40; k++)
        {
            double t = k / 40.0 * Math.PI * 2;
            if (t > 1.9 && t < 3.0) continue; // the crater opens to the south-west
            g.Set((int)Math.Round(38 + Math.Cos(t) * 7), (int)Math.Round(9 + Math.Sin(t) * 7), 'O');
        }
        Trail(g, rng, 24, 14, 33, 12, 'h');
        g.Rect(23, 0, 24, 1, ','); g.Rect(23, H - 2, 24, H - 1, ',');
        g.SealUnreachable(23, H - 2, 'O');
        var m = Make("highlands", "Cinder Highlands", "Lv 44-54", g, new[]
        {
            new Portal(23, H - 1, "harbor", 19, 1, Dir.Down), new Portal(24, H - 1, "harbor", 20, 1, Dir.Down),
            new Portal(23, 0, "monastery", 19, 30, Dir.Up), new Portal(24, 0, "monastery", 20, 30, Dir.Up),
        });
        m.Ground = 'h';
        m.Music = "ember";
        m.Tint = "#ffdcc8";
        m.Spawns = new[]
        {
            new SpawnRule("ash_boar", 8, 2, 14, 45, 35), new SpawnRule("fire_dokkaebi", 7, 2, 2, 45, 30),
            new SpawnRule("ember_bat", 7, 2, 2, 45, 35), new SpawnRule("lava_golem", 5, 2, 2, 45, 24),
            new SpawnRule("cultist", 8, 24, 2, 45, 26), new SpawnRule("bulgasari", 1, 38, 9, 38, 9, true),
        };
        return m;
    }

    private static GameMap Monastery()
    {
        int W = 40, H = 32;
        var g = new Grid(W, H, 'n');
        var rng = new Mulberry32(1010);
        g.Border('#');
        g.Rect(19, 1, 20, H - 2, ',');
        g.Rect(1, 15, W - 2, 16, ',');
        Building(g, 14, 2, 25, 6, new[] { 19, 20 }, 'Q');
        g.Rect(14, 7, 25, 13, '_');
        g.Rect(19, 8, 20, 8, 'S');
        g.Set(14, 7, 'L'); g.Set(25, 7, 'L');
        Building(g, 3, 3, 10, 6, new[] { 7 }); g.Rect(7, 7, 7, 14, ',');
        Building(g, 29, 3, 36, 6, new[] { 32 }); g.Rect(32, 7, 32, 14, ',');
        Building(g, 3, 18, 10, 21, new[] { 7 }); g.Rect(7, 22, 7, 22, ',');
        Building(g, 29, 18, 36, 21, new[] { 32 }); g.Rect(32, 22, 32, 22, ',');
        g.Rect(14, 20, 17, 23, '~');
        foreach (var x in new[] { 12, 27 }) g.Set(x, 18, 'L');
        g.Each((x, y, c) => { if (c == 'n' && Edge(g, x, y) == 1 && rng.Next() < 0.45 && g.OpenAround(x, y, "n#Y")) g.Set(x, y, 'Y'); });
        Sprinkle(g, rng, 'n', ('Y', 0.04));
        foreach (var (gx, gy) in new[] { (19, H - 1), (20, H - 1), (19, 0), (20, 0), (W - 1, 15), (W - 1, 16), (0, 15), (0, 16) }) g.Set(gx, gy, 'G');
        g.SealUnreachable(19, 14, 'Y');
        var m = Make("monastery", "Cloudgate Monastery", "Town", g, new[]
        {
            new Portal(19, H - 1, "highlands", 23, 1, Dir.Down), new Portal(20, H - 1, "highlands", 24, 1, Dir.Down),
            new Portal(19, 0, "skypalace", 22, 38, Dir.Up), new Portal(20, 0, "skypalace", 23, 38, Dir.Up),
            new Portal(W - 1, 15, "frostpeak", 1, 17, Dir.Right), new Portal(W - 1, 16, "frostpeak", 1, 18, Dir.Right),
            new Portal(0, 15, "bamboo", 46, 15, Dir.Left), new Portal(0, 16, "bamboo", 46, 16, Dir.Left),
        });
        m.FolkArea = new Area(14, 9, 25, 13);
        m.Ground = 'n';
        m.Music = "temple";
        m.Spawn = (19, 14);
        return m;
    }

    private static GameMap Frostpeak()
    {
        int W = 48, H = 36;
        var g = new Grid(W, H, 'n');
        var rng = new Mulberry32(1111);
        g.Each((x, y, _) => { int e = Edge(g, x, y); if (e == 0 || (e == 1 && rng.Next() < 0.5)) g.Set(x, y, 'Y'); });
        for (int i = 0; i < 6; i++) { var (cx, cy) = RandIn(rng, 5, 5, W - 6, H - 6); Blob(g, cx, cy, 2 + rng.Next() * 2, 'i'); }
        Sprinkle(g, rng, 'n', ('Y', 0.12), ('r', 0.04));
        for (int i = 0; i < 8; i++) { var (cx, cy) = RandIn(rng, 4, 4, W - 5, H - 5); Blob(g, cx, cy, 2 + rng.Next() * 2, 'n'); }
        Sprinkle(g, rng, 'n', ('Y', 0.1));
        Trail(g, rng, 1, 17, 40, 9, 'n');
        Trail(g, rng, 20, 17, 30, 30, 'n');
        Camp(g, 3, 3, 12, 9, 6, 5, 'n');
        Trail(g, rng, 7, 9, 7, 17, 'n');
        Blob(g, 40, 8, 5, 'n');
        g.Rect(0, 17, 1, 18, 'n');
        g.SealUnreachable(1, 17, 'Y');
        var m = Make("frostpeak", "Frostfang Pass", "Lv 55-65", g, new[]
        {
            new Portal(0, 17, "monastery", 38, 15, Dir.Left), new Portal(0, 18, "monastery", 38, 16, Dir.Left),
        });
        m.Ground = 'n';
        m.Music = "snow";
        m.Tint = "#e4eeff";
        m.Spawns = new[]
        {
            new SpawnRule("snow_wolf", 8, 2, 2, 45, 33), new SpawnRule("frost_bat", 7, 2, 2, 45, 33),
            new SpawnRule("ice_dokkaebi", 7, 14, 2, 45, 33), new SpawnRule("ice_golem", 5, 14, 14, 45, 33),
            new SpawnRule("cult_adept", 7, 26, 2, 45, 22), new SpawnRule("white_tiger", 1, 40, 8, 40, 8, true),
        };
        return m;
    }

    private static GameMap Bamboo()
    {
        int W = 48, H = 36;
        var g = new Grid(W, H, 'g');
        var rng = new Mulberry32(1212);
        g.Each((x, y, _) => { if (Edge(g, x, y) == 0 || rng.Next() < 0.3) g.Set(x, y, 'b'); });
        for (int i = 0; i < 12; i++) { var (cx, cy) = RandIn(rng, 4, 4, W - 5, H - 5); Blob(g, cx, cy, 2 + rng.Next() * 2.5, 'g'); }
        for (int i = 0; i < 4; i++) { var (cx, cy) = RandIn(rng, 5, 5, W - 6, H - 6); Blob(g, cx, cy, 1.5 + rng.Next(), '~'); }
        Sprinkle(g, rng, 'g', ('*', 0.03));
        Trail(g, rng, W - 2, 15, 23, 1, 'g');
        Trail(g, rng, 30, 12, 10, 27, 'g');
        Trail(g, rng, 30, 12, 40, 30, 'g');
        Camp(g, 19, 2, 28, 9, 22, 5, 'g');
        Blob(g, 10, 27, 5, 'g');
        Blob(g, 6, 31, 1.8, '~');
        g.Rect(W - 2, 15, W - 1, 16, 'g'); g.Rect(23, 0, 24, 1, 'g');
        g.SealUnreachable(W - 2, 15, 'b');
        var m = Make("bamboo", "Moonlit Bamboo Grove", "Lv 65-75", g, new[]
        {
            new Portal(W - 1, 15, "monastery", 1, 15, Dir.Right), new Portal(W - 1, 16, "monastery", 1, 16, Dir.Right),
            new Portal(23, 0, "underworld", 22, 38, Dir.Up), new Portal(24, 0, "underworld", 23, 38, Dir.Up),
        });
        m.Ground = 'g';
        m.Music = "moon";
        m.Dark = 0.3;
        m.Tint = "#b8b8f0";
        m.Spawns = new[]
        {
            new SpawnRule("moon_rabbit", 8, 2, 2, 45, 33), new SpawnRule("grove_toad", 6, 2, 2, 45, 33),
            new SpawnRule("moon_fox", 7, 2, 10, 45, 33), new SpawnRule("egg_ghost", 7, 2, 2, 45, 33),
            new SpawnRule("fox_maiden", 8, 2, 14, 32, 33), new SpawnRule("gumiho", 1, 10, 27, 10, 27, true),
        };
        return m;
    }

    private static GameMap Underworld()
    {
        int W = 46, H = 40;
        var g = new Grid(W, H, '~');
        var rng = new Mulberry32(1313);
        int[] turns = { 30, 12, 34, 10, 22 };
        int x = 22, y = H - 2;
        for (int i = 0; i < turns.Length; i++)
        {
            int ty = y - 7;
            Trail(g, rng, x, y, turns[i], Math.Max(4, ty), 'u', 3);
            x = turns[i]; y = Math.Max(4, ty);
        }
        Trail(g, rng, x, y, 22, 5, 'u', 3);
        for (int i = 0; i < 9; i++) { var (cx, cy) = RandIn(rng, 4, 6, W - 5, H - 8); Blob(g, cx, cy, 2 + rng.Next() * 2, 'u'); }
        g.Rect(16, 2, 28, 9, 'M');
        foreach (var (px, py) in new[] { (16, 2), (28, 2), (16, 9), (28, 9) }) g.Set(px, py, 'I');
        g.Rect(22, 32, 32, 38, 'u');
        g.Rect(25, 36, 26, 36, 'S');
        g.Rect(33, 34, 37, 35, 'd');
        g.Each((cx, cy, c) =>
        {
            if (c != 'u') return;
            bool shore = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }.Any(d => g.Get(cx + d.Item1, cy + d.Item2) == '~');
            if (shore && rng.Next() < 0.06 && g.OpenAround(cx, cy, "u~")) g.Set(cx, cy, 'y');
        });
        g.Scatter(rng, "u", ('j', 0.03));
        g.Border('~');
        g.Rect(22, H - 2, 23, H - 1, 'u');
        g.SealUnreachable(22, H - 2, '~');
        var m = Make("underworld", "Road of the Yellow Springs", "Lv 75-85", g, new[]
        {
            new Portal(22, H - 1, "bamboo", 23, 1, Dir.Down), new Portal(23, H - 1, "bamboo", 24, 1, Dir.Down),
        });
        m.Ground = 'u';
        m.Music = "spirit";
        m.Dark = 0.55;
        m.Tint = "#d8b8e8";
        m.Spawns = new[]
        {
            new SpawnRule("plague_rat", 8, 2, 10, 43, 31), new SpawnRule("grave_wisp", 7, 2, 2, 43, 31),
            new SpawnRule("ghost_soldier", 7, 2, 2, 43, 28), new SpawnRule("reaper", 7, 2, 2, 43, 24),
            new SpawnRule("lantern_judge", 1, 22, 5, 22, 5, true),
        };
        return m;
    }

    private static GameMap SkyPalace()
    {
        int W = 46, H = 40;
        var g = new Grid(W, H, 'k');
        var rng = new Mulberry32(1414);
        g.Each((x, y, _) => { int e = Edge(g, x, y); if (e == 0 || (e == 1 && rng.Next() < 0.6)) g.Set(x, y, 'V'); });
        for (int i = 0; i < 8; i++)
        {
            var (cx, cy) = RandIn(rng, 4, 4, W - 5, H - 8);
            if (Math.Abs(cx - 22.5) < 5) continue;
            Blob(g, cx, cy, 1.5 + rng.Next() * 1.5, 'V');
        }
        g.Rect(21, 0, 24, H - 1, 'M');
        g.Rect(13, 7, 32, 14, 'M');
        for (int x = 14; x <= 31; x += 3) { g.Set(x, 7, 'I'); g.Set(x, 14, 'I'); }
        Building(g, 4, 3, 13, 6, new[] { 8 }, 'Q');
        Building(g, 32, 3, 41, 6, new[] { 36 }, 'Q');
        g.Rect(6, 20, 15, 26, 'M'); g.Rect(30, 20, 39, 26, 'M');
        for (int y = 17; y <= 30; y += 3) { g.Set(20, y, 'I'); g.Set(25, y, 'I'); }
        g.Rect(17, 33, 30, 38, 'M');
        g.Rect(19, 36, 20, 36, 'S');
        g.Set(17, 33, 'L'); g.Set(30, 33, 'L');
        g.Scatter(rng, "k", ('r', 0.02));
        g.SealUnreachable(22, H - 2, 'V');
        var m = Make("skypalace", "Ruins of the Sky Palace", "Lv 85-95", g, new[]
        {
            new Portal(22, H - 1, "monastery", 19, 1, Dir.Down), new Portal(23, H - 1, "monastery", 20, 1, Dir.Down),
            new Portal(22, 0, "eye", 17, 34, Dir.Up), new Portal(23, 0, "eye", 18, 34, Dir.Up),
        });
        m.Ground = 'k';
        m.Music = "sky";
        m.Spawns = new[]
        {
            new SpawnRule("thunder_crane", 8, 2, 2, 43, 31), new SpawnRule("heavenly_guard", 8, 2, 2, 43, 31),
            new SpawnRule("haetae", 6, 2, 15, 43, 31), new SpawnRule("sky_sentinel", 5, 2, 2, 43, 26),
            new SpawnRule("thunder_roc", 1, 22, 10, 22, 10, true),
        };
        return m;
    }

    private static GameMap Eye()
    {
        int W = 36, H = 36;
        var g = new Grid(W, H, 'V');
        double cx = 17.5, cy = 16;
        g.Each((x, y, _) =>
        {
            double d = Math.Sqrt((x + 0.5 - cx) * (x + 0.5 - cx) + (y + 0.5 - cy) * (y + 0.5 - cy));
            if (d < 15) g.Set(x, y, d < 9 ? 'M' : 'k');
        });
        for (int k = 0; k < 12; k++)
        {
            double t = k / 12.0 * Math.PI * 2 + 0.26;
            g.Set((int)Math.Round(cx - 0.5 + Math.Cos(t) * 11), (int)Math.Round(cy - 0.5 + Math.Sin(t) * 11), 'I');
        }
        g.Rect(17, 26, 18, H - 1, 'k');
        g.SealUnreachable(17, H - 2, 'V');
        var m = Make("eye", "Eye of the Winds", "Lv 95-100", g, new[]
        {
            new Portal(17, H - 1, "skypalace", 22, 1, Dir.Down), new Portal(18, H - 1, "skypalace", 23, 1, Dir.Down),
        });
        m.Ground = 'k';
        m.Music = "storm";
        m.Dark = 0.3;
        m.Tint = "#c8c0f0";
        m.Spawns = new[]
        {
            new SpawnRule("storm_wisp", 8, 2, 2, 33, 30), new SpawnRule("storm_wraith", 7, 2, 2, 33, 30),
            new SpawnRule("heuk_un", 1, 17, 12, 17, 12, true),
        };
        return m;
    }
}
