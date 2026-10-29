using Godot;
using KotW.Shared;

namespace KotW;

// Title, log-in and character creation. Calls back into Main with the save to play.
public partial class TitleScreen : Control
{
    public Action<SaveData, bool> OnStart = (_, _) => { };
    public Action OnMovedToAccount = () => { }; // this device's character now lives in the account
    public Cloud Cloud = null!;
    public SaveData? Local; // the character saved on this device

    private enum Page { Connecting, Login, Character }
    private static readonly string[] HairColors = { "#1a1a1a", "#3a2418", "#7a4a2a", "#c8a060", "#8a3a3a", "#e8e8e8" };
    private static readonly string[] SkinColors = { "#f2d0b0", "#f2c9a0", "#d9a070", "#a87048" };
    private static readonly (string Id, string Label)[] Styles = { ("topknot", "Topknot"), ("bun", "Bun"), ("long", "Long"), ("bald", "Shaved") };
    private string _hair = HairColors[0], _skin = SkinColors[1], _style = "topknot";
    private readonly List<DollBox> _previews = new();
    private LineEdit? _name;
    private bool _confirmNew;
    private VBoxContainer _card = null!;
    private Page _page;
    private SaveData? _existing; // offered under Continue
    private bool _existingIsLocal; // ...and only on this device so far, though the player is logged in
    private string? _error;
    private bool _busy;
    private string _email = "", _password = "";

    public override void _Ready()
    {
        Theme = Ui.MakeTheme();
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var bg = new ColorRect { Color = Assets.C("#0d0f17") };
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);
        // Scrolls when the card is taller than the screen, as on a phone held sideways.
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        scroll.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(scroll);
        var center = new CenterContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        scroll.AddChild(center);
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(480, 0) };
        panel.AddThemeStyleboxOverride("panel", Ui.Box("#1f2438f4", "#6a5530", 3, Ui.Touch ? 12 : 22));
        center.AddChild(panel);
        _card = Ui.VBox(Ui.Touch ? 6 : 10);
        panel.AddChild(_card);
        if (Cloud.LoggedIn) _ = Connect();
        else { _page = Page.Login; Build(); }
    }

    // Slide the card up while the on-screen keyboard would cover the box being typed in.
    public override void _Process(double delta)
    {
        if (!Ui.Touch) return;
        float shift = 0;
        if (GetViewport().GuiGetFocusOwner() is LineEdit edit)
        {
            float bottom = edit.GetGlobalRect().End.Y - Position.Y + 8;
            shift = Math.Max(0, bottom - (GetViewportRect().Size.Y - Ui.KeyboardHeight(GetTree().Root)));
        }
        Position = new Vector2(0, -shift);
    }

    // Loads the account's character, or offers to move this device's one into it.
    private async Task Connect()
    {
        _page = Page.Connecting;
        _error = null;
        Build();
        var (save, error) = await Cloud.Fetch();
        if (_page != Page.Connecting) return; // the player chose to play offline while waiting
        _error = error;
        if (error != null) { _page = Page.Login; Build(); return; }
        _existing = save ?? Local;
        _existingIsLocal = save == null && Local != null;
        _page = Page.Character;
        Build();
    }

    private async Task LogIn(bool create)
    {
        var email = Accounts.NormalizeEmail(_email);
        _error = Accounts.EmailProblem(email)
            ?? (create ? Accounts.PasswordProblem(_password) : _password.Length == 0 ? "Please enter your password." : null);
        if (_error == null)
        {
            _busy = true;
            Build();
            _error = await Cloud.LogIn(email, _password, create);
            _busy = false;
        }
        if (_error != null) { Build(); return; }
        _password = "";
        await Connect();
    }

    private void LogOut()
    {
        Cloud.LogOut();
        _existing = null;
        _error = null;
        _confirmNew = false;
        _page = Page.Login;
        Build();
    }

    private void PlayOffline()
    {
        Cloud.Offline = true;
        _existing = Local;
        _existingIsLocal = false;
        _error = null;
        _confirmNew = false;
        _page = Page.Character;
        Build();
    }

    private void ShowLogin()
    {
        Cloud.Offline = false;
        _confirmNew = false;
        if (Cloud.LoggedIn) { _ = Connect(); return; }
        _error = null;
        _page = Page.Login;
        Build();
    }

    private void Build()
    {
        foreach (var c in _card.GetChildren()) c.QueueFree();
        _previews.Clear();
        if (!Ui.Touch) // the big glyph doesn't fit a phone held sideways
        {
            var glyph = Ui.Label("風", Ui.Gold, 56);
            glyph.HorizontalAlignment = HorizontalAlignment.Center;
            _card.AddChild(glyph);
        }
        var title = Ui.Label("Kingdom of the Winds", Ui.GoldHi, 34);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeConstantOverride("outline_size", 6);
        title.AddThemeColorOverride("font_outline_color", Colors.Black);
        var sub = Ui.Label("A tile-based RPG of walled towns, rice paddies and mountain spirits", Ui.Muted, 14);
        sub.HorizontalAlignment = HorizontalAlignment.Center;
        _card.With(title, sub);
        switch (_page)
        {
            case Page.Connecting:
                var wait = Ui.Label("Connecting to your account…", Ui.Muted, 14);
                wait.HorizontalAlignment = HorizontalAlignment.Center;
                _card.With(wait, Ui.Button("Play offline on this device", PlayOffline));
                break;
            case Page.Login: BuildLogin(); break;
            default: BuildCharacter(); break;
        }
    }

    private void BuildLogin()
    {
        if (Cloud.LoggedIn) // logged in on this device, but the server couldn't be reached
        {
            _card.AddChild(Ui.Label($"Logged in as {Cloud.Email}", Ui.Muted, 13));
            if (_error != null) _card.AddChild(Ui.Label(_error, Ui.Bad, 14, true));
            _card.With(Ui.Button("Try again", () => _ = Connect(), true), Ui.Button("Play offline on this device", PlayOffline), Ui.Button("Log out", LogOut));
            return;
        }
        _card.AddChild(Ui.Label("Log in to keep your character on every computer, phone and tablet you play on.", Ui.Muted, 14, true));
        var email = new LineEdit { PlaceholderText = "you@example.com", Text = _email, VirtualKeyboardType = LineEdit.VirtualKeyboardTypeEnum.EmailAddress };
        var pass = new LineEdit { PlaceholderText = $"At least {Accounts.MinPassword} characters", Text = _password, Secret = true, VirtualKeyboardType = LineEdit.VirtualKeyboardTypeEnum.Password };
        email.TextChanged += t => _email = t;
        pass.TextChanged += t => _password = t;
        email.TextSubmitted += _ => pass.GrabFocus();
        pass.TextSubmitted += text => { if (!_busy) _ = LogIn(false); };
        _card.With(Ui.Label("Email", Ui.Muted, 13), email, Ui.Label("Password", Ui.Muted, 13), pass);
        if (_error != null) _card.AddChild(Ui.Label(_error, Ui.Bad, 14, true));
        var row = Ui.HBox(8);
        var logIn = Ui.Button(_busy ? "Connecting…" : "Log in", () => _ = LogIn(false), true, _busy);
        var create = Ui.Button("Create account", () => _ = LogIn(true), false, _busy);
        logIn.SizeFlagsHorizontal = create.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _card.AddChild(row.With(logIn, create));
        _card.AddChild(Ui.Button("Play offline on this device", PlayOffline, false, _busy));
        if (!Ui.Touch && !_busy) (_email.Length == 0 ? email : pass).CallDeferred(Control.MethodName.GrabFocus);
    }

    private void BuildCharacter()
    {
        var account = Ui.HBox(8);
        account.Alignment = BoxContainer.AlignmentMode.Center;
        if (Cloud.Active) account.With(Ui.Label($"Logged in as {Cloud.Email}", Ui.Muted, 13), Ui.Button("Log out", LogOut, false, _busy));
        else account.With(Ui.Label("Playing offline: saves stay on this device", Ui.Muted, 13), Ui.Button("Log in", ShowLogin, false, _busy));
        _card.AddChild(account);

        if (_existing != null)
        {
            var cls = Content.Classes[_existing.Cls ?? "none"].Name;
            var existing = _existing;
            _card.AddChild(Ui.Button($"Continue as {existing.Name} · Lv {existing.Level} {cls}", () => _ = Play(existing, false), true, _busy));
            if (_existingIsLocal) _card.AddChild(Ui.Label("This character is only on this device. Continuing moves it to your account.", Ui.Muted, 13, true));
            _card.AddChild(Ui.Label("or start a new character", Ui.Muted, 13));
        }

        var dolls = Ui.HBox(4);
        dolls.Alignment = BoxContainer.AlignmentMode.Center;
        foreach (var d in new[] { Dir.Down, Dir.Right, Dir.Up, Dir.Left })
        {
            var box = new DollBox(Ui.Touch ? 1.8f : 2.5f) { Facing = d };
            _previews.Add(box);
            dolls.AddChild(box);
        }
        _card.AddChild(dolls);

        _name = new LineEdit { PlaceholderText = "Your name", MaxLength = Rules.MaxName };
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
            var b = Ui.Button(text, () => { _style = id; Rebuild(); }, _style == id);
            styleRow.AddChild(b);
        }
        _card.AddChild(styleRow);
        if (_error != null) _card.AddChild(Ui.Label(_error, Ui.Bad, 14, true));
        if (_confirmNew && _existing != null)
            _card.With(Ui.Label($"Starting a new character replaces {_existing.Name}.", Ui.Bad, 14), Ui.Button("Yes, start over", Start, true, _busy));
        else _card.AddChild(Ui.Button("Begin your journey", Begin, true, _busy));
        var foot = Ui.Label(Ui.Touch ? "Drag in the lower left to move · 攻 to attack · Help for more" : "Arrow keys or WASD to move · Space to attack · H for help", Ui.Muted, 13);
        foot.HorizontalAlignment = HorizontalAlignment.Center;
        _card.AddChild(foot);
        UpdatePreview();
    }

    // Build again, keeping the name typed so far.
    private void Rebuild()
    {
        var keep = _name?.Text ?? "";
        Build();
        if (_name != null) _name.Text = keep;
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
            b.Pressed += () => { set(hex); Rebuild(); };
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
        if (_busy || _name == null) return;
        if (Rules.CleanName(_name.Text).Length == 0) { _name.GrabFocus(); _name.PlaceholderText = "Please enter a name"; return; }
        if (_existing != null && !_confirmNew) { _confirmNew = true; Rebuild(); return; }
        Start();
    }

    private void Start()
    {
        var name = Rules.CleanName(_name?.Text ?? "");
        if (_busy || name.Length == 0) return;
        _ = Play(Rules.NewCharacter(name, _skin, _hair, _style), true);
    }

    private async Task Play(SaveData data, bool isNew)
    {
        if (Cloud.Active && (isNew || _existingIsLocal))
        {
            // Save to the account before playing, so a name someone else has is caught here.
            _busy = true;
            _error = null;
            Rebuild();
            _error = await Cloud.Upload(data);
            _busy = false;
            if (_error != null)
            {
                if (!Cloud.LoggedIn) { _page = Page.Login; Build(); } // the login expired
                else Rebuild();
                return;
            }
            if (!isNew) OnMovedToAccount();
        }
        OnStart(data, isNew);
    }
}
