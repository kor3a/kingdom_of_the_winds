using Godot;

namespace KotW;

// Plays the generated sound effects and the looping music for each zone.
public partial class AudioBox : Node
{
    private readonly Dictionary<string, AudioStream?> _sfx = new();
    private readonly List<AudioStreamPlayer> _pool = new();
    private AudioStreamPlayer _music = null!;
    private string _mood = "";
    public bool MusicOn { get; private set; } = true;

    public override void _Ready()
    {
        for (int i = 0; i < 12; i++) { var p = new AudioStreamPlayer { VolumeDb = -8 }; AddChild(p); _pool.Add(p); }
        _music = new AudioStreamPlayer { VolumeDb = -14 };
        _music.Finished += () => { if (MusicOn) _music.Play(); };
        AddChild(_music);
    }

    public void Play(string name)
    {
        if (!_sfx.TryGetValue(name, out var s))
        {
            var path = $"res://audio/sfx/{name}.wav";
            s = ResourceLoader.Exists(path) ? GD.Load<AudioStream>(path) : null;
            _sfx[name] = s;
        }
        if (s == null) return;
        var player = _pool.FirstOrDefault(p => !p.Playing) ?? _pool[0];
        player.Stream = s;
        player.Play();
    }

    public void SetMood(string mood)
    {
        if (mood == _mood) return;
        _mood = mood;
        var path = $"res://audio/music/{mood}.wav";
        if (!ResourceLoader.Exists(path)) return;
        var stream = GD.Load<AudioStreamWav>(path);
        stream.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
        stream.LoopBegin = 0;
        stream.LoopEnd = (int)(stream.GetLength() * stream.MixRate);
        _music.Stream = stream;
        if (MusicOn) _music.Play();
    }

    public bool ToggleMusic()
    {
        MusicOn = !MusicOn;
        if (MusicOn) _music.Play(); else _music.Stop();
        return MusicOn;
    }
}
