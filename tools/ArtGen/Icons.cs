using KotW.Shared;

namespace KotW.ArtGen;

// 32x32 inventory icons, one per item, packed into a grid in item order.
public static class Icons
{
    public const int Size = 32, PerRow = 8;

    public static Canvas Atlas(out List<string> order)
    {
        order = Content.Items.Values.Where(i => !i.Hidden).Select(i => i.Id).ToList();
        int rows = (order.Count + PerRow - 1) / PerRow;
        var atlas = new Canvas(Size * PerRow, Size * rows);
        for (int i = 0; i < order.Count; i++) atlas.Blit(Icon(Content.Items[order[i]]), i % PerRow * Size, i / PerRow * Size);
        return atlas;
    }

    public static Canvas Icon(ItemDef it)
    {
        var c = new Canvas(Size, Size);
        var m = Pal.Ramp(it.Color);
        var wood = Pal.Ramp("#8a5a32");
        var gold = Pal.Ramp("#e0b04a");
        void Diag(double x0, double y0, double x1, double y1, Rgba[] r, double thick)
        {
            c.Line(x0, y0, x1, y1, r[2], thick);
            c.Line(x0 - 0.8, y0 - 0.8, x1 - 0.8, y1 - 0.8, r[3]);
        }
        switch (it.Icon)
        {
            case "sword":
                Diag(9, 23, 25, 7, m, 3);
                c.Line(10, 21, 24, 7, m[4]);
                c.Line(6, 20, 13, 27, gold[2], 2.2);
                Diag(4, 28, 9, 23, Pal.Ramp("#5a3a28"), 2.2);
                c.Shaded(4, 28, 1.8, 1.8, gold);
                break;
            case "dagger":
                Diag(11, 21, 22, 10, m, 3);
                c.Line(12, 19, 21, 10, m[4]);
                c.Line(9, 18, 14, 23, gold[2], 2);
                Diag(6, 26, 10, 22, Pal.Ramp("#5a3a28"), 2.2);
                break;
            case "staff":
                Diag(5, 29, 22, 12, wood, 2.4);
                c.Shaded(24, 9, 5, 5, m, 0.2);
                c.Set(22, 7, Pal.Hex("#ffffff"));
                break;
            case "ringstaff":
                Diag(5, 29, 21, 13, wood, 2.4);
                for (int k = 0; k < 32; k++) { double t = k / 32.0 * Math.PI * 2; c.Set(23 + Math.Cos(t) * 6, 9 + Math.Sin(t) * 6, k < 16 ? m[3] : m[1]); }
                c.Shaded(18, 13, 1.6, 1.6, m); c.Shaded(28, 13, 1.6, 1.6, m);
                break;
            case "glaive":
                Diag(4, 29, 19, 14, Pal.Ramp("#5a3a24"), 2.2);
                c.Poly(new (double, double)[] { (17, 16), (22, 4), (29, 3), (24, 12), (20, 18) }, m[2]);
                c.Line(22, 5, 18, 15, m[4]);
                c.Shaded(18, 17, 2, 2, Pal.Ramp("#c03030"));
                break;
            case "armor":
                c.Poly(new (double, double)[] { (8, 6), (13, 4), (19, 4), (24, 6), (28, 14), (24, 16), (23, 28), (9, 28), (8, 16), (4, 14) }, m[2]);
                c.Poly(new (double, double)[] { (8, 6), (13, 4), (12, 28), (9, 28), (8, 16), (4, 14) }, m[3]);
                c.Line(13, 4, 16, 12, Pal.Hex("#f2ecdc"), 1.6); c.Line(19, 4, 16, 12, Pal.Hex("#e6dfcc"), 1.6);
                c.Rect(9, 22, 14, 2, m[1]);
                break;
            case "robe":
                c.Poly(new (double, double)[] { (9, 5), (14, 3), (18, 3), (23, 5), (29, 15), (25, 17), (26, 30), (6, 30), (7, 17), (3, 15) }, m[2]);
                c.Poly(new (double, double)[] { (9, 5), (14, 3), (13, 30), (6, 30), (7, 17), (3, 15) }, m[3]);
                c.Line(14, 3, 17, 13, Pal.Hex("#f6f2e8"), 1.6); c.Line(18, 3, 17, 13, Pal.Hex("#ece6d6"), 1.6);
                c.Line(17, 13, 16, 30, m[1]);
                c.Rect(18, 13, 1, 6, Pal.Hex("#b8304a")); c.Rect(20, 13, 1, 5, Pal.Hex("#b8304a"));
                break;
            case "straw":
                c.Shaded(16, 21, 14, 5, m, 0.1);
                c.Poly(new (double, double)[] { (8, 20), (16, 6), (24, 20) }, m[2]);
                c.Poly(new (double, double)[] { (8, 20), (16, 6), (15, 20) }, m[3]);
                for (int y = 9; y < 20; y += 3) c.Line(16 - (y - 6) * 0.55, y, 16 + (y - 6) * 0.55, y, m[1]);
                break;
            case "cap":
                c.Shaded(16, 18, 11, 9, m, 0.1);
                for (int y = 19; y < Size; y++) for (int x = 0; x < Size; x++) c.Erase(x, y);
                c.Rect(4, 18, 24, 4, m[1]); c.Rect(4, 18, 24, 1, m[3]);
                break;
            case "helm":
                c.Shaded(16, 18, 11, 10, m, 0.1);
                for (int y = 20; y < Size; y++) for (int x = 0; x < Size; x++) c.Erase(x, y);
                c.Rect(4, 20, 6, 7, m[1]); c.Rect(22, 20, 6, 7, m[1]);
                c.Line(16, 9, 16, 20, m[4]);
                c.Shaded(16, 6, 3, 3, Pal.Ramp("#d03030"));
                break;
            case "shield":
                c.Shaded(16, 16, 12, 13, Pal.Ramp("#c8a050"));
                c.Shaded(16, 16, 10, 11, m);
                c.Shaded(16, 16, 3, 3, Pal.Ramp("#c8a050"));
                c.Line(16, 6, 16, 12, m[1]); c.Line(16, 20, 16, 26, m[1]);
                break;
            case "ring":
                for (int k = 0; k < 40; k++)
                {
                    double t = k / 40.0 * Math.PI * 2;
                    c.Ellipse(16 + Math.Cos(t) * 7, 20 + Math.Sin(t) * 5, 1.4, 1.4, Math.Sin(t) < 0 ? gold[3] : gold[1]);
                }
                c.Shaded(16, 12, 4.5, 4.5, m, 0.2);
                c.Set(14, 10, Pal.Hex("#ffffff"));
                break;
            case "necklace":
                for (int k = 0; k <= 14; k++)
                {
                    double t = k / 14.0;
                    c.Shaded(5 + t * 22, 5 + Math.Sin(t * Math.PI) * 14, 1.5, 1.5, k % 2 == 0 ? gold : Pal.Ramp("#a03828"));
                }
                c.Shaded(16, 24, 4.5, 5, m, 0.15);
                break;
            case "potion":
                c.Shaded(16, 20, 9, 9, Pal.Ramp("#dce8f0"), 0.1);
                c.Shaded(16, 21, 7.5, 7, m, 0.05);
                c.Rect(13, 5, 6, 8, Pal.Ramp("#dce8f0")[2]);
                c.Rect(12, 3, 8, 3, Pal.Ramp("#9a6a3c")[2]);
                c.Rect(12, 17, 3, 3, Pal.Hex("#ffffffa0"));
                break;
            case "gourd":
                c.Shaded(16, 22, 9, 8, Pal.Ramp("#d8b070"), 0.05);
                c.Shaded(16, 10, 5.5, 5, Pal.Ramp("#d8b070"), 0.1);
                c.Rect(10, 19, 12, 3, m[2]); c.Rect(10, 19, 12, 1, m[3]);
                c.Rect(14, 3, 4, 3, Pal.Ramp("#7a4a2a")[2]);
                c.Line(20, 14, 26, 18, Pal.Hex("#c03030"), 1.4);
                break;
            case "scroll":
                c.Rect(7, 7, 18, 18, m[2]); c.Rect(7, 7, 4, 18, m[3]);
                for (int y = 11; y < 22; y += 3) c.Rect(13, y, 9, 1, Pal.Hex("#5a4a3a"));
                c.Shaded(16, 6, 11, 2.4, Pal.Ramp("#a07a40")); c.Shaded(16, 26, 11, 2.4, Pal.Ramp("#a07a40"));
                c.Rect(18, 18, 4, 4, Pal.Hex("#c03030"));
                break;
            case "pelt":
                c.Poly(new (double, double)[] { (7, 8), (12, 10), (20, 10), (25, 8), (27, 15), (24, 21), (26, 27), (20, 25), (12, 25), (6, 27), (8, 21), (5, 15) }, m[2]);
                c.Poly(new (double, double)[] { (7, 8), (12, 10), (13, 25), (6, 27), (8, 21), (5, 15) }, m[3]);
                c.Line(16, 12, 16, 23, m[1]);
                break;
            case "tail":
                for (int i = 0; i < 18; i++) c.Shaded(6 + i * 1.2, 14 + Math.Sin(i / 3.0) * 6, 2.2 - i * 0.05, 2.2 - i * 0.05, m);
                break;
            case "acorn":
                c.Shaded(16, 20, 7.5, 8, m, 0.1);
                c.Shaded(16, 12, 9, 4.5, Pal.Ramp("#6e4a28"));
                for (int x = 9; x < 24; x += 2) c.Set(x, 12, Pal.Ramp("#6e4a28")[1]);
                c.Rect(15, 5, 2, 4, Pal.Ramp("#5a3a20")[2]);
                break;
            case "fang":
            case "horn":
            case "tusk":
                c.Poly(it.Icon == "tusk"
                    ? new (double, double)[] { (8, 27), (13, 26), (20, 18), (24, 6), (19, 12), (11, 20) }
                    : new (double, double)[] { (11, 6), (21, 6), (19, 15), (17, 27), (15, 27), (13, 15) }, m[2]);
                c.Line(it.Icon == "tusk" ? 12 : 13, it.Icon == "tusk" ? 22 : 8, it.Icon == "tusk" ? 21 : 15, it.Icon == "tusk" ? 11 : 24, m[4]);
                if (it.Icon == "horn") for (int y = 9; y < 24; y += 4) c.Line(13, y, 19, y, m[1]);
                break;
            case "wing":
                c.Poly(new (double, double)[] { (4, 8), (28, 6), (26, 14), (22, 12), (20, 20), (16, 16), (12, 24), (9, 16) }, m[2]);
                c.Line(4, 8, 22, 12, m[0]); c.Line(4, 8, 16, 16, m[0]); c.Line(4, 8, 28, 6, m[3]);
                break;
            case "gem":
                c.Poly(new (double, double)[] { (16, 4), (26, 13), (16, 28), (6, 13) }, m[2]);
                c.Poly(new (double, double)[] { (16, 4), (16, 28), (6, 13) }, m[3]);
                c.Poly(new (double, double)[] { (16, 4), (21, 13), (11, 13) }, m[4]);
                break;
            case "whisker":
                for (int k = 0; k < 3; k++) c.Line(5, 22 - k * 4, 27, 12 + k * 3, k == 1 ? m[2] : m[3]);
                c.Shaded(6, 18, 3, 6, Pal.Ramp("#e8902c"));
                break;
            default:
                c.Shaded(16, 16, 9, 9, m);
                break;
        }
        c.Outline(col => Pal.Shift(col, -0.72));
        return c;
    }
}
