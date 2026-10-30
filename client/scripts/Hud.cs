using Godot;
using KotW.Shared;

namespace KotW;

// The in-game interface: status bar, hotbar, chat, minimap and pop-up windows.
// Buttons call World methods; the HUD never changes game state itself.
public partial class Hud : Control
{
    // Desktop: the status bar along the bottom, the hotbar row above it, the map and chat panels on the right.
    public const int BarH = 56, HotH = 72, MapW = 240, ChatW = 360, WinW = 330;
    public World W = null!;
    public AudioBox Audio = null!;
    public LineEdit ChatInput = null!;
    public TouchControls? Pad; // on-screen controls, on touch screens only
    public bool ChatOpen { get; private set; }

    // Where the camera centres the player: the middle of the area the HUD leaves uncovered.
    public Vector2 CameraNudge => Ui.Touch ? Vector2.Zero : new Vector2(0, (BarH + HotH) / 2f);

    private RichTextLabel _chat = null!;
    private Label _name = null!, _class = null!, _gold = null!, _zone = null!, _coords = null!, _buffs = null!;
    private Label _tName = null!, _tLevel = null!, _tStatus = null!, _unread = null!;
    private StatBar _hp = null!, _mp = null!, _exp = null!, _tBar = null!;
    private Button _statsBtn = null!, _musicBtn = null!, _mapBtn = null!;
    private Container _hotbar = null!, _feed = null!;
    private Minimap _minimap = null!;
    private Control _safe = null!, _windows = null!, _target = null!, _chatPanel = null!, _mapPanel = null!, _mapBody = null!;
    private Label _banner = null!;
    private double _bannerT, _refreshT;
    private string _hotbarSig = "", _lastWarn = "";
    private ulong _lastWarnAt;
    private int _unreadCount;
    private bool _statsWithBag; // the character sheet was opened alongside the bag, and closes with it
    private readonly Dictionary<string, GameWindow> _open = new();
    private string? _dialogNpc, _shopId;

    public override void _Ready()
    {
        Theme = Ui.MakeTheme();
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        // Everything sits inside the screen's safe area, clear of a phone's notch and home bar.
        _safe = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _safe.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_safe);
        MakeWidgets();
        if (Ui.Touch) BuildTouch();
        else { BuildBar(); BuildHotbar(); BuildMap(); BuildChat(); }
        _windows = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _windows.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        if (!Ui.Touch) _windows.OffsetBottom = -(BarH + HotH);
        _safe.AddChild(_windows);
        _banner = Ui.Label("", Ui.GoldHi, 30);
        _banner.HorizontalAlignment = HorizontalAlignment.Center;
        _banner.AddThemeConstantOverride("outline_size", 8);
        _banner.AddThemeColorOverride("font_outline_color", Colors.Black);
        _banner.SetAnchorsPreset(LayoutPreset.CenterTop);
        _banner.Position = new Vector2(0, 60);
        _banner.MouseFilter = MouseFilterEnum.Ignore;
        _windows.AddChild(_banner);
    }

    // ---- layout -----------------------------------------------------------------------
    // The widgets are the same on every screen; the Build methods only arrange them.
    private void MakeWidgets()
    {
        _name = Ui.Label("", Ui.GoldHi, Ui.Touch ? 16 : 17);
        _class = Ui.Label("", null, Ui.Touch ? 15 : 13);
        _buffs = Ui.Label("", Assets.C("#f0c8ff"), 17);
        _hp = new StatBar { Fill = Assets.C("#d84838") };
        _mp = new StatBar { Fill = Assets.C("#3a78d8") };
        _exp = new StatBar { Fill = Assets.C("#c8a028") };
        _gold = Ui.Label("", Ui.GoldHi);

        var target = new PanelContainer();
        target.AddThemeStyleboxOverride("panel", Ui.Box("#0c0e16d8", "#6a5530", 1, 6));
        var tv = Ui.VBox(2);
        _tName = Ui.Label("", Assets.C("#ffb0a0"));
        _tLevel = Ui.Label("", Ui.Muted, 13);
        _tBar = new StatBar { Fill = Assets.C("#d84838") };
        _tStatus = Ui.Label("", Assets.C("#b0e890"), 13);
        target.AddChild(tv.With(_tName, _tLevel, _tBar, _tStatus));
        _target = target;
        _zone = Ui.Label("", Assets.C("#90e0ff"), 15);
        _minimap = new Minimap { CustomMinimumSize = new Vector2(0, 150) };
        _coords = Ui.Label("", Ui.Muted, 13);

        _chat = new RichTextLabel { BbcodeEnabled = true, ScrollFollowing = true, SizeFlagsVertical = SizeFlags.ExpandFill, SelectionEnabled = !Ui.Touch };
        _chat.AddThemeFontSizeOverride("normal_font_size", Ui.Touch ? 12 : 14);
        ChatInput = new LineEdit
        {
            PlaceholderText = Ui.Touch ? "Tap to chat  ·  /s shout  ·  /w name whisper" : "Say something  ·  /s shout  ·  /w name whisper",
            MaxLength = 120,
        };
        ChatInput.TextSubmitted += text =>
        {
            if (text.Trim() != "") W.Say(text);
            ChatInput.Text = "";
            ChatInput.ReleaseFocus();
        };
    }

    private GridContainer Menu(int columns)
    {
        var menu = new GridContainer { Columns = columns };
        menu.AddThemeConstantOverride("h_separation", 4);
        menu.AddThemeConstantOverride("v_separation", 4);
        Button Add(string key, string label, Action a)
        {
            var b = Ui.Button(Ui.Touch ? label : $"[{key}] {label}", a);
            b.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            b.AddThemeFontSizeOverride("font_size", 13);
            if (Ui.Touch) b.CustomMinimumSize = new Vector2(0, 30);
            menu.AddChild(b);
            return b;
        }
        Add("I", "Bag", () => Toggle("inventory"));
        _statsBtn = Add("C", "Stats", () => Toggle("stats"));
        Add("K", "Skills", () => Toggle("skills"));
        Add("Q", "Quests", () => Toggle("quests"));
        Add("H", "Help", () => Toggle("help"));
        _musicBtn = Add("M", "Music", ToggleMusic);
        return menu;
    }

    // A strip across the bottom of the screen: who you are, your bars and gold, and the chat button.
    private void BuildBar()
    {
        var bar = new PanelContainer { AnchorTop = 1, AnchorRight = 1, AnchorBottom = 1, OffsetTop = -BarH };
        bar.AddThemeStyleboxOverride("panel", Ui.Box("#1e2336f2", "#6a5530", 2, 6));
        _safe.AddChild(bar);
        var who = Ui.VBox(0).With(_name, _class);
        who.CustomMinimumSize = new Vector2(170, 0);
        who.Alignment = BoxContainer.AlignmentMode.Center;
        _gold.CustomMinimumSize = new Vector2(130, 0);
        _gold.VerticalAlignment = VerticalAlignment.Center;
        var h = Ui.HBox(16).With(who);
        foreach (var (label, sb) in new[] { ("HP", _hp), ("MP", _mp), ("EXP", _exp) })
        {
            sb.CustomMinimumSize = new Vector2(10, 20);
            var row = Row(label, sb);
            row.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            h.AddChild(row);
        }
        h.AddChild(_gold);
        // The chat sits behind this button; a red badge counts the lines that arrived while it was shut.
        var chatBtn = new Button { FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(48, 0), TooltipText = "Chat (Enter)" };
        chatBtn.Pressed += ToggleChat;
        var icon = new ChatIcon();
        icon.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _unread = Ui.Label("", Colors.White, 11);
        _unread.AddThemeStyleboxOverride("normal", Ui.Box("#c83828", "#000000", 0, 2));
        _unread.HorizontalAlignment = HorizontalAlignment.Center;
        _unread.Position = new Vector2(32, -8);
        _unread.Visible = false;
        h.AddChild(chatBtn.With(icon, _unread));
        bar.AddChild(h);

        // Buffs float in the top-left corner and the target's health at the top centre, both clear of the map.
        _buffs.Position = new Vector2(12, 10);
        _buffs.AddThemeConstantOverride("outline_size", 5);
        _buffs.AddThemeColorOverride("font_outline_color", Colors.Black);
        _safe.AddChild(_buffs);
        _target.AnchorLeft = 0.5f; _target.AnchorRight = 0.5f;
        _target.OffsetLeft = -130; _target.OffsetRight = 130; _target.OffsetTop = 12;
        _safe.AddChild(_target);
    }

    // The hotbar and the window menu, centred just above the status bar.
    private void BuildHotbar()
    {
        var panel = new PanelContainer
        {
            AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 1, AnchorBottom = 1,
            OffsetTop = -(BarH + HotH - 4), OffsetBottom = -(BarH + 4), GrowHorizontal = GrowDirection.Both,
        };
        panel.AddThemeStyleboxOverride("panel", Ui.Box("#1e2336d8", "#6a5530", 2, 4));
        var hotbar = Ui.HBox(5);
        hotbar.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        _hotbar = hotbar;
        var menu = Menu(3);
        menu.CustomMinimumSize = new Vector2(250, 0);
        panel.AddChild(Ui.HBox(12).With(hotbar, menu));
        _safe.AddChild(panel);

        // While the chat is shut, new lines show here for a few seconds and fade.
        _feed = new VBoxContainer
        {
            AnchorTop = 1, AnchorBottom = 1, OffsetLeft = 12, OffsetTop = -(BarH + HotH + 8), OffsetBottom = -(BarH + HotH + 8),
            GrowVertical = GrowDirection.Begin, MouseFilter = MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(460, 0),
        };
        _feed.AddThemeConstantOverride("separation", 1);
        _safe.AddChild(_feed);
    }

    // The zone map, a see-through pop-out in the top-right corner that turns solid under the mouse.
    private void BuildMap()
    {
        _mapPanel = new PanelContainer { AnchorLeft = 1, AnchorRight = 1, OffsetLeft = -(MapW + 12), OffsetRight = -12, OffsetTop = 12 };
        _mapPanel.AddThemeStyleboxOverride("panel", Ui.Box("#1e233690", "#6a5530", 2, 6));
        _zone.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _zone.ClipText = true;
        _mapBtn = new Button { Text = "–", Flat = true, FocusMode = FocusModeEnum.None, TooltipText = "Hide the map (N)" };
        _mapBtn.Pressed += ToggleMap;
        _minimap.CustomMinimumSize = new Vector2(0, 170);
        _mapBody = Ui.VBox(3).With(_minimap, _coords);
        _mapPanel.AddChild(Ui.VBox(3).With(Ui.HBox(4).With(_zone, _mapBtn), _mapBody));
        _safe.AddChild(_mapPanel);
    }

    // The chat panel opens down the right-hand side, under the map.
    private void BuildChat()
    {
        _chatPanel = new PanelContainer
        {
            AnchorLeft = 1, AnchorRight = 1, AnchorBottom = 1, OffsetLeft = -(ChatW + 12), OffsetRight = -12,
            OffsetBottom = -(BarH + HotH), Visible = false,
        };
        _chatPanel.AddThemeStyleboxOverride("panel", Ui.Box("#151827e8", "#6a5530", 2, 6));
        var title = Ui.Label("Chat", Ui.GoldHi, 14);
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        var close = new Button { Text = "×", Flat = true, FocusMode = FocusModeEnum.None, TooltipText = "Hide the chat" };
        close.Pressed += ToggleChat;
        _chatPanel.AddChild(Ui.VBox(4).With(Ui.HBox().With(title, close), _chat, ChatInput));
        _safe.AddChild(_chatPanel);
    }

    public void ToggleChat() => SetChat(!ChatOpen);

    // Enter: open the chat if it's shut and start typing.
    public void FocusChat()
    {
        SetChat(true);
        ChatInput.CallDeferred(Control.MethodName.GrabFocus);
    }

    private void SetChat(bool open)
    {
        if (Ui.Touch || open == ChatOpen) return; // phones always show their chat
        ChatOpen = _chatPanel.Visible = open;
        if (open)
        {
            _unreadCount = 0;
            _unread.Visible = false;
            foreach (var c in _feed.GetChildren()) c.QueueFree();
            _chat.CallDeferred(RichTextLabel.MethodName.ScrollToLine, Math.Max(0, _chat.GetLineCount() - 1));
        }
        else ChatInput.ReleaseFocus();
        Audio.Play("ui");
    }

    public void ToggleMap()
    {
        if (Ui.Touch) return;
        _mapBody.Visible = !_mapBody.Visible;
        _mapBtn.Text = _mapBody.Visible ? "–" : "+";
        _mapBtn.TooltipText = _mapBody.Visible ? "Hide the map (N)" : "Show the map (N)";
        _mapPanel.ResetSize();
    }

    // Phones: small panels in the top corners, the on-screen controls along the bottom and the chat between them.
    private void BuildTouch()
    {
        var status = new PanelContainer { CustomMinimumSize = new Vector2(210, 0), OffsetLeft = 4, OffsetTop = 4, OffsetRight = 4, OffsetBottom = 4 };
        status.AddThemeStyleboxOverride("panel", Ui.Box("#1e2336d8", "#6a5530", 2, 6));
        status.AddChild(Ui.VBox(3).With(Ui.HBox(8).With(_name, _class), _buffs, Row("HP", _hp), Row("MP", _mp), Row("EXP", _exp), _gold));
        _safe.AddChild(status);

        _target.AnchorLeft = 0.5f; _target.AnchorRight = 0.5f;
        _target.OffsetLeft = -110; _target.OffsetRight = 110; _target.OffsetTop = 4; _target.OffsetBottom = 4;
        _safe.AddChild(_target);

        var map = new PanelContainer
        {
            CustomMinimumSize = new Vector2(170, 0), AnchorLeft = 1, AnchorRight = 1,
            OffsetLeft = -4, OffsetRight = -4, OffsetTop = 4, OffsetBottom = 4, GrowHorizontal = GrowDirection.Begin,
        };
        map.AddThemeStyleboxOverride("panel", Ui.Box("#1e2336d8", "#6a5530", 2, 6));
        _minimap.CustomMinimumSize = new Vector2(0, 84);
        _coords.Visible = false;
        map.AddChild(Ui.VBox(3).With(_minimap, _zone, _coords, Menu(3)));
        _safe.AddChild(map);

        // Hotbar in two rows of five, just left of the attack button.
        var grid = new GridContainer
        {
            Columns = 5, AnchorLeft = 1, AnchorRight = 1, AnchorTop = 1, AnchorBottom = 1,
            OffsetLeft = -TouchControls.ClusterW, OffsetRight = -TouchControls.ClusterW,
            OffsetTop = -TouchControls.Edge, OffsetBottom = -TouchControls.Edge,
            GrowHorizontal = GrowDirection.Begin, GrowVertical = GrowDirection.Begin,
        };
        grid.AddThemeConstantOverride("h_separation", 5);
        grid.AddThemeConstantOverride("v_separation", 5);
        _hotbar = grid;
        _safe.AddChild(grid);

        const float hotbarW = 5 * 46 + 4 * 5;
        _chatPanel = new PanelContainer
        {
            AnchorTop = 1, AnchorBottom = 1, AnchorRight = 1, GrowVertical = GrowDirection.Begin,
            OffsetLeft = TouchControls.StickW + 8, OffsetRight = -(TouchControls.ClusterW + hotbarW + 12),
        };
        _chatPanel.AddThemeStyleboxOverride("panel", Ui.Box("#151827c0", "#6a5530", 1, 4));
        _chat.CustomMinimumSize = new Vector2(0, 62);
        _chatPanel.AddChild(Ui.VBox(2).With(_chat, ChatInput));
        _safe.AddChild(_chatPanel);

        Pad = new TouchControls { Hotbar = _hotbar, Blocked = at => _open.Values.Any(w => w.GetGlobalRect().HasPoint(at)), OnSlot = i => W.UseHotbar(i) };
        _safe.AddChild(Pad);
    }

    // Follow the safe area, and lift the chat above the on-screen keyboard while typing.
    private void FitScreen()
    {
        var root = GetTree().Root;
        var (side, top, bottom) = Ui.SafeMargins(root);
        _safe.OffsetLeft = side; _safe.OffsetRight = -side; _safe.OffsetTop = top; _safe.OffsetBottom = -bottom;
        float lift = Math.Max(TouchControls.Edge, Ui.KeyboardHeight(root) - bottom);
        _chatPanel.OffsetBottom = -lift;
        _chatPanel.OffsetTop = -lift;
    }

    private static Control Row(string label, StatBar bar)
    {
        var h = Ui.HBox(4);
        var l = Ui.Label(label, Ui.Muted, 13);
        l.CustomMinimumSize = new Vector2(30, 0);
        bar.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        return h.With(l, bar);
    }

    // ---- events -------------------------------------------------------------------------
    public void OnEvent(GameEvent e)
    {
        switch (e.Type)
        {
            case EvType.Msg:
                if (e.Channel == "warn")
                {
                    ulong now = Time.GetTicksMsec();
                    if (e.Text == _lastWarn && now - _lastWarnAt < 1200) return;
                    _lastWarn = e.Text; _lastWarnAt = now;
                }
                Chat(e.Text, e.Channel, e.From);
                break;
            case EvType.Dialog: OpenDialog(e.Id); break;
            case EvType.Map:
                _minimap.Rebuild(W.Map);
                foreach (var id in new[] { "dialog", "shop", "storage" }) Close(id);
                if (!e.Initial) { _banner.Text = $"{W.Map.Name}\n{W.Map.Levels}"; _bannerT = 3; }
                break;
        }
    }

    private static readonly Dictionary<string, string> ChannelColors = new()
    {
        ["system"] = "#d8d4c4", ["info"] = "#9a94a8", ["local"] = "#f4f0e0", ["shout"] = "#ffb050", ["whisper"] = "#f0a0e0",
        ["loot"] = "#a8e0a0", ["quest"] = "#f4d870", ["level"] = "#ffe080", ["warn"] = "#f07060", ["boss"] = "#ff7060",
    };

    public void Chat(string text, string channel = "system", string from = "")
    {
        static string Esc(string s) => s.Replace("[", "[lb]");
        var col = ChannelColors.GetValueOrDefault(channel, "#d8d4c4");
        string prefix = channel switch { "shout" => "[lb]Shout] ", "whisper" => "[lb]Whisper] ", _ => "" };
        string who = from != "" ? $"[color=#ffffff]{prefix}{Esc(from)}:[/color] " : prefix;
        string line = $"[color={col}]{who}{Esc(text)}[/color]";
        _chat.AppendText(line + "\n");
        if (Ui.Touch || ChatOpen) return;
        _unreadCount++;
        _unread.Text = _unreadCount > 99 ? "99+" : _unreadCount.ToString();
        _unread.Visible = true;
        var shown = new RichTextLabel { BbcodeEnabled = true, FitContent = true, ScrollActive = false, MouseFilter = MouseFilterEnum.Ignore, Text = line };
        shown.AddThemeFontSizeOverride("normal_font_size", 14);
        shown.AddThemeConstantOverride("outline_size", 4);
        shown.AddThemeColorOverride("font_outline_color", Colors.Black);
        shown.SetMeta("at", Time.GetTicksMsec());
        _feed.AddChild(shown);
        while (_feed.GetChildCount() > 5) _feed.GetChild(0).Free();
    }

    // Lines in the feed stay six seconds, then fade over one.
    private void FadeFeed()
    {
        ulong now = Time.GetTicksMsec();
        foreach (var c in _feed.GetChildren())
        {
            if (c is not RichTextLabel l) continue;
            float age = (now - l.GetMeta("at").AsUInt64()) / 1000f;
            if (age > 7) l.QueueFree();
            else l.Modulate = new Color(1, 1, 1, Mathf.Clamp(7 - age, 0, 1));
        }
    }

    // ---- per frame ----------------------------------------------------------------------
    public void Tick(double dt)
    {
        var p = W.P; var pd = W.Pd;
        _name.Text = p.Name;
        var cls = Content.Classes[p.Cls ?? "none"];
        _class.Text = $"Lv {p.Level} {cls.Name}{(p.Ghost ? " · Ghost" : "")}";
        _class.AddThemeColorOverride("font_color", p.Ghost ? Assets.C("#b8c8ff") : Assets.C(cls.Color));
        _buffs.Text = p.Ghost ? "Ghost" : string.Join(" · ", p.Buffs.Keys.Select(k => World.BuffLabels.GetValueOrDefault(k, k)));
        _hp.Set(p.Hp, pd.MaxHp, $"{Math.Max(0, (int)p.Hp)} / {pd.MaxHp}");
        _mp.Set(p.Mp, pd.MaxMp, $"{(int)p.Mp} / {pd.MaxMp}");
        int need = Rules.ExpToNext(p.Level);
        _exp.Set(p.Level >= Rules.MaxLevel ? need : p.Exp, need, p.Level >= Rules.MaxLevel ? "MAX" : $"{p.Exp * 100 / need}%");
        _gold.Text = $"● {p.Gold:N0} gold";
        _zone.Text = W.Map.Name;
        _coords.Text = $"{W.Map.Levels} · {p.X}, {p.Y}";
        var stats = Ui.Touch ? "Stats" : "[C] Stats";
        _statsBtn.Text = p.Unspent > 0 ? $"{stats} +{p.Unspent}" : stats;
        _statsBtn.AddThemeColorOverride("font_color", p.Unspent > 0 ? Ui.GoldHi : Ui.Ink);
        if (Ui.Touch) { _buffs.Visible = _buffs.Text != ""; FitScreen(); }
        else
        {
            bool overMap = _mapPanel.GetGlobalRect().HasPoint(GetGlobalMousePosition());
            _mapPanel.Modulate = new Color(1, 1, 1, overMap ? 1 : 0.72f);
            _chatPanel.OffsetTop = _mapPanel.Position.Y + _mapPanel.Size.Y + 8;
            FadeFeed();
        }

        var t = W.GetTarget();
        _target.Visible = t != null;
        if (t != null)
        {
            _tName.Text = t.D.Name;
            _tLevel.Text = $"Lv {t.D.Level}{(t.D.Boss ? " · Boss" : "")}{(t.D.Aggressive ? "" : " · Passive")}";
            _tBar.Set(t.Hp, t.MaxHp, $"{Math.Ceiling(t.Hp)} / {t.MaxHp}");
            _tStatus.Text = string.Join(", ", t.Status.Keys.Select(k => k switch { "poison" => "Poisoned", "slow" => "Slowed", _ => "Paralyzed" }));
        }
        UpdateHotbar();
        _minimap.W = W;
        _minimap.QueueRedraw();

        if (_bannerT > 0)
        {
            _bannerT -= dt;
            _banner.Modulate = new Color(1, 1, 1, (float)Math.Clamp(Math.Min(_bannerT, 3 - _bannerT) * 2, 0, 1));
            _banner.Size = new Vector2(_windows.Size.X, 80);
            _banner.Position = new Vector2(0, Ui.Touch ? 50 : 110); // below the target box
        }
        else _banner.Modulate = Colors.Transparent;

        _refreshT -= dt;
        if (_refreshT > 0) return;
        _refreshT = 0.15;
        foreach (var win in _open.Values.ToList()) Refresh(win);
        if (_dialogNpc != null)
        {
            var n = W.Npcs.Find(x => x.Id == _dialogNpc);
            if (n == null || n.Manhattan(p) > 4) foreach (var id in new[] { "dialog", "shop", "storage" }) Close(id);
        }
    }

    private void UpdateHotbar()
    {
        var p = W.P;
        var slots = W.Hotbar();
        var sig = string.Join(",", slots) + string.Join(",", p.Inv.Select(s => s == null ? "" : $"{s.Id}{s.Qty}"));
        if (sig != _hotbarSig)
        {
            _hotbarSig = sig;
            foreach (var c in _hotbar.GetChildren()) c.QueueFree();
            for (int i = 0; i < slots.Length; i++)
            {
                int idx = i;
                var s = slots[i];
                var b = new Button { FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(46, 46), ClipContents = true };
                b.SetMeta("slot", idx);
                if (!Ui.Touch) b.Pressed += () => W.UseHotbar(idx); // on touch screens Pad presses it, from any finger
                if (s != null && s.StartsWith("potion:"))
                {
                    var which = s[7..];
                    var best = W.BestPotion(which);
                    b.Icon = Assets.Icon(best ?? (which == "hp" ? "herbal_tonic" : "spirit_tea"));
                    b.ExpandIcon = true;
                    b.IconAlignment = HorizontalAlignment.Center;
                    if (best == null) b.Modulate = new Color(1, 1, 1, 0.45f);
                    b.TooltipText = which == "hp" ? "Drink your best healing tonic" : "Drink your best mana tea";
                    var count = Ui.Label(best == null ? "0" : W.CountItem(best).ToString(), Colors.White, 13);
                    count.SetAnchorsPreset(LayoutPreset.BottomRight);
                    count.Position = new Vector2(30, 28);
                    b.AddChild(count);
                }
                else if (s != null)
                {
                    var sk = Content.Skills[s];
                    b.AddThemeStyleboxOverride("normal", Ui.Box(sk.Color, "#1a1410", 2, 0));
                    b.AddThemeStyleboxOverride("hover", Ui.Box(sk.Color, "#f4d88a", 2, 0));
                    b.Icon = Assets.SkillIcon(sk.Id);
                    b.IconAlignment = HorizontalAlignment.Center;
                    b.VerticalIconAlignment = VerticalAlignment.Center;
                    b.TooltipText = SkillTip(sk);
                }
                else b.Disabled = true;
                var cd = new ColorRect { Color = new Color(0, 0, 0, 0.62f), Name = "cd", MouseFilter = MouseFilterEnum.Ignore };
                b.AddChild(cd);
                if (!Ui.Touch)
                {
                    var key = Ui.Label(((i + 1) % 10).ToString(), Ui.GoldHi, 12);
                    key.Position = new Vector2(3, 0);
                    key.AddThemeConstantOverride("outline_size", 3);
                    key.AddThemeColorOverride("font_outline_color", Colors.Black);
                    b.AddChild(key);
                }
                _hotbar.AddChild(b);
            }
        }
        for (int i = 0; i < slots.Length && i < _hotbar.GetChildCount(); i++)
        {
            if (_hotbar.GetChild(i) is not Button b || b.IsQueuedForDeletion()) continue;
            var cd = b.GetNode<ColorRect>("cd");
            double frac = 0;
            var s = slots[i];
            if (s != null && s.StartsWith("potion:")) frac = p.PotCd;
            else if (s != null)
            {
                var sk = Content.Skills[s];
                frac = p.Cds.GetValueOrDefault(s) / sk.Cd;
                bool usable = sk.Type == SkillType.Ghost ? p.Ghost : !p.Ghost && p.Mp >= sk.Mp;
                b.Modulate = usable ? Colors.White : new Color(0.55f, 0.55f, 0.6f);
            }
            float h = 46 * (float)Math.Clamp(frac, 0, 1);
            cd.Position = new Vector2(0, 46 - h);
            cd.Size = new Vector2(46, h);
        }
    }

    private static string SkillTip(SkillDef s) =>
        $"{s.Name}\n{Content.Classes[s.Cls].Name} · Level {s.Level}\n{s.Mp} MP · {s.Cd}s cooldown{(s.Range > 0 ? $" · range {s.Range}" : "")}\n{s.Desc}";

    public static string ItemTip(ItemDef it, Player p, bool buying)
    {
        var lines = new List<string> { it.Name };
        var stats = new List<string>();
        void S(int v, string n) { if (v != 0) stats.Add($"{n} +{v}"); }
        S(it.Atk, "Attack"); S(it.Matk, "Magic"); S(it.Def, "Defense"); S(it.Hp, "Max HP"); S(it.Mp, "Max MP");
        S(it.Str, "STR"); S(it.Dex, "DEX"); S(it.Int, "INT"); S(it.Wis, "WIS"); S(it.Con, "CON");
        if (it.Crit > 0) stats.Add($"Critical +{it.Crit * 100:0}%");
        if (it.Heal > 0) stats.Add($"Restores {it.Heal} HP");
        if (it.Mana > 0) stats.Add($"Restores {it.Mana} MP");
        lines.Add(it.Type switch { ItemType.Use => "Consumable", ItemType.Loot => "Trade good", _ => it.Type.ToString() });
        if (stats.Count > 0) lines.Add(string.Join(" · ", stats));
        if (it.Level > 0) lines.Add($"Requires level {it.Level}{(p.Level < it.Level ? "  (too low)" : "")}");
        if (it.Classes != null) lines.Add($"{string.Join(", ", it.Classes.Select(c => Content.Classes[c].Name))} only");
        if (it.Desc != "") lines.Add(it.Desc);
        lines.Add(buying ? $"Price: {it.Price} gold" : $"Sells for {Rules.SellPrice(it)} gold");
        return string.Join("\n", lines);
    }

    // ---- windows ------------------------------------------------------------------------
    public void ToggleMusic()
    {
        bool on = Audio.ToggleMusic();
        _musicBtn.Modulate = on ? Colors.White : new Color(1, 1, 1, 0.45f);
        Chat(on ? "Music on." : "Music off.", "info");
    }

    public void Toggle(string id)
    {
        if (_open.ContainsKey(id)) { Close(id); return; }
        switch (id)
        {
            case "inventory": Open(id, "Inventory", RenderInventory, right: true); StatsBesideBag(); break;
            case "stats": Open(id, "Character", RenderStats); break;
            case "skills": Open(id, "Skills", RenderSkills); break;
            case "quests": Open(id, "Quest Log", RenderQuests); break;
            case "help": Open(id, "How to Play", RenderHelp, width: 470); break;
        }
    }

    // Opening the bag also opens the character sheet just to its left, unless trading needs that room.
    private void StatsBesideBag()
    {
        var bag = _open["inventory"];
        float x = bag.Position.X - WinW - 8;
        if (x < 12 || _shopId != null || _open.ContainsKey("storage")) return;
        if (!_open.ContainsKey("stats")) { Open("stats", "Character", RenderStats); _statsWithBag = true; }
        _open["stats"].Position = new Vector2(x, bag.Position.Y);
    }

    private void Open(string id, string title, Action<VBoxContainer> render, bool right = false, int width = WinW, float top = 12)
    {
        Close(id);
        var win = new GameWindow(title, () => Close(id)) { Render = render };
        win.CustomMinimumSize = new Vector2(width, 0);
        _windows.AddChild(win);
        if (Ui.Touch) top = 4; // short screens need all the height they have
        float edge = _windows.Size.X - (ChatOpen ? ChatW + 12 : 0); // keep clear of an open chat
        win.Position = new Vector2(right ? Math.Max(12, edge - width - 12) : 12, top);
        win.ZIndex = 0;
        win.GuiInput += e => { if (e is InputEventMouseButton { Pressed: true }) win.MoveToFront(); };
        _open[id] = win;
        Refresh(win, true);
        Audio.Play("ui");
    }

    public void Close(string id)
    {
        if (!_open.Remove(id, out var win)) return;
        win.QueueFree();
        if (id == "dialog") _dialogNpc = null;
        if (id == "shop") _shopId = null;
        if (id == "stats") _statsWithBag = false;
        if (id == "inventory" && _statsWithBag) Close("stats");
    }

    // Esc: shut the top window, or failing that the chat.
    public bool CloseTop()
    {
        var top = _open.Values.OrderByDescending(w => w.GetIndex()).FirstOrDefault();
        if (top == null)
        {
            if (!ChatOpen) return false;
            SetChat(false);
            return true;
        }
        Close(_open.First(kv => kv.Value == top).Key);
        return true;
    }

    private string StateSig()
    {
        var p = W.P;
        return string.Join("|", p.Inv.Select(s => s == null ? "" : $"{s.Id}{s.Qty}")) + string.Join(",", p.Equip.Values) + p.Gold + p.Level +
               p.Stats.Str + p.Stats.Dex + p.Stats.Int + p.Stats.Wis + p.Stats.Con + p.Unspent + p.Cls + p.Ghost +
               string.Join(",", p.Quests.Select(q => $"{q.Key}{q.Value.State}{q.Value.Count}")) + string.Join(",", p.Storage.Select(s => s?.Id + s?.Qty)) +
               string.Join(",", p.Buffs.Keys) + W.Pd.MaxHp + string.Join(",", p.Bar);
    }

    private void Refresh(GameWindow win, bool force = false)
    {
        var sig = StateSig();
        if (!force && sig == win.Sig) return;
        win.Sig = sig;
        foreach (var c in win.Body.GetChildren()) { win.Body.RemoveChild(c); c.QueueFree(); }
        win.Render(win.Body);
        // Shrink to the new contents once the wrapped labels have been laid out.
        win.FitBody();
        win.Size = Vector2.Zero;
        win.CallDeferred(Control.MethodName.ResetSize);
    }

    // onHold runs on a long press, which stands in for right-click and Shift on touch screens.
    private Button Slot(ItemStack? s, string? label, Action<bool, bool>? onClick, bool buying = false, Action? onHold = null)
    {
        var b = new Button { FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(44, 44) };
        b.AddThemeStyleboxOverride("normal", Ui.Box("#0c0e16", "#3a3450", 2, 2));
        b.AddThemeStyleboxOverride("hover", Ui.Box("#141826", "#d8b25a", 2, 2));
        if (s != null)
        {
            b.Icon = Assets.Icon(s.Id);
            b.ExpandIcon = true;
            b.IconAlignment = HorizontalAlignment.Center;
            b.TooltipText = ItemTip(Content.Items[s.Id], W.P, buying);
            if (s.Qty > 1)
            {
                var q = Ui.Label(s.Qty.ToString(), Colors.White, 13);
                q.Position = new Vector2(26, 24);
                q.AddThemeConstantOverride("outline_size", 3);
                q.AddThemeColorOverride("font_outline_color", Colors.Black);
                b.AddChild(q);
            }
        }
        else if (label != null) { b.Text = label; b.AddThemeFontSizeOverride("font_size", 10); b.AddThemeColorOverride("font_color", Assets.C("#5a5470")); }
        ulong downAt = 0;
        if (onClick != null)
            b.GuiInput += e =>
            {
                if (e is not InputEventMouseButton mb || mb.ButtonIndex is not (MouseButton.Left or MouseButton.Right)) return;
                if (Ui.Touch)
                {
                    // Act on release: a quick tap clicks, holding for half a second opens the item's details.
                    if (mb.Pressed) downAt = Time.GetTicksMsec();
                    else if (downAt > 0)
                    {
                        if (Time.GetTicksMsec() - downAt >= 450 && onHold != null) onHold(); else onClick(false, false);
                        downAt = 0;
                    }
                    b.AcceptEvent();
                    return;
                }
                if (!mb.Pressed) return;
                onClick(mb.ButtonIndex == MouseButton.Right, mb.ShiftPressed);
                b.AcceptEvent();
            };
        return b;
    }

    // Touch screens have no hover, right button or Shift, so holding an item shows its details and what you can do with it.
    private void ShowItem(ItemStack s, bool buying, List<(string Label, Action Do)> actions, Action? drop = null)
    {
        var it = Content.Items[s.Id];
        bool sure = false;
        Open("item", it.Name, body =>
        {
            body.AddChild(Ui.Label(ItemTip(it, W.P, buying), null, 14, true));
            for (int i = 0; i < actions.Count; i++)
            {
                var act = actions[i].Do;
                body.AddChild(Ui.Button(actions[i].Label, () => { act(); Close("item"); }, i == 0));
            }
            if (drop != null)
            {
                var b = Ui.Button(sure ? "Tap again to throw it away" : "Throw away", () =>
                {
                    if (!sure) { sure = true; if (_open.TryGetValue("item", out var w)) Refresh(w, true); return; }
                    drop();
                    Close("item");
                });
                b.AddThemeColorOverride("font_color", Ui.Bad);
                body.AddChild(b);
            }
            body.AddChild(Ui.Button("Close", () => Close("item")));
        }, width: 300);
    }

    private void ShowBagItem(int idx)
    {
        if (W.P.Inv[idx] is not { } s) return;
        var it = Content.Items[s.Id];
        var actions = new List<(string Label, Action Do)>();
        if (_shopId != null)
        {
            actions.Add(("Sell one", () => W.Sell(idx, false)));
            if (s.Qty > 1) actions.Add(($"Sell all {s.Qty}", () => W.Sell(idx, true)));
        }
        else if (_open.ContainsKey("storage")) actions.Add(("Put in storage", () => W.Deposit(idx)));
        if (it.IsGear) actions.Add(("Equip", () => W.UseItem(idx)));
        else if (it.Type == ItemType.Use) actions.Add(("Use", () => W.UseItem(idx)));
        ShowItem(s, false, actions, () => W.DropItem(idx));
    }

    private void RenderInventory(VBoxContainer body)
    {
        var p = W.P;
        Button Eq(string slot)
        {
            var worn = p.Equip[slot] is { } id ? new ItemStack(id) : null;
            return Slot(worn, slot switch { "ring1" or "ring2" => "Ring", _ => char.ToUpper(slot[0]) + slot[1..] }, (_, _) => W.Unequip(slot),
                onHold: worn == null ? null : () => ShowItem(worn, false, new() { ("Take off", () => W.Unequip(slot)) }));
        }
        var doll = new DollBox(2);
        doll.Doll.SetLook(p.Look);
        var dollRow = Ui.HBox(6);
        dollRow.Alignment = BoxContainer.AlignmentMode.Center;
        dollRow.With(Ui.VBox(4).With(Eq("helmet"), Eq("weapon"), Eq("ring1")), doll,
            Ui.VBox(4).With(Eq("necklace"), Eq("shield"), Eq("ring2")), Ui.VBox(4).With(Eq("armor")));
        var bag = new GridContainer { Columns = 6 };
        bag.AddThemeConstantOverride("h_separation", 4);
        bag.AddThemeConstantOverride("v_separation", 4);
        for (int i = 0; i < p.Inv.Length; i++)
        {
            int idx = i;
            bag.AddChild(Slot(p.Inv[i], null, (right, shift) =>
            {
                if (p.Inv[idx] == null) return;
                if (!right) { if (_open.ContainsKey("storage")) W.Deposit(idx); else W.UseItem(idx); }
                else if (_shopId != null) W.Sell(idx, shift);
                else if (shift) W.DropItem(idx);
            }, onHold: () => ShowBagItem(idx)));
        }
        string hint = Ui.Touch
            ? _shopId != null ? "Hold an item to sell it."
              : _open.ContainsKey("storage") ? "Tap an item to put it in storage. Hold it for details."
              : "Tap to use or equip. Tap a worn item to take it off. Hold an item for details or to throw it away."
            : _shopId != null ? "Right-click to sell one, Shift + right-click to sell the stack."
            : _open.ContainsKey("storage") ? "Click an item to put it in storage."
            : "Click to use or equip. Click a worn item to take it off. Shift + right-click throws an item away.";
        body.With(dollRow, bag, Ui.Label($"● {p.Gold:N0} gold", Ui.GoldHi), Ui.Label(hint, Ui.Muted, 13, true));
    }

    private void RenderStats(VBoxContainer body)
    {
        var p = W.P; var pd = W.Pd;
        var cls = Content.Classes[p.Cls ?? "none"];
        body.With(Ui.Label(p.Name, Ui.GoldHi, 18), Ui.Label($"Level {p.Level} {cls.Name}", Assets.C(cls.Color)),
            Ui.Label(cls.Recommend != "" ? $"Recommended: {cls.Recommend}" : cls.Desc, Ui.Muted, 13, true));
        foreach (var k in Rules.StatKeys)
        {
            int bonus = pd.Stats[k] - p.Stats[k];
            var row = Ui.HBox(6);
            var name = Ui.Label(Content.StatNames[k]);
            name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            var plus = Ui.Button("+", () => W.AllocStat(k), disabled: p.Unspent <= 0);
            plus.CustomMinimumSize = new Vector2(28, 0);
            body.AddChild(row.With(name, Ui.Label(pd.Stats[k] + (bonus > 0 ? $" (+{bonus})" : ""), bonus > 0 ? Ui.Good : Colors.White), plus));
        }
        body.AddChild(Ui.Label($"Unspent points: {p.Unspent}", p.Unspent > 0 ? Ui.GoldHi : Ui.Muted));
        var grid = new GridContainer { Columns = 2 };
        grid.AddThemeConstantOverride("h_separation", 16);
        foreach (var (k, v) in new[]
        {
            ("HP", $"{(int)p.Hp} / {pd.MaxHp}"), ("MP", $"{(int)p.Mp} / {pd.MaxMp}"), ("Attack", pd.Atk.ToString()), ("Magic", pd.Matk.ToString()),
            ("Defense", pd.Def.ToString()), ("Critical", $"{pd.Crit * 100:0}%"), ("Evasion", $"{pd.Evade * 100:0}%"), ("Attack speed", $"{pd.AtkDelay:0.00}s"),
            ("EXP", p.Level >= Rules.MaxLevel ? "MAX" : $"{p.Exp} / {Rules.ExpToNext(p.Level)}"),
        })
            grid.With(Ui.Label(k, Ui.Muted, 14), Ui.Label(v, Colors.White, 14));
        body.AddChild(grid);
    }

    private void RenderSkills(VBoxContainer body)
    {
        var p = W.P;
        if (p.Cls == null)
        {
            body.AddChild(Ui.Label("You have no class skills yet. At level 5, visit the job masters standing in front of the hall south of the plaza and choose a path:", null, 15, true));
            foreach (var c in Content.Classes.Values.Where(c => c.Id != "none" && c.Base == null))
                body.With(Ui.Label($"{c.Name} · {c.Role}", Assets.C(c.Color)), Ui.Label(c.Desc, Ui.Muted, 13, true));
            return;
        }
        var cls = Content.Classes[p.Cls];
        var adv = cls.Base == null ? Content.AdvancedOf(p.Cls) : null;
        var line = Content.ClassLine(p.Cls).Append(adv?.Id).ToArray();
        var learned = W.LearnedSkills();
        body.AddChild(Ui.Label("Choose which number key each skill sits on. Keys 9 and 0 drink your best tonic and tea.", Ui.Muted, 13, true));
        foreach (var s in Content.Skills.Values.Where(s => line.Contains(s.Cls)).OrderBy(s => s.Level))
        {
            bool has = learned.Contains(s.Id);
            var icon = new TextureRect { Texture = Assets.SkillIcon(s.Id), StretchMode = TextureRect.StretchModeEnum.KeepCentered };
            icon.CustomMinimumSize = new Vector2(Assets.SkillIconSize, Assets.SkillIconSize);
            var bg = new PanelContainer { SizeFlagsVertical = SizeFlags.ShrinkBegin };
            bg.AddThemeStyleboxOverride("panel", Ui.Box(s.Color, "#1a1410", 2, 0));
            bg.AddChild(icon);
            string where = has ? "" : s.Cls == adv?.Id ? $"  · {adv.Name} at level {s.Level}" : $"  · learn at level {s.Level}";
            var info = Ui.VBox(1).With(Ui.Label(s.Name + where, has ? Colors.White : Ui.Muted),
                Ui.Label($"{s.Mp} MP · {s.Cd}s · {s.Desc}", Ui.Muted, 13, true));
            info.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            var row = Ui.HBox(8).With(bg, info);
            if (has)
            {
                // A key picker: "-" takes the skill off the bar; picking a used key swaps the two skills.
                var pick = new OptionButton { FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(52, 0), TooltipText = "Hotbar key" };
                pick.AddItem("–");
                for (int k = 1; k <= Rules.SkillSlots; k++) pick.AddItem(k.ToString());
                pick.Selected = Array.IndexOf(p.Bar, s.Id) + 1;
                string id = s.Id;
                pick.ItemSelected += i => W.AssignSkill(id, (int)i - 1);
                pick.SizeFlagsVertical = SizeFlags.ShrinkCenter;
                row.AddChild(pick);
            }
            row.Modulate = has ? Colors.White : new Color(1, 1, 1, 0.55f);
            row.TooltipText = SkillTip(s);
            body.AddChild(row);
        }
    }

    private void RenderQuests(VBoxContainer body)
    {
        var p = W.P;
        if (p.Quests.Count == 0)
            body.AddChild(Ui.Label("No quests yet. Townsfolk with a ! over their head have work for you. Start with Elder Baek in the plaza.", Ui.Muted, 15, true));
        // Story first, then side quests; finished ones collapse into a count at the bottom.
        var open = p.Quests.Where(q => q.Value.State != "done").Select(q => Content.Quests[q.Key]).OrderByDescending(q => q.Story).ThenBy(q => q.Level);
        foreach (var q in open)
        {
            var st = W.QuestState(q.Id);
            var who = Content.Npcs[q.Ender];
            var box = Ui.VBox(2).With(Ui.Label($"{(q.Story ? "★ " : "")}{q.Title}  · {Content.Npcs[q.Giver].Name}", q.Story ? Ui.GoldHi : Ui.Ink),
                Ui.Label(q.Text, Ui.Muted, 13, true),
                Ui.Label(st == "ready" ? $"Ready to turn in to {who.Name} ({W.MapName(who.Map)})" : W.QuestProgress(q.Id), st == "ready" ? Ui.Good : Ui.GoldHi, 14, true));
            body.AddChild(box);
        }
        var next = Content.Quests.Values.Where(q => q.Story && W.QuestState(q.Id) is "available" or "locked").OrderBy(q => q.Level).FirstOrDefault();
        if (next != null && !p.Quests.Any(q => Content.Quests[q.Key].Story && q.Value.State != "done"))
        {
            var giver = Content.Npcs[next.Giver];
            body.AddChild(Ui.Label(W.QuestState(next.Id) == "available"
                ? $"★ The story continues with {giver.Name} in {W.MapName(giver.Map)}."
                : $"★ The story continues at level {next.Level}, with {giver.Name} in {W.MapName(giver.Map)}.", Ui.GoldHi, 13, true));
        }
        int done = p.Quests.Count(q => q.Value.State == "done");
        if (done > 0) body.AddChild(Ui.Label($"Completed: {done} of {Content.Quests.Count} quests", Ui.Muted, 13));
    }

    private void RenderHelp(VBoxContainer body)
    {
        var grid = new GridContainer { Columns = 2 };
        grid.AddThemeConstantOverride("h_separation", 14);
        grid.AddThemeConstantOverride("v_separation", 5);
        foreach (var (k, v) in Ui.Touch ? new[]
        {
            ("Stick", "Drag in the lower-left corner to walk. Walk into a monster to attack it, into a person to talk."),
            ("攻", "Attack the tile you face (hold to keep swinging). Talks to a person in front of you."),
            ("◎ / tap", "Select the next target, or tap a monster. Spells need a target in range."),
            ("Hotbar", "Class skills (from level 5), then your best healing tonic and mana tea. Set skill keys in the Skills window."),
            ("Menu", "Bag · Stats · Skills · Quests · Help · Music, top right."),
            ("Hold an item", "See its details, sell it or throw it away."),
            ("Chat box", "Tap to chat. /s shout, /w name message to whisper, /who"),
        } : new[]
        {
            ("Arrows / WASD", "Move one tile at a time. Walk into a monster to attack it, into a person to talk."),
            ("Space", "Attack the tile you face (hold to keep swinging). Talks to a person in front of you."),
            ("Tab / click", "Select a target. Spells need a target in range. Esc clears it."),
            ("1 – 8", "Class skills, unlocked from level 5. Pick which key each one sits on in the Skills window (K)."),
            ("9 / 0", "Drink your best healing tonic / mana tea."),
            ("I · C · K · Q", "Inventory (with your stats beside it) · Character stats · Skills · Quest log"),
            ("Enter", "Open the chat (or click the speech bubble, bottom right). /s shout, /w name message to whisper, /who"),
            ("M · N · H", "Toggle music · Hide or show the map · This help"),
        })
        {
            var desc = Ui.Label(v, null, 14, true);
            desc.CustomMinimumSize = new Vector2(300, 0);
            grid.With(Ui.Label(k, Ui.GoldHi, 14), desc);
        }
        body.With(grid, Ui.Label("Start by talking to Elder Baek in the plaza, then head out the south gate. At level 5 choose a class from the job masters; at 50, pass their trial to advance. Quests marked ★ follow the main story, all the way to level 100. If you die you become a ghost: walk to a shrine's golden circle to live again. Progress saves automatically.", Ui.Muted, 14, true));
    }

    // ---- NPC windows ----------------------------------------------------------------------
    private void OpenDialog(string npcId)
    {
        _dialogNpc = npcId;
        Open("dialog", Content.Npcs[npcId].Name, b => RenderDialog(b, npcId), width: 360);
    }

    private string? _confirmClass;

    private void RenderDialog(VBoxContainer body, string npcId)
    {
        var p = W.P;
        var npc = Content.Npcs[npcId];
        var portrait = new DollBox(2);
        portrait.Doll.SetLook(npc.Look);
        var greet = Ui.Label(npc.Greet, null, 15, true);
        body.AddChild(Ui.HBox(10).With(portrait, greet));
        foreach (var q in W.QuestsAt(npcId))
        {
            var st = W.QuestState(q.Id);
            bool here = q.Ender == npcId;
            if (st == "available")
            {
                var reward = $"Reward: {q.RewardExp} EXP, {q.RewardGold} gold" + string.Concat(q.RewardItems.Select(r => $", {Content.Items[r.Item].Name}{(r.Qty > 1 ? $" x{r.Qty}" : "")}"));
                body.With(Ui.Label($"{(q.Story ? "★" : "!")} {q.Title}", Ui.GoldHi), Ui.Label(q.Text, null, 14, true), Ui.Label(reward, Ui.Muted, 13, true),
                    Ui.Button("Accept quest", () => W.AcceptQuest(q.Id), true));
            }
            else if (st == "ready" && here) body.AddChild(Ui.Button($"Complete: {q.Title}", () => W.CompleteQuest(q.Id), true));
            else if (st is "active" or "ready") body.AddChild(Ui.Label($"{q.Title} · in progress · {W.QuestProgress(q.Id)}", Ui.Muted, 14, true));
            else if (st == "locked" && p.Level >= q.Level - 2) body.AddChild(Ui.Label($"Come back at level {q.Level} for more work.", Ui.Muted, 14));
        }
        foreach (var svc in npc.Services)
        {
            var parts = svc.Split(':');
            switch (parts[0])
            {
                case "rest": body.AddChild(Ui.Button(W.RestCost > 0 ? $"Rest ({W.RestCost} gold)" : "Rest (free)", W.Rest)); break;
                case "shop": body.AddChild(Ui.Button("Trade", () => OpenShop(parts[1], npc.Name))); break;
                case "storage": body.AddChild(Ui.Button("Open storage", OpenStorage)); break;
                case "job":
                {
                    var c = Content.Classes[parts[1]];
                    if (p.Cls == null)
                    {
                        body.With(Ui.Label($"{c.Name} · {c.Role}", Assets.C(c.Color)), Ui.Label(c.Desc, null, 14, true),
                            Ui.Label($"Grows {c.HpLv} HP and {c.MpLv} MP per level. Recommended stats: {c.Recommend}.", Ui.Muted, 13, true));
                        if (p.Level < 5) body.AddChild(Ui.Button($"Become a {c.Name} (requires level 5)", () => { }, disabled: true));
                        else if (_confirmClass == c.Id)
                            body.With(Ui.Label("This choice is permanent.", Ui.Bad, 14),
                                Ui.Button($"Yes, become a {c.Name}", () => { _confirmClass = null; W.ChooseClass(c.Id); }, true));
                        else body.AddChild(Ui.Button($"Become a {c.Name}", () => { _confirmClass = c.Id; if (_open.TryGetValue("dialog", out var d)) Refresh(d, true); }, true));
                    }
                    else if (p.Cls == c.Id && Content.AdvancedOf(c.Id) is { } adv)
                    {
                        body.AddChild(Ui.Label($"Train well, {c.Name}. New skills come at levels {string.Join(", ", Content.Skills.Values.Where(s => s.Cls == c.Id).Select(s => s.Level))}.", Ui.Muted, 14, true));
                        body.With(Ui.Label($"{adv.Name} · {adv.Role}", Assets.C(adv.Color)), Ui.Label(adv.Desc, null, 14, true),
                            Ui.Label($"Grows {adv.HpLv} HP and {adv.MpLv} MP per level, and learns {string.Join(", ", Content.Skills.Values.Where(s => s.Cls == adv.Id).Select(s => $"{s.Name} ({s.Level})"))}.", Ui.Muted, 13, true));
                        var why = W.AdvanceProblem(adv.Id);
                        if (why != null) body.AddChild(Ui.Button($"Become a {adv.Name} · {why}", () => { }, disabled: true));
                        else if (_confirmClass == adv.Id)
                            body.With(Ui.Label("This choice is permanent.", Ui.Bad, 14),
                                Ui.Button($"Yes, become a {adv.Name}", () => { _confirmClass = null; W.AdvanceClass(adv.Id); }, true));
                        else body.AddChild(Ui.Button($"Become a {adv.Name}", () => { _confirmClass = adv.Id; if (_open.TryGetValue("dialog", out var d)) Refresh(d, true); }, true));
                    }
                    else if (p.Cls != null && Content.ClassLine(p.Cls).Contains(c.Id))
                        body.AddChild(Ui.Label($"A {Content.Classes[p.Cls].Name}! I taught you to hold a weapon, and now look at you.", Ui.Muted, 14, true));
                    break;
                }
            }
        }
        body.AddChild(Ui.Button("Goodbye", () => { foreach (var id in new[] { "dialog", "shop", "storage" }) Close(id); }));
    }

    private void OpenShop(string shopId, string npcName)
    {
        _shopId = shopId;
        Open("shop", $"{npcName} · Shop", b => RenderShop(b, shopId), width: 360, top: 70);
        _shopId = shopId;
        if (!_open.ContainsKey("inventory")) Toggle("inventory"); else Refresh(_open["inventory"], true);
    }

    private void RenderShop(VBoxContainer body, string shopId)
    {
        var p = W.P;
        body.AddChild(Ui.Label(Ui.Touch ? "Tap an item for details. Hold a bag item to sell it." : "Shift-click Buy for 5 consumables. Right-click bag items to sell.", Ui.Muted, 13, true));
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, Ui.Touch ? 190 : 330), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var list = Ui.VBox(2);
        list.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        foreach (var id in Content.Shops[shopId])
        {
            var it = Content.Items[id];
            var problem = it.IsGear ? Rules.EquipProblem(p, it) : (it.Level > 0 && p.Level < it.Level ? "level" : null);
            var icon = new TextureRect { Texture = Assets.Icon(id), CustomMinimumSize = new Vector2(32, 32), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize };
            var name = Ui.Label(it.Name + (it.Level > 0 ? $"  Lv{it.Level}" : ""), problem != null ? Ui.Bad : Ui.Ink, 14);
            name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            var buy = new Button { Text = "Buy", FocusMode = FocusModeEnum.None, Disabled = p.Gold < it.Price };
            buy.GuiInput += e =>
            {
                if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mb && !buy.Disabled)
                {
                    W.Buy(shopId, id, mb.ShiftPressed && it.Stack ? 5 : 1);
                    buy.AcceptEvent();
                }
            };
            // No Shift on a touch screen, so stackable goods get their own button for five.
            var five = Ui.Touch && it.Stack ? Ui.Button("×5", () => W.Buy(shopId, id, 5), disabled: p.Gold < it.Price * 5) : null;
            var row = Ui.HBox(8).With(icon, name, Ui.Label($"{it.Price}g", Ui.GoldHi, 14), buy, five);
            row.TooltipText = ItemTip(it, p, true);
            row.MouseFilter = MouseFilterEnum.Pass;
            if (Ui.Touch)
                row.GuiInput += e =>
                {
                    if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
                    var acts = new List<(string Label, Action Do)> { ("Buy one", () => W.Buy(shopId, id, 1)) };
                    if (it.Stack) acts.Add(("Buy five", () => W.Buy(shopId, id, 5)));
                    ShowItem(new ItemStack(id), true, acts);
                };
            list.AddChild(row);
        }
        scroll.AddChild(list);
        body.AddChild(scroll);
    }

    private void OpenStorage()
    {
        Open("storage", "Storage", RenderStorage, top: 70);
        if (!_open.ContainsKey("inventory")) Toggle("inventory"); else Refresh(_open["inventory"], true);
    }

    private void RenderStorage(VBoxContainer body)
    {
        var grid = new GridContainer { Columns = 6 };
        grid.AddThemeConstantOverride("h_separation", 4);
        grid.AddThemeConstantOverride("v_separation", 4);
        for (int j = 0; j < W.P.Storage.Length; j++)
        {
            int idx = j;
            var s = W.P.Storage[j];
            grid.AddChild(Slot(s, null, (_, _) => W.Withdraw(idx),
                onHold: s == null ? null : () => ShowItem(s, false, new() { ("Take back", () => W.Withdraw(idx)) })));
        }
        body.With(Ui.Label($"{(Ui.Touch ? "Tap" : "Click")} a stored item to take it back. {(Ui.Touch ? "Tap" : "Click")} bag items to store them.", Ui.Muted, 13, true), grid);
    }
}

// A draggable pop-up panel with a title bar and close button.
public partial class GameWindow : PanelContainer
{
    public Action<VBoxContainer> Render = _ => { };
    public readonly VBoxContainer Body = Ui.VBox(6);
    public string Sig = "";
    private readonly ScrollContainer _scroll = new() { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
    private bool _drag;

    public GameWindow(string title, Action onClose)
    {
        MouseFilter = MouseFilterEnum.Stop;
        var outer = Ui.VBox(6);
        var bar = new PanelContainer();
        bar.AddThemeStyleboxOverride("panel", Ui.Box("#4a3418", "#000000", 0, 4));
        var titleRow = Ui.HBox();
        var label = Ui.Label(title, Ui.GoldHi, 14);
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        var close = new Button { Text = "×", Flat = true, FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(Ui.Touch ? 36 : 0, 0) };
        close.Pressed += onClose;
        bar.AddChild(titleRow.With(label, close));
        bar.GuiInput += e =>
        {
            if (e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left) { _drag = mb.Pressed; MoveToFront(); }
            else if (e is InputEventMouseMotion mm && _drag)
            {
                var parent = GetParent<Control>().Size;
                Position = new Vector2(Mathf.Clamp(Position.X + mm.Relative.X, 0, Math.Max(0, parent.X - 80)), Mathf.Clamp(Position.Y + mm.Relative.Y, 0, Math.Max(0, parent.Y - 30)));
            }
        };
        Body.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _scroll.AddChild(Body);
        AddChild(outer.With(bar, _scroll));
    }

    public override void _Process(double delta) => FitBody();

    // Grow with the contents, but scroll rather than run off the bottom of the screen.
    public void FitBody()
    {
        if (GetParent() is not Control parent) return;
        float chrome = GetCombinedMinimumSize().Y - _scroll.CustomMinimumSize.Y;
        float room = parent.Size.Y - Position.Y - chrome - 6;
        float h = Mathf.Max(60, Mathf.Min(Body.GetCombinedMinimumSize().Y, room));
        if (Mathf.Abs(_scroll.CustomMinimumSize.Y - h) < 0.5f) return;
        _scroll.CustomMinimumSize = new Vector2(0, h);
        ResetSize();
    }
}

// A speech bubble with three dots, drawn for the chat button.
public partial class ChatIcon : Control
{
    public ChatIcon() => MouseFilter = MouseFilterEnum.Ignore;

    public override void _Draw()
    {
        var r = new Rect2(Size.X * 0.2f, Size.Y * 0.2f, Size.X * 0.6f, Size.Y * 0.44f);
        var bubble = new StyleBoxFlat { BgColor = Ui.Ink };
        bubble.SetCornerRadiusAll(6);
        DrawStyleBox(bubble, r);
        DrawColoredPolygon(new[]
        {
            new Vector2(r.Position.X + r.Size.X * 0.22f, r.End.Y - 1), new Vector2(r.Position.X + r.Size.X * 0.48f, r.End.Y - 1),
            new Vector2(r.Position.X + r.Size.X * 0.14f, r.End.Y + Size.Y * 0.16f),
        }, Ui.Ink);
        for (int i = 0; i < 3; i++)
            DrawCircle(new Vector2(r.Position.X + r.Size.X * (0.27f + 0.23f * i), r.Position.Y + r.Size.Y / 2), 2.2f, Assets.C("#2c3352"));
    }
}

// The zone map in its pop-out panel: tiles as coloured pixels plus live dots.
public partial class Minimap : Control
{
    public World? W;
    private ImageTexture? _base;
    private GameMap? _map;

    private static readonly Dictionary<char, string> Colors_ = new()
    {
        ['.'] = "#5b9440", ['g'] = "#3f6e35", [','] = "#a8804e", ['G'] = "#a8804e", ['_'] = "#a5a294", ['#'] = "#5f574d", ['~'] = "#3a6e9e",
        ['T'] = "#2f6b2a", ['P'] = "#22502a", ['R'] = "#3b4a5e", ['Q'] = "#2f6b5e", ['H'] = "#e6d9bb", ['D'] = "#8a5530", ['S'] = "#f4d870",
        ['F'] = "#8a6036", ['*'] = "#74ad52", ['W'] = "#6e6a60", ['L'] = "#8a867a", ['p'] = "#4d7a6a", ['B'] = "#3a7a30", ['r'] = "#6e6c66",
        ['c'] = "#5a4e44", ['X'] = "#1e1a17", ['t'] = "#f0a030", ['m'] = "#6b5e52", ['a'] = "#8a2a20", ['='] = "#8a6238",
        ['s'] = "#e0cc98", ['v'] = "#7a8a40", ['z'] = "#56603a", ['d'] = "#a07850", ['o'] = "#e87070", ['h'] = "#5a5250",
        ['l'] = "#e85020", ['O'] = "#2e2a32", ['n'] = "#e8eef6", ['Y'] = "#2a5a48", ['i'] = "#a8d4ec", ['b'] = "#3a7a3a",
        ['j'] = "#5a4a40", ['u'] = "#6a5a48", ['y'] = "#ff6080", ['k'] = "#e8ecf8", ['V'] = "#6a90d8", ['M'] = "#c8c4d0", ['I'] = "#8a8478",
    };

    public void Rebuild(GameMap map)
    {
        _map = map;
        var img = Image.CreateEmpty(map.W, map.H, false, Image.Format.Rgba8);
        for (int y = 0; y < map.H; y++)
            for (int x = 0; x < map.W; x++)
                img.SetPixel(x, y, Color.FromHtml(Colors_.GetValueOrDefault(map.Tile(x, y), "#000000")));
        _base = ImageTexture.CreateFromImage(img);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_base == null || _map == null || W == null) return;
        float s = Math.Min(Size.X / _map.W, Size.Y / _map.H);
        var off = new Vector2((Size.X - _map.W * s) / 2, (Size.Y - _map.H * s) / 2);
        DrawTextureRect(_base, new Rect2(off, new Vector2(_map.W * s, _map.H * s)), false, _map.Dark > 0 ? new Color(0.7f, 0.7f, 0.7f) : Colors.White);
        void Dot(int x, int y, string hex, float grow = 0) => DrawRect(new Rect2(off + new Vector2(x * s - grow, y * s - grow), new Vector2(s + grow * 2, s + grow * 2)), Color.FromHtml(hex));
        foreach (var pt in _map.Portals.Values) Dot(pt.X, pt.Y, "#7ae8ff", 1);
        foreach (var n in W.Npcs) Dot(n.X, n.Y, "#ffd84a");
        foreach (var f in W.FolkList) Dot(f.X, f.Y, "#9ad8ff");
        foreach (var m in W.Monsters) Dot(m.X, m.Y, m.D.Boss ? "#ff40ff" : "#ff4a3a", m.D.Boss ? 1.5f : 0);
        Dot(W.P.X, W.P.Y, Time.GetTicksMsec() / 300 % 2 == 0 ? "#ffffff" : "#40ff80", 1);
    }
}
