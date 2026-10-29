using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using KotW.Shared;

namespace KotW;

// Save file (de)serialisation generated at compile time: iOS builds are compiled ahead of time,
// where System.Text.Json's reflection mode is switched off.
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SaveData))]
internal partial class SaveJson : JsonSerializerContext { }

// Entry point: title screen, keyboard, mouse and touch input, the game loop and saving.
// The client only turns key presses into World calls; World owns every rule.
public partial class Main : Node2D
{
    private const double SaveEvery = 60;
    private World? _world;
    private GameView _view = null!;
    private Hud _hud = null!;
    private Overlay _overlay = null!;
    private AudioBox _audio = null!;
    private TitleScreen? _title;
    private CanvasLayer _ui = null!;
    private double _saveT = SaveEvery;
    private string? _shotDir; // debug: --shot=<folder> runs a scripted tour and saves screenshots
    private double _shotT;
    private int _shotStep;

    private readonly List<Dir> _held = new();
    private Dir? _tap;
    private bool _fresh, _attack, _attackFresh;

    private static string SavePath => ProjectSettings.GlobalizePath("user://save.json");

    private static readonly (Key Key, Dir Dir)[] DirKeys =
    {
        (Key.Up, Dir.Up), (Key.W, Dir.Up), (Key.Down, Dir.Down), (Key.S, Dir.Down),
        (Key.Left, Dir.Left), (Key.A, Dir.Left), (Key.Right, Dir.Right), (Key.D, Dir.Right),
    };

    public override void _Ready()
    {
        _audio = new AudioBox();
        AddChild(_audio);
        _ui = new CanvasLayer { Layer = 5 };
        AddChild(_ui);
        _audio.SetMood("town");
        Ui.Touch = OS.HasFeature("mobile") || OS.GetCmdlineUserArgs().Contains("--touch");
        if (Ui.Touch) SetupTouchScreen();
        _shotDir = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--shot="))?[7..];
        if (_shotDir != null) { StartGame(Rules.NewCharacter("Baram", "#f2c9a0", "#1a1a1a", "topknot"), false); return; }
        _title = new TitleScreen { Existing = LoadSave(), OnStart = StartGame };
        _ui.AddChild(_title);
    }

    // Phones pack a lot of pixels into a small screen: scale the whole canvas so text stays readable,
    // and stretch it to fill the screen whatever its shape.
    private void SetupTouchScreen()
    {
        bool phone = OS.HasFeature("mobile");
        if (!phone) Input.EmulateTouchFromMouse = true; // --touch on a desktop: the mouse stands in for a finger
        var root = GetTree().Root;
        // Aim for the HUD's 13 px text to come out about 11 points tall; desktops preview a phone-sized canvas.
        float points = root.Size.Y / Math.Max(1f, DisplayServer.ScreenGetScale());
        int h = phone ? (int)Math.Clamp(points * 1.15f, 420, 720) : 450;
        root.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
        root.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
        root.ContentScaleSize = new Vector2I(h * 16 / 9, h);
    }

    private static SaveData? LoadSave()
    {
        try { return File.Exists(SavePath) ? JsonSerializer.Deserialize(File.ReadAllText(SavePath), SaveJson.Default.SaveData) : null; }
        catch (Exception e) { GD.PushWarning($"Could not read save: {e.Message}"); return null; }
    }

    private void Save()
    {
        if (_world == null || _shotDir != null) return;
        try { File.WriteAllText(SavePath, JsonSerializer.Serialize(_world.P.ToSave(), SaveJson.Default.SaveData)); }
        catch (Exception e) { GD.PushWarning($"Could not save: {e.Message}"); }
        _saveT = SaveEvery;
    }

    private void StartGame(SaveData data, bool isNew)
    {
        _title?.QueueFree();
        _title = null;
        _world = new World(data);
        _view = new GameView();
        AddChild(_view);
        _view.Init(_world);
        var worldUi = new CanvasLayer { Layer = 1 };
        AddChild(worldUi);
        _overlay = new Overlay { View = _view };
        worldUi.AddChild(_overlay);
        _hud = new Hud { W = _world, Audio = _audio };
        _ui.AddChild(_hud);
        if (_hud.Pad is { } pad)
        {
            pad.OnDir = d => { _tap = d; _fresh = true; };
            pad.OnAttack = () => _attackFresh = true;
            pad.OnTarget = () => _world!.CycleTarget();
        }
        _hud.Chat($"Welcome to the Kingdom of the Winds! {(Ui.Touch ? "Tap Help" : "Press H")} for controls.", "level");
        if (isNew)
        {
            Save();
            _hud.Toggle("help");
            _hud.Chat($"{data.Name} arrives in Pungseong. Elder Baek in the plaza (marked with !) has work for you.", "quest");
        }
        else _hud.Chat($"Welcome back, {data.Name}.", "quest");
    }

    public override void _Process(double delta)
    {
        if (_world == null) return;
        double dt = Math.Min(0.05, delta);
        bool typing = GetViewport().GuiGetFocusOwner() is LineEdit;
        // Drop directions whose keys were released (also covers releases eaten by the chat box).
        _held.RemoveAll(d => typing || !DirKeys.Any(k => k.Dir == d && Input.IsPhysicalKeyPressed(k.Key)));
        if (typing || !Input.IsPhysicalKeyPressed(Key.Space)) _attack = false;
        bool ready = _world.P.MoveT >= 1;
        var input = new TickInput
        {
            Dir = _held.Count > 0 ? _held[^1] : _hud.Pad?.Held ?? _tap,
            Fresh = _fresh,
            Attack = _attack || _attackFresh || _hud.Pad?.Attacking == true,
            AttackFresh = _attackFresh,
        };
        if (ready) { _tap = null; _fresh = false; }
        _attackFresh = false;

        _world.Update(dt, input);
        foreach (var e in _world.Drain())
        {
            _overlay.OnEvent(e);
            _hud.OnEvent(e);
            if (e.Type == EvType.Sfx) _audio.Play(e.Kind);
            else if (e.Type == EvType.Fx) _view.AddFx(e);
            else if (e.Type == EvType.Map)
            {
                _view.OnMapChanged();
                _audio.SetMood(_world.Maps[e.Id].Music);
                if (!e.Initial) Save();
            }
        }
        // Keep the player centred in the part of the screen the HUD leaves uncovered.
        _view.Sync(dt, _hud.CameraNudge);
        _overlay.Tick(dt);
        _hud.Tick(dt);
        _saveT -= dt;
        if (_saveT <= 0) Save();
        if (_shotDir != null) ShotTour(dt);
    }

    // Debug tour used to check the visuals without a person at the keyboard.
    private void ShotTour(double dt)
    {
        _shotT += dt;
        var w = _world!;
        void Shot(string name) => GetViewport().GetTexture().GetImage().SavePng(System.IO.Path.Combine(_shotDir!, name + ".png"));
        var steps = new (double At, Action Do)[]
        {
            (1.5, () => Shot("1_town")),
            (1.6, () => { w.EnterMap("town", 15, 23, Dir.Up); w.GainExp(2000); w.ChooseClass("warrior"); w.UseItem(Array.FindIndex(w.P.Inv, s => s?.Id == "iron_sword")); w.TalkTo("m_mystic"); }),
            (2.6, () => Shot("2_dialog")),
            (2.7, () => { _hud.CloseTop(); w.EnterMap("fields", 21, 3, Dir.Down); }),
            (3.4, () => { w.CycleTarget(); var t = w.GetTarget(); if (t != null) { w.P.Place(t.X, t.Y - 1); if (!w.Map.Walkable(t.X, t.Y - 1)) w.P.Place(t.X - 1, t.Y); } }),
            (3.6, () => w.UseHotbar(0)),
            (3.75, () => Shot("3_fields_combat")),
            (3.8, () => { _hud.Toggle("inventory"); _hud.Toggle("stats"); }),
            (4.4, () => Shot("4_windows")),
            (4.5, () => { _hud.CloseTop(); _hud.CloseTop(); w.EnterMap("forest", 25, 8, Dir.Up); }),
            (5.3, () => Shot("5_forest")),
            (5.4, () => w.EnterMap("cave", 20, 14, Dir.Up)),
            (6.4, () => Shot("6_cave")),
            (6.5, () => { w.HurtPlayer(99999); w.EnterMap("town", 19, 9, Dir.Up); w.P.Ghost = true; }),
            (7.3, () => Shot("7_ghost")),
            (7.4, () => { w.P.Ghost = false; w.P.Hp = 99999; w.P.Cls = "windstrider"; w.P.Level = 90; w.P.Equip["weapon"] = "phoenix_bow"; w.P.Equip["armor"] = "windrunner_raiment"; w.EnterMap("harbor", 19, 12, Dir.Down); }),
            (8.2, () => Shot("8_harbor")),
            (8.3, () => w.EnterMap("marsh", 23, 12, Dir.Down)),
            (9.1, () => Shot("9_marsh")),
            (9.2, () => w.EnterMap("highlands", 23, 20, Dir.Up)),
            (10.0, () => Shot("10_highlands")),
            (10.1, () => w.EnterMap("monastery", 19, 14, Dir.Up)),
            (10.9, () => Shot("11_monastery")),
            (11.0, () => w.EnterMap("frostpeak", 20, 16, Dir.Right)),
            (11.8, () => Shot("12_frostpeak")),
            (11.9, () => w.EnterMap("bamboo", 24, 10, Dir.Down)),
            (12.7, () => Shot("13_bamboo")),
            (12.8, () => w.EnterMap("underworld", 24, 33, Dir.Up)),
            (13.6, () => Shot("14_underworld")),
            (13.7, () => w.EnterMap("skypalace", 22, 20, Dir.Up)),
            (14.4, () => { w.CycleTarget(); w.CastSkill("arrow_storm"); }),
            (14.6, () => Shot("15_skypalace")),
            (14.7, () => w.EnterMap("eye", 17, 24, Dir.Up)),
            (15.5, () => Shot("16_eye")),
            (15.6, () => w.EnterMap("drowned", 20, 14, Dir.Up)),
            (16.4, () => Shot("17_drowned")),
            (16.5, () => w.EnterMap("coast", 20, 10, Dir.Down)),
            (17.3, () => Shot("18_coast")),
            (17.4, () => GetTree().Quit()),
        };
        while (_shotStep < steps.Length && _shotT >= steps[_shotStep].At) steps[_shotStep++].Do();
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (_world == null) return;
        if (ev is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            GetViewport().GuiReleaseFocus();
            var pos = _view.GetGlobalMousePosition();
            int tx = (int)Math.Floor(pos.X / Assets.T), ty = (int)Math.Floor(pos.Y / Assets.T);
            // Sprites stand taller than their tile, so a click on a head belongs to the tile below.
            var ent = _world.EntityAt(tx, ty) ?? _world.EntityAt(tx, ty + 1);
            if (ent is Monster m) _world.SelectTarget(m.Id);
            else if (ent != null) _world.Interact(ent);
            return;
        }
        if (ev is not InputEventKey { Pressed: true } k) return;
        if (GetViewport().GuiGetFocusOwner() is LineEdit) return;
        var key = k.PhysicalKeycode;
        foreach (var (dk, dir) in DirKeys)
        {
            if (key != dk) continue;
            if (!k.Echo) { _held.Remove(dir); _held.Add(dir); _tap = dir; _fresh = true; }
            GetViewport().SetInputAsHandled();
            return;
        }
        if (k.Echo && key != Key.Space) return;
        if (key >= Key.Key1 && key <= Key.Key9) { _world.UseHotbar((int)(key - Key.Key1)); return; }
        if (key >= Key.Kp1 && key <= Key.Kp9) { _world.UseHotbar((int)(key - Key.Kp1)); return; }
        if (key is Key.Key0 or Key.Kp0) { _world.UseHotbar(9); return; }
        switch (key)
        {
            case Key.Space: if (!k.Echo) _attackFresh = true; _attack = true; break;
            case Key.Tab: _world.CycleTarget(); break;
            case Key.Escape: if (!_hud.CloseTop()) _world.ClearTarget(); break;
            case Key.Enter: case Key.KpEnter: _hud.ChatInput.GrabFocus(); break;
            case Key.I: _hud.Toggle("inventory"); break;
            case Key.C: _hud.Toggle("stats"); break;
            case Key.K: _hud.Toggle("skills"); break;
            case Key.Q: case Key.L: _hud.Toggle("quests"); break;
            case Key.H: _hud.Toggle("help"); break;
            case Key.M: _hud.ToggleMusic(); break;
            default: return;
        }
        GetViewport().SetInputAsHandled();
    }

    public override void _Notification(int what)
    {
        // Phones rarely quit an app: they send it to the background, and may end it there without warning.
        if (what == NotificationWMCloseRequest || what == NotificationApplicationPaused) Save();
    }
}
