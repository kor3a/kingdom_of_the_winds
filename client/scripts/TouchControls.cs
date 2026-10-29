using Godot;
using KotW.Shared;

namespace KotW;

// On-screen controls for touch screens: a floating stick in the lower left, attack and target buttons in the
// lower right, and taps on the hotbar beside them. Godot only turns the first finger into mouse clicks, which
// plain Buttons rely on, so this follows every finger by its touch index: you can walk and cast at once.
public partial class TouchControls : Control
{
    public const float StickW = 210, StickH = 200, Edge = 8, AttackR = 46, TargetR = 27;
    public const float ClusterW = Edge + AttackR * 2 + 10; // room the attack button takes from the right edge
    private const float StickR = 50, DeadZone = 14;

    public Action<Dir> OnDir = _ => { };
    public Action OnAttack = () => { }, OnTarget = () => { };
    public Action<int> OnSlot = _ => { };
    public Func<Vector2, bool> Blocked = _ => false; // true where a pop-up window covers the point
    public Container? Hotbar;

    public Dir? Held { get; private set; }
    public bool Attacking => _fingers.ContainsValue(Part.Attack);

    private enum Part { Stick, Attack, Other }
    private readonly Dictionary<int, Part> _fingers = new();
    private Vector2 _stickAt, _knob; // where the stick finger landed (local), and how far it has moved
    private Control _stickZone = null!, _attack = null!, _target = null!;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        // These areas swallow the mouse clicks Godot makes from the first finger, so a press doesn't also tap the world.
        _stickZone = Area(0, 0, -StickH, StickW, 0);
        float ax = -(Edge + AttackR), ay = -(Edge + AttackR);
        _attack = Area(1, ax - AttackR, ay - AttackR, ax + AttackR, ay + AttackR);
        float ty = ay - AttackR - 10 - TargetR;
        _target = Area(1, ax - TargetR, ty - TargetR, ax + TargetR, ty + TargetR);
    }

    private Control Area(float anchorX, float left, float top, float right, float bottom)
    {
        var c = new Control
        {
            MouseFilter = MouseFilterEnum.Stop, AnchorLeft = anchorX, AnchorRight = anchorX, AnchorTop = 1, AnchorBottom = 1,
            OffsetLeft = left, OffsetTop = top, OffsetRight = right, OffsetBottom = bottom,
        };
        AddChild(c);
        return c;
    }

    public override void _Input(InputEvent ev)
    {
        if (!IsVisibleInTree()) return;
        if (ev is InputEventScreenTouch t)
        {
            if (t.Pressed) Press(t.Index, t.Position);
            else if (_fingers.Remove(t.Index, out var part))
            {
                if (part == Part.Stick) { _knob = Vector2.Zero; SetHeld(null); }
                QueueRedraw();
            }
        }
        else if (ev is InputEventScreenDrag d && _fingers.TryGetValue(d.Index, out var p) && p == Part.Stick)
            Steer(GetGlobalTransform().AffineInverse() * d.Position);
    }

    private void Press(int finger, Vector2 at)
    {
        if (Blocked(at)) return;
        if (Near(_attack, at, AttackR + 6)) { _fingers[finger] = Part.Attack; OnAttack(); }
        else if (Near(_target, at, TargetR + 6)) { _fingers[finger] = Part.Other; OnTarget(); }
        else if (SlotAt(at) is { } slot) { _fingers[finger] = Part.Other; OnSlot(slot); }
        else if (_stickZone.GetGlobalRect().HasPoint(at) && !_fingers.ContainsValue(Part.Stick))
        {
            _fingers[finger] = Part.Stick;
            _stickAt = GetGlobalTransform().AffineInverse() * at;
            _knob = Vector2.Zero;
        }
        else return;
        GetViewport().SetInputAsHandled();
        QueueRedraw();
    }

    private static bool Near(Control c, Vector2 at, float r) => c.GetGlobalRect().GetCenter().DistanceTo(at) <= r;

    private int? SlotAt(Vector2 at)
    {
        if (Hotbar == null) return null;
        foreach (var c in Hotbar.GetChildren())
            if (c is Button { Disabled: false } b && !b.IsQueuedForDeletion() && b.GetGlobalRect().HasPoint(at)) return b.GetMeta("slot").AsInt32();
        return null;
    }

    private void Steer(Vector2 at)
    {
        var v = at - _stickAt;
        // A long drag pulls the stick along, so turning back doesn't need the finger to cross the whole circle.
        if (v.Length() > StickR) { _stickAt = at - v.Normalized() * StickR; v = at - _stickAt; }
        _knob = v;
        Dir? d = v.Length() < DeadZone ? null
            : Math.Abs(v.X) > Math.Abs(v.Y) ? (v.X > 0 ? Dir.Right : Dir.Left)
            : (v.Y > 0 ? Dir.Down : Dir.Up);
        SetHeld(d);
        QueueRedraw();
    }

    private void SetHeld(Dir? d)
    {
        if (d == Held) return;
        Held = d;
        if (d is { } dir) OnDir(dir);
    }

    public override void _Draw()
    {
        // The stick sits where the finger landed, or rests in the middle of its corner.
        bool held = _fingers.ContainsValue(Part.Stick);
        var c = held ? _stickAt : _stickZone.GetRect().GetCenter();
        DrawCircle(c, StickR, new Color(0.05f, 0.06f, 0.1f, held ? 0.5f : 0.3f));
        DrawArc(c, StickR, 0, Mathf.Tau, 48, new Color(Ui.Gold, held ? 0.7f : 0.4f), 2, true);
        DrawCircle(c + _knob, 22, new Color(Ui.Gold, held ? 0.75f : 0.35f));
        Round(_attack, AttackR, "攻", Attacking);
        Round(_target, TargetR, "◎", false);
    }

    private void Round(Control area, float r, string glyph, bool down)
    {
        var c = area.GetRect().GetCenter();
        DrawCircle(c, r, down ? new Color(0.48f, 0.3f, 0.11f, 0.85f) : new Color(0.12f, 0.14f, 0.22f, 0.6f));
        DrawArc(c, r, 0, Mathf.Tau, 48, new Color(Ui.Gold, 0.8f), 2, true);
        var font = ThemeDB.FallbackFont;
        int size = (int)(r * 0.8f);
        float w = font.GetStringSize(glyph, HorizontalAlignment.Left, -1, size).X;
        var p = c + new Vector2(-w / 2, size * 0.35f);
        DrawStringOutline(font, p, glyph, HorizontalAlignment.Left, -1, size, 4, Colors.Black);
        DrawString(font, p, glyph, HorizontalAlignment.Left, -1, size, Colors.White);
    }
}
