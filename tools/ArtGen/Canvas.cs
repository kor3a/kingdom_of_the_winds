using System.IO.Compression;

namespace KotW.ArtGen;

public readonly record struct Rgba(byte R, byte G, byte B, byte A = 255)
{
    public static readonly Rgba Clear = new(0, 0, 0, 0);
    public Rgba WithA(byte a) => this with { A = a };
}

// Colour helpers. Ramps shift shadows toward blue-violet and highlights toward
// warm yellow, which reads far better in pixel art than plain darkening.
public static class Pal
{
    public static Rgba Hex(string s)
    {
        s = s.TrimStart('#');
        byte a = s.Length >= 8 ? Convert.ToByte(s[6..8], 16) : (byte)255;
        return new Rgba(Convert.ToByte(s[0..2], 16), Convert.ToByte(s[2..4], 16), Convert.ToByte(s[4..6], 16), a);
    }

    public static Rgba Gray(double v, byte a = 255)
    {
        byte b = (byte)Math.Clamp(Math.Round(v * 255), 0, 255);
        return new Rgba(b, b, b, a);
    }

    private static (double H, double S, double V) Hsv(Rgba c)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), d = max - min;
        double h = 0;
        if (d > 0)
        {
            if (max == r) h = 60 * (((g - b) / d) % 6);
            else if (max == g) h = 60 * ((b - r) / d + 2);
            else h = 60 * ((r - g) / d + 4);
        }
        if (h < 0) h += 360;
        return (h, max == 0 ? 0 : d / max, max);
    }

    private static Rgba FromHsv(double h, double s, double v, byte a)
    {
        h = ((h % 360) + 360) % 360;
        s = Math.Clamp(s, 0, 1); v = Math.Clamp(v, 0, 1);
        double c = v * s, x = c * (1 - Math.Abs(h / 60 % 2 - 1)), m = v - c;
        var (r, g, b) = h switch
        {
            < 60 => (c, x, 0.0), < 120 => (x, c, 0.0), < 180 => (0.0, c, x),
            < 240 => (0.0, x, c), < 300 => (x, 0.0, c), _ => (c, 0.0, x),
        };
        return new Rgba((byte)Math.Round((r + m) * 255), (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255), a);
    }

    private static double Toward(double h, double target, double amount)
    {
        double diff = ((target - h + 540) % 360) - 180;
        return h + Math.Sign(diff) * Math.Min(Math.Abs(diff), amount);
    }

    // dv: brightness change (-1..1). Negative values cool the hue, positive warm it.
    public static Rgba Shift(Rgba c, double dv)
    {
        var (h, s, v) = Hsv(c);
        if (dv < 0)
        {
            if (s < 0.08) { h = 235; s = Math.Max(s, 0.1 * -dv); }
            h = Toward(h, 245, 30 * -dv);
            s = s + 0.12 * -dv;
            v = v * (1 + dv);
        }
        else
        {
            h = Toward(h, 52, 18 * dv);
            s = s * (1 - 0.5 * dv);
            v = v + (1 - v) * dv * 0.9 + dv * 0.12;
        }
        return FromHsv(h, s, v, c.A);
    }

    // [outline, shadow, base, light, shine]
    public static Rgba[] Ramp(Rgba b) => new[] { Shift(b, -0.68), Shift(b, -0.3), b, Shift(b, 0.25), Shift(b, 0.55) };
    public static Rgba[] Ramp(string hex) => Ramp(Hex(hex));

    // Grey ramp for layers the game tints at runtime (skin and hair).
    public static readonly Rgba[] GrayRamp = { Gray(0.02), Gray(0.3), Gray(0.5), Gray(0.72), Gray(0.9) };
}

public sealed class Canvas
{
    public readonly int W, H;
    public readonly Rgba[] P;

    public Canvas(int w, int h) { W = w; H = h; P = new Rgba[w * h]; }

    public bool In(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;
    public Rgba Get(int x, int y) => In(x, y) ? P[y * W + x] : Rgba.Clear;

    public void Set(int x, int y, Rgba c)
    {
        if (!In(x, y) || c.A == 0) return;
        int i = y * W + x;
        var d = P[i];
        if (c.A == 255 || d.A == 0) { P[i] = c; return; }
        double a = c.A / 255.0, da = d.A / 255.0, oa = a + da * (1 - a);
        byte Mix(byte s, byte t) => (byte)Math.Round((s * a + t * da * (1 - a)) / oa);
        P[i] = new Rgba(Mix(c.R, d.R), Mix(c.G, d.G), Mix(c.B, d.B), (byte)Math.Round(oa * 255));
    }
    public void Set(double x, double y, Rgba c) => Set((int)Math.Floor(x), (int)Math.Floor(y), c);
    public void Erase(int x, int y) { if (In(x, y)) P[y * W + x] = Rgba.Clear; }

    public void Rect(int x, int y, int w, int h, Rgba c)
    {
        for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++) Set(xx, yy, c);
    }

    public void Ellipse(double cx, double cy, double rx, double ry, Rgba c)
    {
        for (int y = (int)Math.Floor(cy - ry); y <= (int)Math.Ceiling(cy + ry); y++)
            for (int x = (int)Math.Floor(cx - rx); x <= (int)Math.Ceiling(cx + rx); x++)
            {
                double nx = (x + 0.5 - cx) / rx, ny = (y + 0.5 - cy) / ry;
                if (nx * nx + ny * ny <= 1) Set(x, y, c);
            }
    }

    // A lit ellipsoid: picks shadow/base/light/shine from the ramp by a fake surface normal.
    public void Shaded(double cx, double cy, double rx, double ry, Rgba[] ramp, double bias = 0, double lx = -0.55, double ly = -0.8)
    {
        for (int y = (int)Math.Floor(cy - ry); y <= (int)Math.Ceiling(cy + ry); y++)
            for (int x = (int)Math.Floor(cx - rx); x <= (int)Math.Ceiling(cx + rx); x++)
            {
                double nx = (x + 0.5 - cx) / rx, ny = (y + 0.5 - cy) / ry, d = nx * nx + ny * ny;
                if (d > 1) continue;
                double nz = Math.Sqrt(1 - d);
                double l = -(nx * lx + ny * ly) * 0.55 + nz * 0.55 + bias;
                int idx = l < 0.22 ? 1 : l < 0.62 ? 2 : l < 0.9 || ramp.Length < 5 ? 3 : 4;
                Set(x, y, ramp[Math.Min(idx, ramp.Length - 1)]);
            }
    }

    // Rectangle lit from the upper left: light left column, shadow right and bottom.
    public void ShadedRect(int x, int y, int w, int h, Rgba[] ramp)
    {
        Rect(x, y, w, h, ramp[2]);
        Rect(x, y, Math.Max(1, w / 5), h, ramp[3]);
        Rect(x + w - Math.Max(1, w / 4), y, Math.Max(1, w / 4), h, ramp[1]);
        Rect(x, y + h - 1, w, 1, ramp[1]);
    }

    public void Line(double x0, double y0, double x1, double y1, Rgba c, double thick = 1)
    {
        double len = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));
        int steps = Math.Max(1, (int)Math.Ceiling(len * 2));
        for (int i = 0; i <= steps; i++)
        {
            double t = (double)i / steps, x = x0 + (x1 - x0) * t, y = y0 + (y1 - y0) * t;
            if (thick <= 1) Set((int)Math.Floor(x), (int)Math.Floor(y), c);
            else Ellipse(x, y, thick / 2, thick / 2, c);
        }
    }

    public void Poly(IReadOnlyList<(double X, double Y)> pts, Rgba c)
    {
        double minY = pts.Min(p => p.Y), maxY = pts.Max(p => p.Y);
        for (int y = (int)Math.Floor(minY); y <= (int)Math.Ceiling(maxY); y++)
        {
            double sy = y + 0.5;
            var xs = new List<double>();
            for (int i = 0; i < pts.Count; i++)
            {
                var a = pts[i]; var b = pts[(i + 1) % pts.Count];
                if ((a.Y <= sy && b.Y > sy) || (b.Y <= sy && a.Y > sy)) xs.Add(a.X + (sy - a.Y) / (b.Y - a.Y) * (b.X - a.X));
            }
            xs.Sort();
            for (int k = 0; k + 1 < xs.Count; k += 2)
                for (int x = (int)Math.Round(xs[k]); x < (int)Math.Round(xs[k + 1]); x++) Set(x, y, c);
        }
    }

    // Adds a 1px outline around everything drawn so far, coloured from the neighbour it borders.
    public Canvas Outline(Func<Rgba, Rgba>? color = null, bool diagonals = false)
    {
        color ??= c => Pal.Shift(c, -0.68).WithA(c.A);
        var src = (Rgba[])P.Clone();
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                if (src[y * W + x].A != 0) continue;
                Rgba? n = null;
                foreach (var (dx, dy) in diagonals ? Around8 : Around4)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
                    var s = src[ny * W + nx];
                    if (s.A > 100) { n = s; break; }
                }
                if (n != null) P[y * W + x] = color(n.Value).WithA(Math.Max((byte)200, n.Value.A));
            }
        return this;
    }
    private static readonly (int, int)[] Around4 = { (1, 0), (-1, 0), (0, 1), (0, -1) };
    private static readonly (int, int)[] Around8 = { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, 1), (1, -1), (-1, -1) };

    public void Blit(Canvas s, int dx, int dy, bool flip = false)
    {
        for (int y = 0; y < s.H; y++)
            for (int x = 0; x < s.W; x++)
                Set(dx + x, dy + y, s.P[y * s.W + (flip ? s.W - 1 - x : x)]);
    }

    public Canvas Mirror()
    {
        var c = new Canvas(W, H);
        c.Blit(this, 0, 0, true);
        return c;
    }

    public void Map(Func<Rgba, Rgba> f)
    {
        for (int i = 0; i < P.Length; i++) if (P[i].A != 0) P[i] = f(P[i]);
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, Png.Encode(W, H, P));
    }
}

public static class Png
{
    private static readonly uint[] Crc = Enumerable.Range(0, 256).Select(n =>
    {
        uint c = (uint)n;
        for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xedb88320u ^ (c >> 1) : c >> 1;
        return c;
    }).ToArray();

    public static byte[] Encode(int w, int h, Rgba[] px)
    {
        var raw = new byte[h * (w * 4 + 1)];
        for (int y = 0; y < h; y++)
        {
            int o = y * (w * 4 + 1);
            for (int x = 0; x < w; x++)
            {
                var c = px[y * w + x];
                raw[o + 1 + x * 4] = c.R; raw[o + 2 + x * 4] = c.G; raw[o + 3 + x * 4] = c.B; raw[o + 4 + x * 4] = c.A;
            }
        }
        using var z = new MemoryStream();
        using (var zs = new ZLibStream(z, CompressionLevel.Optimal, true)) zs.Write(raw);
        using var ms = new MemoryStream();
        ms.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var ihdr = new byte[13];
        Be(ihdr, 0, (uint)w); Be(ihdr, 4, (uint)h);
        ihdr[8] = 8; ihdr[9] = 6;
        Chunk(ms, "IHDR", ihdr);
        Chunk(ms, "IDAT", z.ToArray());
        Chunk(ms, "IEND", Array.Empty<byte>());
        return ms.ToArray();
    }

    private static void Be(byte[] b, int o, uint v) { b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v; }

    private static void Chunk(Stream s, string type, byte[] data)
    {
        var len = new byte[4]; Be(len, 0, (uint)data.Length);
        s.Write(len);
        var body = new byte[4 + data.Length];
        for (int i = 0; i < 4; i++) body[i] = (byte)type[i];
        data.CopyTo(body, 4);
        s.Write(body);
        uint c = 0xffffffffu;
        foreach (var b in body) c = Crc[(c ^ b) & 0xff] ^ (c >> 8);
        var crc = new byte[4]; Be(crc, 0, c ^ 0xffffffffu);
        s.Write(crc);
    }
}
