namespace KotW.ArtGen;

// Monster strips: 3 frames side by side (walk A, walk B, attack), facing right.
// The game flips them for left. Feet sit 2px above the bottom edge.
public static class Monsters
{
    public static readonly Dictionary<string, (int W, int H, Func<int, Canvas> Draw)> All = new()
    {
        ["rabbit"] = (32, 32, Rabbit),
        ["rat"] = (32, 32, Rat),
        ["squirrel"] = (32, 32, Squirrel),
        ["snake"] = (32, 32, Snake),
        ["fox"] = (40, 32, Fox),
        ["wolf"] = (48, 36, Wolf),
        ["boar"] = (48, 36, Boar),
        ["bat"] = (40, 36, Bat),
        ["dokkaebi"] = (44, 52, Dokkaebi),
        ["golem"] = (48, 52, Golem),
        ["tiger"] = (88, 60, Tiger),
    };

    public static Canvas Strip(string id)
    {
        var (w, h, draw) = All[id];
        var strip = new Canvas(w * 3, h);
        for (int f = 0; f < 3; f++) strip.Blit(draw(f), f * w, 0);
        return strip;
    }

    private static Canvas Finish(Canvas c, string outline)
    {
        var o = Pal.Hex(outline);
        c.Outline(_ => o);
        return c;
    }

    private static void Legs(Canvas c, Rgba[] r, double y, int len, params (double X, int Lift)[] legs)
    {
        foreach (var (x, lift) in legs)
        {
            c.Rect((int)x, (int)y - lift, 2, len, r[1]);
            c.Rect((int)x, (int)y - lift, 1, len, r[2]);
        }
    }

    private static Canvas Rabbit(int f)
    {
        var c = new Canvas(32, 32);
        var w = Pal.Ramp("#f4f2ee");
        var pink = Pal.Hex("#f0a0b4");
        int dy = f == 1 ? -3 : 0, dx = f == 2 ? 3 : 0;
        c.Shaded(9 + dx, 22 + dy, 2.4, 2.4, w, 0.3);
        if (f == 1) c.Shaded(10, 27, 3.5, 1.5, w); else c.Shaded(12 + dx, 28, 3.6, 1.7, w);
        c.Shaded(15 + dx, 23 + dy, 7, 5.2, w);
        c.Shaded(19.5 + dx, 28 + (f == 1 ? -1 : 0), 1.6, 1.5, w);
        c.Shaded(21.5 + dx, 18 + dy, 4.6, 4.1, w, 0.1);
        double earBack = f == 2 ? 2 : 0;
        c.Shaded(18.5 + dx - earBack, 11 + dy, 1.5, 4.8, w);
        c.Shaded(21.5 + dx - earBack, 11.5 + dy, 1.5, 4.8, w);
        c.Rect(21 + dx - (int)earBack, 9 + dy, 1, 5, pink);
        c.Rect(23 + dx, 17 + dy, 2, 2, Pal.Hex("#3a1a2a"));
        c.Set(23 + dx, 17 + dy, Pal.Hex("#ffffff"));
        c.Set(26 + dx, 19 + dy, Pal.Hex("#e07088"));
        return Finish(c, "#5a5878");
    }

    private static Canvas Rat(int f)
    {
        var c = new Canvas(32, 32);
        var g = Pal.Ramp("#8e8a98");
        var pink = Pal.Ramp("#e8a0a8");
        int dx = f == 2 ? 3 : 0;
        for (int i = 0; i < 9; i++) c.Set(8 - i * 0.7, 26 - Math.Sin(i / 2.5) * 3 - (f == 1 ? 1 : 0), pink[2]);
        Legs(c, g, 28, 2, (10 + dx, f == 1 ? 1 : 0), (19 + dx, f == 1 ? 0 : 1));
        c.Shaded(15 + dx, 25, 7.5, 4.5, g);
        c.Shaded(15 + dx, 27.5, 5, 1.5, Pal.Ramp("#c8c4cc"));
        c.Shaded(22.5 + dx, 24.5, 4, 3.4, g, 0.1);
        c.Shaded(25.5 + dx, 25.5, 2.2, 1.6, g, 0.2);
        c.Shaded(20.5 + dx, 20.5, 2.2, 2.2, g);
        c.Ellipse(20.5 + dx, 20.5, 1.1, 1.1, pink[2]);
        c.Set(27 + dx, 25, pink[1]);
        c.Rect(23 + dx, 23, 1, 2, Pal.Hex("#1a1018"));
        return Finish(c, "#3a3444");
    }

    private static Canvas Squirrel(int f)
    {
        var c = new Canvas(32, 32);
        var o = Pal.Ramp("#c07a3a");
        var cream = Pal.Ramp("#f0dcb4");
        int dy = f == 1 ? -2 : 0, dx = f == 2 ? 3 : 0;
        double sway = f == 1 ? 1 : 0;
        c.Shaded(8 - sway, 16 + dy, 4.6, 7.6, o, 0.1);
        for (int y = 10; y < 22; y += 3) c.Line(6 - sway, y + dy, 10 - sway, y + 1 + dy, o[1]);
        c.Shaded(14 + dx, 23 + dy, 5.4, 5.4, o);
        c.Shaded(15.5 + dx, 25 + dy, 2.8, 3, cream);
        c.Line(11 + dx, 19 + dy, 16 + dx, 20 + dy, Pal.Hex("#4a2a18"));
        c.Line(10 + dx, 22 + dy, 15 + dx, 22 + dy, Pal.Hex("#4a2a18"));
        c.Shaded(19.5 + dx, 18 + dy, 4.2, 3.9, o, 0.1);
        c.Shaded(17.5 + dx, 14 + dy, 1.6, 1.8, o);
        c.Rect(21 + dx, 17 + dy, 2, 2, Pal.Hex("#1a1018"));
        c.Set(21 + dx, 17 + dy, Pal.Hex("#ffffff"));
        c.Set(23 + dx, 20 + dy, Pal.Hex("#3a2010"));
        c.Shaded(14 + dx, 28.5, 3, 1.4, o);
        c.Shaded(20 + dx, 23 + dy, 1.4, 1.4, cream);
        return Finish(c, "#4a2a18");
    }

    private static Canvas Snake(int f)
    {
        var c = new Canvas(32, 32);
        var g = Pal.Ramp("#5aa848");
        var belly = Pal.Ramp("#e8d878");
        double sw = f == 1 ? 1.2 : 0;
        int dx = f == 2 ? 3 : 0;
        for (int i = 0; i <= 22; i++)
        {
            double t = i / 22.0;
            double x = 5 + t * 16 + dx * t, y = 27 - Math.Sin(t * Math.PI * 1.6 + sw) * 2.6 - t * 3;
            c.Shaded(x, y, 2.6, 2.6, g);
        }
        for (int i = 0; i <= 22; i += 2)
        {
            double t = i / 22.0;
            c.Set(5 + t * 16 + dx * t, 28.5 - Math.Sin(t * Math.PI * 1.6 + sw) * 2.6 - t * 3, belly[2]);
        }
        for (int i = 0; i < 8; i++) c.Shaded(21 + dx + i * 0.1, 22 - i * 1.0, 2.2, 2, g);
        c.Shaded(23.5 + dx, 13.5, 3.8, 2.8, g, 0.1);
        c.Rect(24 + dx, 12, 1, 2, Pal.Hex("#e8c030"));
        c.Rect(25 + dx, 12, 1, 2, Pal.Hex("#1a1010"));
        for (int i = 0; i < 12; i += 3) c.Set(9 + i, 25 - (i % 2), Pal.Hex("#d8b030"));
        if (f != 1) { c.Rect(27 + dx, 14, 3, 1, Pal.Hex("#e03838")); c.Set(30 + dx, 13, Pal.Hex("#e03838")); c.Set(30 + dx, 15, Pal.Hex("#e03838")); }
        return Finish(c, "#1e3a1a");
    }

    private static Canvas Fox(int f)
    {
        var c = new Canvas(40, 32);
        var o = Pal.Ramp("#e07a30");
        var w = Pal.Ramp("#f6eee0");
        var sock = Pal.Ramp("#3a2420");
        int dx = f == 2 ? 3 : 0;
        int a = f == 1 ? 2 : 0;
        c.Shaded(9, 17 - a * 0.5, 7, 3.8, o, 0.05);
        c.Shaded(3.5, 15.5 - a * 0.5, 3, 2.6, w);
        Legs(c, sock, 23, 6, (13 + dx - a, 0), (16 + dx + a, 0), (23 + dx + a, 0), (26 + dx - a, 0));
        c.Shaded(19 + dx, 20, 8.5, 4.4, o);
        c.Shaded(24 + dx, 21, 3.4, 3, w);
        c.Shaded(29 + dx, 15, 4.6, 4, o, 0.1);
        c.Shaded(33 + dx, 16.5, 3, 1.8, o, 0.15);
        c.Shaded(32 + dx, 17.5, 2.5, 1.2, w);
        c.Poly(new (double, double)[] { (25.5 + dx, 13), (27 + dx, 7), (29.5 + dx, 12) }, o[2]);
        c.Poly(new (double, double)[] { (28.5 + dx, 12), (30.5 + dx, 6.5), (31.5 + dx, 12) }, o[3]);
        c.Set(27 + dx, 10, Pal.Hex("#3a2420"));
        c.Rect(30 + dx, 14, 1, 2, Pal.Hex("#1a1010"));
        c.Set(35 + dx, 16, Pal.Hex("#1a1010"));
        return Finish(c, "#4a2010");
    }

    private static Canvas Wolf(int f)
    {
        var c = new Canvas(48, 36);
        var g = Pal.Ramp("#8a8c98");
        var l = Pal.Ramp("#cfcfd6");
        var d = Pal.Ramp("#55565f");
        int dx = f == 2 ? 3 : 0;
        int a = f == 1 ? 2 : 0;
        c.Shaded(8, 20, 7, 3.6, g, -0.05);
        c.Shaded(3.5, 21.5, 2.6, 2.2, l);
        Legs(c, d, 25, 8, (15 + dx - a, 0), (18 + dx + a, 0), (28 + dx + a, 0), (31 + dx - a, 0));
        c.Shaded(23 + dx, 21, 11, 5.6, g);
        c.Shaded(22 + dx, 17.5, 8, 2.4, d, 0.1);
        c.Shaded(23 + dx, 24.5, 8, 2, l);
        c.Shaded(32 + dx, 18, 4.4, 5.6, g, 0.05);
        c.Shaded(36 + dx, 14, 4.8, 4.2, g, 0.1);
        c.Shaded(41 + dx, 16, 3.4, 2.1, g, 0.15);
        c.Shaded(40 + dx, 17.5, 3, 1.2, l);
        c.Poly(new (double, double)[] { (32.5 + dx, 12), (34 + dx, 5), (36.5 + dx, 11) }, g[2]);
        c.Poly(new (double, double)[] { (35.5 + dx, 11), (37.5 + dx, 4.5), (39 + dx, 11) }, g[3]);
        c.Rect(38 + dx, 13, 2, 1, Pal.Hex("#f0d040"));
        c.Set(39 + dx, 13, Pal.Hex("#1a1010"));
        c.Set(44 + dx, 15, Pal.Hex("#1a1010"));
        if (f == 2) { c.Rect(40 + dx, 18, 4, 1, Pal.Hex("#7a2030")); c.Set(41 + dx, 17, Pal.Hex("#ffffff")); }
        return Finish(c, "#2a2a34");
    }

    private static Canvas Boar(int f)
    {
        var c = new Canvas(48, 36);
        var b = Pal.Ramp("#6a4a34");
        var d = Pal.Ramp("#3e2a1e");
        var snout = Pal.Ramp("#b88070");
        var tusk = Pal.Ramp("#f4eedc");
        int dx = f == 2 ? 3 : 0;
        int a = f == 1 ? 2 : 0;
        Legs(c, d, 27, 6, (11 + dx - a, 0), (15 + dx + a, 0), (27 + dx + a, 0), (31 + dx - a, 0));
        c.Shaded(21 + dx, 22, 13, 7.6, b);
        for (int x = 11; x <= 31; x += 2) c.Line(x + dx, 14.5 + Math.Abs(x - 21) * 0.08, x + 1 + dx, 12.5 + Math.Abs(x - 21) * 0.1, d[2]);
        c.Shaded(34 + dx, 22.5, 6, 5.4, b, 0.05);
        c.Shaded(40 + dx, 24.5, 2.8, 2.8, snout, 0.1);
        c.Set(41 + dx, 24, snout[0]); c.Set(41 + dx, 25.5, snout[0]);
        c.Poly(new (double, double)[] { (30 + dx, 17), (31.5 + dx, 12.5), (33.5 + dx, 17) }, b[1]);
        c.Line(37 + dx, 27, 39 + dx, 22.5, tusk[2], 1.6);
        c.Set(39 + dx, 22, tusk[4]);
        c.Rect(36 + dx, 20, 1, 2, Pal.Hex("#1a0e0a"));
        c.Line(8 + dx, 20, 6 + dx, 18, d[2]);
        return Finish(c, "#1e140e");
    }

    private static Canvas Bat(int f)
    {
        var c = new Canvas(40, 36);
        var p = Pal.Ramp("#5e4a78");
        var m = Pal.Ramp("#8a6a9a");
        double up = f == 1 ? -1 : 1;
        var wingL = f == 1
            ? new (double, double)[] { (17, 16), (5, 6), (2, 14), (6, 13), (8, 19), (12, 17), (15, 21) }
            : new (double, double)[] { (17, 16), (4, 20), (3, 27), (8, 23), (10, 27), (13, 23), (16, 23) };
        if (f == 2) wingL = new (double, double)[] { (17, 15), (1, 10), (2, 18), (6, 16), (8, 21), (12, 18), (15, 21) };
        c.Poly(wingL, m[1]);
        c.Poly(wingL.Select(pt => (40 - pt.Item1, pt.Item2)).ToArray(), m[2]);
        foreach (var pt in wingL.Skip(1).Take(3)) { c.Line(17, 16, pt.Item1, pt.Item2, p[0]); c.Line(23, 16, 40 - pt.Item1, pt.Item2, p[0]); }
        c.Shaded(20, 18, 4.4, 5.6, p);
        c.Shaded(20, 12.5 + up * 0.2, 3.8, 3.4, p, 0.1);
        c.Poly(new (double, double)[] { (16.5, 11), (17, 6), (19, 10) }, p[2]);
        c.Poly(new (double, double)[] { (21, 10), (23, 6), (23.5, 11) }, p[2]);
        c.Rect(18, 12, 1, 1, Pal.Hex("#ff4040")); c.Rect(21, 12, 1, 1, Pal.Hex("#ff4040"));
        if (f == 2) { c.Rect(19, 14, 2, 2, Pal.Hex("#4a1020")); c.Set(19, 14, Pal.Hex("#ffffff")); c.Set(20, 14, Pal.Hex("#ffffff")); }
        return Finish(c, "#1c1424");
    }

    private static Canvas Dokkaebi(int f)
    {
        var c = new Canvas(44, 52);
        var skin = Pal.Ramp("#d0503c");
        var hair = Pal.Ramp("#2a3050");
        var cloth = Pal.Ramp("#e8b838");
        var horn = Pal.Ramp("#f2e4b8");
        var club = Pal.Ramp("#8a5e34");
        int a = f == 1 ? 1 : 0;
        // Legs
        c.Rect(15 - a, 38, 4, 9, skin[1]); c.Rect(23 + a, 38, 4, 9, skin[2]);
        c.Rect(14 - a, 46, 6, 3, skin[0]); c.Rect(22 + a, 46, 6, 3, skin[1]);
        // Tiger-striped loincloth
        c.Poly(new (double, double)[] { (12, 34), (30, 34), (29, 42), (13, 42) }, cloth[2]);
        for (int x = 14; x < 30; x += 4) c.Line(x, 35, x + 1, 41, Pal.Hex("#2a1a10"));
        // Back arm
        c.Shaded(12, 28, 3, 6, skin, -0.1);
        // Torso and belly
        c.Shaded(21, 28, 9.5, 8.5, skin);
        c.Shaded(22, 31, 5, 4, skin, 0.15);
        // Head
        c.Shaded(23, 14, 8, 7.4, skin, 0.05);
        c.Shaded(18, 10, 7.5, 5, hair, 0.05);
        c.Rect(13, 8, 3, 8, hair[1]);
        for (int k = 0; k < 4; k++) c.Line(13 + k * 2, 6, 11 + k * 2.5, 2 + (k % 2), hair[2]);
        c.Poly(new (double, double)[] { (24, 8), (27, -0.5), (29, 8) }, horn[2]);
        c.Line(26, 7, 27, 1, horn[4]);
        // Face: one big eye, grin with fangs
        c.Shaded(27, 13, 2.6, 2.4, Pal.Ramp("#f8f0d0"));
        c.Rect(27, 12, 2, 2, Pal.Hex("#1a1010"));
        c.Rect(23, 11, 6, 1, hair[0]);
        c.Rect(23, 18, 9, 2, Pal.Hex("#4a1010"));
        for (int x = 23; x < 32; x += 2) c.Set(x, 18, Pal.Hex("#ffffff"));
        c.Set(24, 20, Pal.Hex("#ffffff")); c.Set(30, 20, Pal.Hex("#ffffff"));
        // Front arm with a studded club
        double ang = f == 2 ? 1.6 : f == 1 ? -0.35 : -0.15;
        double hx = f == 2 ? 34 : 31, hy = f == 2 ? 30 : 33;
        c.Shaded(hx - 2, hy - 3, 3, 5, skin);
        double ux = Math.Sin(ang), uy = -Math.Cos(ang);
        for (int k = 0; k < 16; k++)
        {
            double r = 1.2 + k * 0.17;
            c.Ellipse(hx + ux * k, hy + uy * k, r, r, club[k > 10 ? 3 : 2]);
        }
        for (int k = 8; k < 16; k += 3)
        {
            c.Set(hx + ux * k + uy * 3, hy + uy * k - ux * 3, Pal.Hex("#c8ccd4"));
            c.Set(hx + ux * k - uy * 3, hy + uy * k + ux * 3, Pal.Hex("#c8ccd4"));
        }
        c.Shaded(hx, hy, 2, 2, skin, 0.1);
        return Finish(c, "#2a0e10");
    }

    private static Canvas Golem(int f)
    {
        var c = new Canvas(48, 52);
        var s = Pal.Ramp("#8e8c84");
        var d = Pal.Ramp("#6a6a66");
        var moss = Pal.Ramp("#5a8a48");
        var glow = Pal.Hex("#70f0ff");
        int a = f == 1 ? 1 : 0;
        c.Shaded(17 - a, 44, 5, 5, d); c.Shaded(31 + a, 44, 5, 5, d);
        c.Shaded(10, 28 + (f == 2 ? -4 : 0), 5, 7, d);
        c.Shaded(24, 28, 12, 11, s);
        c.Shaded(20, 20, 8, 3, moss, 0.1);
        c.Shaded(25, 11, 7, 6, s, 0.1);
        c.Shaded(23, 6.5, 5, 1.8, moss, 0.2);
        c.Rect(23, 10, 2, 2, glow); c.Rect(28, 10, 2, 2, glow);
        c.Line(20, 27, 25, 31, glow); c.Line(25, 31, 23, 36, glow); c.Line(28, 24, 31, 28, glow);
        double fx = f == 2 ? 41 : 37, fy = f == 2 ? 24 : 29;
        c.Shaded(fx, fy, 5.5, 7, s, 0.05);
        c.Shaded(fx + (f == 2 ? 2 : 0), fy + 6, 4, 3, d);
        return Finish(c, "#26262a");
    }

    private static Canvas Tiger(int f)
    {
        var c = new Canvas(88, 60);
        var o = Pal.Ramp("#e8902c");
        var w = Pal.Ramp("#f8f0e0");
        var k = Pal.Hex("#2a1a14");
        int a = f == 1 ? 3 : 0;
        bool roar = f == 2;
        // Tail
        for (int i = 0; i < 18; i++)
        {
            double t = i / 17.0;
            double x = 18 - t * 12, y = 32 - Math.Sin(t * 2.4) * 16 - (f == 1 ? t * 3 : 0);
            c.Shaded(x, y, 2.6, 2.6, i % 4 == 1 ? Pal.Ramp("#3a2418") : o);
        }
        // Back legs
        c.Shaded(26 - a, 46, 5, 9, o, -0.1); c.Shaded(34 + a, 47, 5, 8, o);
        // Body
        c.Shaded(41, 35, 23, 12, o);
        c.Shaded(43, 43, 17, 4, w);
        foreach (var sx in new[] { 26, 33, 40, 47, 54 })
        {
            c.Line(sx, 24, sx - 2, 31, k);
            c.Line(sx - 2, 31, sx + 1, 36, k);
            c.Line(sx + 2, 25, sx + 1, 29, k);
        }
        // Front legs
        double lift = roar ? -8 : 0;
        c.Shaded(54 + a, 48 + lift * 0.5, 5, 9, o, 0.05); c.Shaded(62 - a, 49 + lift, 5, 8, o, 0.1);
        c.Shaded(54 + a, 55 + lift * 0.5, 4, 2, w); c.Shaded(62 - a, 55 + lift, 4, 2, w);
        // Head
        double hx = roar ? 69 : 68, hy = roar ? 22 : 24;
        c.Shaded(hx, hy, 12, 11, o, 0.05);
        c.Shaded(hx + 8, hy + 5, 6, 5, w, 0.1);
        c.Shaded(hx - 6, hy + 4, 5, 5, w, 0.05);
        c.Shaded(hx - 6, hy - 10, 3.5, 3.5, o); c.Shaded(hx + 4, hy - 11, 3.5, 3.5, o);
        c.Ellipse(hx - 6, hy - 10, 1.5, 1.5, w[1]); c.Ellipse(hx + 4, hy - 11, 1.5, 1.5, w[1]);
        c.Line(hx - 3, hy - 9, hx - 1, hy - 4, k); c.Line(hx + 2, hy - 9, hx + 1, hy - 4, k);
        c.Line(hx - 9, hy - 2, hx - 5, hy, k); c.Line(hx - 10, hy + 2, hx - 6, hy + 3, k);
        c.Rect((int)hx + 1, (int)hy - 3, 4, 2, Pal.Hex("#ffd040")); c.Rect((int)hx + 3, (int)hy - 3, 1, 2, k);
        c.Rect((int)hx - 5, (int)hy - 3, 3, 2, Pal.Hex("#ffd040")); c.Rect((int)hx - 4, (int)hy - 3, 1, 2, k);
        c.Shaded(hx + 11, hy + 2, 2, 1.6, Pal.Ramp("#d06070"));
        if (roar)
        {
            c.Ellipse(hx + 7, hy + 8, 5, 3.5, Pal.Hex("#5a1018"));
            c.Rect((int)hx + 4, (int)hy + 5, 1, 3, Pal.Hex("#ffffff")); c.Rect((int)hx + 9, (int)hy + 5, 1, 3, Pal.Hex("#ffffff"));
        }
        else c.Line(hx + 5, hy + 8, hx + 11, hy + 7, k);
        return Finish(c, "#2a1208");
    }
}
