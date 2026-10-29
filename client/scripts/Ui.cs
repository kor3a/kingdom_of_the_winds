using Godot;

namespace KotW;

// Theme and small widget helpers shared by the HUD and the title screen.
public static class Ui
{
    public static readonly Color Gold = Assets.C("#d8b25a"), GoldHi = Assets.C("#f4d88a"), Ink = Assets.C("#e9e2cc"),
        Muted = Assets.C("#9a94a8"), Good = Assets.C("#7ad88a"), Bad = Assets.C("#f07060"), Edge = Assets.C("#6a5530");

    public static StyleBoxFlat Box(string bg, string border, int bw = 2, int margin = 8)
    {
        var s = new StyleBoxFlat { BgColor = Assets.C(bg), BorderColor = Assets.C(border) };
        s.SetBorderWidthAll(bw);
        s.SetContentMarginAll(margin);
        return s;
    }

    public static Theme MakeTheme()
    {
        var t = new Theme { DefaultFontSize = 15 };
        t.SetColor("font_color", "Label", Ink);
        t.SetStylebox("panel", "PanelContainer", Box("#1e2336f2", "#6a5530"));
        t.SetStylebox("panel", "Panel", Box("#1e2336f2", "#6a5530"));
        t.SetStylebox("normal", "Button", Box("#2c3352", "#5a4a34", 2, 4));
        t.SetStylebox("hover", "Button", Box("#37406a", "#d8b25a", 2, 4));
        t.SetStylebox("pressed", "Button", Box("#22283f", "#d8b25a", 2, 4));
        t.SetStylebox("disabled", "Button", Box("#22263a", "#3a3448", 2, 4));
        t.SetStylebox("focus", "Button", new StyleBoxEmpty());
        t.SetColor("font_color", "Button", Ink);
        t.SetColor("font_hover_color", "Button", Colors.White);
        t.SetColor("font_disabled_color", "Button", Muted);
        t.SetStylebox("normal", "LineEdit", Box("#0c0e16", "#4a4058", 2, 5));
        t.SetStylebox("focus", "LineEdit", Box("#0c0e16", "#d8b25a", 2, 5));
        t.SetStylebox("panel", "TooltipPanel", Box("#0c0e16f4", "#6a5530", 2, 7));
        t.SetColor("font_color", "TooltipLabel", Ink);
        t.SetStylebox("normal", "RichTextLabel", new StyleBoxEmpty());
        return t;
    }

    public static Label Label(string text, Color? color = null, int size = 15, bool wrap = false)
    {
        var l = new Label { Text = text };
        if (color is { } c) l.AddThemeColorOverride("font_color", c);
        if (size != 15) l.AddThemeFontSizeOverride("font_size", size);
        if (wrap) { l.AutowrapMode = TextServer.AutowrapMode.WordSmart; l.CustomMinimumSize = new Vector2(10, 0); l.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; }
        return l;
    }

    public static Button Button(string text, Action onPress, bool primary = false, bool disabled = false)
    {
        var b = new Button { Text = text, FocusMode = Control.FocusModeEnum.None, Disabled = disabled };
        if (primary)
        {
            b.AddThemeStyleboxOverride("normal", Box("#7a4e1c", "#d8b25a", 2, 5));
            b.AddThemeStyleboxOverride("hover", Box("#946024", "#f4d88a", 2, 5));
            b.AddThemeColorOverride("font_color", Assets.C("#fff3d0"));
        }
        b.Pressed += onPress;
        return b;
    }

    public static VBoxContainer VBox(int sep = 6) { var v = new VBoxContainer(); v.AddThemeConstantOverride("separation", sep); return v; }
    public static HBoxContainer HBox(int sep = 6) { var h = new HBoxContainer(); h.AddThemeConstantOverride("separation", sep); return h; }

    public static T With<T>(this T parent, params Node?[] kids) where T : Node
    {
        foreach (var k in kids) if (k != null) parent.AddChild(k);
        return parent;
    }
}

// A labelled bar (HP, MP, EXP, target health).
public partial class StatBar : Control
{
    public Color Fill = Colors.Red;
    public double Value, Max = 1;
    public string Text = "";

    public StatBar() { CustomMinimumSize = new Vector2(10, 18); MouseFilter = MouseFilterEnum.Ignore; }

    public override void _Draw()
    {
        var r = new Rect2(Vector2.Zero, Size);
        DrawRect(r, Assets.C("#07080c"));
        float f = Max > 0 ? Mathf.Clamp((float)(Value / Max), 0, 1) : 0;
        var fill = new Rect2(1, 1, (Size.X - 2) * f, Size.Y - 2);
        DrawRect(fill, Fill);
        DrawRect(new Rect2(1, 1, fill.Size.X, 3), Fill.Lightened(0.3f));
        DrawRect(new Rect2(1, Size.Y - 4, fill.Size.X, 3), Fill.Darkened(0.35f));
        DrawRect(r, Assets.C("#3a3020"), false, 1);
        var font = ThemeDB.FallbackFont;
        float w = font.GetStringSize(Text, HorizontalAlignment.Left, -1, 13).X;
        var p = new Vector2((Size.X - w) / 2, Size.Y / 2 + 5);
        DrawStringOutline(font, p, Text, HorizontalAlignment.Left, -1, 13, 3, Colors.Black);
        DrawString(font, p, Text, HorizontalAlignment.Left, -1, 13, Colors.White);
    }

    public void Set(double v, double max, string text)
    {
        if (v == Value && max == Max && text == Text) return;
        Value = v; Max = max; Text = text;
        QueueRedraw();
    }
}

// Shows a paper-doll character inside a UI layout (portraits, the inventory, character creation).
public partial class DollBox : Control
{
    public readonly Actor Doll = Actor.Person();
    public KotW.Shared.Dir Facing = KotW.Shared.Dir.Down;
    private readonly float _scale;

    public DollBox(float scale = 2)
    {
        _scale = scale;
        CustomMinimumSize = new Vector2(32 * scale, 50 * scale);
        MouseFilter = MouseFilterEnum.Ignore;
        Doll.Scale = new Vector2(scale, scale);
        AddChild(Doll);
    }

    public override void _Process(double delta)
    {
        Doll.Position = new Vector2(Size.X / 2, Size.Y - 3 * _scale);
        Doll.ShowPerson(Facing, 0, false, 1);
    }
}
