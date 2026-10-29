namespace KotW.Shared;

// Deterministic randomness so every map is identical on each load
// (and identical on the future server that builds the same maps).
public sealed class Mulberry32
{
    private uint _a;
    public Mulberry32(uint seed) { _a = seed; }

    public double Next()
    {
        unchecked
        {
            _a += 0x6d2b79f5;
            uint t = _a;
            t = (t ^ (t >> 15)) * (t | 1);
            t ^= t + (t ^ (t >> 7)) * (t | 61);
            return ((t ^ (t >> 14)) & 0xffffffffu) / 4294967296.0;
        }
    }

    public int Int(int lo, int hi) => lo + (int)(Next() * (hi - lo + 1));
}

public static class Rnd
{
    private static readonly Random R = new();
    public static double Next() => R.NextDouble();
    public static double Range(double a, double b) => a + R.NextDouble() * (b - a);
    public static int Int(int a, int b) => R.Next(a, b + 1);
    public static T Pick<T>(IReadOnlyList<T> list) => list[R.Next(list.Count)];

    // Stable 0..1 hash of a tile coordinate, used for tile variants.
    public static double Hash2(int x, int y, int salt = 0)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + salt * -2048144789);
            h = (h ^ (h >> 13)) * 1274126177u;
            return (h ^ (h >> 16)) / 4294967296.0;
        }
    }
}
