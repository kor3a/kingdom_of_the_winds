using KotW.Shared;

namespace KotW.ArtGen;

// 40x40 skill pictures, one per skill, packed eight to a row in skill order. Each is a motif
// with a soft light behind it, drawn over a dark backdrop in the skill's colour.
public static class SkillIcons
{
    public const int Size = 40, PerRow = 8;

    public static Canvas Atlas()
    {
        var ids = Content.Skills.Keys.ToList();
        var atlas = new Canvas(Size * PerRow, Size * ((ids.Count + PerRow - 1) / PerRow));
        for (int i = 0; i < ids.Count; i++) atlas.Blit(Icon(Content.Skills[ids[i]]), i % PerRow * Size, i / PerRow * Size);
        return atlas;
    }

    public static Canvas Icon(SkillDef s)
    {
        var col = Pal.Hex(s.Color);
        var back = Backdrop(Pal.Sat(col) < 0.1 ? Pal.Hex("#8090d0") : col);
        var glow = new Canvas(Size, Size);
        var fig = new Canvas(Size, Size);
        Draw(s.Id, fig, glow, Pal.Ramp(col));
        fig.Outline(c => Pal.Shift(c, -0.72));
        back.Blit(glow, 0, 0);
        back.Blit(fig, 0, 0);
        return back;
    }

    // A dark radial gradient in the skill's colour, a little lighter just above the middle.
    private static Canvas Backdrop(Rgba col)
    {
        var c = new Canvas(Size, Size);
        Rgba inner = Pal.Shift(col, -0.5), outer = Pal.Shift(col, -0.86);
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                double d = Math.Clamp(Math.Sqrt(Math.Pow(x + 0.5 - Size / 2.0, 2) + Math.Pow(y + 0.5 - Size * 0.45, 2)) / (Size * 0.64), 0, 1);
                c.P[y * Size + x] = Mix(inner, outer, d * d);
            }
        return c;
    }

    private static Rgba Mix(Rgba a, Rgba b, double t) =>
        new((byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t));

    private static void Draw(string id, Canvas f, Canvas g, Rgba[] m)
    {
        var steel = Rp("#c8d0dc");
        var gold = Rp("#e8b848");
        var wood = Rp("#8a5a32");
        var dark = Hx("#140a10");
        switch (id)
        {
            // ---- Warrior / Warlord ----
            case "tiger_fang":
                g.Glow(20, 20, 19, m[3], 130);
                for (int k = 0; k < 3; k++)
                {
                    double o = (k - 1) * 8;
                    Lens(f, 27 + o, 4, 12 + o, 36, 5.5, m[3], -3);
                    Lens(f, 27 + o, 4, 12 + o, 36, 2, White, -3.4);
                }
                break;
            case "lions_challenge":
                g.Glow(20, 20, 19, m[3], 120);
                Star(f, 20, 20, 18, 13, 14, Rp("#9a4a22")[2]);
                Star(f, 20, 20, 15, 11, 14, Rp("#c87a38")[2], -77);
                f.Shaded(20, 21, 9, 9.5, Rp("#e8b060"), 0.1);
                f.Line(13.5, 14.5, 17.5, 16.5, Hx("#5a2a10"), 1.4);
                f.Line(26.5, 14.5, 22.5, 16.5, Hx("#5a2a10"), 1.4);
                f.Ellipse(16, 18, 1.4, 1, dark); f.Ellipse(24, 18, 1.4, 1, dark);
                f.Poly(Pts(17.5, 20.5, 22.5, 20.5, 20, 23), Hx("#5a2a1a"));
                f.Ellipse(20, 26.5, 4.4, 3.4, Hx("#3a0c10"));
                f.Poly(Pts(16.4, 23.6, 18, 23.6, 17.2, 26.4), White); f.Poly(Pts(22, 23.6, 23.6, 23.6, 22.8, 26.4), White);
                f.Poly(Pts(17.2, 29.6, 18.6, 29.6, 17.9, 27.6), White); f.Poly(Pts(21.4, 29.6, 22.8, 29.6, 22.1, 27.6), White);
                Ring(f, 20, 26, 19, 15, m[4].WithA(170), 1, 200, 240); Ring(f, 20, 26, 19, 15, m[4].WithA(170), 1, 300, 340);
                break;
            case "bloodlust":
                g.Glow(20, 22, 19, Hx("#ff3020"), 160);
                Flame(f, 10, 34, 6, 22, Rp("#c02018"), -2);
                Flame(f, 30, 34, 6, 22, Rp("#c02018"), 2);
                Flame(f, 20, 32, 9, 30, Rp("#e83820"));
                Fist(f, 20, 21, 8, Rp("#e8a878"));
                break;
            case "cyclone_blade":
                g.Glow(20, 20, 19, m[3], 130);
                for (int k = 0; k < 3; k++)
                {
                    Ring(f, 20, 20, 17, 17, m[3], 2.4, k * 120, k * 120 + 75);
                    Ring(f, 20, 20, 12, 12, m[4], 1.4, k * 120 + 40, k * 120 + 100);
                }
                Sword(f, 9, 31, 31, 9, steel, 3.6);
                break;
            case "thunderclap_bash":
                g.Glow(22, 20, 19, Hx("#fff080"), 120);
                foreach (var a in new[] { -45.0, -18, 10, 38 }) { var (x0, y0) = Polar(18, 20, a, 15.5); var (x1, y1) = Polar(18, 20, a, 20); f.Line(x0, y0, x1, y1, Hx("#fff4a0"), 1.6); }
                f.Shaded(18, 20, 14, 14, Rp("#4a5a88"), 0.05);
                Ring(f, 18, 20, 13, 13, gold[2], 2);
                for (int k = 0; k < 8; k++) { var (x, y) = Polar(18, 20, k * 45 + 22.5, 10.4); f.Shaded(x, y, 1.1, 1.1, gold); }
                Bolt(f, Hx("#ffe040"), 3, 21, 9, 14.5, 20.5, 20, 20.5, 15, 31);
                break;
            case "iron_mountain":
            {
                g.Glow(20, 18, 19, m[4], 110);
                var rock = Rp("#7a8698");
                f.Poly(Pts(1, 36, 13, 11, 19, 19, 26, 6, 39, 36), rock[2]);
                f.Poly(Pts(1, 36, 13, 11, 15, 36), rock[3]);
                f.Poly(Pts(19, 19, 26, 6, 24, 36, 17, 36), rock[3]);
                f.Poly(Pts(13, 11, 10.2, 16.4, 12.5, 15, 14, 17, 15.6, 14.4), White);
                f.Poly(Pts(26, 6, 22.6, 13, 25, 11.6, 26.8, 13.8, 29.3, 11.8), White);
                f.Line(3, 29.5, 37, 29.5, rock[1], 1.4);
                f.Line(5, 23.5, 34, 23.5, rock[1]);
                for (int x = 6; x < 36; x += 6) f.Shaded(x, 29.5, 1.1, 1.1, steel);
                Star(f, 6, 7, 3.6, 1, 4, White); Star(f, 35, 15, 3, 0.9, 4, White);
                break;
            }
            case "earthshatter":
            {
                g.Glow(20, 28, 17, Hx("#ff8020"), 160);
                var earth = Rp("#7a5434");
                f.Rect(0, 26, 40, 14, earth[2]); f.Rect(0, 26, 40, 2, earth[3]);
                Crack(f, 4, 20, 26, 17, 30, 22, 33, 18, 37, 21, 40);
                Crack(f, 2.4, 17, 30, 10, 32, 5, 31);
                Crack(f, 2.4, 22, 33, 29, 35, 35, 34);
                Sword(f, 20, 3, 20, 28, steel, 3.8);
                f.Rect(8, 19, 2, 2, earth[3]); f.Rect(30, 16, 3, 2, earth[3]); f.Rect(26, 21, 2, 2, earth[2]); f.Rect(11, 13, 2, 2, earth[3]);
                break;
            }
            case "meteor_leap":
                g.Glow(28, 30, 16, Hx("#ffb040"), 180);
                for (int k = 10; k >= 0; k--)
                {
                    double t = k / 10.0, r = 6.5 * (1 - t * 0.7);
                    f.Ellipse(28 - t * 24, 28 - t * 24, r, r, Rp("#f06020")[2].WithA((byte)(225 - t * 170)));
                    f.Ellipse(28 - t * 24, 28 - t * 24, r * 0.5, r * 0.5, Hx("#ffd060").WithA((byte)(210 - t * 170)));
                }
                f.Rect(0, 35, 40, 5, Rp("#6a4a30")[2]);
                Star(f, 30, 33, 11, 3.8, 8, Hx("#ffb040"));
                Sword(f, 9, 9, 30, 30, steel, 4);
                Star(f, 30, 33, 5.5, 2.2, 8, Hx("#fff0a0"), -67.5);
                f.Rect(17, 31, 2, 2, Rp("#6a4a30")[3]); f.Rect(37, 25, 2, 2, Rp("#6a4a30")[3]); f.Rect(22, 26, 2, 1, Rp("#6a4a30")[3]);
                break;
            case "hundred_battle_banner":
                g.Glow(22, 16, 18, m[3], 120);
                f.Poly(Pts(10, 5, 20, 7, 30, 4, 37, 6, 33, 15, 37, 24, 27, 22, 18, 25, 10, 23), m[2]);
                f.Poly(Pts(10, 18, 18, 20, 27, 17, 35, 19, 37, 24, 27, 22, 18, 25, 10, 23), m[1]);
                Path(f, gold[2], 1.4, 10, 5, 20, 7, 30, 4, 37, 6);
                f.Shaded(22, 14, 4.8, 4.8, gold);
                f.Ellipse(22, 14, 2.6, 2.6, m[1]);
                f.Line(14, 24, 14, 29, gold[2]); f.Line(22, 24.5, 22, 30, gold[2]); f.Line(30, 23, 30, 28, gold[2]);
                f.Line(9, 3, 9, 39, wood[2], 2.4); f.Line(8.4, 3, 8.4, 39, wood[3]);
                f.Shaded(9, 3, 2.2, 2.2, gold);
                break;
            case "crescent_cleave":
                g.Glow(20, 20, 19, m[3], 130);
                Lens(f, 12, 3, 12, 37, 1, m[4].WithA(180), -7);
                Lens(f, 12, 3, 12, 37, 1, m[4].WithA(180), -11);
                Lens(f, 12, 3, 12, 37, 7, m[3], -17);
                Lens(f, 12, 3, 12, 37, 2.2, White, -19.5);
                break;
            case "undying_will":
                g.Glow(20, 21, 19, Hx("#ff7020"), 170);
                Flame(f, 20, 36, 15, 35, Rp("#e85020"));
                Heart(f, 20, 22, 9, Rp("#d81838"));
                f.Ellipse(15.5, 18, 1.5, 1.5, Hx("#ffd0d0"));
                break;
            case "headsmans_verdict":
                g.Glow(24, 14, 17, Hx("#ff4040"), 120);
                f.Line(7, 38, 26, 9, wood[2], 2.8); f.Line(6.4, 37.4, 25.4, 8.4, wood[3]);
                f.Poly(Pts(22, 10, 27, 2, 33, 3, 37, 8, 38, 14, 36, 20, 31, 23, 27, 17), steel[2]);
                f.Poly(Pts(22, 10, 27, 2, 29, 3, 26, 15), steel[1]);
                Path(f, steel[4], 1.4, 33, 3.4, 36.6, 8, 37.6, 14, 35.6, 19.6);
                f.Ellipse(36.6, 16, 1.2, 1.6, Hx("#c01818"));
                Drop(f, 35, 28, 1.6, Rp("#c01818")); Drop(f, 31, 33, 1.2, Rp("#c01818"));
                break;
            case "tyrants_domain":
                g.Glow(20, 21, 19, m[3], 160);
                foreach (var a in new[] { 45.0, 135, 225, 315 })
                {
                    var (ox, oy) = Polar(20, 21, a, 20); var (ix, iy) = Polar(20, 21, a, 13);
                    Arrow(f, ox, oy, ix, iy, White, m[3], 0.75);
                }
                f.Poly(Pts(11, 27, 11, 15, 15.5, 20, 20, 11, 24.5, 20, 29, 15, 29, 27), gold[2]);
                f.Rect(11, 24, 18, 3, gold[1]);
                f.Line(12, 16, 12, 24, gold[3]);
                f.Shaded(20, 11, 1.7, 1.7, Rp("#e02020")); f.Shaded(11, 15, 1.4, 1.4, Rp("#e02020")); f.Shaded(29, 15, 1.4, 1.4, Rp("#e02020"));
                f.Shaded(20, 21.5, 2, 2, Rp("#e02020"));
                break;

            // ---- Rogue / Shadowblade ----
            case "shadow_fang":
            {
                g.Glow(20, 20, 19, m[3], 130);
                var smoke = Rp("#2e2448");
                f.Shaded(13, 24, 9, 8, smoke, 0.05); f.Shaded(27, 23, 9, 8, smoke, 0.05); f.Shaded(20, 17, 10, 8, smoke, 0.1);
                f.Shaded(20, 27, 11, 6, smoke, 0.05);
                Swirl(f, 8, 12, 1, 5, 0.9, 90, smoke[3], 1.6); Swirl(f, 33, 32, 1, 4.5, 0.9, 270, smoke[3], 1.6);
                f.Ellipse(15, 18, 1.6, 1, m[4]); f.Ellipse(25, 18, 1.6, 1, m[4]);
                var fang = Rp("#f0e8ff");
                Lens(f, 12, 21, 17, 34, 4.4, fang[2], 2); Lens(f, 28, 21, 23, 34, 4.4, fang[2], -2);
                Lens(f, 12, 21, 17, 34, 1.4, White, 2.6); Lens(f, 28, 21, 23, 34, 1.4, White, -1.4);
                break;
            }
            case "vipers_kiss":
                g.Glow(20, 18, 18, m[3], 130);
                Ring(f, 20, 32, 13, 4.5, m[1], 4);
                f.Line(20, 31, 20, 16, m[2], 6); f.Line(18.4, 31, 18.4, 16, m[3]);
                f.Poly(Pts(9, 12, 20, 3, 31, 12, 27, 19, 13, 19), m[2]);
                f.Poly(Pts(9, 12, 20, 3, 20, 8, 13, 13), m[3]);
                f.Rect(14, 10, 3, 2, Hx("#ffe040")); f.Rect(23, 10, 3, 2, Hx("#ffe040"));
                f.Set(15, 10, dark); f.Set(24, 10, dark);
                f.Poly(Pts(13.5, 18, 26.5, 18, 20, 27), Hx("#5a0a1a"));
                f.Poly(Pts(15, 18, 17.4, 18, 16.2, 23.6), White); f.Poly(Pts(22.6, 18, 25, 18, 23.8, 23.6), White);
                Drop(f, 16.2, 27.5, 1.3, Rp("#90f060")); Drop(f, 23.8, 28.5, 1.3, Rp("#90f060"));
                break;
            case "vanishing_veil":
            {
                g.Glow(20, 18, 18, m[3], 100);
                var hood = Rp("#3a3060");
                f.Poly(Pts(20, 2, 31, 11, 35, 28, 31, 39, 9, 39, 5, 28, 9, 11), hood[2]);
                f.Poly(Pts(20, 2, 9, 11, 5, 28, 9, 39, 14, 39, 11, 22), hood[3]);
                f.Ellipse(20, 20, 7.5, 9, Hx("#08060e"));
                f.Ellipse(16.5, 18.5, 2.6, 1.6, m[4].WithA(110)); f.Ellipse(23.5, 18.5, 2.6, 1.6, m[4].WithA(110));
                f.Rect(15, 18, 3, 1, White); f.Rect(22, 18, 3, 1, White);
                for (int y = 28; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                        if ((x * 7 + y * 3) % 6 < (y - 27) / 2) f.Erase(x, y);
                break;
            }
            case "crimson_flurry":
                g.Glow(20, 20, 19, m[3], 130);
                foreach (var (x0, y0, x1, y1, w) in new[] { (4.0, 8.0, 36.0, 30.0, 3.4), (34.0, 5.0, 6.0, 35.0, 3.4), (3.0, 22.0, 36.0, 13.0, 2.6), (15.0, 2.0, 25.0, 38.0, 2.6) })
                {
                    Lens(f, x0, y0, x1, y1, w, m[3], 1.5);
                    Lens(f, x0, y0, x1, y1, 1, White, 1.5);
                }
                f.Ellipse(9, 30, 1.2, 1.2, Hx("#c01830")); f.Ellipse(32, 33, 1, 1, Hx("#c01830")); f.Ellipse(31, 10, 1, 1, Hx("#c01830"));
                break;
            case "blackpowder_bomb":
            {
                var smoke = Rp("#7a7a8a");
                f.Shaded(9, 31, 7, 6, smoke, 0.1); f.Shaded(31, 32, 7, 6, smoke, 0.1); f.Shaded(20, 35, 9, 4.5, smoke, 0.1);
                f.Shaded(19, 22, 11, 11, Rp("#3a3a48"), 0.1);
                f.Ellipse(15, 17, 2.6, 2, Hx("#9a9ab0"));
                f.ShadedRect(16, 8, 7, 4, Rp("#6a6a7a"));
                f.Line(19.5, 8, 22, 5, Hx("#c8a060"), 1.4); f.Line(22, 5, 26, 4, Hx("#c8a060"), 1.4);
                g.Glow(27, 4, 8, Hx("#ffb040"), 220);
                Star(f, 27, 4, 4.4, 1.5, 6, Hx("#ffe040"));
                f.Set(27, 4, White);
                break;
            }
            case "phantom_step":
                g.Glow(20, 20, 19, m[3], 110);
                Swirl(f, 9, 30, 1, 7, 1, 90, Rp("#3a2a5a")[3].WithA(220), 2);
                for (int k = 2; k >= 1; k--)
                {
                    var ghost = new Canvas(Size, Size);
                    Sword(ghost, 9 - k * 6, 33, 28 - k * 6, 14, Rp("#c0b0f0"), 3.4, 6);
                    byte a = (byte)(170 - k * 50);
                    ghost.Map(c => Pal.Shift(c, 0.2).WithA(a));
                    f.Blit(ghost, 0, 0);
                }
                Sword(f, 12, 33, 33, 12, Rp("#c0b0f0"), 3.6, 6);
                break;
            case "predators_eye":
                g.Glow(20, 20, 19, m[3], 130);
                var red = Hx("#ff4040");
                f.Line(20, 1, 20, 7, red, 1.6); f.Line(20, 33, 20, 39, red, 1.6); f.Line(1, 20, 4, 20, red, 1.6); f.Line(36, 20, 39, 20, red, 1.6);
                Eye(f, 20, 20, 16, 8.5, Hx("#f8f0d8"), Rp("#e0b020"), true);
                break;
            case "blade_tempest":
                g.Glow(20, 20, 19, m[3], 150);
                for (int k = 0; k < 8; k++)
                {
                    var (ix, iy) = Polar(20, 20, k * 45 - 67.5, 6); var (ox, oy) = Polar(20, 20, k * 45 - 67.5, 19);
                    Lens(f, ix, iy, ox, oy, 3.6, steel[2]);
                    Lens(f, ix, iy, ox, oy, 1.2, steel[4]);
                    f.Shaded(ix, iy, 1.4, 1.4, m);
                }
                f.Shaded(20, 20, 3, 3, m, 0.2);
                break;
            case "mark_of_death":
                g.Glow(20, 20, 19, m[3], 140);
                Ring(f, 20, 20, 16, 16, m[3], 1.6);
                f.Line(20, 1, 20, 7, m[3], 1.6); f.Line(20, 33, 20, 39, m[3], 1.6); f.Line(1, 20, 7, 20, m[3], 1.6); f.Line(33, 20, 39, 20, m[3], 1.6);
                Skull(f, 20, 18, 7.5, Rp("#e8dcc8"), Hx("#ff2040"));
                break;
            case "hundred_petal_dance":
                g.Glow(20, 20, 19, m[3], 130);
                Sword(f, 7, 33, 33, 7, steel, 2.8, 6);
                for (int k = 0; k < 10; k++)
                {
                    double t = k / 9.0, a = t * 400, r = 4 + t * 13;
                    var (px, py) = Polar(20, 20, a, r);
                    var (x0, y0) = Polar(px, py, a + 90, -3); var (x1, y1) = Polar(px, py, a + 90, 3);
                    Lens(f, x0, y0, x1, y1, 3.4, k % 2 == 0 ? m[3] : m[4]);
                }
                break;
            case "quicksilver":
                g.Glow(20, 20, 19, m[3], 130);
                f.Poly(Pts(11, 7, 29, 7, 21.5, 20, 29, 33, 11, 33, 18.5, 20), Hx("#c8f0ff").WithA(150));
                f.Poly(Pts(13.5, 10, 26.5, 10, 21, 18.5, 19, 18.5), m[3]);
                f.Poly(Pts(12.5, 33, 27.5, 33, 20, 26.5), m[3]);
                f.Line(20, 19, 20, 28, m[4]);
                f.Line(10.5, 7, 10.5, 33, steel[1], 1.4); f.Line(29.5, 7, 29.5, 33, steel[1], 1.4);
                f.ShadedRect(8, 4, 24, 3, steel); f.ShadedRect(8, 33, 24, 3, steel);
                f.Line(1, 14, 6, 14, m[4]); f.Line(2, 20, 8, 20, m[4]); f.Line(1, 26, 6, 26, m[4]);
                break;
            case "final_whisper":
                g.Glow(24, 13, 16, Hx("#ffd8e0"), 120);
                f.Ellipse(25, 13, 10, 10, Hx("#f0e0e8"));
                Cut(f, 29.5, 10, 9, 9);
                Sword(f, 7, 34, 27, 14, Rp("#c8c0d0"), 3.4, 6);
                Drop(f, 29, 21, 1.4, Rp("#c01830")); Drop(f, 25, 27, 1.6, Rp("#c01830")); Drop(f, 30, 30, 1.2, Rp("#c01830"));
                break;
            case "eclipse_of_blades":
                g.Glow(20, 20, 19, m[4], 210);
                Star(f, 20, 20, 18, 12, 16, m[3]);
                Star(f, 20, 20, 15, 11, 16, m[4], -78.75);
                f.Ellipse(20, 20, 10.5, 10.5, Hx("#0a0610"));
                Ring(f, 20, 20, 10.5, 10.5, White, 1);
                Lens(f, 3, 31, 37, 9, 2.8, White);
                Lens(f, 6, 7, 34, 33, 2, m[4]);
                break;

            // ---- Mystic / Stormcaller ----
            case "foxfire":
                g.Glow(23, 17, 17, Hx("#ff9040"), 180);
                for (int k = 8; k >= 0; k--)
                {
                    double t = k / 8.0, r = 8.5 * (1 - t) + 1;
                    f.Ellipse(24 - t * 19, 16 + t * 19, r, r, Rp("#e04018")[2].WithA((byte)(235 - t * 140)));
                }
                Lens(f, 26, 9, 31, 2, 3, Rp("#ff8030")[3]); Lens(f, 30, 13, 37, 9, 3, Rp("#ff8030")[3]);
                f.Shaded(24, 16, 8.5, 8.5, Rp("#ff7020"), 0.25);
                f.Ellipse(25, 15, 5.2, 5.2, Hx("#ffd070"));
                f.Ellipse(26, 14, 2.6, 2.6, White);
                break;
            case "frost_lance":
                g.Glow(20, 20, 19, m[3], 140);
                Lens(f, 8, 32, 3, 26, 3.4, m[2]); Lens(f, 8, 32, 14, 37, 3.4, m[2]);
                Lens(f, 4, 36, 36, 4, 7, m[3]);
                Lens(f, 4, 36, 36, 4, 3, m[4]);
                f.Line(9, 31, 33, 7, White);
                Star(f, 31, 28, 3.6, 1, 4, White); Star(f, 9, 10, 3, 0.9, 4, White);
                break;
            case "thunder_seal":
                g.Glow(20, 20, 19, m[3], 160);
                Ring(f, 20, 20, 17, 17, m[3], 1.6);
                Ring(f, 20, 20, 12.5, 12.5, m[3]);
                for (int k = 0; k < 8; k++)
                {
                    var (x0, y0) = Polar(20, 20, k * 45 + 22.5, 13); var (x1, y1) = Polar(20, 20, k * 45 + 22.5, 16.4);
                    f.Line(x0, y0, x1, y1, m[4]);
                    var (rx, ry) = Polar(20, 20, k * 45, 14.8);
                    f.Rect((int)rx - 1, (int)ry - 1, 2, 2, m[4]);
                }
                Path(f, m[2], 1, 20, 7.5, 30.8, 26.2, 9.2, 26.2, 20, 7.5);
                Bolt(f, Hx("#fff070"), 3, 22.5, 8, 16, 20, 23, 20, 17, 32);
                break;
            case "thunderstrike":
            {
                g.Glow(20, 26, 17, m[3], 170);
                Bolt(f, m[3], 3.6, 22, 12, 15.5, 22, 23, 22, 16, 36);
                Star(f, 16, 36, 6, 2, 6, White);
                var cloud = Rp("#6a6a88");
                f.Shaded(11, 10, 7, 5, cloud, 0.1); f.Shaded(29, 10, 7, 5, cloud, 0.1); f.Shaded(20, 7, 9, 6.4, cloud, 0.15);
                f.Rect(6, 11, 28, 3, cloud[1]);
                break;
            }
            case "dragons_breath":
            {
                g.Glow(28, 22, 16, Hx("#ff9030"), 170);
                f.Poly(Pts(15, 19, 39, 7, 39, 37, 15, 25), Rp("#e85018")[2]);
                f.Poly(Pts(15, 20, 39, 13, 39, 31, 15, 24), Rp("#f8a030")[2]);
                f.Poly(Pts(15, 21, 37, 19, 37, 25, 15, 23), Hx("#fff0a0"));
                var hide = Rp("#8a2a20");
                f.Poly(Pts(1, 12, 8, 9, 14, 12, 18, 18, 14, 19.5, 18, 22, 13, 26, 6, 27, 1, 24), hide[2]);
                f.Poly(Pts(1, 12, 8, 9, 14, 12, 9, 15, 2, 18), hide[3]);
                f.Poly(Pts(4, 11, 0, 2, 8, 9), Rp("#e8d8b0")[2]);
                f.Rect(9, 13, 2, 2, Hx("#ffe040"));
                f.Set(15, 15, dark);
                f.Poly(Pts(14.6, 18.6, 16.4, 18.4, 15.5, 20.4), White);
                break;
            }
            case "arcane_aegis":
            {
                g.Glow(20, 20, 19, m[3], 180);
                var hex = Enumerable.Range(0, 6).Select(k => Polar(20, 20, k * 60 - 90, 17.5)).ToArray();
                var inner = Enumerable.Range(0, 6).Select(k => Polar(20, 20, k * 60 - 90, 9)).ToArray();
                f.Poly(hex, m[2].WithA(160));
                for (int k = 0; k < 6; k++)
                {
                    var (a, b) = (hex[k], hex[(k + 1) % 6]);
                    f.Line(a.X, a.Y, b.X, b.Y, m[4], 1.8);
                    f.Line(inner[k].X, inner[k].Y, inner[(k + 1) % 6].X, inner[(k + 1) % 6].Y, m[3]);
                    f.Line(inner[k].X, inner[k].Y, a.X, a.Y, m[3]);
                }
                Star(f, 20, 20, 5, 1.6, 4, White);
                break;
            }
            case "howling_blizzard":
                g.Glow(20, 20, 19, m[3], 130);
                Ring(f, 20, 20, 18, 14, m[3], 1.4, 195, 300); Ring(f, 20, 20, 16, 12, m[3], 1.4, 15, 120);
                Flake(f, 20, 20, 12, White, 2);
                Flake(f, 6, 8, 3.4, m[4], 1); Flake(f, 33, 32, 3.4, m[4], 1); Flake(f, 33, 7, 2.6, m[4], 1);
                break;
            case "meteor_strike":
            {
                g.Glow(15, 26, 17, Hx("#ff8030"), 170);
                for (int k = 10; k >= 0; k--)
                {
                    double t = k / 10.0, r = 8.5 * (1 - t * 0.8);
                    f.Ellipse(16 + t * 21, 25 - t * 21, r, r, Rp("#f05020")[2].WithA((byte)(235 - t * 150)));
                    f.Ellipse(16 + t * 21, 25 - t * 21, r * 0.5, r * 0.5, Hx("#ffd060").WithA((byte)(200 - t * 150)));
                }
                f.Shaded(15, 26, 8, 8, Rp("#6a4038"));
                var lava = Hx("#ffb040");
                f.Line(10, 23, 15, 27, lava); f.Line(15, 27, 20, 25, lava); f.Line(15, 27, 14, 32, lava);
                Ring(f, 15, 26, 8.6, 8.6, Hx("#ffd040"), 1.4, 100, 200);
                break;
            }
            case "chain_lightning":
                g.Glow(20, 20, 19, m[3], 160);
                Bolt(f, m[3], 2.4, 6, 31, 10, 22, 8, 19, 16, 9);
                Bolt(f, m[3], 2.4, 16, 9, 19, 20, 22, 18, 26, 31);
                Bolt(f, m[3], 2.4, 26, 31, 29, 20, 32, 22, 35, 10);
                foreach (var (x, y) in new[] { (6.0, 31.0), (16.0, 9.0), (26.0, 31.0), (35.0, 10.0) }) { f.Shaded(x, y, 3, 3, m, 0.3); f.Set(x, y, White); }
                break;
            case "absolute_zero":
                g.Glow(20, 20, 19, m[3], 150);
                for (int k = 0; k < 8; k++)
                {
                    var (x, y) = Polar(20, 20, k * 45 - 90, k % 2 == 0 ? 19 : 13);
                    Lens(f, 20, 20, x, y, 6, m[3]);
                    Lens(f, 20, 20, x, y, 2.2, White);
                }
                f.Shaded(20, 20, 4, 4, m, 0.3);
                break;
            case "overcharge":
                g.Glow(20, 20, 19, m[3], 210);
                f.Shaded(20, 20, 9, 9, m, 0.2);
                f.Ellipse(17, 17, 2.5, 2, White.WithA(210));
                var arc = Hx("#f0d8ff");
                Bolt(f, arc, 1.6, 12, 13, 7, 10, 8, 6, 4, 3);
                Bolt(f, arc, 1.6, 28, 13, 33, 10, 32, 6, 36, 3);
                Bolt(f, arc, 1.6, 28, 27, 32, 31, 31, 34, 36, 37);
                Bolt(f, arc, 1.6, 12, 27, 7, 30, 9, 34, 4, 37);
                break;
            case "phoenix_inferno":
            {
                g.Glow(20, 18, 19, Hx("#ff8020"), 180);
                var fl = Rp("#f05010");
                var wing = Pts(18, 16, 12, 10, 6, 4, 1, 3, 4, 9, 1, 11, 6, 15, 2, 18, 9, 20, 7, 23, 17, 22);
                f.Poly(wing, fl[2]);
                f.Poly(wing.Select(p => (40 - p.X, p.Y)).ToArray(), fl[2]);
                f.Poly(Pts(18, 17, 11, 12, 7, 9, 8, 14, 12, 19, 17, 21), fl[3]);
                f.Poly(Pts(22, 17, 29, 12, 33, 9, 32, 14, 28, 19, 23, 21), fl[3]);
                Lens(f, 20, 22, 13, 38, 5, fl[2], 2); Lens(f, 20, 22, 27, 38, 5, fl[2], -2); Lens(f, 20, 22, 20, 39, 5, fl[3]);
                f.Shaded(20, 18, 4, 6, Rp("#ffb030"), 0.2);
                Lens(f, 20, 9, 23, 2, 2.4, fl[3]);
                f.Shaded(20, 10, 3, 3, Rp("#ffd040"));
                f.Poly(Pts(19, 11.5, 21, 11.5, 20, 14.5), Hx("#fff0a0"));
                break;
            }
            case "nine_heavens":
            {
                g.Glow(20, 30, 19, Hx("#d0d8ff"), 170);
                var bolt = Hx("#fff4c0");
                Bolt(f, bolt, 2.6, 9, 9, 6, 20, 11, 20, 7, 35);
                Bolt(f, bolt, 2.8, 20, 10, 23, 21, 17.5, 22, 21, 38);
                Bolt(f, bolt, 2.6, 31, 9, 34, 19, 29, 20, 33, 34);
                var cloud = Rp("#4a4a70");
                f.Shaded(8, 7, 8, 5, cloud, 0.1); f.Shaded(32, 7, 8, 5, cloud, 0.1); f.Shaded(20, 5, 9, 5.6, cloud, 0.15);
                f.Rect(2, 8, 36, 3, cloud[1]);
                break;
            }

            // ---- Monk / Lotus Saint ----
            case "sunbeam":
                g.Glow(13, 12, 15, Hx("#ffe080"), 210);
                f.Poly(Pts(8, 16, 16, 7, 39, 30, 30, 39), Hx("#fff0a0").WithA(170));
                f.Poly(Pts(11, 14, 14, 11, 37, 33, 33, 37), White.WithA(210));
                Star(f, 12, 12, 11, 7, 12, Hx("#ffc030"));
                f.Shaded(12, 12, 6.5, 6.5, Rp("#ffd040"), 0.2);
                break;
            case "mending_light":
                g.Glow(20, 20, 19, m[3], 200);
                f.Shaded(20, 20, 12.5, 12.5, m, 0.25);
                f.Rect(17, 10, 6, 20, White); f.Rect(10, 17, 20, 6, White);
                Star(f, 6, 7, 4, 1, 4, White); Star(f, 34, 31, 3.6, 1, 4, White); Star(f, 33, 7, 2.6, 0.8, 4, m[4]);
                break;
            case "golden_blessing":
                g.Glow(20, 16, 19, Hx("#ffe080"), 180);
                for (int k = -2; k <= 2; k++) f.Line(20 + k * 2.4, 14, 20 + k * 7.5, 38, Hx("#fff0b0").WithA(150), 1.6);
                Ring(f, 20, 10, 12, 4.5, gold[3], 2.6);
                Ring(f, 20, 10, 12, 4.5, White, 1, 180, 360);
                Star(f, 8, 25, 3.4, 1, 4, White); Star(f, 32, 29, 3.4, 1, 4, White); Star(f, 20, 31, 2.6, 0.8, 4, m[4]);
                break;
            case "return_of_the_soul":
                g.Glow(20, 16, 19, Hx("#d8e0ff"), 170);
                f.Rect(0, 35, 40, 5, Rp("#4a5a40")[2]); f.Rect(0, 35, 40, 1, Rp("#4a5a40")[3]);
                Ghost(f, 20, 11, 6.5, Rp("#e8eeff"), 0.6, false);
                f.Line(12, 32, 20, 27, m[4], 1.8); f.Line(20, 27, 28, 32, m[4], 1.8);
                f.Line(14, 36, 20, 32.5, m[4].WithA(150), 1.4); f.Line(20, 32.5, 26, 36, m[4].WithA(150), 1.4);
                break;
            case "spring_of_life":
                g.Glow(20, 22, 19, m[3], 170);
                Drop(f, 20, 25, 10, Rp("#58c0d8"));
                f.Line(20, 33, 20, 22, Hx("#3a8a40"), 1.6);
                Lens(f, 20, 25, 12, 20, 4.6, Hx("#70d060"), -1.5);
                Lens(f, 20, 23, 28, 17, 4.6, Hx("#90e070"), 1.5);
                f.Ellipse(14.5, 21, 1.4, 3, White.WithA(210));
                Star(f, 7, 9, 3.4, 1, 4, White); Star(f, 33, 31, 3, 0.9, 4, White); Star(f, 32, 8, 2.4, 0.8, 4, m[4]);
                break;
            case "golden_bell":
                g.Glow(20, 20, 19, Hx("#ffe070"), 180);
                Ring(f, 20, 21, 18, 16, gold[4].WithA(190), 1.2, 150, 210); Ring(f, 20, 21, 18, 16, gold[4].WithA(190), 1.2, -30, 30);
                Ring(f, 20, 4.6, 2.6, 2.4, gold[1], 1.4);
                f.Poly(Pts(13, 13, 16, 7, 24, 7, 27, 13, 29, 28, 33, 32, 7, 32, 11, 28), gold[2]);
                f.Poly(Pts(13, 13, 16, 7, 18, 7, 16, 13, 14, 28, 11, 28), gold[3]);
                f.Line(13, 14, 27, 14, gold[1]); f.Line(11, 27, 29, 27, gold[1], 1.4);
                for (int x = 15; x <= 25; x += 5) f.Shaded(x, 20, 1, 1, gold);
                f.Shaded(20, 34.5, 2.4, 2.4, gold);
                break;
            case "pillar_of_light":
                g.Glow(20, 20, 17, Hx("#fff0a0"), 210);
                f.Ellipse(20, 34, 17, 4.4, Hx("#ffd860").WithA(220));
                f.Rect(13, 0, 14, 34, Hx("#ffe880").WithA(210));
                f.Rect(16, 0, 8, 35, Hx("#fffbe0").WithA(235));
                f.Rect(19, 0, 2, 35, White);
                f.Ellipse(20, 34, 9, 2.4, White);
                Star(f, 7, 22, 3, 0.9, 4, White); Star(f, 33, 15, 3, 0.9, 4, White); Star(f, 32, 28, 2.4, 0.8, 4, m[4]);
                break;
            case "thunder_palm":
            {
                g.Glow(20, 20, 19, Hx("#fff070"), 180);
                Ring(f, 20, 20, 16, 16, Hx("#fff8c0").WithA(160), 1.2);
                var arcCol = Hx("#ffe040");
                Bolt(f, arcCol, 1.8, 4, 6, 8, 11, 5, 13, 9, 18);
                Bolt(f, arcCol, 1.8, 36, 6, 32, 11, 35, 13, 31, 18);
                Bolt(f, arcCol, 1.6, 3, 31, 7, 28, 6, 33, 10, 34);
                Palm(f, 20, 24, 6.4, Rp("#f0c090"));
                break;
            }
            case "thousand_petal_lotus":
                g.Glow(20, 22, 19, m[3], 180);
                f.Ellipse(20, 32, 14, 3, Rp("#4a9a60")[2]);
                foreach (var a in new[] { -150.0, -120, -90, -60, -30 })
                {
                    var (x, y) = Polar(20, 30, a, 18);
                    Lens(f, 20, 30, x, y, 7, m[2]); Lens(f, 20, 30, x, y, 2.4, m[3]);
                }
                foreach (var a in new[] { -165.0, -15 }) { var (x, y) = Polar(20, 31, a, 14); Lens(f, 20, 31, x, y, 7, m[3]); }
                foreach (var a in new[] { -112.0, -68 }) { var (x, y) = Polar(20, 31, a, 12); Lens(f, 20, 31, x, y, 6.6, m[4]); }
                f.Shaded(20, 27, 3, 3, Rp("#ffe080"), 0.3);
                break;
            case "sanctuary":
                g.Glow(20, 26, 19, Hx("#fff0b0"), 180);
                for (int y = 12; y <= 30; y++)
                    for (int x = 2; x <= 38; x++)
                        if (Math.Pow((x + 0.5 - 20) / 17.0, 2) + Math.Pow((y + 0.5 - 30) / 17.0, 2) <= 1) f.Set(x, y, Hx("#fff8d0").WithA(80));
                f.Ellipse(20, 31.5, 19, 3.6, Rp("#c0a060")[2]);
                Ring(f, 20, 30, 17, 6, m[3].WithA(170), 1, 180, 360);
                Ring(f, 20, 30, 8, 17, m[3].WithA(170), 1, 180, 360);
                Ring(f, 20, 30, 17, 17, White, 1.6, 180, 360);
                f.Shaded(20, 22, 2.6, 2.6, Rp("#f0c090"));
                f.Poly(Pts(16.5, 30, 20, 24.5, 23.5, 30), Rp("#e0a030")[2]);
                Star(f, 8, 8, 3, 0.9, 4, White); Star(f, 33, 10, 2.4, 0.8, 4, White);
                break;
            case "prism_of_mercy":
                g.Glow(20, 20, 19, m[3], 160);
                f.Line(0, 25, 15, 20.5, White, 2.2);
                f.Line(25, 18, 39, 10, Hx("#ff7070"), 2.2);
                f.Line(26, 20, 39, 20, Hx("#ffe060"), 2.2);
                f.Line(25, 22, 39, 30, Hx("#70c0ff"), 2.2);
                f.Poly(Pts(20, 6, 31.5, 31, 8.5, 31), Hx("#e8f4ff").WithA(200));
                f.Line(20, 6, 22, 31, Hx("#c0d8f0"));
                Path(f, White, 1, 20, 6, 31.5, 31, 8.5, 31, 20, 6);
                break;
            case "avatar_of_mercy":
            {
                g.Glow(20, 18, 19, Hx("#fff0ff"), 190);
                Star(f, 20, 13, 14, 9.5, 16, Hx("#ffe8a0").WithA(210));
                Ring(f, 20, 13, 9, 9, gold[3], 1.6);
                var skin = Rp("#f0c8a0");
                f.Ellipse(20, 35.5, 15, 2.8, Rp("#f8a0c8")[2]);
                Lens(f, 6, 35, 14, 33, 3, Rp("#f8a0c8")[3], 2); Lens(f, 34, 35, 26, 33, 3, Rp("#f8a0c8")[3], -2);
                f.Shaded(20, 31, 12, 4, Rp("#e0d0f0"), 0.1);
                f.Poly(Pts(20, 16, 27, 21, 29, 31, 11, 31, 13, 21), Rp("#f0e0ff")[2]);
                f.Poly(Pts(20, 16, 13, 21, 11, 31, 16, 31, 17, 21), Rp("#f0e0ff")[3]);
                f.Poly(Pts(19, 20, 21, 20, 20.6, 25.5, 19.4, 25.5), skin[2]);
                f.Shaded(20, 13, 4, 4.4, skin, 0.1);
                f.Shaded(20, 8.4, 2.2, 1.7, Rp("#3a2a3a"));
                break;
            }
            case "heavens_gate":
            {
                g.Glow(20, 20, 19, Hx("#fff8d0"), 210);
                var lacquer = Rp("#c03030");
                f.Rect(11, 9, 18, 26, Hx("#fff8d8"));
                for (int k = -2; k <= 2; k++) f.Line(20, 22, 20 + k * 4, 9, Hx("#ffe890"), 1);
                f.ShadedRect(6, 8, 5, 28, lacquer); f.ShadedRect(29, 8, 5, 28, lacquer);
                f.ShadedRect(5, 11, 30, 2, lacquer);
                f.Poly(Pts(0, 3, 6, 5, 34, 5, 40, 3, 37, 9, 3, 9), Rp("#4a4a68")[2]);
                f.Line(3, 6, 37, 6, Rp("#4a4a68")[3]);
                var cloud = Rp("#f0f0ff");
                f.Shaded(7, 36, 7.5, 3.6, cloud, 0.2); f.Shaded(33, 36, 7.5, 3.6, cloud, 0.2); f.Shaded(20, 37, 9, 3.6, cloud, 0.2);
                break;
            }

            // ---- Hunter / Windstrider ----
            case "eagle_shot":
                g.Glow(26, 14, 15, m[3], 150);
                f.Line(3, 25, 13, 15, m[3]); f.Line(7, 32, 17, 22, m[3]); f.Line(2, 18, 9, 11, m[3]);
                Arrow(f, 5, 35, 33, 7, steel[3], Hx("#7a5030"), 1.25);
                Star(f, 34, 6, 4.4, 1.2, 4, White);
                break;
            case "snare_shot":
            {
                g.Glow(20, 30, 17, m[3], 130);
                var earth = Rp("#6a5030");
                f.Ellipse(20, 33, 17, 5.4, earth[2]); f.Ellipse(20, 32, 14, 3.6, earth[3]);
                Ring(f, 20, 31.5, 11, 3.6, Hx("#d8c080"), 1.6);
                Arrow(f, 27, 3, 20, 31, steel[2], Hx("#e8e0d0"), 1.25);
                f.Ellipse(20, 35, 13, 2.6, earth[2]);
                f.Line(29, 30, 34, 26, Hx("#d8c080"), 1.4);
                break;
            }
            case "hawk_eye":
            {
                g.Glow(20, 20, 19, m[3], 130);
                var plume = Rp("#7a5030");
                f.Shaded(19, 21, 12, 12, plume, 0.1);
                for (int k = 0; k < 4; k++) f.Line(9 + k * 3, 26 + k, 12 + k * 3, 33, plume[1]);
                f.Poly(Pts(18, 22, 30, 19, 31, 27, 23, 32, 15, 31), Rp("#f0e8d8")[2]);
                f.Poly(Pts(27, 14, 37, 17, 38, 24, 33, 22, 28, 22), Rp("#f0c040")[2]);
                f.Poly(Pts(35, 20, 38, 17, 38.5, 25), gold[1]);
                f.Line(18, 12.5, 29, 14, plume[0], 1.8);
                f.Shaded(24, 17, 3.2, 3.2, Rp("#ffd030"));
                f.Ellipse(24.5, 17, 1.5, 1.5, dark);
                f.Set(23, 16, White);
                break;
            }
            case "split_shot":
                g.Glow(28, 14, 15, m[3], 150);
                Arrow(f, 5, 33, 20, 3, steel[3], null, 1);
                Arrow(f, 7, 35, 37, 19, steel[3], null, 1);
                Arrow(f, 5, 35, 35, 5, steel[3], null, 1.1);
                break;
            case "bone_piercer":
            {
                g.Glow(20, 20, 19, m[3], 130);
                var bone = Rp("#e8dcc0");
                foreach (var x in new[] { 11.0, 20, 29 }) Ring(f, x, 20, 2.6, 7.5, bone[1], 2.2, 90, 270);
                Arrow(f, 1, 20, 38, 20, steel[3], Hx("#f0ece0"), 1.25);
                foreach (var x in new[] { 11.0, 20, 29 }) Ring(f, x, 20, 2.6, 7.5, bone[3], 2.2, -90, 90);
                f.Line(13, 13, 15, 10, bone[3]); f.Line(31, 26, 33, 29, bone[3]);
                break;
            }
            case "fleet_foot":
            {
                g.Glow(20, 20, 19, m[3], 130);
                var leather = Rp("#8a5a32");
                Lens(f, 14, 13, 2, 6, 4, White); Lens(f, 14, 16, 2, 14, 3.6, White); Lens(f, 14, 19, 4, 21, 3, White);
                f.Poly(Pts(14, 6, 23, 6, 23, 23, 33, 27, 35, 33, 13, 33), leather[2]);
                f.Poly(Pts(14, 6, 17, 6, 17, 33, 13, 33), leather[3]);
                f.ShadedRect(13, 5, 11, 4, Rp("#b08050"));
                f.Rect(13, 32, 23, 3, Rp("#4a3020")[2]);
                f.Line(18, 14, 22, 14, leather[1]); f.Line(18, 18, 22, 18, leather[1]); f.Line(18, 22, 22, 22, leather[1]);
                f.Line(1, 29, 9, 29, m[4]); f.Line(3, 34, 10, 34, m[4]);
                break;
            }
            case "serpent_arrow":
            {
                g.Glow(28, 12, 15, Hx("#80f060"), 170);
                Arrow(f, 4, 36, 32, 8, Hx("#60d040"), Hx("#e8e0d0"), 1.3);
                var coil = new List<double>();
                double hx = 0, hy = 0;
                for (int i = 0; i <= 24; i++)
                {
                    double t = 0.15 + i / 24.0 * 0.55, off = Math.Sin(t * Math.PI * 6) * 3.2;
                    hx = 4 + 28 * t + off * 0.707; hy = 36 - 28 * t + off * 0.707;
                    coil.Add(hx); coil.Add(hy);
                }
                Path(f, m[2], 2, coil.ToArray());
                f.Shaded(hx, hy, 2, 1.7, m);
                Drop(f, 34, 15, 1.4, Rp("#80f060")); Drop(f, 30, 20, 1.2, Rp("#80f060"));
                break;
            }
            case "arrow_rain":
                g.Glow(20, 30, 17, m[3], 130);
                f.Rect(0, 37, 40, 3, Rp("#5a4a30")[2]);
                foreach (var (hx, hy) in new[] { (5.0, 16.0), (14.0, 20.0), (23.0, 15.0), (33.0, 19.0), (9.0, 34.0), (19.0, 36.0), (29.0, 34.0) })
                    Arrow(f, hx + 4, hy - 13, hx, hy, steel[3], null, 0.75);
                break;
            case "typhoon_arrow":
                g.Glow(20, 20, 19, m[3], 150);
                Swirl(f, 20, 20, 3, 17, 1.7, 0, m[3], 2.2);
                Arrow(f, 4, 36, 35, 5, steel[3], Hx("#f0fff8"), 1.3);
                break;
            case "arrow_storm":
                g.Glow(20, 20, 19, m[3], 140);
                for (int k = 0; k < 7; k++) Ring(f, 20 + Math.Sin(k * 1.3) * 1.6, 6 + k * 4.6, 17 - k * 2.2, 2.2, k % 2 == 0 ? m[3] : m[2], 1.6);
                Arrow(f, 4, 10, 15, 8, steel[3], null, 0.65);
                Arrow(f, 36, 17, 25, 15, steel[3], null, 0.65);
                Arrow(f, 9, 22, 20, 25, steel[3], null, 0.65);
                Arrow(f, 30, 30, 22, 33, steel[3], null, 0.6);
                break;
            case "ride_the_wind":
                g.Glow(20, 20, 19, m[3], 140);
                Swirl(f, 11, 27, 1, 7, 1, 180, m[3], 1.6);
                Ring(f, 22, 30, 16, 7, m[3], 1.4, 200, 320);
                Lens(f, 8, 33, 34, 6, 10, White, -2.5);
                for (int k = 1; k < 6; k++) { double t = k / 6.0; double x = 8 + 26 * t, y = 33 - 27 * t; f.Line(x, y, x + 3, y + 3.4, Rp("#a0c8c0")[2]); }
                f.Line(8, 33, 34, 6, Rp("#a0c8c0")[1]);
                f.Line(4, 37, 9, 32, Rp("#d0c0a0")[2], 1.4);
                break;
            case "heartseeker":
                g.Glow(20, 21, 19, Hx("#ff6070"), 160);
                Arrow(f, 3, 34, 37, 8, steel[3], Hx("#ffe0e0"), 1.25);
                Heart(f, 20, 21, 10, Rp("#e02848"));
                f.Ellipse(14.6, 16.5, 1.6, 1.6, Hx("#ffc0c8"));
                Arrow(f, 26, 16.6, 37, 8, steel[3], null, 1.25, false);
                break;
            case "sky_splitter":
                g.Glow(20, 20, 19, White, 180);
                foreach (var (x, y) in new[] { (5, 6), (12, 3), (30, 34), (35, 27), (8, 15), (28, 4) }) f.Set(x, y, White);
                Lens(f, 37, 2, 3, 38, 6.5, Hx("#e0f8ff"));
                Lens(f, 37, 2, 3, 38, 2.4, White);
                f.Line(14, 26, 7, 24, White); f.Line(26, 14, 33, 17, White); f.Line(9, 32, 4, 33, White);
                Arrow(f, 22, 18, 36, 3, steel[4], White, 1.3);
                break;

            // ---- Shaman / Grand Shaman ----
            case "wailing_spirit":
                g.Glow(20, 18, 19, m[3], 160);
                f.Line(3, 18, 9, 18, m[4]); f.Line(2, 24, 8, 24, m[4]); f.Line(32, 20, 37, 20, m[4]);
                Ghost(f, 20, 13, 8.5, Rp("#e8d8ff"), 1.2, true);
                break;
            case "crows_hex":
            {
                g.Glow(20, 20, 19, m[3], 150);
                Ring(f, 20, 21, 17, 17, m[3], 1.4);
                var penta = Enumerable.Range(0, 5).Select(k => Polar(20, 21, k * 144 - 90, 17)).ToArray();
                for (int k = 0; k < 5; k++) f.Line(penta[k].X, penta[k].Y, penta[(k + 1) % 5].X, penta[(k + 1) % 5].Y, m[2]);
                var feather = Rp("#2a2438");
                f.Poly(Pts(12, 24, 3, 30, 6, 32, 14, 27), feather[2]);
                f.Shaded(19, 22, 8, 6, feather, 0.05);
                f.Poly(Pts(13, 18, 24, 19, 20, 27, 8, 27), Rp("#3a3450")[2]);
                f.Shaded(26, 14, 4.6, 4.6, feather);
                f.Poly(Pts(29.5, 12.5, 37, 15, 29.5, 16.6), Rp("#5a5a6a")[2]);
                f.Rect(26, 13, 2, 2, Hx("#ff3040"));
                f.Line(18, 28, 17, 32, Hx("#7a7a8a")); f.Line(21, 28, 22, 32, Hx("#7a7a8a"));
                break;
            }
            case "ancestor_totem":
            {
                g.Glow(20, 20, 19, m[3], 120);
                var log = Rp("#a07850");
                f.ShadedRect(12, 6, 16, 34, log);
                f.Ellipse(20, 7, 11, 2.6, Hx("#1c1c22"));
                f.Rect(14, 1, 12, 6, Hx("#1c1c22")); f.Rect(14, 1, 3, 6, Hx("#3a3a46"));
                f.Line(12, 11, 18, 12.5, dark, 1.6); f.Line(28, 11, 22, 12.5, dark, 1.6);
                f.Shaded(15.5, 15.5, 3, 3, Rp("#f0e8d8")); f.Shaded(24.5, 15.5, 3, 3, Rp("#f0e8d8"));
                f.Ellipse(15.5, 15.5, 1.3, 1.3, dark); f.Ellipse(24.5, 15.5, 1.3, 1.3, dark);
                f.Poly(Pts(20, 15, 23, 22, 17, 22), log[3]);
                f.Line(17, 22, 23, 22, log[1]);
                f.Rect(14, 25, 12, 4, Hx("#3a1010"));
                f.Rect(14, 25, 12, 1, White); f.Rect(15, 25, 1, 3, White); f.Rect(24, 25, 1, 3, White);
                f.Rect(12, 32, 16, 2, Hx("#c03030"));
                break;
            }
            case "soul_drain":
            {
                g.Glow(28, 12, 13, m[3], 180);
                var stream = new List<double>();
                for (int i = 0; i <= 16; i++) { double t = i / 16.0; stream.Add(12 + 16 * t + Math.Sin(t * Math.PI * 2) * 4); stream.Add(27 - 15 * t + Math.Sin(t * Math.PI * 2) * 3); }
                Path(f, m[3].WithA(200), 2.6, stream.ToArray());
                Skull(f, 11, 27, 6.4, Rp("#d8d0c0"), Hx("#ff4060"));
                f.Shaded(28, 12, 6, 6, m, 0.2);
                f.Ellipse(26.5, 10.5, 1.6, 1.6, White);
                f.Ellipse(33, 22, 1.2, 1.2, m[4]); f.Ellipse(20, 8, 1, 1, m[4]);
                break;
            }
            case "withering_curse":
            {
                g.Glow(20, 20, 19, Hx("#8a3aa0"), 140);
                var petal = Rp("#8a6030");
                Path(f, Hx("#5a5a30"), 1.6, 20, 38, 19, 28, 20, 18, 24, 13, 29, 16, 30, 20);
                Lens(f, 19, 30, 9, 26, 4.4, Rp("#7a6a30")[2], 1.5);
                Lens(f, 30, 21, 25, 30, 4.2, petal[2]); Lens(f, 30, 21, 34, 30, 4.2, petal[2]); Lens(f, 30, 21, 30, 32, 4.2, petal[3]);
                f.Shaded(30, 21, 2.4, 2.4, Rp("#5a4020"));
                Lens(f, 24, 35, 28, 37, 2.4, petal[2]); Lens(f, 9, 13, 12, 15, 2, petal[3]);
                Swirl(f, 11, 9, 1, 5, 1, 0, Rp("#c070e0")[2], 1.4);
                break;
            }
            case "dance_of_the_dead":
                g.Glow(20, 20, 19, m[3], 150);
                Swirl(f, 20, 21, 4, 17.5, 1.2, 0, Rp("#e8d8ff")[2].WithA(170), 2);
                f.Line(20, 26, 13, 37, Hx("#e03030"), 1.4); f.Line(20, 26, 27, 36, Hx("#3060e0"), 1.4); f.Line(20, 26, 17, 38, Hx("#f0d030"), 1.4);
                f.Line(20, 39, 20, 22, wood[2], 2.4);
                foreach (var (x, y) in new[] { (20.0, 7.0), (14.0, 12.0), (26.0, 12.0), (20.0, 14.0), (16.0, 19.0), (24.0, 19.0) }) Bell(f, x, y, gold);
                break;
            case "grave_chains":
            {
                g.Glow(20, 20, 19, m[3], 160);
                var iron = Rp("#a0a8c8");
                Chain(f, 2, 2, 38, 38, iron);
                Chain(f, 38, 2, 2, 38, iron);
                Ring(f, 20, 17, 3.6, 4.4, steel[2], 2, 180, 360);
                f.Line(16.4, 17, 16.4, 20, steel[2], 2); f.Line(23.6, 17, 23.6, 20, steel[2], 2);
                f.ShadedRect(14, 19, 12, 10, Rp("#6a6a88"));
                f.Ellipse(20, 23, 1.4, 1.4, dark); f.Line(20, 23, 20, 26, dark);
                break;
            }
            case "blood_pact":
                g.Glow(20, 22, 19, Hx("#ff2040"), 160);
                Ring(f, 20, 32, 15, 4.6, m[3], 1.4);
                for (int k = 0; k < 6; k++) f.Set(20 + Math.Cos(k * Math.PI / 3) * 15, 32 + Math.Sin(k * Math.PI / 3) * 4.6, m[4]);
                Drop(f, 20, 21, 8, Rp("#c81830"));
                f.Ellipse(16.6, 19, 1.8, 2.6, Hx("#ff8090"));
                Drop(f, 8, 26, 1.6, Rp("#c81830")); Drop(f, 32, 24, 1.4, Rp("#c81830"));
                break;
            case "night_parade":
                g.Glow(20, 20, 19, m[3], 150);
                Ghost(f, 31, 9, 4.4, Rp("#a898c8"), 0.8, false);
                Ghost(f, 22, 13, 6, Rp("#c8b8e8"), 1, false);
                Ghost(f, 11, 19, 8, Rp("#f0e8ff"), 1.2, true);
                g.Glow(33.5, 30, 8, Hx("#ffa040"), 220);
                f.Line(33.5, 22, 33.5, 26, dark);
                f.ShadedRect(31, 26, 5, 7, Rp("#e05030"));
                f.Rect(31, 26, 5, 1, gold[2]); f.Rect(31, 32, 5, 1, gold[2]);
                break;
            case "crimson_harvest":
                g.Glow(20, 18, 19, m[3], 160);
                f.Line(10, 39, 24, 6, wood[2], 2.4); f.Line(9.4, 38.6, 23.4, 5.6, wood[3]);
                Lens(f, 24, 7, 3, 17, 5, steel[2], 7);
                Lens(f, 24, 7, 3, 17, 1.6, White, 9);
                Lens(f, 24, 7, 3, 17, 1.4, m[3], 5);
                Drop(f, 6, 24, 1.4, Rp("#e02040")); Drop(f, 11, 28, 1.2, Rp("#e02040"));
                Swirl(f, 31, 27, 1, 5, 1, 90, m[3].WithA(200), 1.4);
                break;
            case "great_gut":
            {
                g.Glow(20, 20, 19, m[3], 160);
                var lacquer = Rp("#c03030");
                Ring(f, 20, 17, 19, 11, gold[4].WithA(160), 1, 195, 345);
                f.Line(7, 28, 3, 37, Hx("#3060e0"), 1.4); f.Line(33, 28, 37, 37, Hx("#f0d030"), 1.4);
                f.Ellipse(20, 30, 13, 4, lacquer[1]);
                f.ShadedRect(7, 17, 27, 13, lacquer);
                f.Line(7, 23, 33, 23, gold[2], 1.4);
                for (int x = 9; x <= 31; x += 4) f.Set(x, 19, gold[3]);
                f.Shaded(20, 17, 13, 5, Rp("#f0e0c0"), 0.2);
                Ring(f, 20, 17, 7, 2.6, lacquer[2]);
                f.Line(30, 3, 23, 13, wood[3], 2); f.Shaded(23, 13, 1.6, 1.6, Rp("#e0d0b0"));
                f.Line(10, 3, 16, 12, wood[3], 2); f.Shaded(16, 12, 1.6, 1.6, Rp("#e0d0b0"));
                break;
            }
            case "call_of_the_grave":
            {
                g.Glow(20, 26, 19, m[3], 170);
                var bone = Hx("#e8e0d0");
                SkeletonHand(f, 20, 33, 1, bone);
                SkeletonHand(f, 32, 34, 0.6, bone);
                SkeletonHand(f, 8, 35, 0.5, bone);
                var soil = Rp("#3a3048");
                f.Rect(0, 32, 40, 8, soil[2]); f.Rect(0, 32, 40, 1, soil[3]);
                f.Rect(6, 34, 3, 2, soil[1]); f.Rect(25, 36, 4, 2, soil[1]); f.Rect(15, 37, 2, 1, soil[3]);
                break;
            }
            case "underworld_gate":
                g.Glow(20, 20, 19, m[3], 190);
                f.Ellipse(20, 20, 17, 17, Hx("#1a0610"));
                Swirl(f, 20, 20, 2, 17, 2.2, 0, m[2], 2.2);
                Swirl(f, 20, 20, 2, 17, 2.2, 180, m[3], 1.6);
                Ring(f, 20, 20, 17, 17, m[3], 1.4);
                Skull(f, 20, 18, 5, Rp("#e8dcd0"), Hx("#ff2050"));
                break;

            default:
                throw new InvalidOperationException($"skill {id} has no picture in SkillIcons.Draw");
        }
    }

    // ---- shapes --------------------------------------------------------------------------
    private static readonly Rgba White = Pal.Hex("#ffffff");
    private static Rgba Hx(string hex) => Pal.Hex(hex);
    private static Rgba[] Rp(string hex) => Pal.Ramp(hex);

    private static (double X, double Y)[] Pts(params double[] v)
    {
        var p = new (double X, double Y)[v.Length / 2];
        for (int i = 0; i < p.Length; i++) p[i] = (v[2 * i], v[2 * i + 1]);
        return p;
    }

    // Degrees, clockwise from the right (screen y points down).
    private static (double X, double Y) Polar(double cx, double cy, double deg, double r) =>
        (cx + Math.Cos(deg * Math.PI / 180) * r, cy + Math.Sin(deg * Math.PI / 180) * r);

    private static void Path(Canvas c, Rgba col, double thick, params double[] xy)
    {
        var p = Pts(xy);
        for (int i = 0; i + 1 < p.Length; i++) c.Line(p[i].X, p[i].Y, p[i + 1].X, p[i + 1].Y, col, thick);
    }

    // A pointed, bulging stroke from one point to another: blades, slashes, petals, shards. With a
    // bend both edges bow the same way, which makes a crescent.
    private static void Lens(Canvas c, double x0, double y0, double x1, double y1, double w, Rgba col, double bend = 0)
    {
        double dx = x1 - x0, dy = y1 - y0, len = Math.Sqrt(dx * dx + dy * dy), px = -dy / len, py = dx / len;
        var pts = new List<(double X, double Y)>();
        const int n = 14;
        for (int i = 0; i <= n; i++)
        {
            double t = (double)i / n, s = Math.Sin(Math.PI * t), o = (bend + w / 2) * s;
            pts.Add((x0 + dx * t + px * o, y0 + dy * t + py * o));
        }
        for (int i = n; i >= 0; i--)
        {
            double t = (double)i / n, s = Math.Sin(Math.PI * t), o = (bend - w / 2) * s;
            pts.Add((x0 + dx * t + px * o, y0 + dy * t + py * o));
        }
        c.Poly(pts, col);
    }

    // A straight sword from the end of its grip to its tip.
    private static void Sword(Canvas c, double x0, double y0, double x1, double y1, Rgba[] steel, double w = 3.4, double grip = 7)
    {
        double dx = x1 - x0, dy = y1 - y0, len = Math.Sqrt(dx * dx + dy * dy), ux = dx / len, uy = dy / len, px = -uy, py = ux;
        double gx = x0 + ux * grip, gy = y0 + uy * grip, h = w / 2;
        var gold = Rp("#e0b04a");
        c.Line(x0, y0, gx, gy, Rp("#5a3a28")[2], 2.4);
        c.Poly(Pts(gx + px * h, gy + py * h, x1 - ux * w + px * h, y1 - uy * w + py * h, x1, y1,
                   x1 - ux * w - px * h, y1 - uy * w - py * h, gx - px * h, gy - py * h), steel[2]);
        c.Line(gx + px * h * 0.4, gy + py * h * 0.4, x1 - ux * w * 0.8 + px * h * 0.4, y1 - uy * w * 0.8 + py * h * 0.4, steel[4]);
        c.Line(gx - px * h * 0.8, gy - py * h * 0.8, x1 - ux * w - px * h * 0.8, y1 - uy * w - py * h * 0.8, steel[1]);
        c.Line(gx + px * w * 1.3, gy + py * w * 1.3, gx - px * w * 1.3, gy - py * w * 1.3, gold[2], 2.2);
        c.Shaded(x0, y0, 1.8, 1.8, gold);
    }

    // An arrow from its fletching to its head.
    private static void Arrow(Canvas c, double x0, double y0, double x1, double y1, Rgba head, Rgba? fletch = null, double k = 1, bool fletched = true)
    {
        double dx = x1 - x0, dy = y1 - y0, len = Math.Sqrt(dx * dx + dy * dy), ux = dx / len, uy = dy / len, px = -uy, py = ux;
        c.Line(x0, y0, x1 - ux * 4 * k, y1 - uy * 4 * k, Hx("#a07040"), 1.6 * k);
        c.Poly(Pts(x1, y1, x1 - ux * 6 * k + px * 3 * k, y1 - uy * 6 * k + py * 3 * k, x1 - ux * 4.5 * k, y1 - uy * 4.5 * k,
                   x1 - ux * 6 * k - px * 3 * k, y1 - uy * 6 * k - py * 3 * k), head);
        if (!fletched) return;
        var f = fletch ?? Hx("#f0ece0");
        for (int side = -1; side <= 1; side += 2)
            c.Poly(Pts(x0 + ux * 6 * k, y0 + uy * 6 * k, x0 + ux * 2 * k + px * side * 3 * k, y0 + uy * 2 * k + py * side * 3 * k,
                       x0 - ux * 1 * k + px * side * 3 * k, y0 - uy * 1 * k + py * side * 3 * k, x0, y0), f);
    }

    // A flame standing on (cx, by), w wide and h tall, its tip leaning by lean pixels; three layers hotter toward the core.
    private static void Flame(Canvas c, double cx, double by, double w, double h, Rgba[] ramp, double lean = 0)
    {
        void Layer(double ww, double hh, Rgba col)
        {
            var pts = new List<(double X, double Y)>();
            for (int i = 0; i < 32; i++)
            {
                double t = i / 32.0 * Math.PI * 2, up = (1 + Math.Cos(t)) / 2;
                pts.Add((cx + ww * Math.Sin(t) * Math.Pow(Math.Abs(Math.Sin(t / 2)), 1.3) + lean * up * up, by - hh * up));
            }
            c.Poly(pts, col);
        }
        Layer(w, h, ramp[2]);
        Layer(w * 0.66, h * 0.72, ramp[3]);
        Layer(w * 0.34, h * 0.44, Hx("#fff0b0"));
    }

    // A zigzag of lightning with a white-hot core.
    private static void Bolt(Canvas c, Rgba col, double thick, params double[] xy)
    {
        Path(c, col, thick, xy);
        Path(c, White, 1, xy);
    }

    // A dark split in the ground with lava glowing inside it.
    private static void Crack(Canvas c, double thick, params double[] xy)
    {
        Path(c, Hx("#2a160c"), thick, xy);
        Path(c, Hx("#ffa030"), Math.Max(1, thick - 2.2), xy);
    }

    private static void Star(Canvas c, double cx, double cy, double r1, double r2, int n, Rgba col, double rot = -90)
    {
        var p = new List<(double X, double Y)>();
        for (int i = 0; i < n * 2; i++) p.Add(Polar(cx, cy, rot + i * 180.0 / n, i % 2 == 0 ? r1 : r2));
        c.Poly(p, col);
    }

    // An elliptical ring or arc, from angle a0 to a1 in degrees.
    private static void Ring(Canvas c, double cx, double cy, double rx, double ry, Rgba col, double thick = 1, double a0 = 0, double a1 = 360)
    {
        int steps = (int)Math.Max(8, (a1 - a0) / 360 * (rx + ry) * 3);
        for (int i = 0; i < steps; i++)
        {
            double t0 = (a0 + (a1 - a0) * i / steps) * Math.PI / 180, t1 = (a0 + (a1 - a0) * (i + 1) / steps) * Math.PI / 180;
            c.Line(cx + Math.Cos(t0) * rx, cy + Math.Sin(t0) * ry, cx + Math.Cos(t1) * rx, cy + Math.Sin(t1) * ry, col, thick);
        }
    }

    // A spiral from radius r0 to r1, thickening as it goes out.
    private static void Swirl(Canvas c, double cx, double cy, double r0, double r1, double turns, double start, Rgba col, double thick)
    {
        int n = (int)(turns * 60);
        double px = cx + Math.Cos(start * Math.PI / 180) * r0, py = cy + Math.Sin(start * Math.PI / 180) * r0;
        for (int i = 1; i <= n; i++)
        {
            double t = (double)i / n, a = (start + t * turns * 360) * Math.PI / 180, r = r0 + (r1 - r0) * t;
            double x = cx + Math.Cos(a) * r, y = cy + Math.Sin(a) * r;
            c.Line(px, py, x, y, col, Math.Max(1, thick * (0.4 + 0.6 * t)));
            (px, py) = (x, y);
        }
    }

    // Clears an ellipse out of what is drawn so far (the dark side of a crescent moon).
    private static void Cut(Canvas c, double cx, double cy, double rx, double ry)
    {
        for (int y = (int)(cy - ry) - 1; y <= cy + ry + 1; y++)
            for (int x = (int)(cx - rx) - 1; x <= cx + rx + 1; x++)
                if (Math.Pow((x + 0.5 - cx) / rx, 2) + Math.Pow((y + 0.5 - cy) / ry, 2) <= 1) c.Erase(x, y);
    }

    private static void Heart(Canvas c, double cx, double cy, double s, Rgba[] ramp)
    {
        c.Poly(Pts(cx - s * 0.98, cy - s * 0.15, cx + s * 0.98, cy - s * 0.15, cx, cy + s * 1.05), ramp[2]);
        c.Shaded(cx - s * 0.5, cy - s * 0.35, s * 0.56, s * 0.56, ramp, 0.15);
        c.Shaded(cx + s * 0.5, cy - s * 0.35, s * 0.56, s * 0.56, ramp, 0.15);
    }

    private static void Drop(Canvas c, double cx, double cy, double r, Rgba[] ramp)
    {
        c.Poly(Pts(cx, cy - r * 2.3, cx + r * 0.88, cy - r * 0.45, cx - r * 0.88, cy - r * 0.45), ramp[2]);
        c.Shaded(cx, cy, r, r, ramp, 0.1);
    }

    private static void Flake(Canvas c, double cx, double cy, double r, Rgba col, double thick)
    {
        for (int k = 0; k < 6; k++)
        {
            double a = k * 60 - 90;
            var (ex, ey) = Polar(cx, cy, a, r);
            c.Line(cx, cy, ex, ey, col, thick);
            var (bx, by) = Polar(cx, cy, a, r * 0.55);
            foreach (var turn in new[] { -50.0, 50 }) { var (tx, ty) = Polar(bx, by, a + turn, r * 0.34); c.Line(bx, by, tx, ty, col, Math.Max(1, thick * 0.7)); }
        }
    }

    private static void Skull(Canvas c, double cx, double cy, double s, Rgba[] bone, Rgba eyes)
    {
        c.ShadedRect((int)Math.Round(cx - s * 0.55), (int)Math.Round(cy + s * 0.5), (int)Math.Round(s * 1.1), (int)Math.Round(s * 0.62), bone);
        c.Shaded(cx, cy, s, s * 0.9, bone, 0.15);
        var hole = Hx("#1a0c14");
        c.Ellipse(cx - s * 0.4, cy + s * 0.12, s * 0.28, s * 0.3, hole);
        c.Ellipse(cx + s * 0.4, cy + s * 0.12, s * 0.28, s * 0.3, hole);
        c.Ellipse(cx - s * 0.4, cy + s * 0.12, s * 0.11, s * 0.11, eyes);
        c.Ellipse(cx + s * 0.4, cy + s * 0.12, s * 0.11, s * 0.11, eyes);
        c.Poly(Pts(cx, cy + s * 0.38, cx - s * 0.14, cy + s * 0.62, cx + s * 0.14, cy + s * 0.62), hole);
        for (int k = 0; k < 3; k++) c.Line(cx - s * 0.25 + k * s * 0.25, cy + s * 0.78, cx - s * 0.25 + k * s * 0.25, cy + s * 1.08, bone[1]);
    }

    private static void Ghost(Canvas c, double cx, double cy, double s, Rgba[] ramp, double tail, bool wail)
    {
        var pts = new List<(double X, double Y)> { (cx - s, cy) };
        for (int i = 0; i <= 12; i++)
        {
            double t = i / 12.0;
            pts.Add((cx - s + t * 2 * s - tail * s * 0.4 * t, cy + s * 1.5 + Math.Sin(t * Math.PI * 3) * s * 0.22 + tail * s * 0.35 * (1 - t)));
        }
        pts.Add((cx + s, cy));
        c.Poly(pts, ramp[2]);
        c.Shaded(cx, cy, s, s, ramp, 0.2);
        var hole = Hx("#1a1028");
        c.Ellipse(cx - s * 0.38, cy - s * 0.05, s * 0.2, s * 0.3, hole);
        c.Ellipse(cx + s * 0.38, cy - s * 0.05, s * 0.2, s * 0.3, hole);
        if (wail) c.Ellipse(cx, cy + s * 0.6, s * 0.26, s * 0.4, hole);
        else c.Ellipse(cx, cy + s * 0.5, s * 0.14, s * 0.12, hole);
    }

    private static void Eye(Canvas c, double cx, double cy, double w, double h, Rgba white, Rgba[] iris, bool slit)
    {
        var pts = new List<(double X, double Y)>();
        for (int i = 0; i <= 20; i++) { double t = i / 20.0; pts.Add((cx - w + 2 * w * t, cy - h * Math.Sin(Math.PI * t))); }
        for (int i = 19; i > 0; i--) { double t = i / 20.0; pts.Add((cx - w + 2 * w * t, cy + h * Math.Sin(Math.PI * t))); }
        c.Poly(pts, white);
        c.Shaded(cx, cy, h * 0.95, h * 0.95, iris, 0.1);
        var pupil = Hx("#100808");
        if (slit) c.Ellipse(cx, cy, h * 0.22, h * 0.85, pupil);
        else c.Ellipse(cx, cy, h * 0.4, h * 0.4, pupil);
        c.Set(cx - h * 0.4, cy - h * 0.45, White);
    }

    // An open hand, fingers up, palm toward the viewer.
    private static void Palm(Canvas c, double cx, double cy, double s, Rgba[] skin)
    {
        c.ShadedRect((int)(cx - s * 0.7), (int)(cy + s * 0.8), (int)(s * 1.4), (int)(s * 1.4), Rp("#b05a30"));
        for (int k = 0; k < 4; k++)
        {
            double fx = cx - s * 0.75 + k * s * 0.5, len = k is 1 or 2 ? s * 1.4 : s * 1.1;
            c.Line(fx, cy - s * 0.2, fx, cy - s * 0.2 - len, skin[2], s * 0.44);
            c.Shaded(fx, cy - s * 0.2 - len, s * 0.22, s * 0.22, skin);
        }
        c.Line(cx - s * 0.9, cy + s * 0.35, cx - s * 1.55, cy - s * 0.3, skin[2], s * 0.44);
        c.Shaded(cx, cy + s * 0.25, s, s * 0.85, skin, 0.1);
    }

    // A clenched fist seen from the front, knuckles up.
    private static void Fist(Canvas c, double cx, double cy, double s, Rgba[] skin)
    {
        c.ShadedRect((int)(cx - s * 0.75), (int)(cy + s * 0.9), (int)(s * 1.5), (int)(s * 1.1), Rp("#8a3a28"));
        c.Shaded(cx, cy + s * 0.2, s * 1.15, s * 0.95, skin, 0.05);
        for (int k = 0; k < 4; k++) c.Shaded(cx - s * 0.75 + k * s * 0.5, cy - s * 0.5, s * 0.3, s * 0.38, skin, 0.15);
        for (int k = 0; k < 3; k++) c.Line(cx - s * 0.5 + k * s * 0.5, cy - s * 0.3, cx - s * 0.5 + k * s * 0.5, cy + s * 0.2, skin[1]);
        c.Line(cx - s * 1.0, cy + s * 0.45, cx + s * 0.2, cy + s * 0.3, skin[3], s * 0.45);
    }

    // A chain of links from one point to another: links seen face-on alternate with links seen edge-on.
    private static void Chain(Canvas c, double x0, double y0, double x1, double y1, Rgba[] iron)
    {
        double dx = x1 - x0, dy = y1 - y0, len = Math.Sqrt(dx * dx + dy * dy), rot = Math.Atan2(dy, dx);
        int n = (int)(len / 5.2);
        for (int k = 0; k <= n; k++)
        {
            double t = (double)k / n, cx = x0 + dx * t, cy = y0 + dy * t;
            if (k % 2 == 1) { c.Line(cx - Math.Cos(rot) * 3.6, cy - Math.Sin(rot) * 3.6, cx + Math.Cos(rot) * 3.6, cy + Math.Sin(rot) * 3.6, iron[2], 2.2); continue; }
            for (int i = 0; i < 24; i++)
            {
                double a0 = i / 24.0 * Math.PI * 2, a1 = (i + 1) / 24.0 * Math.PI * 2;
                (double X, double Y) P(double a) => (cx + Math.Cos(a) * 4 * Math.Cos(rot) - Math.Sin(a) * 2.4 * Math.Sin(rot), cy + Math.Cos(a) * 4 * Math.Sin(rot) + Math.Sin(a) * 2.4 * Math.Cos(rot));
                var (ax, ay) = P(a0); var (bx, by) = P(a1);
                c.Line(ax, ay, bx, by, iron[3], 1.6);
            }
        }
    }

    private static void Bell(Canvas c, double x, double y, Rgba[] gold)
    {
        c.Poly(Pts(x - 2.8, y + 2.6, x - 2, y - 1, x, y - 2.8, x + 2, y - 1, x + 2.8, y + 2.6), gold[2]);
        c.Line(x - 2.8, y + 2.6, x + 2.8, y + 2.6, gold[1]);
        c.Set(x - 1, y - 1, gold[4]);
    }

    // A skeletal hand clawing up out of the ground at (cx, by).
    private static void SkeletonHand(Canvas c, double cx, double by, double k, Rgba bone)
    {
        double X(double dx) => cx + dx * k;
        double Y(double dy) => by - dy * k;
        double t = Math.Max(1, 1.6 * k);
        c.Line(X(-2), Y(0), X(-1.4), Y(11), bone, t); c.Line(X(2), Y(0), X(1.4), Y(11), bone, t);
        c.Poly(Pts(X(-4), Y(11), X(4), Y(11), X(5), Y(15), X(-5), Y(15)), bone);
        foreach (var (ax, bx, cx2, cy2) in new[] { (-4.0, -7.0, -6.0, 25.0), (-1.5, -2.5, -2.0, 27.0), (1.5, 2.5, 2.0, 27.0), (4.0, 7.0, 6.0, 25.0) })
        {
            c.Line(X(ax), Y(15), X(bx), Y(22), bone, t);
            c.Line(X(bx), Y(22), X(cx2), Y(cy2), bone, t);
        }
        c.Line(X(-5), Y(13), X(-9), Y(16), bone, t);
        c.Line(X(-9), Y(16), X(-10), Y(19), bone, t);
    }
}
