using Godot;
using KotW.Shared;

namespace KotW;

// Title and character creation. Calls back into Main with the save to play.
public partial class TitleScreen : Control
{
    public Action<SaveData, bool> OnStart = (_, _) => { };
    public SaveData? Existing;

    private static readonly string[] HairColors = { "#1a1a1a", "#3a2418", "#7a4a2a", "#c8a060", "#8a3a3a", "#e8e8e8" };
    private static readonly string[] SkinColors = { "#f2d0b0", "#f2c9a0", "#d9a070", "#a87048" };
    private static readonly (string Id, string Label)[] Styles = { ("topknot", "Topknot"), ("bun", "Bun"), ("long", "Long"), ("bald", "Shaved") };
    private string _hair = HairColors[0], _skin = SkinColors[1], _style = "topknot";
    private readonly List<DollBox> _previews = new();
    private LineEdit _name = null!;
    private bool _confirmNew;
    private VBoxContainer _card = null!;

    public override void _Ready()
    {
        Theme = Ui.MakeTheme();
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var bg = new ColorRect { Color = Assets.C("#0d0f17") };
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(480, 0) };
        panel.AddThemeStyleboxOverride("panel", Ui.Box("#1f2438f4", "#6a5530", 3, 22));
        center.AddChild(panel);
        _card = Ui.VBox(10);
        panel.AddChild(_card);
        Build();
    }

    private void Build()
    {
        foreach (var c in _card.GetChildren()) c.QueueFree();
        _previews.Clear();
        var glyph = Ui.Label("風", Ui.Gold, 56);
        glyph.HorizontalAlignment = HorizontalAlignment.Center;
        var title = Ui.Label("Kingdom of the Winds", Ui.GoldHi, 34);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeConstantOverride("outline_size", 6);
        title.AddThemeColorOverride("font_outline_color", Colors.Black);
        var sub = Ui.Label("A tile-based RPG of walled towns, rice paddies and mountain spirits", Ui.Muted, 14);
        sub.HorizontalAlignment = HorizontalAlignment.Center;
        _card.With(glyph, title, sub);

        if (Existing != null)
        {
            var cls = Content.Classes[Existing.Cls ?? "none"].Name;
            _card.AddChild(Ui.Button($"Continue as {Existing.Name} · Lv {Existing.Level} {cls}", () => OnStart(Existing, false), true));
            _card.AddChild(Ui.Label("or start a new character", Ui.Muted, 13));
        }

        var dolls = Ui.HBox(4);
        dolls.Alignment = BoxContainer.AlignmentMode.Center;
        foreach (var d in new[] { Dir.Down, Dir.Right, Dir.Up, Dir.Left })
        {
            var box = new DollBox(2.5f) { Facing = d };
            _previews.Add(box);
            dolls.AddChild(box);
        }
        _card.AddChild(dolls);

        _name = new LineEdit { PlaceholderText = "Your name", MaxLength = 12 };
        _name.TextSubmitted += _ => Begin();
        _card.With(Ui.Label("Name", Ui.Muted, 13), _name);
        _card.AddChild(SwatchRow("Hair", HairColors, () => _hair, v => _hair = v));
        _card.AddChild(SwatchRow("Skin", SkinColors, () => _skin, v => _skin = v));
        var styleRow = Ui.HBox(6);
        var label = Ui.Label("Style", Ui.Muted, 13);
        label.CustomMinimumSize = new Vector2(50, 0);
        styleRow.AddChild(label);
        foreach (var (id, text) in Styles)
        {
            var b = Ui.Button(text, () => { _style = id; Build(); }, _style == id);
            styleRow.AddChild(b);
        }
        _card.AddChild(styleRow);
        if (_confirmNew && Existing != null)
            _card.With(Ui.Label($"Starting a new character replaces {Existing.Name}.", Ui.Bad, 14), Ui.Button("Yes, start over", Start, true));
        else _card.AddChild(Ui.Button("Begin your journey", Begin, true));
        var foot = Ui.Label("Arrow keys or WASD to move · Space to attack · H for help", Ui.Muted, 13);
        foot.HorizontalAlignment = HorizontalAlignment.Center;
        _card.AddChild(foot);
        UpdatePreview();
    }

    private Control SwatchRow(string title, string[] colors, Func<string> get, Action<string> set)
    {
        var row = Ui.HBox(6);
        var label = Ui.Label(title, Ui.Muted, 13);
        label.CustomMinimumSize = new Vector2(50, 0);
        row.AddChild(label);
        foreach (var hex in colors)
        {
            var b = new Button { CustomMinimumSize = new Vector2(28, 28), FocusMode = FocusModeEnum.None, TooltipText = hex };
            bool on = get() == hex;
            b.AddThemeStyleboxOverride("normal", Ui.Box(hex, on ? "#f4d88a" : "#000000", on ? 3 : 2, 0));
            b.AddThemeStyleboxOverride("hover", Ui.Box(hex, "#f4d88a", 2, 0));
            b.Pressed += () => { set(hex); var keep = _name.Text; Build(); _name.Text = keep; };
            row.AddChild(b);
        }
        return row;
    }

    private void UpdatePreview()
    {
        foreach (var p in _previews) p.Doll.SetLook(new Look(_skin, _hair, _style, "hemp_robe", "wooden_sword"));
    }

    private void Begin()
    {
        var name = new string(_name.Text.Trim().Where(ch => char.IsLetterOrDigit(ch) || ch is ' ' or '-' or '_').ToArray());
        if (name.Length == 0) { _name.GrabFocus(); _name.PlaceholderText = "Please enter a name"; return; }
        if (Existing != null && !_confirmNew) { var keep = _name.Text; _confirmNew = true; Build(); _name.Text = keep; return; }
        Start();
    }

    private void Start()
    {
        var name = new string(_name.Text.Trim().Where(ch => char.IsLetterOrDigit(ch) || ch is ' ' or '-' or '_').ToArray());
        if (name.Length == 0) return;
        OnStart(Rules.NewCharacter(name.Length > 12 ? name[..12] : name, _skin, _hair, _style), true);
    }
}
