namespace KotW.Shared;

public enum EntityKind { Player, Monster, Npc, Folk }

// Anything standing on a tile. (FX, FY) -> (X, Y) is the step being animated;
// logic always uses (X, Y), which is claimed the moment a step starts.
public abstract class Entity
{
    public abstract EntityKind Kind { get; }
    public string Id = "";
    public string Name = "";
    public int X, Y, FX, FY;
    public double MoveT = 1, StepDur = 0.2;
    public Dir Facing = Dir.Down;
    public double AttackAnim;

    public void Place(int x, int y) { X = FX = x; Y = FY = y; MoveT = 1; }
    public void StartStep(int nx, int ny, double dur) { FX = X; FY = Y; X = nx; Y = ny; MoveT = 0; StepDur = dur; }
    public int Manhattan(int x, int y) => Math.Abs(X - x) + Math.Abs(Y - y);
    public int Manhattan(Entity o) => Manhattan(o.X, o.Y);
    public int Chebyshev(Entity o) => Math.Max(Math.Abs(X - o.X), Math.Abs(Y - o.Y));
}

public sealed class Player : Entity
{
    public override EntityKind Kind => EntityKind.Player;
    public string Skin = "", Hair = "", Style = "";
    public string? Cls;
    public int Level, Exp, Unspent, Gold;
    public double Hp, Mp;
    public StatBlock Stats = new();
    public ItemStack?[] Inv = new ItemStack?[Rules.BagSize];
    public ItemStack?[] Storage = new ItemStack?[Rules.StorageSize];
    public Dictionary<string, string?> Equip = new();
    public Dictionary<string, QuestProgress> Quests = new();
    public string MapId = "town";
    public bool Ghost;

    // Runtime only
    public Dictionary<string, double> Buffs = new();
    public Dictionary<string, double> Cds = new();
    public double AtkCd, PotCd, HurtFlash, LastCombat = -99, GhostAt;

    public Look Look => new(Skin, Hair, Style, Equip.GetValueOrDefault("armor"), Equip.GetValueOrDefault("weapon"),
        Equip.GetValueOrDefault("helmet"), Equip.GetValueOrDefault("shield"));

    public static Player FromSave(SaveData d)
    {
        var p = new Player
        {
            Id = "player", Name = d.Name, Skin = d.Skin, Hair = d.Hair, Style = d.Style, Cls = d.Cls, Level = d.Level, Exp = d.Exp,
            Stats = d.Stats, Unspent = d.Unspent, Hp = d.Hp, Mp = d.Mp, Gold = d.Gold, MapId = d.MapId, Facing = d.Facing, Ghost = d.Ghost,
            Equip = new Dictionary<string, string?>(d.Equip), Quests = d.Quests,
        };
        for (int i = 0; i < Rules.BagSize; i++) p.Inv[i] = i < d.Inv.Length ? d.Inv[i]?.Clone() : null;
        for (int i = 0; i < Rules.StorageSize; i++) p.Storage[i] = i < d.Storage.Length ? d.Storage[i]?.Clone() : null;
        foreach (var slot in Content.EquipSlots) p.Equip.TryAdd(slot, null);
        p.Place(d.X, d.Y);
        return p;
    }

    public SaveData ToSave() => new()
    {
        Name = Name, Skin = Skin, Hair = Hair, Style = Style, Cls = Cls, Level = Level, Exp = Exp, Stats = Stats, Unspent = Unspent,
        Hp = Hp, Mp = Mp, Gold = Gold, Inv = Inv.Select(s => s?.Clone()).ToArray(), Equip = new(Equip),
        Storage = Storage.Select(s => s?.Clone()).ToArray(), Quests = Quests, MapId = MapId, X = X, Y = Y, Facing = Facing, Ghost = Ghost,
    };
}

public sealed class StatusEffect
{
    public double T, Tick = 1;
    public int Dmg;
}

public sealed class Monster : Entity
{
    public override EntityKind Kind => EntityKind.Monster;
    public required MonsterDef D;
    public required SpawnRule Spawn;
    public double Hp, MaxHp;
    public string State = "idle"; // idle | chase
    public double ThinkT, AtkCd, LastHit = -99, Flash, SpecialT = 5, RoarT;
    public Dir HFace = Dir.Left;
    public int HomeX, HomeY;
    public bool Summoned, NoRespawn;
    public Dictionary<string, StatusEffect> Status = new();
}

public sealed class Npc : Entity
{
    public override EntityKind Kind => EntityKind.Npc;
    public required NpcDef Def;
}

public sealed class Folk : Entity
{
    public override EntityKind Kind => EntityKind.Folk;
    public required FolkDef Def;
    public double ThinkT;
}

public enum EvType { Msg, Dmg, Fx, Sfx, Bubble, Dialog, Map }

// Everything the simulation tells the client. Over the network these become server messages.
public sealed class GameEvent
{
    public EvType Type;
    public string Text = "", Channel = "", From = "", Kind = "", Id = "", Color = "";
    public int X, Y, FromX, FromY, R;
    public bool Big, Initial;
    public double Dur;
    public Dir Dir;
}

public sealed class TickInput
{
    public Dir? Dir;
    public bool Fresh, Attack, AttackFresh;
}
