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
        if (d == Dir.Right) { head.Ellipse(14.5, 21 + p.Bob, 1.4, 1.8, g[1]); head.Set(14, 21 + p.Bob, g[2]); }
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
        var eye = Pal.Hex("#22161c");
        var blush = Pal.Hex("#f08a8a").WithA(150);
        if (d == Dir.Down)
        {
            c.Rect(12, 21 + b, 2, 2, eye); c.Set(12, 21 + b, Pal.Hex("#5a4048"));
            c.Rect(18, 21 + b, 2, 2, eye); c.Set(19, 21 + b, Pal.Hex("#5a4048"));
            c.Rect(10, 24 + b, 2, 1, blush); c.Rect(20, 24 + b, 2, 1, blush);
            c.Rect(15, 25 + b, 2, 1, Pal.Hex("#9a4a4a"));
        }
        else if (d == Dir.Right)
        {
            c.Rect(20, 21 + b, 1, 2, eye);
            c.Rect(19, 24 + b, 2, 1, blush);
            c.Set(22, 25 + b, Pal.Hex("#9a4a4a"));
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
            if (ribbon is { } r) { torso.Set(20, 31 + b, r); torso.Rect(20, 32 + b, 1, 4, r); torso.Set(21, 33 + b, r); }
            // Near arm: shoulder to hand
            double sx = 16, sy = 29 + b;
            ArmLine(sleeves, sx, sy, p.HX, p.HY, cloth, trim);
        }
        else
        {
            if (robe)
                torso.Poly(new (double, double)[] { (10, 27 + b), (22, 27 + b), (23, 43), (9, 43) }, cloth[2]);
            else torso.Rect(10, 27 + b, 12, 11, cloth[2]);
            torso.Rect(10, 27 + b, 2, robe ? 16 - b : 11, cloth[3]);
            torso.Rect(20, 27 + b, 2, robe ? 16 - b : 11, cloth[1]);
            if (plate) Scales(torso, 10, 29 + b, 12, robe ? 12 : 8, cloth);
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
            ArmLine(sleeves, p.HX < 16 ? 9 : 23, 29 + b, p.HX, p.HY, cloth, trim);
            ArmLine(sleeves, p.BX < 16 ? 9 : 23, 29 + b, p.BX, p.BY, cloth, trim);
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

    private static void ArmLine(Canvas c, double sx, double sy, double hx, double hy, Rgba[] cloth, Rgba[] trim)
    {
        double dx = hx - sx, dy = hy - sy, len = Math.Max(0.01, Math.Sqrt(dx * dx + dy * dy));
        double ex = hx - dx / len * 2, ey = hy - dy / len * 2;
        c.Line(sx, sy, ex, ey, cloth[2], 3.2);
        c.Line(sx - 0.6, sy - 0.6, ex - 0.6, ey - 0.6, cloth[3], 1);
        c.Line(ex - dx / len, ey - dy / len, ex, ey, trim[2], 3.4);
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
            if (style == "bun") c.Shaded(16, 25 + b, 3.4, 2.6, g, 0.15);
        }
        else if (d == Dir.Right)
        {
            var cap = New();
            cap.Shaded(15.5, 18 + b, 7.3, 6.2, g, 0.05);
            for (int y = 0; y < FH; y++) for (int x = 0; x < FW; x++)
                if (x >= 18 && y >= 17 + b) cap.Erase(x, y);
            cap.Rect(9, 19 + b, 4, 5, g[2]);
            cap.Rect(9, 19 + b, 1, 5, g[1]);
            c.Blit(cap, 0, 0);
            if (style == "long") { c.Rect(9, 22 + b, 5, 12, g[2]); c.Rect(9, 22 + b, 1, 12, g[1]); c.Rect(13, 24 + b, 1, 10, g[3]); }
            if (style == "bun") c.Shaded(10, 22 + b, 3, 2.8, g, 0.15);
        }
        else
        {
            var cap = New();
            cap.Shaded(16, 17.5 + b, 7.7, 5.6, g, 0.05);
            for (int x = 0; x < FW; x++)
            {
                int bottom = 18 + b + ((x % 3 == 0) ? 1 : 0);
                for (int y = bottom; y < FH; y++) cap.Erase(x, y);
            }
            cap.Rect(8, 17 + b, 2, 6, g[2]); cap.Rect(22, 17 + b, 2, 6, g[1]);
            if (style == "long") { cap.Rect(7, 18 + b, 3, 16, g[2]); cap.Rect(7, 18 + b, 1, 16, g[3]); cap.Rect(22, 18 + b, 3, 16, g[1]); }
            cap.Line(15, 13 + b, 16, 17 + b, g[1]);
            c.Blit(cap, 0, 0);
        }
        if (style == "topknot")
        {
            double kx = d == Dir.Right ? 14.5 : 16;
            c.Shaded(kx, 11.2 + b, 2.6, 2.4, g, 0.1);
            c.Rect((int)kx - 2, 13 + b, 4, 1, g[1]);
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
    public static Canvas Shield(ItemDef it, Dir d, int f)
    {
        var c = New();
        var p = GetPose(d, f);
        var m = Pal.Ramp(it.Color);
        var rim = Pal.Ramp("#c8a050");
        int b = p.Bob;
        if (d == Dir.Down)
        {
            c.Shaded(24, 33 + b, 5, 6, rim);
            c.Shaded(24, 33 + b, 4, 5, m);
            c.Shaded(24, 33 + b, 1.5, 1.5, rim);
            c.Set(22, 30 + b, m[4]);
        }
        else if (d == Dir.Up) c.Shaded(8, 33 + b, 4.5, 5.5, Pal.Ramp("#7a5434"));
        else { c.Shaded(11, 32 + b, 2.6, 6, m); c.Rect(10, 27 + b, 1, 11, rim[2]); }
        c.Outline();
        return c;
    }

    // ---- weapons -------------------------------------------------------------------
    public static Canvas Weapon(ItemDef it, Dir d, int f)
    {
        var c = New();
        var p = GetPose(d, f);
        double hx = p.HX, hy = p.HY + p.Bob * 0;
        double a = p.Angle * Math.PI / 180;
        double ux = Math.Sin(a), uy = -Math.Cos(a); // along the weapon
        double vx = Math.Cos(a), vy = Math.Sin(a);  // across it
        (double, double) At(double along, double across = 0) => (hx + ux * along + vx * across, hy + uy * along + vy * across);
        void Seg(double a0, double a1, Rgba col, double across = 0, double thick = 1)
        {
            var (x0, y0) = At(a0, across); var (x1, y1) = At(a1, across);
            c.Line(x0, y0, x1, y1, col, thick);
        }
        var m = Pal.Ramp(it.Color);
        var wood = Pal.Ramp("#8a5a32");
        var gold = Pal.Ramp("#e0b04a");
        switch (it.Look)
        {
            case "sword":
            case "dagger":
            {
                double len = it.Look == "dagger" ? 7 : 14;
                Seg(-2.5, 0.5, Pal.Ramp("#5a3a28")[2], 0, 2);
                Seg(1, 1, gold[2], -2.5); Seg(1, 1, gold[2], 2.5);
                Seg(1, 1, gold[3], -1.5); Seg(1, 1, gold[1], 1.5);
                Seg(1, 1, gold[2], 0);
                Seg(2, len, m[2], 0.5, 1.6);
                Seg(2, len - 1, m[4], -0.4);
                Seg(2, len - 1, m[1], 1.2);
                var (tx, ty) = At(len + 1);
                c.Set(tx, ty, m[3]);
                break;
            }
            case "staff":
            {
                Seg(-8, 14, wood[2], 0, 2);
                Seg(-8, 13, wood[3], -0.6);
                var (gx, gy) = At(16.5);
                c.Shaded(gx, gy, 3, 3, m, 0.2);
                c.Set(gx - 1, gy - 1.5, Pal.Hex("#ffffff"));
                Seg(13, 14.5, gold[2], 0, 2.4);
                break;
            }
            case "ringstaff":
            {
                Seg(-8, 14, wood[2], 0, 2);
                Seg(-8, 13, wood[3], -0.6);
                var (rx, ry) = At(17.5);
                for (int k = 0; k < 24; k++)
                {
                    double t = k / 24.0 * Math.PI * 2;
                    c.Set(rx + Math.Cos(t) * 3.6, ry + Math.Sin(t) * 3.6, k < 12 ? m[3] : m[1]);
                }
                Seg(14, 21, m[2]);
                foreach (var side in new[] { -3.6, 3.6 }) { var (bx, by) = At(16, side); c.Shaded(bx, by + 1, 1.2, 1.2, m); }
                break;
            }
            case "glaive":
            {
                Seg(-9, 13, Pal.Ramp("#5a3a24")[2], 0, 2);
                Seg(-9, 12, Pal.Ramp("#5a3a24")[3], -0.6);
                Seg(12, 13.5, Pal.Hex("#c03030"), 0, 3);
                for (int k = 0; k <= 8; k++)
                {
                    double along = 13 + k, bend = Math.Sin(k / 8.0 * Math.PI) * 1.8 + k * 0.15;
                    var (bx, by) = At(along, bend);
                    c.Ellipse(bx, by, 1.4 - k * 0.08, 1.4 - k * 0.08, m[2]);
                    var (ex, ey) = At(along, bend - 1);
                    c.Set(ex, ey, m[4]);
                }
                break;
            }
        }
        c.Outline(col => Pal.Shift(col, -0.72));
        return c;
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
