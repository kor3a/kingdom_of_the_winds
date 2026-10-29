namespace KotW.ArtGen;

// Offline synthesizer: retro sound effects and looping background tunes
// (plucked-string melody on a pentatonic scale over a light drum pattern,
// in the spirit of gayageum and janggu). Writes 16-bit mono WAV files.
public sealed class Sound
{
    public const int Rate = 22050;
    public float[] Buf;
    public Sound(double seconds) { Buf = new float[(int)(seconds * Rate) + 1]; }

    private static readonly Random R = new(7);

    private static double Wave(string type, double phase) => type switch
    {
        "sine" => Math.Sin(phase * 2 * Math.PI),
        "square" => phase % 1 < 0.5 ? 1 : -1,
        "saw" => 2 * (phase % 1) - 1,
        _ => 1 - 4 * Math.Abs((phase % 1) - 0.5), // triangle
    };

    // Exponential decay envelope with a short attack, like a plucked or struck note.
    public void Tone(double freq, double dur, string type = "square", double vol = 0.3, double? slide = null, double delay = 0, double attack = 0.005, bool wrap = false)
    {
        int start = (int)(delay * Rate), n = (int)(dur * Rate);
        double phase = 0;
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / Rate;
            double f = slide is { } s ? freq * Math.Pow(s / freq, t / dur) : freq;
            phase += f / Rate;
            double env = t < attack ? t / attack : Math.Exp(-5 * (t - attack) / dur);
            Add(start + i, Wave(type, phase) * vol * env, wrap);
        }
    }

    // Filtered noise. type: low | high | band. The cutoff sweeps from f0 to f1.
    public void Noise(double dur, double vol = 0.3, double f0 = 1200, double? f1 = null, string type = "low", double delay = 0, bool wrap = false)
    {
        int start = (int)(delay * Rate), n = (int)(dur * Rate);
        double y1 = 0, y2 = 0, x1 = 0, x2 = 0;
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / Rate;
            double f = f1 is { } e ? f0 * Math.Pow(e / f0, t / dur) : f0;
            // RBJ biquad, recomputed per sample so the cutoff can sweep
            double w = 2 * Math.PI * Math.Min(f, Rate * 0.45) / Rate, cos = Math.Cos(w), alpha = Math.Sin(w) / (2 * 0.8);
            double b0, b1, b2;
            if (type == "high") { b0 = (1 + cos) / 2; b1 = -(1 + cos); b2 = b0; }
            else if (type == "band") { b0 = alpha; b1 = 0; b2 = -alpha; }
            else { b0 = (1 - cos) / 2; b1 = 1 - cos; b2 = b0; }
            double a0 = 1 + alpha, a1 = -2 * cos, a2 = 1 - alpha;
            double x = R.NextDouble() * 2 - 1;
            double y = (b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2) / a0;
            x2 = x1; x1 = x; y2 = y1; y1 = y;
            Add(start + i, y * vol * Math.Exp(-4 * t / dur), wrap);
        }
    }

    private void Add(int i, double v, bool wrap)
    {
        if (wrap) i %= Buf.Length;
        if (i >= 0 && i < Buf.Length) Buf[i] += (float)v;
    }

    public void Save(string path, double gain = 1)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        float peak = Math.Max(0.0001f, Buf.Max(Math.Abs));
        double scale = Math.Min(gain, 0.95 / peak);
        using var w = new BinaryWriter(File.Create(path));
        int bytes = Buf.Length * 2;
        w.Write("RIFF"u8); w.Write(36 + bytes); w.Write("WAVE"u8);
        w.Write("fmt "u8); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
        w.Write("data"u8); w.Write(bytes);
        foreach (var s in Buf) w.Write((short)Math.Clamp(s * scale * 32767, -32768, 32767));
    }

    public static void Effects(string dir)
    {
        void Make(string name, double secs, Action<Sound> build) { var s = new Sound(secs); build(s); s.Save(Path.Combine(dir, $"{name}.wav")); }
        Make("swing", 0.15, s => s.Noise(0.12, 0.5, 2500, 800, "high"));
        Make("hit", 0.15, s => { s.Tone(160, 0.12, "triangle", 0.5, 60); s.Noise(0.08, 0.4, 900); });
        Make("crit", 0.2, s => { s.Tone(220, 0.16, "square", 0.3, 70); s.Noise(0.14, 0.5, 1600); s.Tone(880, 0.1, "square", 0.1, delay: 0.02); });
        Make("hurt", 0.2, s => { s.Tone(240, 0.16, "square", 0.3, 110); s.Noise(0.06, 0.25, 700); });
        Make("kill", 0.3, s => { foreach (var (f, i) in new[] { 523.0, 659, 784 }.Select((f, i) => (f, i))) s.Tone(f, 0.14, "triangle", 0.3, delay: i * 0.05); });
        Make("bossdie", 1.4, s => { foreach (var (f, i) in new[] { 262.0, 330, 392, 523, 659, 784 }.Select((f, i) => (f, i))) s.Tone(f, 0.5, "triangle", 0.25, delay: i * 0.09); s.Noise(0.8, 0.4, 400); });
        Make("die", 0.8, s => { foreach (var (f, i) in new[] { 392.0, 330, 262, 196 }.Select((f, i) => (f, i))) s.Tone(f, 0.3, "triangle", 0.3, delay: i * 0.15); });
        Make("levelup", 0.7, s => { foreach (var (f, i) in new[] { 523.0, 659, 784, 1047, 784, 1047 }.Select((f, i) => (f, i))) s.Tone(f, 0.18, "square", 0.15, delay: i * 0.08); });
        Make("quest", 0.6, s => { foreach (var (f, i) in new[] { 392.0, 523, 659, 784 }.Select((f, i) => (f, i))) s.Tone(f, 0.22, "triangle", 0.3, delay: i * 0.1); });
        Make("coin", 0.3, s => { s.Tone(988, 0.07, "square", 0.15); s.Tone(1319, 0.18, "square", 0.15, delay: 0.07); });
        Make("drink", 0.3, s => { for (int i = 0; i < 4; i++) s.Tone(300 + i * 120, 0.06, "sine", 0.3, 500 + i * 140, i * 0.05); });
        Make("equip", 0.1, s => { s.Noise(0.05, 0.3, 3000, null, "band"); s.Tone(600, 0.05, "square", 0.1); });
        Make("ui", 0.06, s => s.Tone(880, 0.04, "square", 0.1));
        Make("portal", 0.65, s => s.Noise(0.6, 0.6, 300, 3000, "band"));
        Make("fire", 0.45, s => s.Noise(0.4, 0.7, 400, 2400, "band"));
        Make("ice", 0.4, s => { foreach (var (f, i) in new[] { 1568.0, 2093, 2637 }.Select((f, i) => (f, i))) s.Tone(f, 0.25, "sine", 0.2, delay: i * 0.04); });
        Make("paralyze", 0.45, s => s.Tone(440, 0.4, "saw", 0.15, 880));
        Make("lightning", 0.55, s => { s.Noise(0.5, 0.8, 5000, 200, "high"); s.Tone(80, 0.4, "saw", 0.25, 40); });
        Make("holy", 0.55, s => { foreach (var f in new[] { 784.0, 988, 1175 }) s.Tone(f, 0.5, "sine", 0.15); });
        Make("arrow", 0.25, s => { s.Noise(0.18, 0.45, 4000, 1200, "high"); s.Tone(1200, 0.05, "triangle", 0.12, 500); });
        Make("spirit", 0.6, s => { s.Tone(330, 0.5, "sine", 0.2, 220); s.Tone(494, 0.45, "sine", 0.14, 300, 0.05); s.Noise(0.4, 0.15, 900, 300, "band"); });
        Make("drain", 0.6, s => { s.Tone(660, 0.5, "sine", 0.2, 220); s.Tone(990, 0.4, "triangle", 0.1, 330, 0.08); });
        Make("nova", 0.6, s => { s.Noise(0.5, 0.6, 200, 3000, "band"); s.Tone(110, 0.45, "sine", 0.4, 55); });
        Make("smoke", 0.45, s => s.Noise(0.4, 0.45, 1800, 300, "band"));
        Make("heal", 0.5, s => { foreach (var (f, i) in new[] { 523.0, 659, 784, 1047 }.Select((f, i) => (f, i))) s.Tone(f, 0.3, "sine", 0.2, delay: i * 0.06); });
        Make("buff", 0.5, s => { foreach (var (f, i) in new[] { 392.0, 494, 587 }.Select((f, i) => (f, i))) s.Tone(f, 0.4, "triangle", 0.2, delay: i * 0.03); });
        Make("stealth", 0.45, s => s.Noise(0.4, 0.4, 3000, 300, "high"));
        Make("poison", 0.15, s => s.Tone(120, 0.12, "sine", 0.3, 80));
        Make("revive", 0.9, s => { foreach (var (f, i) in new[] { 262.0, 392, 523, 784 }.Select((f, i) => (f, i))) s.Tone(f, 0.5, "sine", 0.22, delay: i * 0.1); });
        Make("growl", 0.65, s => { s.Tone(90, 0.6, "saw", 0.25, 60); s.Noise(0.6, 0.3, 300); });
        Make("roar", 0.55, s => { s.Tone(70, 0.5, "saw", 0.35, 40); s.Noise(0.5, 0.6, 600, 100); });
    }

    // Renders a seamless loop: notes that run past the end wrap to the start.
    public static void Music(string path, double root, double stepDur, double density, bool drums, double octave, int seed, int bars = 16)
    {
        int steps = bars * 12;
        var s = new Sound(steps * stepDur);
        var rng = new Random(seed);
        int[] scale = { 0, 2, 5, 7, 9 };
        double Note(int deg)
        {
            int oct = (int)Math.Floor(deg / 5.0), idx = ((deg % 5) + 5) % 5;
            return root * octave * Math.Pow(2, oct + scale[idx] / 12.0);
        }
        int degree = 4;
        for (int step = 0; step < steps; step++)
        {
            int st = step % 12, bar = step / 12;
            double t = step * stepDur;
            if (drums)
            {
                if (st == 0 || (st == 6 && bar % 2 == 1)) s.Tone(110, 0.3, "sine", 0.5, 55, t, 0.002, true);
                if (st is 3 or 8 or 9) s.Noise(0.06, 0.25, 2800, null, "band", t, true);
            }
            if (st == 0 && bar % 4 == 0)
            {
                // soft wind-instrument drone under each phrase
                double f = Note(0) / 2, dur = stepDur * 24;
                int start = (int)(t * Rate);
                for (int i = 0; i < (int)(dur * Rate); i++)
                {
                    double tt = (double)i / Rate;
                    double env = Math.Sin(Math.PI * tt / dur);
                    double v = Math.Sin(2 * Math.PI * f * tt + 0.3 * Math.Sin(2 * Math.PI * 5 * tt)) * 0.1 * env;
                    s.Buf[(start + i) % s.Buf.Length] += (float)v;
                }
            }
            bool rest = st >= 10 && bar % 2 == 1;
            if (!rest && rng.NextDouble() < density)
            {
                degree += new[] { -2, -1, -1, 0, 1, 1, 2 }[rng.Next(7)];
                degree = Math.Clamp(degree, 0, 10);
                if (st == 0) degree = new[] { 0, 2, 4, 5 }[rng.Next(4)];
                double f = Note(degree);
                bool bend = rng.NextDouble() < 0.3;
                s.Tone(bend ? f * 1.06 : f, 1.3, "triangle", 0.22, bend ? f : null, t, 0.006, true);
                s.Tone(f * 2, 1.0, "sine", 0.07, null, t, 0.006, true);
            }
        }
        s.Save(path, 0.8);
    }
}
