using Godot;
using KotW.Shared;

namespace KotW;

// Screen-space text and bars over the world: names, monster health, quest marks,
// floating damage numbers and chat bubbles. Drawn at screen resolution so text stays sharp.
public partial class Overlay : Control
{
    public GameView View = null!;
    private readonly List<Number> _numbers = new();
    private readonly Dictionary<string, (string Text, double T)> _bubbles = new();
    private Font _font = ThemeDB.FallbackFont;

    private sealed class Number { public required string Text; public Color Color; public int Size; public Vector2 World; public double T; }

    private static readonly Dictionary<string, (string Hex, int Size)> Styles = new()
    {
        ["hit"] = ("#ffffff", 18), ["crit"] = ("#ffd84a", 24), ["spell"] = ("#9fd8ff", 18), ["poison"] = ("#8ae070", 16),
        ["miss"] = ("#b8b8b8", 15), ["hurt"] = ("#ff6a5a", 18), ["heal"] = ("#7af08a", 18), ["mana"] = ("#7ab0ff", 18),
    };

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    public void OnEvent(GameEvent e)
    {
        if (e.Type == EvType.Dmg)
        {
            var (hex, size) = Styles.GetValueOrDefault(e.Kind, ("#ffffff", 18));
            _numbers.Add(new Number
            {
                Text = e.Kind == "crit" ? e.Text + "!" : e.Text, Color = Color.FromHtml(hex), Size = size,
                World = new Vector2(e.X * Assets.T + 16 + (GD.Randf() - 0.5f) * 10, e.Y * Assets.T - 6),
            });
        }
        else if (e.Type == EvType.Bubble) _bubbles[e.Id] = (e.Text, 0);
        else if (e.Type == EvType.Map) { _numbers.Clear(); _bubbles.Clear(); }
    }

    public void Tick(double dt)
    {
        foreach (var n in _numbers) n.T += dt;
        _numbers.RemoveAll(n => n.T > 0.9);
        foreach (var k in _bubbles.Keys.ToList())
        {
            var b = _bubbles[k];
            if (b.T + dt > 4.5) _bubbles.Remove(k); else _bubbles[k] = (b.Text, b.T + dt);
        }
        QueueRedraw();
    }

    private Vector2 Screen(Vector2 world) => View.GetGlobalTransformWithCanvas() * world;
    private float Zoom => View.Cam.Zoom.X;

    private void Text(string s, Vector2 pos, Color col, int size, HorizontalAlignment align = HorizontalAlignment.Center)
    {
        float w = _font.GetStringSize(s, HorizontalAlignment.Left, -1, size).X;
        var p = align == HorizontalAlignment.Center ? pos - new Vector2(w / 2, 0) : pos;
        DrawStringOutline(_font, p, s, HorizontalAlignment.Left, -1, size, 4, new Color(0, 0, 0, 0.85f * col.A));
        DrawString(_font, p, s, HorizontalAlignment.Left, -1, size, col);
    }

    public override void _Draw()
    {
        if (View?.W == null) return;
        var w = View.W;
        var target = w.GetTarget();
        float z = Zoom;
        foreach (var m in w.Monsters)
        {
            var feet = Screen(GameView.Feet(m));
            bool isTarget = target?.Id == m.Id;
            if (m.Hp < m.MaxHp || isTarget)
            {
                float bw = (m.D.Boss ? 48 : 28) * z / 2, top = feet.Y - (m.D.Boss ? 62 : m.D.Sprite == "bat" ? 40 : m.D.Sprite is "dokkaebi" or "golem" ? 54 : 34) * z / 2;
                var r = new Rect2(feet.X - bw / 2, top, bw, 5);
                DrawRect(r.Grow(1), Colors.Black);
                DrawRect(r, Assets.C("#5a1010"));
                DrawRect(new Rect2(r.Position, new Vector2(bw * (float)(m.Hp / m.MaxHp), 5)), m.Hp / m.MaxHp > 0.3 ? Assets.C("#e04a3a") : Assets.C("#ff8a3a"));
            }
            if (isTarget) Text($"{m.D.Name}  Lv{m.D.Level}", feet + new Vector2(0, 8 * z), Assets.C("#ffb0a0"), 15);
        }
        void Name(Entity e, string name, Color col)
        {
            var feet = Screen(GameView.Feet(e));
            Text(name, feet + new Vector2(0, 8 * z), col, 15);
        }
        Name(w.P, w.P.Name, w.P.Ghost ? Assets.C("#b8c8ff") : Colors.White);
        foreach (var n in w.Npcs)
        {
            Name(n, n.Name, Assets.C("#ffe28a"));
            var mark = w.QuestMark(n.Id);
            if (mark != null)
            {
                var head = Screen(GameView.Feet(n) - new Vector2(0, 52)) + new Vector2(0, Mathf.Sin((float)View.Time * 4) * 3);
                Text(mark.ToString()!, head, mark == '?' ? Assets.C("#7af08a") : Assets.C("#ffd84a"), 26);
            }
        }
        foreach (var f in w.FolkList) Name(f, f.Name, Assets.C("#a8e0ff"));

        foreach (var n in _numbers)
        {
            float k = (float)(n.T / 0.9);
            float pop = n.T < 0.1 ? 1 + (float)(0.1 - n.T) * 4 : 1;
            var p = Screen(n.World) - new Vector2(0, (1 - (1 - k) * (1 - k)) * 22 * z);
            Text(n.Text, p, n.Color with { A = k > 0.6f ? 1 - (k - 0.6f) / 0.4f : 1 }, (int)(n.Size * pop));
        }
        foreach (var e in View.Effects)
            if (e.Kind == "levelup")
                Text("LEVEL UP!", Screen(new Vector2(e.X * 32 + 16, e.Y * 32 - 30 - (float)(e.T / e.Dur) * 12)), Assets.C("#ffd84a") with { A = 1 - (float)(e.T / e.Dur) }, 22);

        foreach (var (id, (text, t)) in _bubbles)
        {
            var ent = w.EntityById(id);
            if (ent == null) continue;
            var head = Screen(GameView.Feet(ent) - new Vector2(0, 48));
            const int size = 15;
            var lines = Wrap(text, 190, size);
            float bw = lines.Max(l => _font.GetStringSize(l, HorizontalAlignment.Left, -1, size).X) + 14;
            float bh = lines.Count * 17 + 8;
            var box = new Rect2(Mathf.Clamp(head.X - bw / 2, 4, Size.X - bw - 4), head.Y - bh - 6, bw, bh);
            float a = t > 4 ? (float)(4.5 - t) / 0.5f : 1;
            DrawRect(box.Grow(2), new Color(0.16f, 0.13f, 0.12f, a));
            DrawRect(box, new Color(0.99f, 0.97f, 0.9f, a));
            DrawRect(new Rect2(head.X - 3, box.End.Y, 6, 5), new Color(0.99f, 0.97f, 0.9f, a));
            for (int i = 0; i < lines.Count; i++)
                DrawString(_font, box.Position + new Vector2(7, 17 + i * 17), lines[i], HorizontalAlignment.Left, -1, size, new Color(0.16f, 0.13f, 0.12f, a));
        }
    }

    private List<string> Wrap(string text, float maxW, int size)
    {
        var lines = new List<string>();
        var line = "";
        foreach (var word in text.Split(' '))
        {
            var test = line == "" ? word : $"{line} {word}";
            if (_font.GetStringSize(test, HorizontalAlignment.Left, -1, size).X > maxW && line != "") { lines.Add(line); line = word; }
            else line = test;
        }
        if (line != "") lines.Add(line);
        return lines.Take(4).ToList();
    }
}
