using KotW.Shared;

namespace KotW.ArtGen;

// Tile atlas: one row per tile char (in Content.TileOrder), 16 columns =
// 4 variants x 4 animation frames. Still tiles repeat the same image per frame.
public static class Tiles
{
    private const int T = 32;
    private static readonly HashSet<char> Animated = new() { '~', 'S', 't', 'p', 'a', '=', 'l', 'V', 'y' };

    public static Canvas Atlas()
    {
        var order = Content.TileOrder;
        var atlas = new Canvas(T * 16, T * order.Length);
        for (int row = 0; row < order.Length; row++)
            for (int v = 0; v < 4; v++)
                for (int f = 0; f < 4; f++)
                {
                    var c = new Canvas(T, T);
                    var rng = new Mulberry32((uint)(v * 7919 + order[row] * 31 + 5));
                    Draw(order[row], c, rng, Animated.Contains(order[row]) ? f : 0, v);
                    atlas.Blit(c, (v * 4 + f) * T, row * T);
                }
        return atlas;
    }

    private static void Speckle(Canvas c, Mulberry32 r, Rgba[] cols, int n, int size = 1)
    {
        for (int i = 0; i < n; i++) c.Rect(r.Int(0, T - size), r.Int(0, T - size), size, size, cols[r.Int(0, cols.Length - 1)]);
    }

    private static void Grass(Canvas c, Mulberry32 r, string baseHex = "#5f9a44")
    {
        var g = Pal.Ramp(baseHex);
        c.Rect(0, 0, T, T, g[2]);
        Speckle(c, r, new[] { g[1], g[3] }, 46);
        for (int i = 0; i < 7; i++)
        {
            int x = r.Int(1, T - 4), y = r.Int(3, T - 2);
            c.Set(x, y, g[1]); c.Set(x + 1, y - 1, g[1]); c.Set(x + 1, y - 2, g[3]); c.Set(x + 2, y, g[1]); c.Set(x + 3, y - 1, g[3]);
        }
    }

    // A plain ground: base colour, speckles of its shadow and light, and a few larger flecks.
    private static void Ground(Canvas c, Mulberry32 r, string hex, int specks = 40, int flecks = 3)
    {
        var g = Pal.Ramp(hex);
        c.Rect(0, 0, T, T, g[2]);
        Speckle(c, r, new[] { g[1], g[3] }, specks);
        for (int i = 0; i < flecks; i++) { int x = r.Int(2, T - 5), y = r.Int(2, T - 4); c.Rect(x, y, 3, 1, g[3]); c.Rect(x, y + 1, 3, 1, g[1]); }
    }

    // Draws an object on its own layer and outlines it, so it sits on any ground.
    private static void Prop(Canvas c, Action<Canvas> draw)
    {
        var o = new Canvas(T, T);
        draw(o);
        o.Outline();
        c.Blit(o, 0, 0);
    }

    private static void Dirt(Canvas c, Mulberry32 r)
    {
        var d = Pal.Ramp("#b08654");
        c.Rect(0, 0, T, T, d[2]);
        Speckle(c, r, new[] { d[1], d[3] }, 40);
        for (int i = 0; i < 4; i++)
        {
            int x = r.Int(2, T - 4), y = r.Int(2, T - 4);
            c.Rect(x, y, 2, 2, d[3]); c.Set(x + 1, y + 1, d[1]); c.Set(x + 2, y + 1, d[0].WithA(90));
        }
    }

    private static void Shadow(Canvas c, double cx, double cy, double rx, double ry) => c.Ellipse(cx, cy, rx, ry, new Rgba(0, 0, 0, 70));

    private static void Draw(char ch, Canvas c, Mulberry32 r, int frame, int v)
    {
        switch (ch)
        {
            case '.': Grass(c, r); break;
            case 'g': Grass(c, r, "#426f3a"); break;
            case ',': Dirt(c, r); break;
            case 'G':
                Dirt(c, r);
                c.Rect(0, 0, T, 5, new Rgba(0, 0, 0, 60));
                break;
            case '_':
            {
                var s = Pal.Ramp("#aeaa9a");
                c.Rect(0, 0, T, T, s[0].WithA(255));
                int off = v % 2 * 8;
                for (int row = 0; row < 2; row++)
                    for (int col = -1; col < 2; col++)
                    {
                        int x = col * 16 + (row == 1 ? off + 8 : off) % 16, y = row * 16;
                        var tone = Pal.Ramp(new[] { "#b4b0a0", "#a8a494", "#bab6a6" }[r.Int(0, 2)]);
                        c.Rect(x + 1, y + 1, 14, 14, tone[2]);
                        c.Rect(x + 1, y + 1, 14, 1, tone[3]);
                        c.Rect(x + 1, y + 1, 1, 14, tone[3]);
                        c.Rect(x + 1, y + 14, 14, 1, tone[1]);
                        c.Rect(x + 14, y + 1, 1, 14, tone[1]);
                    }
                Speckle(c, r, new[] { s[1] }, 6);
                break;
            }
            case '#':
            {
                c.Rect(0, 0, T, T, Pal.Hex("#4e463e"));
                for (int row = 0; row < 4; row++)
                {
                    int off = row % 2 == 1 ? 8 : 0;
                    for (int x = -16; x < T; x += 16)
                    {
                        var st = Pal.Ramp(new[] { "#8c8072", "#807566", "#968a7a" }[r.Int(0, 2)]);
                        c.Rect(x + off + 1, row * 8 + 1, 14, 6, st[2]);
                        c.Rect(x + off + 1, row * 8 + 1, 14, 1, st[3]);
                        c.Rect(x + off + 1, row * 8 + 6, 14, 1, st[1]);
                    }
                }
                var roof = Pal.Ramp("#3e4a5c");
                c.Rect(0, 0, T, 5, roof[2]);
                for (int x = 0; x < T; x += 4) { c.Rect(x, 3, 3, 2, roof[1]); c.Set(x + 1, 0, roof[3]); }
                c.Rect(0, 5, T, 1, roof[0]);
                break;
            }
            case '~':
            {
                var w = Pal.Ramp("#3c74a8");
                c.Rect(0, 0, T, T, w[2]);
                Speckle(c, r, new[] { w[1] }, 18, 2);
                for (int i = 0; i < 5; i++)
                {
                    int y = 3 + i * 6, x = (r.Int(0, 20) + frame * 3) % 28;
                    c.Rect(x, y, 5, 1, w[3]); c.Rect(x + 1, y - 1, 3, 1, w[4]);
                    c.Rect((x + 14) % 28, y + 3, 3, 1, w[3]);
                }
                break;
            }
            case 'T':
            {
                Shadow(c, 16, 27, 11, 4);
                var bark = Pal.Ramp("#7a5232");
                c.Rect(13, 19, 6, 11, bark[2]); c.Rect(13, 19, 2, 11, bark[3]); c.Rect(17, 19, 2, 11, bark[1]);
                c.Rect(12, 29, 8, 1, bark[1]);
                var leaf = Pal.Ramp("#3e8a38");
                var crown = new Canvas(T, T);
                crown.Shaded(16, 13, 13, 11, leaf, 0.05);
                crown.Shaded(10, 15, 6, 5, leaf);
                crown.Shaded(22, 15, 6, 5, leaf, -0.05);
                crown.Shaded(14, 8, 6, 5, leaf, 0.15);
                for (int i = 0; i < 10; i++) crown.Set(r.Int(6, 25), r.Int(4, 21), leaf[r.Int(1, 3)]);
                crown.Outline();
                c.Blit(crown, 0, 0);
                break;
            }
            case 'P':
            {
                Shadow(c, 16, 28, 10, 3.5);
                var bark = Pal.Ramp("#6a4428");
                c.Rect(14, 24, 4, 7, bark[2]); c.Rect(14, 24, 1, 7, bark[3]);
                var n = Pal.Ramp("#2a6a40");
                var crown = new Canvas(T, T);
                foreach (var (top, half, bottom) in new[] { (13, 13.0, 26), (7, 10.0, 19), (1, 7.0, 12) })
                {
                    crown.Poly(new (double, double)[] { (16, top), (16 + half, bottom), (16 - half, bottom) }, n[2]);
                    crown.Poly(new (double, double)[] { (16, top), (16 - half * 0.2, bottom), (16 - half, bottom) }, n[3]);
                    crown.Poly(new (double, double)[] { (16, top), (16 + half, bottom), (16 + half * 0.5, bottom) }, n[1]);
                    for (int x = (int)(16 - half) + 2; x < 16 + half - 1; x += 3) crown.Set(x, bottom - 1, n[1]);
                }
                crown.Outline();
                c.Blit(crown, 0, 0);
                break;
            }
            case 'R': Roof(c, Pal.Ramp("#46566e"), false); break;
            case 'Q': Roof(c, Pal.Ramp("#2f7466"), true); break;
            case 'H':
            {
                var wall = Pal.Ramp("#ece2c8");
                var wood = Pal.Ramp("#6e4428");
                c.Rect(0, 0, T, T, wall[2]);
                Speckle(c, r, new[] { wall[3], wall[1] }, 10);
                c.Rect(0, 0, T, 4, wood[2]); c.Rect(0, 3, T, 1, wood[0]);
                c.Rect(0, 28, T, 4, Pal.Ramp("#8a8476")[2]); c.Rect(0, 28, T, 1, Pal.Ramp("#8a8476")[3]);
                c.Rect(0, 4, 3, 24, wood[2]); c.Rect(0, 4, 1, 24, wood[3]);
                if (v % 3 != 2)
                {
                    c.Rect(9, 9, 15, 14, wood[1]);
                    c.Rect(10, 10, 13, 12, Pal.Hex("#f8f0dc"));
                    for (int x = 12; x < 23; x += 3) c.Rect(x, 10, 1, 12, wood[2]);
                    for (int y = 13; y < 22; y += 3) c.Rect(10, y, 13, 1, wood[2]);
                }
                break;
            }
            case 'D':
            {
                var wood = Pal.Ramp("#8a5630");
                c.Rect(0, 0, T, T, Pal.Hex("#ece2c8"));
                c.Rect(0, 0, T, 4, Pal.Ramp("#6e4428")[2]);
                c.Rect(3, 4, 26, 28, wood[0]);
                foreach (var x0 in new[] { 5, 17 })
                {
                    c.Rect(x0, 6, 10, 26, wood[2]); c.Rect(x0, 6, 1, 26, wood[3]);
                    for (int y = 9; y < 30; y += 5) c.Rect(x0 + 1, y, 8, 1, wood[1]);
                }
                c.Rect(14, 17, 1, 4, Pal.Hex("#e8c050")); c.Rect(17, 17, 1, 4, Pal.Hex("#e8c050"));
                break;
            }
            case 'S':
            {
                var s = Pal.Ramp("#c8c2ac");
                c.Rect(0, 0, T, T, s[1]);
                c.Rect(1, 1, 30, 30, s[2]);
                c.Rect(1, 1, 30, 1, s[3]);
                var glow = Pal.Ramp("#e8c050")[1 + frame % 4 / 2 + (frame == 2 ? 1 : 0)];
                for (int k = 0; k < 28; k++)
                {
                    double t = k / 28.0 * Math.PI * 2;
                    c.Set(16 + Math.Cos(t) * 11, 16 + Math.Sin(t) * 11, glow);
                }
                c.Rect(15, 9, 2, 14, glow); c.Rect(9, 15, 14, 2, glow);
                c.Rect(12, 12, 8, 8, glow.WithA(70));
                break;
            }
            case 'F':
            {
                Grass(c, r);
                var wood = Pal.Ramp("#8e6438");
                c.Rect(0, 15, T, 2, wood[2]); c.Rect(0, 15, T, 1, wood[3]);
                c.Rect(0, 22, T, 2, wood[2]); c.Rect(0, 22, T, 1, wood[3]);
                foreach (var x in new[] { 5, 24 })
                {
                    c.Rect(x, 10, 4, 18, wood[2]); c.Rect(x, 10, 1, 18, wood[3]); c.Rect(x + 3, 10, 1, 18, wood[1]);
                    c.Rect(x, 9, 4, 1, wood[4]);
                }
                break;
            }
            case '*':
            {
                Grass(c, r);
                var petals = new[] { "#f088b0", "#f2d050", "#f6f2ee", "#e86a6a", "#b48ae8" };
                for (int i = 0; i < 7; i++)
                {
                    int x = r.Int(3, T - 4), y = r.Int(3, T - 4);
                    var p = Pal.Ramp(petals[r.Int(0, petals.Length - 1)]);
                    c.Set(x - 1, y, p[2]); c.Set(x + 1, y, p[1]); c.Set(x, y - 1, p[3]); c.Set(x, y + 1, p[1]);
                    c.Set(x, y, Pal.Hex("#f8e070"));
                    c.Set(x, y + 2, Pal.Hex("#3e7a30"));
                }
                break;
            }
            case 'W':
            {
                Draw('_', c, r, 0, v);
                Shadow(c, 16, 25, 13, 5);
                var stone = Pal.Ramp("#8a8678");
                c.Shaded(16, 19, 12, 9, stone);
                c.Ellipse(16, 18, 8, 5.5, Pal.Hex("#1a2a3c"));
                c.Ellipse(16, 19, 6, 3.5, Pal.Hex("#2a4a6a"));
                var wood = Pal.Ramp("#6e4428");
                c.Rect(4, 3, 3, 18, wood[2]); c.Rect(25, 3, 3, 18, wood[2]);
                c.Rect(2, 1, 28, 4, Pal.Ramp("#46566e")[2]); c.Rect(2, 4, 28, 1, Pal.Ramp("#46566e")[0]);
                c.Rect(15, 5, 2, 9, Pal.Hex("#c8b070"));
                c.Rect(13, 13, 6, 4, wood[3]);
                break;
            }
            case 'L':
            {
                Shadow(c, 16, 28, 9, 3);
                var s = Pal.Ramp("#9a968a");
                var lantern = new Canvas(T, T);
                lantern.Rect(11, 24, 10, 5, s[2]); lantern.Rect(11, 24, 10, 1, s[3]);
                lantern.Rect(14, 17, 4, 7, s[2]); lantern.Rect(14, 17, 1, 7, s[3]);
                lantern.Rect(9, 9, 14, 8, s[2]);
                lantern.Rect(12, 11, 8, 4, Pal.Hex("#f8d870"));
                lantern.Poly(new (double, double)[] { (6, 9), (16, 3), (26, 9) }, s[1]);
                lantern.Poly(new (double, double)[] { (6, 9), (16, 3), (14, 9) }, s[3]);
                lantern.Rect(15, 1, 2, 2, s[2]);
                lantern.Outline();
                c.Blit(lantern, 0, 0);
                break;
            }
            case 'p':
            {
                var w = Pal.Ramp("#507e70");
                c.Rect(0, 0, T, T, w[2]);
                Speckle(c, r, new[] { w[1], w[3] }, 14);
                int sx = (frame * 5 + r.Int(0, 20)) % 24;
                c.Rect(sx, 4 + r.Int(0, 5) * 4, 7, 1, w[4]);
                var rice = Pal.Ramp("#94c85a");
                for (int y = 4; y < T; y += 8)
                    for (int x = 4; x < T; x += 8)
                    {
                        c.Set(x, y + 2, Pal.Hex("#2a4a3a").WithA(120));
                        c.Rect(x, y - 1, 1, 3, rice[2]);
                        c.Set(x - 1, y - 2, rice[3]); c.Set(x + 1, y - 2, rice[1]); c.Set(x - 2, y - 3, rice[4]);
                    }
                c.Rect(0, 0, T, 1, Pal.Hex("#7a6a48"));
                break;
            }
            case 'B':
            {
                Shadow(c, 16, 26, 12, 4);
                var b = Pal.Ramp("#428a36");
                var bush = new Canvas(T, T);
                bush.Shaded(16, 18, 12, 9, b);
                bush.Shaded(11, 16, 6, 5, b, 0.1);
                bush.Shaded(21, 15, 6, 5, b, 0.05);
                for (int i = 0; i < 5; i++) { int x = r.Int(8, 23), y = r.Int(12, 22); bush.Rect(x, y, 2, 2, Pal.Hex("#e04848")); bush.Set(x, y, Pal.Hex("#ff9090")); }
                bush.Outline();
                c.Blit(bush, 0, 0);
                break;
            }
            case 'r':
            {
                Shadow(c, 16, 25, 13, 5);
                var rock = new Canvas(T, T);
                rock.Shaded(16, 18, 12, 10, Pal.Ramp("#848278"));
                rock.Shaded(23, 22, 5, 4, Pal.Ramp("#74726a"));
                rock.Line(11, 14, 15, 19, Pal.Ramp("#848278")[1]);
                rock.Shaded(13, 11, 5, 2, Pal.Ramp("#5a8a48"), 0.2);
                rock.Outline();
                c.Blit(rock, 0, 0);
                break;
            }
            case 'c':
            {
                var f = Pal.Ramp("#504438");
                c.Rect(0, 0, T, T, f[2]);
                Speckle(c, r, new[] { f[1], f[3] }, 40);
                for (int i = 0; i < 3; i++) { int x = r.Int(2, 27), y = r.Int(2, 28); c.Rect(x, y, 3, 2, f[3]); c.Rect(x, y + 2, 3, 1, f[1]); }
                break;
            }
            case 'X':
            {
                var w = Pal.Ramp("#3a322c");
                c.Rect(0, 0, T, T, w[1]);
                for (int i = 0; i < 5; i++)
                {
                    int x = r.Int(-4, 24), y = r.Int(-4, 24), s = r.Int(7, 12);
                    c.Shaded(x + s / 2.0, y + s / 2.0, s / 2.0, s / 2.2, w);
                }
                Speckle(c, r, new[] { w[0] }, 14);
                break;
            }
            case 't':
            {
                Draw('X', c, r, 0, v);
                var wood = Pal.Ramp("#6a4428");
                c.Rect(14, 15, 4, 10, wood[2]); c.Rect(14, 15, 1, 10, wood[3]);
                c.Rect(11, 13, 10, 3, Pal.Ramp("#5a4a3a")[2]);
                var fl = new[] { "#f0a030", "#f8c048", "#f08a28", "#f8d060" };
                c.Ellipse(16, 9 - frame % 2, 4.4, 5.5, Pal.Hex(fl[frame]));
                c.Ellipse(16, 10, 2.4, 3.2, Pal.Hex("#fff0b0"));
                c.Ellipse(16, 9, 8, 8, Pal.Hex("#f8b040").WithA(40));
                break;
            }
            case 'm':
            {
                Shadow(c, 16, 28, 9, 3);
                var s = Pal.Ramp("#7a6c5e");
                var sp = new Canvas(T, T);
                sp.Poly(new (double, double)[] { (16, 2), (25, 29), (7, 29) }, s[2]);
                sp.Poly(new (double, double)[] { (16, 2), (14, 29), (7, 29) }, s[3]);
                sp.Poly(new (double, double)[] { (16, 2), (25, 29), (20, 29) }, s[1]);
                sp.Outline();
                c.Blit(sp, 0, 0);
                break;
            }
            case 'a':
            {
                Draw('c', c, r, 0, v);
                var s = Pal.Ramp("#6a645a");
                c.Rect(2, 12, 28, 18, s[2]); c.Rect(2, 12, 28, 2, s[3]); c.Rect(2, 28, 28, 2, s[1]);
                c.Rect(4, 18, 24, 3, Pal.Hex("#9a2a24"));
                c.Rect(4, 18, 24, 1, Pal.Hex("#c8443a"));
                foreach (var x in new[] { 7, 15, 23 })
                {
                    c.Rect(x, 6, 2, 6, Pal.Hex("#f2ecd8"));
                    c.Ellipse(x + 1, 4 - frame % 2 * 0.5, 1.4, 2, Pal.Hex(frame % 2 == 1 ? "#f8d860" : "#f0a030"));
                }
                break;
            }
            case '=':
            {
                Draw('~', c, r, frame, v);
                var wood = Pal.Ramp("#946a3e");
                for (int x = 0; x < T; x += 6) { c.Rect(x, 2, 5, 28, wood[2]); c.Rect(x, 2, 1, 28, wood[3]); c.Rect(x + 4, 2, 1, 28, wood[1]); }
                c.Rect(0, 2, T, 2, wood[0]); c.Rect(0, 28, T, 2, wood[0]);
                break;
            }
            case 's':
            {
                Ground(c, r, "#e0cc98", 50, 2);
                var d = Pal.Ramp("#e0cc98");
                for (int i = 0; i < 3; i++) { int y = r.Int(4, 28); for (int x = 0; x < T; x++) if ((x + i * 5) % 9 < 5) c.Set(x, y + (int)Math.Round(Math.Sin(x / 3.0)), d[3]); }
                if (v == 3) { c.Rect(20, 12, 3, 2, Pal.Hex("#f4e8e0")); c.Set(21, 11, Pal.Hex("#e8a8a0")); }
                break;
            }
            case 'v':
                Prop(c, o =>
                {
                    var g = Pal.Ramp("#8a9a4a");
                    for (int i = 0; i < 9; i++)
                    {
                        int x = 4 + i * 3 + r.Int(-1, 1), top = r.Int(3, 12);
                        o.Line(x, 30, x + r.Int(-2, 2), top, g[i % 3 + 1]);
                        if (i % 3 == 0) { o.Rect(x - 1 + r.Int(-1, 1), top - 1, 2, 5, Pal.Ramp("#7a5030")[2]); }
                    }
                });
                break;
            case 'z': Ground(c, r, "#56603a", 50, 4); Speckle(c, r, new[] { Pal.Hex("#3a4a3a") }, 6, 2); break;
            case 'd':
            {
                var wood = Pal.Ramp("#a07850");
                c.Rect(0, 0, T, T, wood[0]);
                for (int y = 0; y < T; y += 8) { c.Rect(0, y + 1, T, 6, wood[2]); c.Rect(0, y + 1, T, 1, wood[3]); c.Set(r.Int(2, 28), y + 4, wood[1]); }
                c.Rect(v * 7 + 3, 0, 1, T, wood[1]);
                break;
            }
            case 'o':
                Prop(c, o =>
                {
                    var p = Pal.Ramp(new[] { "#f07870", "#f0a060", "#e868a8", "#f07870" }[v]);
                    o.Line(16, 30, 16, 12, p[2], 2.6);
                    o.Line(16, 22, 9, 13, p[2], 2.2); o.Line(16, 19, 23, 9, p[2], 2.2); o.Line(10, 14, 7, 7, p[2], 1.8); o.Line(22, 10, 26, 5, p[3], 1.8);
                    foreach (var (x, y) in new[] { (7, 7), (26, 5), (16, 11), (9, 13) }) o.Shaded(x, y, 1.6, 1.6, p, 0.2);
                });
                break;
            case 'h':
            {
                Ground(c, r, "#5a5250", 46, 3);
                if (v == 1) { int x = r.Int(3, 18), y = r.Int(6, 24); c.Line(x, y, x + 6, y + 2, Pal.Hex("#8a3020")); c.Set(x + 3, y + 1, Pal.Hex("#e06020")); }
                break;
            }
            case 'l':
            {
                var m = Pal.Ramp("#e85020");
                c.Rect(0, 0, T, T, m[1]);
                for (int i = 0; i < 6; i++)
                {
                    int x = (r.Int(0, 28) + frame * 2) % 30, y = r.Int(2, 28);
                    c.Ellipse(x, y, 4, 2.5, m[2]);
                    c.Ellipse(x, y, 2, 1, m[3]);
                }
                c.Rect(r.Int(4, 26), (frame * 7 + v * 5) % 28 + 2, 2, 2, Pal.Hex("#fff0a0"));
                break;
            }
            case 'O':
                Shadow(c, 16, 26, 12, 4);
                Prop(c, o =>
                {
                    var b = Pal.Ramp("#3a3640");
                    o.Poly(new (double, double)[] { (6, 28), (9, 9), (15, 4), (22, 7), (27, 28) }, b[2]);
                    o.Poly(new (double, double)[] { (6, 28), (9, 9), (15, 4), (14, 28) }, b[3]);
                    o.Line(15, 5, 16, 27, b[1]);
                    o.Line(20, 9, 24, 27, b[1]);
                });
                break;
            case 'n':
            {
                Ground(c, r, "#e8eef6", 30, 2);
                var sn = Pal.Ramp("#e8eef6");
                for (int i = 0; i < 4; i++) { int x = r.Int(2, 26), y = r.Int(3, 28); c.Rect(x, y, 4, 1, sn[1]); c.Rect(x + 1, y - 1, 2, 1, sn[4]); }
                break;
            }
            case 'Y':
            {
                Shadow(c, 16, 28, 10, 3.5);
                var bark = Pal.Ramp("#5a3c28");
                c.Rect(14, 24, 4, 7, bark[2]);
                var n = Pal.Ramp("#2a5a48");
                var snow = Pal.Ramp("#f4f8ff");
                var crown = new Canvas(T, T);
                foreach (var (top, half, bottom) in new[] { (13, 13.0, 26), (7, 10.0, 19), (1, 7.0, 12) })
                {
                    crown.Poly(new (double, double)[] { (16, top), (16 + half, bottom), (16 - half, bottom) }, n[2]);
                    crown.Poly(new (double, double)[] { (16, top), (16 + half, bottom), (16 + half * 0.5, bottom) }, n[1]);
                    crown.Poly(new (double, double)[] { (16, top), (16 + half * 0.55, top + (bottom - top) * 0.5), (16 - half * 0.7, top + (bottom - top) * 0.6) }, snow[2]);
                    for (int x = (int)(16 - half) + 1; x < 16 + half; x += 4) crown.Rect(x, bottom - 1, 2, 1, snow[3]);
                }
                crown.Outline();
                c.Blit(crown, 0, 0);
                break;
            }
            case 'i':
            {
                var ice = Pal.Ramp("#a8d4ec");
                c.Rect(0, 0, T, T, ice[2]);
                Speckle(c, r, new[] { ice[3] }, 16);
                c.Line(r.Int(0, 10), r.Int(0, 31), r.Int(20, 31), r.Int(0, 31), ice[1]);
                c.Line(r.Int(4, 12), 4, r.Int(14, 28), 10, ice[4]);
                c.Line(r.Int(4, 12), 22, r.Int(14, 28), 26, ice[4]);
                break;
            }
            case 'b':
                Shadow(c, 16, 28, 11, 3);
                Prop(c, o =>
                {
                    var g = Pal.Ramp("#4a9a4a");
                    var leaf = Pal.Ramp("#3a7a3a");
                    foreach (var x in new[] { 7 + v % 2, 15, 23 - v % 3 })
                    {
                        o.Rect(x, 1, 3, 30, g[2]); o.Rect(x, 1, 1, 30, g[3]); o.Rect(x + 2, 1, 1, 30, g[1]);
                        for (int y = 4 + x % 5; y < 30; y += 8) o.Rect(x, y, 3, 1, g[0]);
                        o.Poly(new (double, double)[] { (x + 2, 6 + x % 7), (x + 9, 3 + x % 7), (x + 3, 8 + x % 7) }, leaf[2]);
                    }
                });
                break;
            case 'j':
                Shadow(c, 16, 28, 9, 3);
                Prop(c, o =>
                {
                    var b = Pal.Ramp("#5a4a40");
                    o.Line(16, 30, 15, 8, b[2], 3.4);
                    o.Line(15, 16, 7, 8, b[2], 1.8); o.Line(15, 12, 24, 5, b[2], 1.8); o.Line(8, 9, 5, 3, b[1], 1.2); o.Line(22, 6, 26, 9, b[1], 1.2);
                    o.Line(14, 28, 14, 10, b[3]);
                });
                break;
            case 'u':
            {
                Ground(c, r, "#6a5a48", 36, 2);
                var y = Pal.Ramp("#c8a040");
                if (v % 2 == 0) for (int i = 0; i < 3; i++) c.Set(r.Int(2, 29), r.Int(2, 29), y[3]);
                break;
            }
            case 'y':
                Shadow(c, 16, 29, 7, 2.5);
                Prop(c, o =>
                {
                    var pole = Pal.Ramp("#3a3038");
                    o.Rect(15, 10, 2, 20, pole[2]);
                    o.Rect(11, 8, 10, 2, pole[2]);
                    var glow = Pal.Ramp(frame % 2 == 0 ? "#ff6080" : "#ff8098");
                    o.Ellipse(12, 15, 3, 4, glow[2]); o.Ellipse(12, 14, 1.6, 2.4, glow[4]);
                    o.Ellipse(20, 15, 3, 4, glow[2]); o.Ellipse(20, 14, 1.6, 2.4, glow[4]);
                    o.Line(12, 10, 12, 11, pole[1]); o.Line(20, 10, 20, 11, pole[1]);
                });
                c.Ellipse(16, 15, 12, 10, Pal.Hex("#ff6080").WithA(36));
                break;
            case 'k':
            {
                var cl = Pal.Ramp("#e8ecf8");
                c.Rect(0, 0, T, T, cl[2]);
                for (int i = 0; i < 6; i++) c.Shaded(r.Int(0, 31), r.Int(0, 31), r.Int(4, 8), r.Int(3, 5), cl, 0.1);
                break;
            }
            case 'V':
            {
                var sky = Pal.Ramp("#6a90d8");
                for (int y = 0; y < T; y++) c.Rect(0, y, T, 1, y < 16 ? sky[2] : sky[1]);
                for (int i = 0; i < 2; i++)
                {
                    int x = (r.Int(0, 31) + frame * 3) % 40 - 6, y = r.Int(4, 26);
                    c.Ellipse(x, y, 6, 2, Pal.Hex("#ffffff").WithA(110));
                }
                break;
            }
            case 'M':
            {
                var m = Pal.Ramp("#d8d4e0");
                c.Rect(0, 0, T, T, m[1]);
                foreach (var (x, y) in new[] { (0, 0), (16, 0), (0, 16), (16, 16) })
                {
                    c.Rect(x + 1, y + 1, 14, 14, m[2]); c.Rect(x + 1, y + 1, 14, 1, m[3]); c.Rect(x + 1, y + 1, 1, 14, m[3]);
                }
                c.Line(r.Int(2, 12), r.Int(2, 12), r.Int(10, 20), r.Int(14, 28), m[1]);
                if (v == 2) c.Rect(6, 6, 20, 20, Pal.Hex("#c8a050").WithA(50));
                break;
            }
            case 'I':
                Shadow(c, 16, 28, 9, 3);
                Prop(c, o =>
                {
                    var m = Pal.Ramp("#e0dcd0");
                    o.Rect(10, 6, 12, 22, m[2]); o.Rect(10, 6, 3, 22, m[3]); o.Rect(19, 6, 3, 22, m[1]);
                    for (int x = 14; x < 19; x += 2) o.Rect(x, 6, 1, 22, m[1]);
                    o.Rect(8, 2, 16, 4, m[2]); o.Rect(8, 2, 16, 1, m[4]); o.Rect(8, 27, 16, 3, m[1]);
                    o.Rect(8, 3, 16, 1, Pal.Hex("#c03a30"));
                });
                break;
            default: Grass(c, r); break;
        }
    }

    private static void Roof(Canvas c, Rgba[] m, bool temple)
    {
        c.Rect(0, 0, T, T, m[1]);
        for (int row = 0; row < 4; row++)
        {
            int y = row * 8, off = row % 2 == 1 ? 4 : 0;
            for (int x = -8 + off; x < T; x += 8)
            {
                c.Rect(x + 1, y, 6, 6, m[2]);
                c.Rect(x + 1, y, 2, 6, m[3]);
                c.Rect(x + 2, y, 1, 3, m[4]);
                c.Ellipse(x + 4, y + 6.5, 3, 1.5, m[1]);
                c.Rect(x + 7, y, 1, 7, m[0]);
            }
        }
        if (temple)
        {
            c.Rect(0, 27, T, 5, Pal.Hex("#b03c2c"));
            for (int x = 1; x < T; x += 5) { c.Rect(x, 28, 3, 3, Pal.Hex("#2f9a7a")); c.Set(x + 1, 29, Pal.Hex("#f2d050")); }
        }
    }
}
