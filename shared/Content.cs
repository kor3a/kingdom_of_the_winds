namespace KotW.Shared;

// Static game content: the types, plus tiles here; classes and skills, items, monsters and
// the story live in the other Content*.cs files. Plain data keyed by string IDs so the client,
// the art generator and the future server all read the same tables.

public enum Dir { Down = 0, Left = 1, Right = 2, Up = 3 } // matches sprite sheet rows

public static class DirX
{
    public static readonly Dir[] All = { Dir.Up, Dir.Down, Dir.Left, Dir.Right };
    public static (int X, int Y) Vec(this Dir d) => d switch
    {
        Dir.Up => (0, -1), Dir.Down => (0, 1), Dir.Left => (-1, 0), _ => (1, 0),
    };
    public static Dir Of(int dx, int dy) =>
        Math.Abs(dx) >= Math.Abs(dy) ? (dx > 0 ? Dir.Right : Dir.Left) : (dy > 0 ? Dir.Down : Dir.Up);
    public static bool Horizontal(this Dir d) => d is Dir.Left or Dir.Right;
}

public enum Stat { Str, Dex, Int, Wis, Con }

public sealed class TileDef
{
    public required string Name;
    public bool Solid;
}

// Base classes are chosen at level 5. Advanced classes name their Base and are earned at level 50
// after the master's trial; they keep every skill of the base class.
public sealed class ClassDef
{
    public required string Id, Name, Role, Color, Desc;
    public string Weapon = "", Recommend = "";
    public string? Base, Trial;
    public int HpLv, MpLv, Reach; // Reach > 0: basic attacks are arrows that fly this many tiles
    public double Evade;
}

// Melee: the foe in reach. Around: every foe within Radius (1 if unset). Target: a foe in Range,
// plus everyone within Radius of it and up to Chain more nearby, struck Hits times. Line: every foe
// in a straight line ahead, up to Range. Dash: leap next to a foe in Range, then strike it and
// everyone within Radius of it. Self: buffs and heals.
public enum SkillType { Melee, Around, Target, Self, Ghost, Line, Dash }

public sealed class SkillDef
{
    public required string Id, Cls, Name, Color, Desc;
    public int Level, Mp, Hits = 1, Range, Radius, Chain, Knock, Pull; // Pull: drag foes up to this many tiles toward you
    public double Cd, Mult = 1, BehindMult, Power, Dur, Drain, Execute;
    public bool Phys; // Target and Line skills: roll with attack power (arrows) instead of magic
    public SkillType Type;
    public string Effect = "", Fx = "";
}

public enum ItemType { Weapon, Armor, Helmet, Shield, Ring, Necklace, Use, Loot }

public sealed class ItemDef
{
    public required string Id, Name;
    public ItemType Type;
    public string Look = "", Color = "#c0c0c0", Icon = "stone", Desc = "";
    public int Atk, Matk, Def, Hp, Mp, Str, Dex, Int, Wis, Con, Level, Price, Heal, Mana;
    public int? Sell;
    public double Crit;
    public string[]? Classes;
    public bool Stack, Teleport, Robe, Hidden;
    public string Dest = "town"; // where a Teleport item takes you
    public bool IsGear => Type <= ItemType.Necklace;
    public int Bonus(Stat s) => s switch
    {
        Stat.Str => Str, Stat.Dex => Dex, Stat.Int => Int, Stat.Wis => Wis, _ => Con,
    };
}

public sealed class MonsterDef
{
    public required string Id, Name, Sprite;
    public int Level, Hp, Atk, Def, Exp, GoldMin, GoldMax, AggroRange, Respawn;
    public double MoveDur, AtkDelay;
    public bool Aggressive, Boss;
    public string? Summon; // bosses call two of these at half health
    public (string Item, double Chance)[] Drops = Array.Empty<(string, double)>();
}

// A paper-doll outfit. Gear fields are item IDs so the sprite layers can be looked up.
public sealed record Look(string Skin, string Hair, string Style, string? Armor = null,
    string? Weapon = null, string? Helmet = null, string? Shield = null);

public sealed class NpcDef
{
    public required string Id, Name, Map, Greet;
    public required Look Look;
    public int X, Y;
    public Dir Facing = Dir.Down;
    public string[] Services = Array.Empty<string>();
}

// A quest is a hunt (KillMonster), an errand (BringItem) or, with neither, a message to carry
// to TurnIn. Requires chains quests together; Story marks the main storyline; Cls limits a
// class trial to one class line. Outro is what the turn-in NPC says when it is done.
public sealed class QuestDef
{
    public required string Id, Giver, Title, Text;
    public int Level, RewardExp, RewardGold;
    public string? KillMonster, BringItem, TurnIn, Requires, Cls, Outro;
    public bool Story;
    public int Count;
    public (string Item, int Qty)[] RewardItems = Array.Empty<(string, int)>();
    public string Ender => TurnIn ?? Giver;
}

public sealed class FolkDef
{
    public required string Name;
    public string? Cls;
    public int Level;
    public required Look Look;
}

public static partial class Content
{
    public const int Tile = 32;

    public static readonly Dictionary<char, TileDef> Tiles = new()
    {
        ['.'] = new() { Name = "grass" }, ['g'] = new() { Name = "forest floor" },
        [','] = new() { Name = "dirt road" }, ['_'] = new() { Name = "stone plaza" },
        ['#'] = new() { Name = "town wall", Solid = true }, ['~'] = new() { Name = "water", Solid = true },
        ['T'] = new() { Name = "tree", Solid = true }, ['P'] = new() { Name = "pine", Solid = true },
        ['R'] = new() { Name = "roof", Solid = true }, ['Q'] = new() { Name = "shrine roof", Solid = true },
        ['H'] = new() { Name = "house wall", Solid = true }, ['D'] = new() { Name = "door", Solid = true },
        ['S'] = new() { Name = "shrine circle" }, ['F'] = new() { Name = "fence", Solid = true },
        ['*'] = new() { Name = "flowers" }, ['W'] = new() { Name = "well", Solid = true },
        ['L'] = new() { Name = "stone lantern", Solid = true }, ['p'] = new() { Name = "rice paddy", Solid = true },
        ['B'] = new() { Name = "bush", Solid = true }, ['r'] = new() { Name = "boulder", Solid = true },
        ['c'] = new() { Name = "cave floor" }, ['X'] = new() { Name = "cave wall", Solid = true },
        ['m'] = new() { Name = "stalagmite", Solid = true }, ['t'] = new() { Name = "torch", Solid = true },
        ['a'] = new() { Name = "altar", Solid = true }, ['='] = new() { Name = "bridge" },
        ['G'] = new() { Name = "gate" },
        ['s'] = new() { Name = "sand" }, ['v'] = new() { Name = "reeds", Solid = true },
        ['z'] = new() { Name = "marsh mud" }, ['d'] = new() { Name = "dock" },
        ['o'] = new() { Name = "coral", Solid = true }, ['h'] = new() { Name = "ash" },
        ['l'] = new() { Name = "lava", Solid = true }, ['O'] = new() { Name = "basalt", Solid = true },
        ['n'] = new() { Name = "snow" }, ['Y'] = new() { Name = "snowy pine", Solid = true },
        ['i'] = new() { Name = "ice" }, ['b'] = new() { Name = "bamboo", Solid = true },
        ['j'] = new() { Name = "dead tree", Solid = true }, ['u'] = new() { Name = "spirit road" },
        ['y'] = new() { Name = "soul lantern", Solid = true }, ['k'] = new() { Name = "cloud" },
        ['V'] = new() { Name = "open sky", Solid = true }, ['M'] = new() { Name = "marble" },
        ['I'] = new() { Name = "pillar", Solid = true },
    };
    public static readonly string TileOrder = ".g,_#~TPRQHDSF*WLpBrcXmta=GsvzdohlOnYibjuykVMI"; // atlas row order
    public const string ObjectTiles = "TPBrLmvoOYbjyI"; // drawn over the zone's ground tile

    public static readonly Dictionary<Stat, string> StatNames = new()
    {
        [Stat.Str] = "Strength", [Stat.Dex] = "Dexterity", [Stat.Int] = "Intelligence",
        [Stat.Wis] = "Wisdom", [Stat.Con] = "Constitution",
    };

    public static readonly string[] EquipSlots = { "weapon", "armor", "helmet", "shield", "ring1", "ring2", "necklace" };

    public static readonly string[] HairStyles = { "topknot", "bun", "long", "bald" };
}
