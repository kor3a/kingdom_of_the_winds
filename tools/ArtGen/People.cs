using KotW.Shared;

namespace KotW.ArtGen;

// Paper-doll character layers. Every sheet is 6 columns x 4 rows of 32x48 frames:
// columns = idle, walk1, walk2, walk3, attack wind-up, attack strike
// rows    = down, left, right, up (left is the mirror of right).
// Skin and hair are drawn in grey and tinted in game; gear is drawn per item in colour.
public static class People
{
    public const int FW = 32, FH = 48, Cols = 6;

    public sealed record Pose(int Bob, int LegL, int LegR, int Stride, double HX, double HY, double Angle, double BX, double BY);

    public static Pose GetPose(Dir d, int f) => d switch
    {
        Dir.Up => f switch
        {
            1 => new(0, 0, -2, 0, 23.5, 37, 0, 8.5, 35),
            2 => new(-1, 0, 0, 0, 23.5, 35, 0, 8.5, 35),
            3 => new(0, -2, 0, 0, 23.5, 35, 0, 8.5, 37),
            4 => new(0, 0, 0, 0, 22.5, 28, 35, 8.5, 35),
            5 => new(1, 0, 0, 0, 19.5, 27, -5, 8.5, 36),
            _ => new(0, 0, 0, 0, 23.5, 36, 0, 8.5, 36),
        },
        Dir.Right or Dir.Left => f switch
        {
            1 => new(0, 0, 0, 2, 19, 35, 40, 0, 0),
            2 => new(-1, 0, 0, 0, 16.5, 35, 25, 0, 0),
            3 => new(0, 0, 0, -2, 13.5, 35, 10, 0, 0),
            4 => new(0, 0, 0, 1, 12.5, 29, -70, 0, 0),
            5 => new(1, 0, 0, 2, 22.5, 33, 100, 0, 0),
            _ => new(0, 0, 0, 0, 16.5, 36, 25, 0, 0),
        },
        _ => f switch
        {
            1 => new(0, 0, -2, 0, 8.5, 37, 0, 23.5, 35),
            2 => new(-1, 0, 0, 0, 8.5, 35, 0, 23.5, 35),
            3 => new(0, -2, 0, 0, 8.5, 35, 0, 23.5, 37),
            4 => new(0, 0, 0, 0, 9.5, 27, -30, 23.5, 36),
            5 => new(1, 0, 0, 0, 11.5, 40, 165, 23.5, 36),
            _ => new(0, 0, 0, 0, 8.5, 36, 0, 23.5, 36),
        },
    };

    private static readonly Dir[] Rows = { Dir.Down, Dir.Left, Dir.Right, Dir.Up };

    // Builds a full sheet from a per-frame painter (which only needs to handle down, right and up).
    public static Canvas Sheet(Func<Dir, int, Canvas> frame)
    {
        var sheet = new Canvas(FW * Cols, FH * Rows.Length);
        for (int r = 0; r < Rows.Length; r++)
            for (int f = 0; f < Cols; f++)
            {
                var d = Rows[r];
                var c = d == Dir.Left ? frame(Dir.Right, f).Mirror() : frame(d, f);
                sheet.Blit(c, f * FW, r * FH);
            }
        return sheet;
    }

    private static Canvas New() => new(FW, FH);
    private static readonly Func<Rgba, Rgba> GrayOutline = c => Pal.Gray(0.02);

    // ---- skin (grey, tinted in game) -----------------------------------------------
    public static Canvas Skin(Dir d, int f)
    {
        var p = GetPose(d, f);
        var g = Pal.GrayRamp;
        var head = New();
        double cx = d == Dir.Right ? 16.5 : 16;
        head.Shaded(cx, 19.5 + p.Bob, 7, 6.8, g, 0.1);
        head.Rect(14 + (d == Dir.Right ? 1 : 0), 26 + p.Bob, 4, 2, g[1]);
        if (d == Dir.Right)
        {
            head.Ellipse(14.5, 21 + p.Bob, 1.4, 1.8, g[1]); head.Set(14, 21 + p.Bob, g[2]);
            head.Set(23, 22 + p.Bob, g[2]); // nose, so the profile isn't a plain circle
        }
        head.Outline(GrayOutline);
        var hands = New();
        if (d == Dir.Right) hands.Shaded(p.HX, p.HY, 1.7, 1.7, g);
        else { hands.Shaded(p.HX, p.HY, 1.6, 1.6, g); hands.Shaded(p.BX, p.BY, 1.6, 1.6, g); }
        hands.Outline(GrayOutline);
        head.Blit(hands, 0, 0);
        return head;
    }

    // ---- base body: undershirt, trousers, shoes and face (fixed colours) -------------
    private static readonly Rgba[] Shirt = Pal.Ramp("#ece4d0");
    private static readonly Rgba[] Pants = Pal.Ramp("#d4cfc0");
    private static readonly Rgba[] Shoe = Pal.Ramp("#463a44");
    private static readonly Rgba Tie = Pal.Hex("#6a5868");

    public static Canvas Body(Dir d, int f)
    {
        var p = GetPose(d, f);
        var c = New();
        // Legs
        var legs = New();
        if (d == Dir.Right)
        {
            int back = 13 - p.Stride, front = 16 + p.Stride;
            foreach (var (lx, shade) in new[] { (back, 1), (front, 2) })
            {
                legs.Rect(lx, 37, 4, 6, Pants[shade]);
                legs.Rect(lx, 42, 4, 1, Tie);
                legs.Rect(lx, 43, 5, 3, Shoe[shade]);
                legs.Set(lx + 5, 43, Shoe[shade]);
            }
        }
        else
        {
            foreach (var (lx, lift, light) in new[] { (11, p.LegL, true), (17, p.LegR, false) })
            {
                legs.Rect(lx, 37, 4, 6 + lift, Pants[2]);
                legs.Rect(light ? lx : lx + 3, 37, 1, 6 + lift, Pants[light ? 3 : 1]);
                legs.Rect(lx, 42 + lift, 4, 1, Tie);
                legs.Rect(lx, 43 + lift, 4, 3, Shoe[2]);
                legs.Rect(lx, 43 + lift, 4, 1, Shoe[3]);
            }
        }
        legs.Outline();
        c.Blit(legs, 0, 0);
        // Undershirt so a character without armour is still dressed
        c.Blit(Torso(d, p, Shirt, Shirt, null, false, false), 0, 0);
        // Face
        int b = p.Bob;
        // Eyes are 2x3: a dark lash line, a catchlight over the iris, and a lighter iris below.
        var lash = Pal.Hex("#1c1218");
        var iris = Pal.Hex("#3c2832");
        var irisLow = Pal.Hex("#6c4a54");
        var shine = Pal.Hex("#fffaf0");
        var blush = Pal.Hex("#f07a7a").WithA(120);
        var mouth = Pal.Hex("#a04a4a");
        void Eye(int x)
        {
            c.Rect(x, 21 + b, 2, 1, lash);
            c.Set(x, 22 + b, shine); c.Set(x + 1, 22 + b, iris);
            c.Rect(x, 23 + b, 2, 1, irisLow);
        }
        if (d == Dir.Down)
        {
            Eye(11); Eye(19);
            c.Set(10, 21 + b, lash); c.Set(21, 21 + b, lash);
            c.Rect(10, 24 + b, 2, 1, blush); c.Rect(20, 24 + b, 2, 1, blush);
            c.Rect(15, 25 + b, 2, 1, mouth);
        }
        else if (d == Dir.Right)
        {
            Eye(20);
            c.Rect(19, 24 + b, 2, 1, blush);
            c.Set(22, 25 + b, mouth);
        }
        return c;
    }

    // Torso, collar and sleeves. Shared by the undershirt and every armour piece.
    private static Canvas Torso(Dir d, Pose p, Rgba[] cloth, Rgba[] trim, Rgba? ribbon, bool robe, bool plate)
    {
        int b = p.Bob;
        var back = New();
        var torso = New();
        var sleeves = New();
        if (d == Dir.Right)
        {
            if (robe)
                torso.Poly(new (double, double)[] { (12, 27 + b), (21, 27 + b), (21.5, 43), (10.5, 43) }, cloth[2]);
            else torso.Rect(12, 27 + b, 9, 11, cloth[2]);
            torso.Rect(12, 27 + b, 2, robe ? 16 - b : 11, cloth[1]);
            torso.Rect(19, 27 + b, 2, robe ? 16 - b : 11, cloth[3]);
            if (plate) Scales(torso, 12, 29 + b, 9, robe ? 12 : 8, cloth);
            torso.Line(19, 27 + b, 20, 30 + b, trim[3]);
            torso.Line(18, 27 + b, 19, 31 + b, trim[2]);
            if (!robe) torso.Rect(12, 36 + b, 9, 1, cloth[1]);
            Sash(torso, 12, 9, (robe ? 33 : 34) + b, cloth, plate);
            if (ribbon is { } r) { torso.Set(20, 31 + b, r); torso.Rect(20, 32 + b, 1, 4, r); torso.Set(21, 33 + b, r); }
            // Near arm: shoulder to hand
            double sx = 16, sy = 29 + b;
            ArmLine(sleeves, sx, sy, p.HX, p.HY, cloth);
        }
        else
        {
            if (robe)
                torso.Poly(new (double, double)[] { (10, 27 + b), (22, 27 + b), (23, 43), (9, 43) }, cloth[2]);
            else torso.Rect(10, 27 + b, 12, 11, cloth[2]);
            torso.Rect(10, 27 + b, 2, robe ? 16 - b : 11, cloth[3]);
            torso.Rect(20, 27 + b, 2, robe ? 16 - b : 11, cloth[1]);
            if (plate) Scales(torso, 10, 29 + b, 12, robe ? 12 : 8, cloth);
            Sash(torso, 10, 12, (robe ? 33 : 34) + b, cloth, plate);
            if (d == Dir.Down)
            {
                torso.Line(12, 27 + b, 16.5, 32 + b, trim[3]);
                torso.Line(13, 27 + b, 17, 31.5 + b, trim[2]);
                torso.Line(20, 27 + b, 17.5, 29.5 + b, trim[2]);
                if (robe) torso.Line(16.5, 32 + b, 15.5, 43, cloth[1]);
                if (ribbon is { } r) { torso.Set(17, 32 + b, r); torso.Set(18, 32 + b, r); torso.Rect(17, 33 + b, 1, 4, r); torso.Rect(19, 33 + b, 1, 3, r); }
            }
            else torso.Rect(13, 27 + b, 6, 1, trim[2]);
            if (!robe) torso.Rect(10, 36 + b, 12, 1, cloth[1]);
            // Each hand hangs from the shoulder on its own side (the weapon hand switches sides when facing up).
            ArmLine(sleeves, p.HX < 16 ? 9 : 23, 29 + b, p.HX, p.HY, cloth);
            ArmLine(sleeves, p.BX < 16 ? 9 : 23, 29 + b, p.BX, p.BY, cloth);
            if (plate)
            {
                sleeves.Shaded(9, 29.5 + b, 3, 2.4, cloth);
                sleeves.Shaded(23, 29.5 + b, 3, 2.4, cloth);
            }
        }
        torso.Outline();
        sleeves.Outline();
        back.Blit(torso, 0, 0);
        back.Blit(sleeves, 0, 0);
        return back;
    }

    // A sleeve that widens toward a cuff band in a deep shade of the cloth, the way hanbok sleeves flare.
    private static void ArmLine(Canvas c, double sx, double sy, double hx, double hy, Rgba[] cloth)
    {
        double dx = hx - sx, dy = hy - sy, len = Math.Max(0.01, Math.Sqrt(dx * dx + dy * dy));
        double ux = dx / len, uy = dy / len;
        double ex = hx - ux * 2.2, ey = hy - uy * 2.2;
        var cuff = Pal.Ramp(Pal.Shift(cloth[2], -0.42));
        c.Line(sx, sy, ex, ey, cloth[2], 3.4);
        c.Line(sx + (ex - sx) * 0.5, sy + (ey - sy) * 0.5, ex - ux, ey - uy, cloth[2], 3.8);
        c.Line(sx - 0.7, sy - 0.7, ex - ux - 0.7, ey - uy - 0.7, cloth[3], 1);
        c.Line(sx + 0.8, sy + 0.8, ex - ux + 0.8, ey - uy + 0.8, cloth[1], 1);
        c.Line(ex - ux * 0.6, ey - uy * 0.6, ex, ey, cuff[2], 3.6);
        c.Line(ex - ux * 0.6 - 0.7, ey - uy * 0.6 - 0.7, ex - ux * 0.6 - 0.7, ey - uy * 0.6 - 0.7, cuff[3], 1);
    }

    // A waist sash in a deep shade of the cloth (a leather belt with a buckle on plate).
    private static void Sash(Canvas c, int x, int w, int y, Rgba[] cloth, bool plate)
    {
        var s = plate ? Pal.Ramp("#5a3a28") : Pal.Ramp(Pal.Shift(cloth[2], -0.42));
        for (int xx = x; xx < x + w; xx++)
        {
            if (c.Get(xx, y).A == 0) continue;
            c.Set(xx, y, s[2]); c.Set(xx, y + 1, s[1]);
        }
        if (plate) { c.Rect(x + w / 2 - 1, y, 2, 2, Pal.Hex("#e0b04a")); c.Set(x + w / 2 - 1, y, Pal.Hex("#fff0a0")); }
    }

    private static void Scales(Canvas c, int x0, int y0, int w, int h, Rgba[] m)
    {
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int px = x0 + x, py = y0 + y;
                if (c.Get(px, py).A == 0) continue;
                bool edge = y % 3 == 2 || (x + (y / 3 % 2) * 2) % 4 == 0;
                c.Set(px, py, edge ? m[1] : (y % 3 == 0 ? m[3] : m[2]));
            }
    }

    // ---- armour --------------------------------------------------------------------
    public static Canvas Armor(ItemDef it, Dir d, int f)
    {
        var cloth = Pal.Ramp(it.Color);
        var (_, _, v) = Brightness(Pal.Hex(it.Color));
        var trim = Pal.Ramp(it.Look == "plate" ? "#b8c0cc" : v > 0.75 ? "#f6f2e8" : "#ece6d4");
        Rgba? ribbon = it.Look == "plate" ? null : Pal.Hex(v > 0.6 ? "#b8304a" : "#e8b44a");
        return Torso(d, GetPose(d, f), cloth, trim, ribbon, it.Robe, it.Look == "plate");
    }

    private static (double, double, double) Brightness(Rgba c) => (0, 0, Math.Max(c.R, Math.Max(c.G, c.B)) / 255.0);

    // ---- hair (grey, tinted in game) ------------------------------------------------
    public static Canvas Hair(string style, Dir d, int f)
    {
        var c = New();
        if (style == "bald") return c;
        var p = GetPose(d, f);
        var g = Pal.GrayRamp;
        int b = p.Bob;
        if (d == Dir.Up)
        {
            c.Shaded(16, 19 + b, 7.4, 7.3, g, -0.05);
            if (style == "long") { c.Rect(9, 24 + b, 14, 10, g[2]); c.Rect(9, 24 + b, 2, 10, g[3]); c.Rect(20, 24 + b, 3, 10, g[1]); }
            // Strands gathered toward the crown, and a shine band
            c.Line(12, 15 + b, 11, 23 + b, g[1]); c.Line(16, 14 + b, 16, 24 + b, g[1]); c.Line(20, 15 + b, 21, 23 + b, g[1]);
            c.Line(11, 15 + b, 13, 13 + b, g[4]); c.Set(14, 13 + b, g[3]);
            if (style == "long") { c.Line(13, 26 + b, 13, 33 + b, g[1]); c.Line(18, 26 + b, 18, 33 + b, g[1]); }
            if (style == "bun") c.Shaded(16, 25 + b, 3.4, 2.6, g, 0.15);
        }
        else if (d == Dir.Right)
        {
            var cap = New();
            cap.Shaded(15.5, 18 + b, 7.3, 6.2, g, 0.05);
            // Bangs over the forehead, cut short enough to leave the eye clear
            int[] bangs = { 18, 19, 18, 17, 17, 17 };
            for (int x = 18; x < FW; x++)
            {
                int bottom = (x - 18 < bangs.Length ? bangs[x - 18] : 17) + b;
                for (int y = bottom; y < FH; y++) cap.Erase(x, y);
            }
            cap.Rect(9, 19 + b, 4, 5, g[2]);
            cap.Rect(9, 19 + b, 1, 5, g[1]);
            cap.Line(17, 13 + b, 19, 18 + b, g[1]);
            cap.Line(12, 14 + b, 10, 19 + b, g[1]);
            cap.Line(12, 13 + b, 15, 12 + b, g[4]); cap.Set(11, 14 + b, g[3]);
            c.Blit(cap, 0, 0);
            if (style == "long") { c.Rect(9, 22 + b, 5, 12, g[2]); c.Rect(9, 22 + b, 1, 12, g[1]); c.Rect(13, 24 + b, 1, 10, g[3]); c.Line(11, 25 + b, 11, 33 + b, g[1]); }
            if (style == "bun") c.Shaded(10, 22 + b, 3, 2.8, g, 0.15);
        }
        else
        {
            var cap = New();
            cap.Shaded(16, 17.5 + b, 7.7, 5.6, g, 0.05);
            // Bangs: locks of uneven length with a parting just left of centre
            int[] bangs = { 18, 19, 18, 17, 16, 17, 18, 19, 18, 17, 18 }; // x = 11..21
            for (int x = 0; x < FW; x++)
            {
                int bottom = (x >= 11 && x <= 21 ? bangs[x - 11] : 18) + b;
                for (int y = bottom; y < FH; y++) cap.Erase(x, y);
            }
            cap.Rect(8, 17 + b, 2, 6, g[2]); cap.Rect(22, 17 + b, 2, 6, g[1]);
            cap.Set(8, 23 + b, g[2]); cap.Set(23, 23 + b, g[1]);
            if (style == "long") { cap.Rect(7, 18 + b, 3, 16, g[2]); cap.Rect(7, 18 + b, 1, 16, g[3]); cap.Rect(22, 18 + b, 3, 16, g[1]); }
            cap.Line(15, 13 + b, 14, 17 + b, g[1]);
            cap.Line(19, 14 + b, 18, 18 + b, g[1]);
            cap.Line(12, 15 + b, 11, 17 + b, g[1]);
            cap.Line(10, 15 + b, 12, 13 + b, g[4]); cap.Set(13, 13 + b, g[3]); cap.Set(19, 13 + b, g[3]);
            c.Blit(cap, 0, 0);
        }
        if (style == "topknot")
        {
            double kx = d == Dir.Right ? 14.5 : 16;
            c.Shaded(kx, 11.2 + b, 2.6, 2.4, g, 0.1);
            c.Rect((int)kx - 2, 13 + b, 4, 1, g[1]);
            c.Set(kx - 1, 10 + b, g[4]);
        }
        if (style == "bun" && d == Dir.Down) c.Shaded(16, 12.5 + b, 3.4, 2, g, 0.1);
        c.Outline(GrayOutline);
        return c;
    }

    // ---- headgear ------------------------------------------------------------------
    public static Canvas Helmet(ItemDef it, Dir d, int f)
    {
        var c = New();
        var p = GetPose(d, f);
        var m = Pal.Ramp(it.Color);
        int b = p.Bob;
        double cx = d == Dir.Right ? 16 : 16;
        switch (it.Look)
        {
            case "straw":
            {
                c.Shaded(cx, 15.5 + b, 11.5, 3.2, m, 0.15);
                c.Poly(new (double, double)[] { (cx - 6, 15 + b), (cx, 6 + b), (cx + 6, 15 + b) }, m[2]);
                c.Poly(new (double, double)[] { (cx - 6, 15 + b), (cx, 6 + b), (cx - 1, 15 + b) }, m[3]);
                for (int y = 8; y < 15; y += 2) c.Line(cx - (y - 6) * 0.6, y + b, cx + (y - 6) * 0.6, y + b, m[1]);
                for (int x = (int)cx - 10; x <= cx + 10; x += 3) c.Set(x, 16 + b, m[1]);
                break;
            }
            case "cap":
            {
                c.Shaded(cx, 15.5 + b, 7.8, 4.4, m, 0.1);
                for (int y = 0; y < FH; y++) for (int x = 0; x < FW; x++) if (y > 16 + b) c.Erase(x, y);
                c.Rect((int)cx - 8, 16 + b, 16, 2, m[1]);
                c.Rect((int)cx - 8, 16 + b, 16, 1, m[3]);
                break;
            }
            case "helm":
            {
                c.Shaded(cx, 16 + b, 8, 6, m, 0.1);
                for (int y = 0; y < FH; y++) for (int x = 0; x < FW; x++) if (y > 17 + b) c.Erase(x, y);
                // Flared neck guard and a red tassel on top
                if (d != Dir.Down) c.Rect(8, 17 + b, 16, 6, m[1]);
                else { c.Rect(7, 17 + b, 3, 6, m[1]); c.Rect(22, 17 + b, 3, 6, m[1]); }
                c.Rect(7, 17 + b, 18, 1, m[3]);
                c.Line(cx, 10 + b, cx, 17 + b, m[4]);
                c.Shaded(cx, 8 + b, 2, 2, Pal.Ramp("#d03030"));
                c.Rect((int)cx, 5 + b, 1, 2, Pal.Hex("#e04040"));
                break;
            }
            case "gat":
            {
                var brim = Pal.Hex("#1c1c22").WithA(215);
                c.Ellipse(cx, 15.5 + b, 12, 2.7, brim);
                c.Ellipse(cx, 15.5 + b, 9, 1.6, Pal.Hex("#34343e").WithA(200));
                c.Rect((int)cx - 4, 7 + b, 8, 9, Pal.Hex("#1c1c22"));
                c.Rect((int)cx - 4, 7 + b, 2, 9, Pal.Hex("#3a3a46"));
                c.Rect((int)cx - 4, 7 + b, 8, 1, Pal.Hex("#4a4a56"));
                if (d == Dir.Down) for (int y = 26; y < 34; y += 2) c.Set(cx + (y - 26) * 0.2 + 3, y + b, Pal.Hex("#c09060"));
                break;
            }
        }
        c.Outline(col => Pal.Shift(col, -0.7).WithA(230));
        return c;
    }

    // ---- shields -------------------------------------------------------------------
    // Look "wood" is a planked round shield; "iron" has a raised ring and rivets.
    public static Canvas Shield(ItemDef it, Dir d, int f)
    {
        var c = New();
        var p = GetPose(d, f);
        var m = Pal.Ramp(it.Color);
        bool iron = it.Look == "iron";
        var rim = Pal.Ramp(iron ? "#c8a050" : "#6a6a74");
        int b = p.Bob;
        if (d == Dir.Down)
        {
            double cx = 24, cy = 33 + b;
            c.Shaded(cx, cy, 5, 6, rim);
            c.Shaded(cx, cy, 4, 5, m);
            bool Face(int x, int y) => Math.Pow((x + 0.5 - cx) / 4, 2) + Math.Pow((y + 0.5 - cy) / 5, 2) <= 1;
            for (int y = (int)cy - 5; y <= cy + 5; y++)
                for (int x = (int)cx - 4; x <= cx + 4; x++)
                {
                    if (!Face(x, y)) continue;
                    if (!iron && (x == 22 || x == 25)) c.Set(x, y, m[1]);
                    double r = Math.Sqrt(Math.Pow((x + 0.5 - cx) / 2.8, 2) + Math.Pow((y + 0.5 - cy) / 3.6, 2));
                    if (iron && r > 0.85 && r <= 1.15) c.Set(x, y, y < cy ? m[3] : m[1]);
                }
            foreach (var (rx, ry) in new[] { (cx, cy - 5.5), (cx, cy + 5), (cx - 4.6, cy), (cx + 4.4, cy) }) c.Set(rx, ry, rim[4]);
            c.Shaded(cx, cy, 1.6, 1.8, rim, 0.2);
            c.Set(cx - 1, cy - 1, rim[4]);
        }
        else if (d == Dir.Up)
        {
            c.Shaded(8, 33 + b, 4.5, 5.5, Pal.Ramp("#7a5434"));
            c.Rect(4, 32 + b, 9, 2, Pal.Ramp("#4a3024")[2]);
            c.Rect(4, 32 + b, 9, 1, Pal.Ramp("#4a3024")[3]);
        }
        else
        {
            c.Shaded(11, 32 + b, 2.6, 6, m);
            c.Rect(10, 27 + b, 1, 11, rim[2]);
            c.Rect(10, 27 + b, 1, 1, rim[4]);
            c.Shaded(9.5, 32 + b, 1.1, 1.4, rim, 0.2);
        }
        c.Outline();
        return c;
    }

    // ---- weapons -------------------------------------------------------------------
    // Drawn in "weapon space" around the hand: along runs from the grip toward the tip,
    // across is perpendicular (negative is the lit edge). The strike frame adds a swing trail.
    public static Canvas Weapon(ItemDef it, Dir d, int f)
    {
        var c = New();
        var behind = New(); // glows and trails, kept out of the outline pass
        var p = GetPose(d, f);
        double hx = p.HX, hy = p.HY;
        double a = p.Angle * Math.PI / 180;
        double ux = Math.Sin(a), uy = -Math.Cos(a); // along the weapon
        double vx = Math.Cos(a), vy = Math.Sin(a);  // across it
        (double, double) At(double along, double across = 0) => (hx + ux * along + vx * across, hy + uy * along + vy * across);
        void Seg(double a0, double a1, Rgba col, double across = 0, double thick = 1)
        {
            var (x0, y0) = At(a0, across); var (x1, y1) = At(a1, across);
            c.Line(x0, y0, x1, y1, col, thick);
        }
        void Fill(Rgba col, params (double Along, double Across)[] pts) => c.Poly(pts.Select(q => At(q.Along, q.Across)).ToList(), col);
        void Dot(double along, double across, Rgba col) { var (x, y) = At(along, across); c.Set(x, y, col); }
        var m = Pal.Ramp(it.Color);
        var gold = Pal.Ramp("#e0b04a");
        var red = Pal.Ramp("#c83a3a");
        var iron = Pal.Ramp("#a8b0bc");
        void Grip(double from, double to)
        {
            var wrap = Pal.Ramp("#4a2c34");
            for (double k = from; k < to; k += 1) Seg(k, Math.Min(k + 1, to), ((int)Math.Floor(k) & 1) == 0 ? wrap[2] : wrap[3], 0, 2.2);
        }
        // A cord tassel hanging straight down from a point, whichever way the weapon points.
        void Tassel(double along, double across)
        {
            var (x, y) = At(along, across);
            c.Line(x, y, x - 1, y + 4, red[2]); c.Line(x, y, x + 1, y + 4, red[1]); c.Line(x, y, x, y + 5, red[3]);
            c.Set(x, y, gold[3]);
        }
        void Pole(double from, double to, Rgba[] wood)
        {
            Seg(from, to, wood[2], 0, 2.2);
            Seg(from, to - 0.5, wood[3], -0.6);
            Seg(from + 0.5, to - 0.5, wood[1], 0.7);
        }
        double reach;
        var wood = Pal.Ramp("#8a5a32");
        switch (it.Look)
        {
            case "sword":
            case "dagger":
            {
                bool dagger = it.Look == "dagger";
                double len = dagger ? 8.5 : 15;
                reach = len;
                Grip(-3, 0);
                var (px, py) = At(-3.6);
                c.Shaded(px, py, 1.2, 1.2, gold, 0.2);
                double guard = dagger ? 2.2 : 3.2;
                Seg(0.6, 0.6, gold[2], -guard, 1); Seg(0.6, 0.6, gold[2], guard, 1);
                var (g0x, g0y) = At(0.7, -guard); var (g1x, g1y) = At(0.7, guard);
                c.Line(g0x, g0y, g1x, g1y, gold[2], 1.7);
                Dot(0.7, -guard, gold[4]); Dot(0.7, 0, gold[4]);
                // Blade: a 3px strip with a bright edge, dark back edge and a narrowing point
                Seg(1.6, len - 2, m[2], 0, 2.7);
                Fill(m[2], (len - 2.5, -1.3), (len + 1, 0), (len - 2.5, 1.3));
                Seg(1.8, len - 1, m[4], -0.8);
                Seg(1.8, len - 2, m[1], 0.9);
                Seg(2.2, len - 3, m[3], 0);
                Dot(len + 0.4, 0, m[4]);
                break;
            }
            case "saber":
            {
                // A broad curved hwando: round guard, wrapped grip and a red pommel tassel
                reach = 17;
                Grip(-3.5, 0);
                Tassel(-4, 0);
                var (g0x, g0y) = At(0.8, -2.6); var (g1x, g1y) = At(0.8, 2.6);
                c.Line(g0x, g0y, g1x, g1y, gold[2], 2.2);
                Dot(0.6, -1.8, gold[4]);
                const int n = 30;
                for (int k = 0; k <= n; k++)
                {
                    double t = k / (double)n, along = 1.8 + t * 15, bend = t * t * 2.6, w = 3.2 - t * 1.4;
                    var (bx, by) = At(along, bend);
                    c.Ellipse(bx, by, w / 2, w / 2, m[2]);
                }
                for (int k = 0; k <= n; k++)
                {
                    double t = k / (double)n, along = 1.8 + t * 15, bend = t * t * 2.6, w = 3.2 - t * 1.4;
                    Dot(along, bend - w / 2 + 0.5, m[4]);
                    Dot(along, bend + w / 2 - 0.4, m[1]);
                    if (k % 6 == 3 && t < 0.8) Dot(along, bend + 0.2, m[3]);
                }
                Dot(17.4, 2.8, m[4]);
                break;
            }
            case "staff":
            {
                // A gnarled staff whose forked head cradles a glowing orb
                reach = 18;
                Pole(-10, 13, wood);
                foreach (double k in new[] { -5.0, 2.0, 8.0 }) Seg(k, k + 0.6, wood[1], 0.3, 2.6);
                Seg(-10.5, -9, iron[2], 0, 2.4);
                foreach (int side in new[] { -1, 1 })
                {
                    var pts = new[] { (12.5, 0.8 * side), (14, 3.0 * side), (16.6, 3.8 * side), (19.2, 2.8 * side), (20.2, 1.2 * side) };
                    for (int i = 0; i + 1 < pts.Length; i++)
                    {
                        var (x0, y0) = At(pts[i].Item1, pts[i].Item2); var (x1, y1) = At(pts[i + 1].Item1, pts[i + 1].Item2);
                        c.Line(x0, y0, x1, y1, side < 0 ? wood[3] : wood[2], 1.3);
                    }
                }
                var (ox, oy) = At(16.6);
                c.Shaded(ox, oy, 2.8, 2.8, m, 0.3);
                c.Set(ox - 1, oy - 1, Pal.Hex("#ffffff"));
                behind.Glow(ox, oy, 7, m[3], 170);
                break;
            }
            case "ringstaff":
            {
                // A monk's khakkhara: an arched loop with jingling rings and a pointed finial
                reach = 22;
                Pole(-10, 13, wood);
                Seg(12, 13.6, m[2], 0, 2.8);
                Dot(12.4, -1, m[4]);
                Seg(13.5, 21.6, m[1]);
                const int n = 56;
                for (int k = 0; k < n; k++)
                {
                    double t = k / (double)n * Math.PI * 2, al = 17.6 + Math.Cos(t) * 4.4, ac = Math.Sin(t) * 3.6;
                    var (x, y) = At(al, ac);
                    c.Ellipse(x, y, 0.75, 0.75, ac < 0 || Math.Cos(t) > 0.6 ? m[3] : m[2]);
                }
                for (int k = 0; k < n; k += 4)
                {
                    double t = k / (double)n * Math.PI * 2;
                    if (Math.Sin(t) < -0.3 && Math.Cos(t) > -0.2) Dot(17.6 + Math.Cos(t) * 4.4, Math.Sin(t) * 3.6, m[4]);
                }
                var (fx, fy) = At(22.6);
                c.Shaded(fx, fy, 1.4, 1.6, m, 0.2);
                foreach (int side in new[] { -1, 1 })
                    foreach (double al in new[] { 15.2, 18.6 })
                    {
                        var (rx, ry) = At(al, side * 4.7);
                        c.Shaded(rx, ry + 0.6, 1.2, 1.2, m, 0.2);
                    }
                break;
            }
            case "glaive":
            {
                // A woldo: long pole, gold collar with a red tassel and a broad crescent blade
                reach = 26;
                Pole(-11, 12, Pal.Ramp("#5a3a24"));
                Seg(-11.5, -10, iron[2], 0, 2.4);
                // Straight back with a notch spike; the edge bulges out and sweeps up into a backward-hooked tip
                Fill(m[2], (13.2, 1.2), (19, 1.3), (23, 1.5), (26.2, 2.4), (25.2, -0.6), (23.4, -2.9), (20, -4.0), (16.2, -3.3), (13.2, -1.5));
                Fill(m[2], (17.4, 1.2), (18.6, 3.2), (19.8, 1.3));
                foreach (var (a0, c0, a1, c1) in new[] { (13.8, -1.8, 16.4, -2.8), (16.4, -2.8, 20, -3.4), (20, -3.4, 23.2, -2.4), (23.2, -2.4, 25, -0.4) })
                {
                    var (x0, y0) = At(a0, c0); var (x1, y1) = At(a1, c1);
                    c.Line(x0, y0, x1, y1, m[4]);
                }
                Seg(14, 24, m[1], 0.8);
                Seg(15, 22, m[3], -1.2);
                Seg(11.4, 13.4, gold[2], 0, 3);
                Dot(11.8, -1, gold[4]);
                Tassel(12.4, 1.6);
                break;
            }
            case "bow":
            {
                // A recurve bow held upright: limbs sweep back from the grip and curl forward at the tips
                reach = 0;
                var horn = Pal.Ramp(it.Color);
                var pts = new List<(double X, double Y)>();
                for (int k = -10; k <= 10; k++)
                {
                    double t = k / 10.0;
                    pts.Add(At(t * 10, 2.6 * (1 - t * t) - 0.9 * Math.Pow(Math.Abs(t), 6)));
                }
                for (int i = 0; i + 1 < pts.Count; i++) c.Line(pts[i].X, pts[i].Y, pts[i + 1].X, pts[i + 1].Y, horn[2], 1.8);
                for (int i = 2; i + 3 < pts.Count; i++) { var (x, y) = pts[i]; c.Set(x - 0.4, y - 0.4, horn[3]); }
                var (s0x, s0y) = At(-9.6, -0.5); var (s1x, s1y) = At(9.6, -0.5);
                c.Line(s0x, s0y, s1x, s1y, Pal.Hex("#e8e4d8").WithA(200));
                Grip(-1.5, 1.5);
                Dot(-10.2, -0.6, gold[3]); Dot(10.2, -0.6, gold[3]);
                break;
            }
            case "fan":
            {
                // A folding ritual fan: ribs from a gold rivet, painted leaf with a darker border band
                reach = 10;
                var leaf = Pal.Ramp(it.Color);
                var rib = Pal.Ramp("#5a3a28");
                const int ribs = 9;
                for (int k = 0; k <= 24; k++)
                {
                    double t = (k / 24.0 - 0.5) * 2.1;
                    for (double r = 4; r <= 9.5; r += 0.5)
                    {
                        var (x, y) = At(r * Math.Cos(t), r * Math.Sin(t));
                        c.Set(x, y, r > 8.6 ? leaf[1] : (k % 6 < 3 ? leaf[2] : leaf[3]));
                    }
                }
                for (int k = 0; k < ribs; k++)
                {
                    double t = (k / (double)(ribs - 1) - 0.5) * 2.1;
                    var (x0, y0) = At(0.5 * Math.Cos(t), 0.5 * Math.Sin(t)); var (x1, y1) = At(4.2 * Math.Cos(t), 4.2 * Math.Sin(t));
                    c.Line(x0, y0, x1, y1, rib[2]);
                }
                var (px, py) = At(0);
                c.Shaded(px, py, 1.1, 1.1, gold, 0.2);
                Tassel(-0.8, 0);
                break;
            }
            default: reach = 12; break;
        }
        c.Outline(col => Pal.Shift(col, -0.72));
        // Strike frame: a fading arc over the last part of the swing, brightest at the blade's edge.
        if (f == 5 && reach > 0)
        {
            double a1 = p.Angle, a0 = GetPose(d, 4).Angle;
            a0 = a1 - Math.Sign(a1 - a0) * Math.Min(Math.Abs(a1 - a0), 110);
            var trail = Pal.Hex("#f4f8ff");
            for (int k = 0; k <= 60; k++)
            {
                double t = k / 60.0, ang = (a0 + (a1 - a0) * t) * Math.PI / 180;
                for (double r = reach * 0.5; r <= reach + 0.5; r += 0.5)
                {
                    double edge = (r - reach * 0.5) / (reach * 0.5 + 0.5);
                    var col = trail.WithA((byte)(230 * Math.Pow(t, 0.6) * (0.1 + 0.9 * edge * edge)));
                    int x = (int)Math.Floor(hx + Math.Sin(ang) * r), y = (int)Math.Floor(hy - Math.Cos(ang) * r);
                    if (behind.In(x, y) && col.A > behind.Get(x, y).A) behind.P[y * FW + x] = col;
                }
            }
        }
        c.BlitUnder(behind);
        return c;
    }

    // ---- whole characters ------------------------------------------------------------
    // One frame of a paper-doll character, layered the way client/scripts/Actor.cs draws it.
    public static Canvas Doll(Look l, Dir d, int f = 0)
    {
        var frame = New();
        void Layer(Canvas c, string tint) { if (tint != "") c.Map(px => Tint(px, Pal.Hex(tint))); frame.Blit(c, 0, 0); }
        Canvas? Gear(string? id, Func<ItemDef, Dir, int, Canvas> draw) => id != null ? draw(Content.Items[id], d, f) : null;
        var weapon = Gear(l.Weapon, Weapon);
        var shield = Gear(l.Shield, Shield);
        if (d == Dir.Up && weapon != null) frame.Blit(weapon, 0, 0);
        if (d != Dir.Down && shield != null) frame.Blit(shield, 0, 0);
        Layer(Skin(d, f), l.Skin);
        frame.Blit(Body(d, f), 0, 0);
        if (l.Armor != null) frame.Blit(Armor(Content.Items[l.Armor], d, f), 0, 0);
        Layer(Hair(l.Style, d, f), l.Hair);
        if (l.Helmet != null) frame.Blit(Helmet(Content.Items[l.Helmet], d, f), 0, 0);
        if (d == Dir.Down && shield != null) frame.Blit(shield, 0, 0);
        if (d != Dir.Up && weapon != null) frame.Blit(weapon, 0, 0);
        return frame;
    }

    // Same maths as client/shaders/tint.gdshader, so the preview matches the game.
    public static Rgba Tint(Rgba px, Rgba baseCol)
    {
        double v = px.R / 255.0;
        double[] b = { baseCol.R / 255.0, baseCol.G / 255.0, baseCol.B / 255.0 };
        double[] dark = { b[0] * 0.36, b[1] * 0.32, b[2] * 0.42 };
        double[] light = { b[0] + (1 - b[0]) * 0.45, b[1] + (1 - b[1]) * 0.42, b[2] + (1 - b[2]) * 0.32 };
        double Ch(int k) => v < 0.5 ? dark[k] + (b[k] - dark[k]) * (v * 2) : b[k] + (light[k] - b[k]) * ((v - 0.5) * 2);
        return new Rgba((byte)(Ch(0) * 255), (byte)(Ch(1) * 255), (byte)(Ch(2) * 255), px.A);
    }

    // ---- ghost ---------------------------------------------------------------------
    public static Canvas Ghost(Dir d, int f)
    {
        var c = New();
        int bob = (f % 2 == 0) ? 0 : -1;
        var g = Pal.Ramp("#a8c8ff");
        double cx = d == Dir.Right ? 16.5 : 16;
        c.Shaded(cx, 19 + bob, 7, 6.6, g, 0.15);
        c.Poly(new (double, double)[] { (cx - 7, 25 + bob), (cx + 7, 25 + bob), (cx + 5, 38), (cx - 5, 38) }, g[2]);
        for (int i = 0; i < 4; i++)
        {
            double tx = cx - 5 + i * 3.3 + ((f + i) % 2);
            c.Ellipse(tx, 39 + (i % 2), 1.6, 2.4, g[1]);
        }
        c.Rect((int)cx - 7, 25 + bob, 2, 12, g[3]);
        if (d == Dir.Down)
        {
            c.Rect(12, 20 + bob, 2, 3, Pal.Hex("#2a3a7a")); c.Rect(18, 20 + bob, 2, 3, Pal.Hex("#2a3a7a"));
            c.Ellipse(16, 25 + bob, 1.5, 1, Pal.Hex("#2a3a7a"));
        }
        else if (d == Dir.Right) c.Rect(20, 20 + bob, 2, 3, Pal.Hex("#2a3a7a"));
        c.Outline(col => Pal.Hex("#5068b0"));
        c.Map(px => px.WithA((byte)(px.A * 0.78)));
        return c;
    }
}
