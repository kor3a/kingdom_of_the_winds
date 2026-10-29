using Godot;
using KotW.Shared;

namespace KotW;

// The world on screen: tiles, y-sorted actors, effects, camera and lighting.
public partial class GameView : Node2D
{
    private const int T = Assets.T;
    public World W = null!;
    public Camera2D Cam = null!;
    public double Time;
    public readonly List<FxItem> Effects = new();

    private TileLayer _tiles = null!;
    private Node2D _actors = null!;
    private FxLayer _fx = null!;
    private CanvasModulate _mod = null!;
    private Node2D _lights = null!;
    private PointLight2D _playerLight = null!;
    private readonly Dictionary<string, Actor> _actorMap = new();
    private static GradientTexture2D? _lightTex;

    public sealed class FxItem
    {
        public required string Kind;
        public double T, Dur;
        public int X, Y, FromX, FromY, R;
        public Dir Dir;
        public Color Color = Colors.White;
        public bool Big;
        public float Seed = GD.Randf();
    }

    public void Init(World w)
    {
        W = w;
        _tiles = new TileLayer { View = this, ZIndex = -10 };
        _actors = new Node2D { YSortEnabled = true };
        _fx = new FxLayer { View = this, ZIndex = 50 };
        _mod = new CanvasModulate();
        _lights = new Node2D();
        Cam = new Camera2D { PositionSmoothingEnabled = false };
        AddChild(_tiles); AddChild(_actors); AddChild(_fx); AddChild(_mod); AddChild(_lights); AddChild(Cam);
        Cam.MakeCurrent();
        _lightTex ??= new GradientTexture2D
        {
            Width = 256, Height = 256, Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(1f, 0.5f),
            Gradient = new Gradient { Colors = new[] { Colors.White, new Color(1, 1, 1, 0.55f), new Color(1, 1, 1, 0) }, Offsets = new[] { 0f, 0.45f, 1f } },
        };
        _playerLight = new PointLight2D { Texture = _lightTex, TextureScale = 1.25f, Energy = 1.1f, Color = new Color(1f, 0.92f, 0.8f) };
        AddChild(_playerLight);
    }

    public static Vector2 Feet(Entity e)
    {
        double t = e.MoveT;
        return new Vector2((float)((e.FX + (e.X - e.FX) * t) * T + 16), (float)((e.FY + (e.Y - e.FY) * t) * T + 30));
    }

    public void OnMapChanged()
    {
        foreach (var a in _actorMap.Values) a.QueueFree();
        _actorMap.Clear();
        Effects.Clear();
        foreach (var l in _lights.GetChildren()) l.QueueFree();
        var map = W.Map;
        if (map.Dark > 0)
        {
            for (int y = 0; y < map.H; y++)
                for (int x = 0; x < map.W; x++)
                {
                    char c = map.Tile(x, y);
                    if (c != 't' && c != 'a') continue;
                    _lights.AddChild(new PointLight2D
                    {
                        Texture = _lightTex, TextureScale = c == 't' ? 0.75f : 1f, Energy = 0.9f,
                        Color = new Color(1f, 0.7f, 0.35f), Position = new Vector2(x * T + 16, y * T + 14),
                    });
                }
        }
        Cam.LimitLeft = 0; Cam.LimitTop = 0;
        Cam.LimitRight = map.W * T; Cam.LimitBottom = map.H * T;
        Cam.ResetSmoothing();
    }

    public void Sync(double dt, Vector2 screenOffset)
    {
        Time += dt;
        var p = W.P;
        // Camera follows the player, nudged so they sit in the middle of the uncovered play area.
        // Touch screens show about nine rows of tiles: whole-number zoom would leave too few on a phone.
        float h = GetViewportRect().Size.Y;
        float zoom = Ui.Touch ? Mathf.Max(1, h / 300f) : Mathf.Clamp(Mathf.Round(h / 360f), 2, 4);
        Cam.Zoom = new Vector2(zoom, zoom);
        Cam.Offset = screenOffset / zoom;
        Cam.Position = Feet(p) - new Vector2(0, 14);

        var map = W.Map;
        _mod.Color = map.Dark > 0 ? new Color(0.09f, 0.08f, 0.12f) : map.Tint != "" ? new Color(0.86f, 0.95f, 0.88f) : Colors.White;
        if (p.Ghost) _mod.Color *= new Color(0.75f, 0.82f, 1.05f);
        _playerLight.Visible = map.Dark > 0;
        _playerLight.Position = Feet(p) - new Vector2(0, 16);
        _playerLight.Energy = 1.1f + Mathf.Sin((float)Time * 7) * 0.04f;

        var alive = new HashSet<string>();
        void Person(Entity e, Look look, bool ghost, float alpha)
        {
            alive.Add(e.Id);
            if (!_actorMap.TryGetValue(e.Id, out var a)) { a = Actor.Person(); _actors.AddChild(a); _actorMap[e.Id] = a; }
            a.SetLook(look);
            a.Position = Feet(e);
            int col = e.AttackAnim > 0 ? (e.AttackAnim > 0.12 ? 4 : 5) : WalkCol(e);
            if (ghost) a.Position += new Vector2(0, Mathf.Sin((float)Time * 3) * 2 - 3);
            a.ShowPerson(e.Facing, col, ghost, alpha);
        }
        Person(p, p.Look, p.Ghost, p.Buffs.ContainsKey("stealth") ? 0.4f : 1f);
        foreach (var n in W.Npcs) Person(n, n.Def.Look, false, 1);
        foreach (var f in W.FolkList) Person(f, f.Def.Look, false, 1);
        foreach (var m in W.Monsters)
        {
            alive.Add(m.Id);
            if (!_actorMap.TryGetValue(m.Id, out var a)) { a = Actor.ForMonster(m.D); _actors.AddChild(a); _actorMap[m.Id] = a; }
            a.Position = Feet(m);
            bool moving = m.MoveT < 1;
            bool flier = m.D.Sprite == "bat";
            int col = m.AttackAnim > 0 ? 2 : (moving || flier) ? (int)(Time * (flier ? 8 : 6) + m.X) % 2 : 0;
            float bob = flier ? -8 + Mathf.Sin((float)Time * 5 + m.Y) * 2 : 0;
            if (m.AttackAnim > 0)
            {
                var (dx, dy) = m.Facing.Vec();
                float k = Mathf.Sin((float)(m.AttackAnim / 0.25) * Mathf.Pi) * 4;
                a.Position += new Vector2(dx * k, dy * k);
            }
            a.ShowMonster(m.HFace, col, m.Flash > 0, bob);
        }
        foreach (var id in _actorMap.Keys.Where(k => !alive.Contains(k)).ToList())
        {
            _actorMap[id].QueueFree();
            _actorMap.Remove(id);
        }

        foreach (var e in Effects) e.T += dt;
        Effects.RemoveAll(e => e.T >= e.Dur);
        _tiles.QueueRedraw();
        _fx.QueueRedraw();
    }

    private static int WalkCol(Entity e)
    {
        if (e.MoveT >= 1 || e.MoveT < 0.05) return 0;
        if (e.MoveT >= 0.5) return 2;
        return (e.X + e.Y) % 2 == 0 ? 1 : 3;
    }

    public void AddFx(GameEvent e)
    {
        var durations = new Dictionary<string, double>
        {
            ["slash"] = 0.18, ["fire"] = 0.5, ["ice"] = 0.5, ["holy"] = 0.45, ["paralyze"] = 0.6, ["lightning"] = 0.4,
            ["heal"] = 0.8, ["buff"] = 0.7, ["smoke"] = 0.6, ["whirl"] = 0.35, ["levelup"] = 1.4, ["death"] = 0.55,
            ["revive"] = 1, ["portal"] = 0.6, ["roar"] = 0.45,
        };
        Effects.Add(new FxItem
        {
            Kind = e.Kind, X = e.X, Y = e.Y, FromX = e.FromX, FromY = e.FromY, R = e.R, Dir = e.Dir, Big = e.Big,
            Color = e.Color != "" ? Color.FromHtml(e.Color) : Colors.White,
            Dur = e.Dur > 0 ? e.Dur : durations.GetValueOrDefault(e.Kind, 0.5),
        });
    }

    public Rect2 VisibleWorldRect()
    {
        var size = GetViewportRect().Size / Cam.Zoom;
        var center = Cam.GetScreenCenterPosition();
        return new Rect2(center - size / 2, size);
    }
}

// Draws only the tiles in view, plus exit markers and the target bracket under the actors.
public partial class TileLayer : Node2D
{
    private const int T = Assets.T;
    private const string ObjectTiles = "TPBrLm"; // trees, bushes, rocks... drawn over the zone's ground
    public GameView View = null!;

    public override void _Draw()
    {
        var w = View.W;
        var map = w.Map;
        var r = View.VisibleWorldRect();
        int x0 = Math.Max(0, (int)(r.Position.X / T) - 1), y0 = Math.Max(0, (int)(r.Position.Y / T) - 1);
        int x1 = Math.Min(map.W - 1, (int)(r.End.X / T) + 1), y1 = Math.Min(map.H - 1, (int)(r.End.Y / T) + 1);
        int frame = (int)(View.Time * 3) % 4;
        var atlas = Assets.TileAtlas;
        char ground = map.Id switch { "forest" => 'g', "cave" => 'c', _ => '.' };
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                int variant = (int)(Rnd.Hash2(x, y, map.Id.Length) * 4);
                char tile = map.Tile(x, y);
                var dest = new Rect2(x * T, y * T, T, T);
                if (ObjectTiles.Contains(tile))
                    DrawTextureRectRegion(atlas, dest, new Rect2(variant * 4 * T, Assets.TileRow(ground) * T, T, T));
                DrawTextureRectRegion(atlas, dest, new Rect2((variant * 4 + frame) * T, Assets.TileRow(tile) * T, T, T));
            }
        float pulse = 0.22f + 0.12f * Mathf.Sin((float)View.Time * 4);
        foreach (var pt in map.Portals.Values)
            DrawRect(new Rect2(pt.X * T + 4, pt.Y * T + 4, T - 8, T - 8), new Color(0.65f, 0.92f, 1f, pulse));
        foreach (var e in View.Effects)
        {
            if (e.Kind != "roarwarn") continue;
            float a = 0.18f + 0.15f * Mathf.Abs(Mathf.Sin((float)e.T * 12));
            DrawRect(new Rect2((e.X - e.R) * T, (e.Y - e.R) * T, (e.R * 2 + 1) * T, (e.R * 2 + 1) * T), new Color(1, 0.15f, 0.1f, a));
        }
        var t = w.GetTarget();
        if (t != null)
        {
            var c = GameView.Feet(t) - new Vector2(16, 30);
            float s = 6, o = -2 - Mathf.Round(Mathf.Sin((float)View.Time * 6) * 1.5f), e2 = T + 2 - o - 2;
            var col = new Color(1f, 0.32f, 0.25f);
            foreach (var (px, py, dx, dy) in new[] { (o, o, 1, 1), (e2, o, -1, 1), (o, e2, 1, -1), (e2, e2, -1, -1) })
            {
                DrawRect(new Rect2(c.X + px + (dx < 0 ? -s : 0), c.Y + py + (dy < 0 ? -2 : 0), s, 2), col);
                DrawRect(new Rect2(c.X + px + (dx < 0 ? -2 : 0), c.Y + py + (dy < 0 ? -s : 0), 2, s), col);
            }
        }
    }
}

// Spell and hit effects, drawn in world space so they share the pixel scale.
public partial class FxLayer : Node2D
{
    private const int T = Assets.T;
    public GameView View = null!;

    private static float Ease(float t) => 1 - (1 - t) * (1 - t);

    public override void _Draw()
    {
        foreach (var e in View.Effects)
        {
            float k = (float)(e.T / e.Dur);
            var c = new Vector2(e.X * T + 16, e.Y * T + 16);
            switch (e.Kind)
            {
                case "slash":
                {
                    float baseAng = e.Dir switch { Dir.Right => 0, Dir.Down => Mathf.Pi / 2, Dir.Left => Mathf.Pi, _ => -Mathf.Pi / 2 };
                    var col = e.Color with { A = 1 - k };
                    DrawArc(c, 11, baseAng - 1.4f + k * 0.6f, baseAng - 0.2f + k * 1.4f, 10, col, 3);
                    DrawArc(c, 14, baseAng - 1.2f + k * 0.6f, baseAng - 0.4f + k * 1.4f, 10, new Color(1, 1, 1, 1 - k), 1);
                    break;
                }
                case "fire":
                case "ice":
                {
                    var cols = e.Kind == "fire"
                        ? new[] { Assets.C("#ffd060"), Assets.C("#f06030"), Assets.C("#a02010") }
                        : new[] { Assets.C("#ffffff"), Assets.C("#90d8ff"), Assets.C("#3080d0") };
                    var from = new Vector2(e.FromX * T + 16, e.FromY * T + 8);
                    if (k < 0.36f)
                    {
                        var p = from.Lerp(c, k / 0.36f);
                        DrawCircle(p, 4, cols[1]);
                        DrawCircle(p, 2, cols[0]);
                        DrawCircle(from.Lerp(c, Mathf.Max(0, k / 0.36f - 0.15f)), 2, cols[2]);
                    }
                    else
                    {
                        float q = (k - 0.36f) / 0.64f;
                        for (int i = 0; i < 10; i++)
                        {
                            float a = i / 10f * Mathf.Tau + e.Seed * 6;
                            DrawRect(new Rect2(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Ease(q) * 18 - new Vector2(2, 2), 4, 4), cols[i % 3] with { A = 1 - q });
                        }
                    }
                    break;
                }
                case "holy":
                    DrawRect(new Rect2(c.X - 6, c.Y - 220, 12, 230), new Color(1, 0.96f, 0.7f, 1 - k));
                    DrawRect(new Rect2(c.X - 3, c.Y - 220, 6, 230), new Color(1, 0.85f, 0.38f, 1 - k));
                    break;
                case "paralyze":
                    for (int i = 0; i < 3; i++) DrawArc(c, 4 + (k * 30 + i * 8) % 22, 0, Mathf.Tau, 24, new Color(1, 0.88f, 0.38f, 1 - k), 1);
                    break;
                case "lightning":
                {
                    DrawRect(new Rect2((e.X - e.R) * T, (e.Y - e.R) * T, (e.R * 2 + 1) * T, (e.R * 2 + 1) * T), new Color(0.8f, 0.75f, 1, 0.25f * (1 - k)));
                    var offs = new[] { (0, 0), (-1, -1), (1, 1), (1, -1), (-1, 1) };
                    for (int i = 0; i < offs.Length; i++)
                    {
                        var target = c + new Vector2(offs[i].Item1, offs[i].Item2) * e.R * 16;
                        var pts = new Vector2[7];
                        for (int s = 0; s <= 6; s++)
                        {
                            float jitter = s is 0 or 6 ? 0 : ((float)Rnd.Hash2(i, s, (int)(e.T * 20)) - 0.5f) * 16;
                            pts[s] = new Vector2(target.X + jitter, target.Y - 200 + 200 * s / 6f);
                        }
                        DrawPolyline(pts, new Color(1, 1, 1, 1 - k), 2);
                    }
                    break;
                }
                case "heal":
                case "revive":
                case "levelup":
                {
                    var col = e.Kind == "heal" ? Assets.C("#7af08a") : e.Kind == "revive" ? Colors.White : Assets.C("#ffd84a");
                    if (e.Kind != "heal") DrawRect(new Rect2(c.X - 12, c.Y - 200, 24, 216), col with { A = (1 - k) * 0.45f });
                    for (int i = 0; i < 12; i++)
                    {
                        float a = e.Seed * 10 + i * 1.7f;
                        var p = new Vector2(c.X + Mathf.Cos(a) * (6 + i % 4 * 3), c.Y + 12 - k * 40 - i % 3 * 6);
                        if (e.Kind == "heal") { DrawRect(new Rect2(p.X - 1, p.Y - 3, 2, 6), col with { A = 1 - k }); DrawRect(new Rect2(p.X - 3, p.Y - 1, 6, 2), col with { A = 1 - k }); }
                        else DrawRect(new Rect2(p.X - 1, p.Y - 1, 2, 2), col with { A = 1 - k });
                    }
                    break;
                }
                case "buff":
                {
                    float r = 8 + k * 10;
                    DrawSetTransform(new Vector2(c.X, c.Y + 12 - k * 24), 0, new Vector2(1, 0.4f));
                    DrawArc(Vector2.Zero, r, 0, Mathf.Tau, 24, e.Color with { A = 1 - k }, 2);
                    DrawSetTransform(Vector2.Zero, 0, Vector2.One);
                    break;
                }
                case "smoke":
                case "death":
                case "portal":
                {
                    int n = e.Big ? 22 : 10;
                    for (int i = 0; i < n; i++)
                    {
                        float a = i / (float)n * Mathf.Tau + e.Seed * 5 + (e.Kind == "portal" ? k * 6 : 0);
                        float r = Ease(k) * (e.Big ? 40 : 16), s = e.Big ? 6 : 4;
                        var col = e.Kind == "portal" ? (i % 2 == 0 ? Assets.C("#a0e8ff") : Colors.White) : new[] { Assets.C("#d8d8d8"), Assets.C("#a0a0a0"), Colors.White }[i % 3];
                        DrawRect(new Rect2(c.X + Mathf.Cos(a) * r - s / 2, c.Y + Mathf.Sin(a) * r * 0.7f - k * 8 - s / 2, s, s), col with { A = 1 - k });
                    }
                    break;
                }
                case "whirl":
                    DrawArc(c, 26, k * 10, k * 10 + Mathf.Pi * 1.3f, 20, new Color(1, 0.85f, 0.56f, 1 - k), 3);
                    break;
                case "roar":
                    DrawArc(c, 10 + Ease(k) * (e.R + 0.5f) * T, 0, Mathf.Tau, 40, new Color(1, 0.5f, 0.25f, 1 - k), 4);
                    break;
            }
        }
    }
}
