namespace KotW.Shared;

// Static game content: tiles, classes, skills, items, monsters, NPCs, quests.
// Plain data keyed by string IDs so the client, the art generator and the
// future server all read the same tables.

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

public sealed class ClassDef
{
    public required string Id, Name, Role, Color, Desc;
    public string Weapon = "", Recommend = "";
    public int HpLv, MpLv;
    public double Evade;
}

public enum SkillType { Melee, Around, Target, Self, Ghost }

public sealed class SkillDef
{
    public required string Id, Cls, Name, Glyph, Color, Desc;
    public int Level, Mp, Hits = 1, Range, Radius;
    public double Cd, Mult = 1, BehindMult, Power, Dur;
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

public sealed class QuestDef
{
    public required string Id, Giver, Title, Text;
    public int Level, RewardExp, RewardGold;
    public string? KillMonster, BringItem;
    public int Count;
    public (string Item, int Qty)[] RewardItems = Array.Empty<(string, int)>();
}

public sealed class FolkDef
{
    public required string Name;
    public string? Cls;
    public int Level;
    public required Look Look;
}

public static class Content
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
    };
    public static readonly string TileOrder = ".g,_#~TPRQHDSF*WLpBrcXmta=G"; // atlas row order

    public static readonly Dictionary<Stat, string> StatNames = new()
    {
        [Stat.Str] = "Strength", [Stat.Dex] = "Dexterity", [Stat.Int] = "Intelligence",
        [Stat.Wis] = "Wisdom", [Stat.Con] = "Constitution",
    };

    public static readonly Dictionary<string, ClassDef> Classes = new[]
    {
        new ClassDef { Id = "none", Name = "Wanderer", Role = "", HpLv = 6, MpLv = 3, Color = "#b8b0a0",
            Desc = "You have not chosen a path yet. Reach level 5 and visit a job master in town." },
        new ClassDef { Id = "warrior", Name = "Warrior", Role = "Tank, melee damage", HpLv = 14, MpLv = 3, Color = "#e06848",
            Weapon = "iron_sword", Recommend = "STR and CON",
            Desc = "Stands at the front so the party can fight. Taunts foes, shrugs off blows and hits hard." },
        new ClassDef { Id = "rogue", Name = "Rogue", Role = "Burst melee, evasion", HpLv = 10, MpLv = 4, Color = "#b08ae8", Evade = 0.12,
            Weapon = "dagger", Recommend = "DEX and STR",
            Desc = "Strikes from the shadows. Backstabs, poisons and slips out of sight. Dodges more often." },
        new ClassDef { Id = "mystic", Name = "Mystic", Role = "Ranged magic, area damage", HpLv = 6, MpLv = 10, Color = "#5fa8f0",
            Weapon = "oak_staff", Recommend = "INT and WIS",
            Desc = "Commands fire, ice and lightning from range, and can paralyze a foe in place." },
        new ClassDef { Id = "monk", Name = "Monk", Role = "Healer, support", HpLv = 9, MpLv = 8, Color = "#f0c860",
            Weapon = "pilgrim_staff", Recommend = "WIS and CON",
            Desc = "Heals, blesses and smites with holy light. Can call their own spirit back from death." },
    }.ToDictionary(c => c.Id);

    public static readonly Dictionary<string, SkillDef> Skills = new[]
    {
        new SkillDef { Id = "heavy_strike", Cls = "warrior", Level = 5, Name = "Heavy Strike", Glyph = "擊", Color = "#d0583e", Mp = 6, Cd = 3, Type = SkillType.Melee, Mult = 2.1, Desc = "A crushing blow on the foe in front of you." },
        new SkillDef { Id = "taunt", Cls = "warrior", Level = 7, Name = "Taunt", Glyph = "挑", Color = "#c08040", Mp = 8, Cd = 12, Type = SkillType.Self, Effect = "taunt", Radius = 5, Dur = 8, Desc = "Pull every foe within 5 tiles onto you and take 35% less damage for 8s." },
        new SkillDef { Id = "war_cry", Cls = "warrior", Level = 10, Name = "War Cry", Glyph = "吼", Color = "#e07030", Mp = 12, Cd = 30, Type = SkillType.Self, Effect = "warcry", Dur = 20, Desc = "+30% attack for 20s." },
        new SkillDef { Id = "whirlwind", Cls = "warrior", Level = 14, Name = "Whirlwind", Glyph = "旋", Color = "#f0a040", Mp = 15, Cd = 6, Type = SkillType.Around, Mult = 1.5, Desc = "Strike every foe around you." },
        new SkillDef { Id = "backstab", Cls = "rogue", Level = 5, Name = "Backstab", Glyph = "刺", Color = "#9a72d8", Mp = 5, Cd = 4, Type = SkillType.Melee, Mult = 1.6, BehindMult = 2.8, Desc = "Stab the foe in reach. Much stronger from behind or against an unaware foe." },
        new SkillDef { Id = "poison_blade", Cls = "rogue", Level = 7, Name = "Poison Blade", Glyph = "毒", Color = "#60c050", Mp = 8, Cd = 8, Type = SkillType.Melee, Mult = 1.0, Effect = "poison", Dur = 6, Desc = "Strike and poison the foe for 6s." },
        new SkillDef { Id = "stealth", Cls = "rogue", Level = 10, Name = "Shadow Veil", Glyph = "隱", Color = "#6a5a90", Mp = 10, Cd = 20, Type = SkillType.Self, Effect = "stealth", Dur = 6, Desc = "Vanish for 6s. Foes lose track of you and your next strike always crits." },
        new SkillDef { Id = "flurry", Cls = "rogue", Level = 14, Name = "Flurry", Glyph = "亂", Color = "#c070e0", Mp = 14, Cd = 7, Type = SkillType.Melee, Hits = 3, Mult = 0.9, Desc = "Three quick strikes on the foe in reach." },
        new SkillDef { Id = "fire_bolt", Cls = "mystic", Level = 5, Name = "Fire Bolt", Glyph = "火", Color = "#f06030", Mp = 6, Cd = 1.2, Type = SkillType.Target, Range = 7, Power = 1.7, Fx = "fire", Desc = "Hurl fire at your target." },
        new SkillDef { Id = "frost_shard", Cls = "mystic", Level = 7, Name = "Frost Shard", Glyph = "氷", Color = "#70c8f0", Mp = 9, Cd = 3, Type = SkillType.Target, Range = 7, Power = 1.3, Effect = "slow", Dur = 4, Fx = "ice", Desc = "Ice that damages and slows the target for 4s." },
        new SkillDef { Id = "paralyze", Cls = "mystic", Level = 10, Name = "Paralyze", Glyph = "麻", Color = "#e8d040", Mp = 12, Cd = 15, Type = SkillType.Target, Range = 6, Power = 0.4, Effect = "stun", Dur = 3, Fx = "paralyze", Desc = "Freeze the target in place for 3s (1s on bosses)." },
        new SkillDef { Id = "lightning", Cls = "mystic", Level = 14, Name = "Thunder Call", Glyph = "雷", Color = "#b0a0ff", Mp = 20, Cd = 6, Type = SkillType.Target, Range = 7, Power = 1.5, Radius = 2, Fx = "lightning", Desc = "Lightning strikes the target and every foe within 2 tiles of it." },
        new SkillDef { Id = "smite", Cls = "monk", Level = 5, Name = "Smite", Glyph = "光", Color = "#f0d060", Mp = 5, Cd = 1.5, Type = SkillType.Target, Range = 6, Power = 1.3, Fx = "holy", Desc = "A beam of holy light on your target." },
        new SkillDef { Id = "heal", Cls = "monk", Level = 5, Name = "Heal", Glyph = "治", Color = "#70e090", Mp = 8, Cd = 2, Type = SkillType.Self, Effect = "heal", Desc = "Restore your health. Scales with Wisdom." },
        new SkillDef { Id = "blessing", Cls = "monk", Level = 8, Name = "Blessing", Glyph = "福", Color = "#f0a0c0", Mp = 12, Cd = 30, Type = SkillType.Self, Effect = "blessing", Dur = 25, Desc = "+30% defense, +10% attack and faster regeneration for 25s." },
        new SkillDef { Id = "resurrect", Cls = "monk", Level = 12, Name = "Resurrect", Glyph = "魂", Color = "#e0e0ff", Mp = 0, Cd = 60, Type = SkillType.Ghost, Desc = "While a ghost, call your spirit back into your body. (With other players this will revive them.)" },
    }.ToDictionary(s => s.Id);

    public static readonly string[] EquipSlots = { "weapon", "armor", "helmet", "shield", "ring1", "ring2", "necklace" };

    public static readonly Dictionary<string, ItemDef> Items = new[]
    {
        // Weapons: Look picks the sprite shape, Color tints the blade or gem.
        new ItemDef { Id = "wooden_sword", Name = "Wooden Sword", Type = ItemType.Weapon, Look = "sword", Color = "#b07a40", Icon = "sword", Atk = 4, Price = 20, Desc = "The practice blade every youth starts with." },
        new ItemDef { Id = "iron_sword", Name = "Iron Sword", Type = ItemType.Weapon, Look = "sword", Color = "#c8ccd4", Icon = "sword", Atk = 11, Level = 5, Classes = new[] { "warrior", "rogue" }, Price = 160 },
        new ItemDef { Id = "steel_glaive", Name = "Steel Glaive", Type = ItemType.Weapon, Look = "glaive", Color = "#e4e8f0", Icon = "glaive", Atk = 24, Str = 2, Level = 12, Classes = new[] { "warrior" }, Price = 950 },
        new ItemDef { Id = "dagger", Name = "Bone Dagger", Type = ItemType.Weapon, Look = "dagger", Color = "#ece4cc", Icon = "dagger", Atk = 9, Crit = 0.04, Level = 5, Classes = new[] { "rogue" }, Price = 140 },
        new ItemDef { Id = "viper_fang", Name = "Viper Fang", Type = ItemType.Weapon, Look = "dagger", Color = "#7ad06a", Icon = "dagger", Atk = 19, Crit = 0.08, Dex = 2, Level = 12, Classes = new[] { "rogue" }, Price = 900 },
        new ItemDef { Id = "oak_staff", Name = "Oak Staff", Type = ItemType.Weapon, Look = "staff", Color = "#7ab0e0", Icon = "staff", Atk = 3, Matk = 10, Level = 5, Classes = new[] { "mystic", "monk" }, Price = 150 },
        new ItemDef { Id = "spirit_staff", Name = "Spirit Staff", Type = ItemType.Weapon, Look = "staff", Color = "#50e0f0", Icon = "staff", Atk = 5, Matk = 24, Int = 2, Level = 12, Classes = new[] { "mystic" }, Price = 950 },
        new ItemDef { Id = "pilgrim_staff", Name = "Pilgrim's Staff", Type = ItemType.Weapon, Look = "ringstaff", Color = "#e0c060", Icon = "ringstaff", Atk = 5, Matk = 8, Wis = 1, Level = 5, Classes = new[] { "monk" }, Price = 150 },
        new ItemDef { Id = "lotus_staff", Name = "Lotus Staff", Type = ItemType.Weapon, Look = "ringstaff", Color = "#f4a0c8", Icon = "ringstaff", Atk = 8, Matk = 19, Wis = 3, Level = 12, Classes = new[] { "monk" }, Price = 920 },
        new ItemDef { Id = "tiger_fang_blade", Name = "Tiger Lord's Fang", Type = ItemType.Weapon, Look = "sword", Color = "#f4c848", Icon = "sword", Atk = 32, Matk = 22, Str = 2, Dex = 2, Int = 2, Level = 15, Price = 3000, Desc = "Torn from the Mountain Lord. Any class can wield it." },

        // Armor: Color is the cloth, Robe picks the long hanbok-coat silhouette.
        new ItemDef { Id = "hemp_robe", Name = "Hemp Robe", Type = ItemType.Armor, Robe = true, Color = "#d6c49a", Icon = "robe", Def = 2, Price = 15 },
        new ItemDef { Id = "leather_armor", Name = "Leather Vest", Type = ItemType.Armor, Color = "#94643a", Icon = "armor", Def = 6, Level = 5, Price = 130 },
        new ItemDef { Id = "silk_robe", Name = "Silk Robe", Type = ItemType.Armor, Robe = true, Color = "#4f74c8", Icon = "robe", Def = 5, Matk = 6, Mp = 20, Level = 8, Classes = new[] { "mystic", "monk" }, Price = 320 },
        new ItemDef { Id = "lamellar", Name = "Iron Lamellar", Type = ItemType.Armor, Look = "plate", Color = "#8a929e", Icon = "armor", Def = 15, Level = 12, Classes = new[] { "warrior" }, Price = 780 },
        new ItemDef { Id = "shadow_garb", Name = "Shadow Garb", Type = ItemType.Armor, Color = "#433a58", Icon = "armor", Def = 10, Dex = 3, Level = 12, Classes = new[] { "rogue" }, Price = 760 },

        new ItemDef { Id = "straw_hat", Name = "Straw Hat", Type = ItemType.Helmet, Look = "straw", Color = "#dcc478", Icon = "straw", Def = 1, Price = 15 },
        new ItemDef { Id = "leather_cap", Name = "Leather Cap", Type = ItemType.Helmet, Look = "cap", Color = "#84552e", Icon = "cap", Def = 3, Level = 4, Price = 70 },
        new ItemDef { Id = "iron_helm", Name = "Iron Helm", Type = ItemType.Helmet, Look = "helm", Color = "#9aa2ae", Icon = "helm", Def = 7, Level = 10, Price = 360 },

        new ItemDef { Id = "wooden_shield", Name = "Wooden Shield", Type = ItemType.Shield, Color = "#946a3a", Icon = "shield", Def = 3, Level = 3, Price = 55 },
        new ItemDef { Id = "iron_shield", Name = "Iron Shield", Type = ItemType.Shield, Color = "#9aa2ae", Icon = "shield", Def = 8, Level = 10, Classes = new[] { "warrior" }, Price = 340 },

        new ItemDef { Id = "copper_ring", Name = "Copper Ring", Type = ItemType.Ring, Color = "#d08848", Icon = "ring", Str = 1, Price = 45 },
        new ItemDef { Id = "jade_ring", Name = "Jade Ring", Type = ItemType.Ring, Color = "#50c888", Icon = "ring", Int = 2, Wis = 1, Level = 6, Price = 220 },
        new ItemDef { Id = "wolf_ring", Name = "Wolf-Tooth Ring", Type = ItemType.Ring, Color = "#e8e8e8", Icon = "ring", Dex = 2, Str = 1, Level = 8, Price = 260 },
        new ItemDef { Id = "bead_necklace", Name = "Bead Necklace", Type = ItemType.Necklace, Color = "#c05838", Icon = "necklace", Hp = 15, Price = 60 },
        new ItemDef { Id = "spirit_amulet", Name = "Spirit Amulet", Type = ItemType.Necklace, Color = "#60b8e8", Icon = "necklace", Mp = 30, Wis = 2, Level = 10, Price = 420 },
        new ItemDef { Id = "tiger_amulet", Name = "Tiger Amulet", Type = ItemType.Necklace, Color = "#f4c848", Icon = "necklace", Hp = 60, Str = 3, Dex = 3, Int = 3, Wis = 3, Con = 3, Level = 15, Price = 3000 },

        new ItemDef { Id = "herbal_tonic", Name = "Herbal Tonic", Type = ItemType.Use, Heal = 45, Price = 8, Stack = true, Color = "#d84848", Icon = "potion" },
        new ItemDef { Id = "ginseng_tonic", Name = "Red Ginseng Tonic", Type = ItemType.Use, Heal = 130, Level = 8, Price = 30, Stack = true, Color = "#a01838", Icon = "gourd" },
        new ItemDef { Id = "spirit_tea", Name = "Spirit Tea", Type = ItemType.Use, Mana = 30, Price = 10, Stack = true, Color = "#4878d8", Icon = "potion" },
        new ItemDef { Id = "mountain_tea", Name = "Mountain Tea", Type = ItemType.Use, Mana = 90, Level = 8, Price = 34, Stack = true, Color = "#2848a8", Icon = "gourd" },
        new ItemDef { Id = "return_scroll", Name = "Return Scroll", Type = ItemType.Use, Teleport = true, Price = 25, Stack = true, Color = "#ece0b0", Icon = "scroll", Desc = "Carries you back to the inn in town." },

        new ItemDef { Id = "rabbit_fur", Name = "Rabbit Fur", Type = ItemType.Loot, Sell = 3, Price = 9, Stack = true, Color = "#f0f0f0", Icon = "pelt" },
        new ItemDef { Id = "rat_tail", Name = "Rat Tail", Type = ItemType.Loot, Sell = 2, Price = 6, Stack = true, Color = "#d89098", Icon = "tail" },
        new ItemDef { Id = "acorn", Name = "Acorn", Type = ItemType.Loot, Sell = 3, Price = 9, Stack = true, Color = "#b07434", Icon = "acorn" },
        new ItemDef { Id = "snake_skin", Name = "Snake Skin", Type = ItemType.Loot, Sell = 7, Price = 20, Stack = true, Color = "#6aa850", Icon = "pelt" },
        new ItemDef { Id = "fox_pelt", Name = "Fox Pelt", Type = ItemType.Loot, Sell = 14, Price = 40, Stack = true, Color = "#e88038", Icon = "pelt" },
        new ItemDef { Id = "wolf_fang", Name = "Wolf Fang", Type = ItemType.Loot, Sell = 18, Price = 50, Stack = true, Color = "#f0eadc", Icon = "fang" },
        new ItemDef { Id = "boar_tusk", Name = "Boar Tusk", Type = ItemType.Loot, Sell = 25, Price = 70, Stack = true, Color = "#ece0b8", Icon = "tusk" },
        new ItemDef { Id = "bat_wing", Name = "Bat Wing", Type = ItemType.Loot, Sell = 30, Price = 90, Stack = true, Color = "#6a5880", Icon = "wing" },
        new ItemDef { Id = "dokkaebi_horn", Name = "Dokkaebi Horn", Type = ItemType.Loot, Sell = 45, Price = 130, Stack = true, Color = "#f0d070", Icon = "horn" },
        new ItemDef { Id = "spirit_stone", Name = "Spirit Stone", Type = ItemType.Loot, Sell = 60, Price = 180, Stack = true, Color = "#70e0f0", Icon = "gem" },
        new ItemDef { Id = "tiger_whisker", Name = "Tiger Whisker", Type = ItemType.Loot, Sell = 250, Price = 700, Stack = true, Color = "#ffffff", Icon = "whisker" },

        // Outfits only NPCs wear; they never drop or sell.
        new ItemDef { Id = "npc_inn", Name = "Innkeeper's Dress", Type = ItemType.Armor, Robe = true, Color = "#c05058", Hidden = true },
        new ItemDef { Id = "npc_priest", Name = "Shrine Robe", Type = ItemType.Armor, Robe = true, Color = "#ecebe2", Hidden = true },
        new ItemDef { Id = "npc_monk", Name = "Monk Robe", Type = ItemType.Armor, Robe = true, Color = "#e0a038", Hidden = true },
        new ItemDef { Id = "npc_herb", Name = "Herbalist Robe", Type = ItemType.Armor, Robe = true, Color = "#5a9060", Hidden = true },
        new ItemDef { Id = "npc_elder", Name = "Elder Robe", Type = ItemType.Armor, Robe = true, Color = "#7a8a68", Hidden = true },
        new ItemDef { Id = "npc_keeper", Name = "Keeper Tunic", Type = ItemType.Armor, Color = "#8a7050", Hidden = true },
        new ItemDef { Id = "npc_red", Name = "War Tunic", Type = ItemType.Armor, Color = "#a8382c", Hidden = true },
        new ItemDef { Id = "npc_hermit", Name = "Hermit Robe", Type = ItemType.Armor, Robe = true, Color = "#8a7a64", Hidden = true },
        new ItemDef { Id = "npc_gat", Name = "Horsehair Hat", Type = ItemType.Helmet, Look = "gat", Color = "#202024", Hidden = true },
    }.ToDictionary(i => i.Id);

    public static readonly Dictionary<string, MonsterDef> Monsters = new[]
    {
        new MonsterDef { Id = "rabbit", Name = "Paddy Rabbit", Sprite = "rabbit", Level = 1, Hp = 18, Atk = 4, Def = 0, Exp = 8, GoldMin = 1, GoldMax = 3, MoveDur = 0.42, AtkDelay = 1.4, Drops = new[] { ("rabbit_fur", 0.5), ("herbal_tonic", 0.05) } },
        new MonsterDef { Id = "rat", Name = "Field Rat", Sprite = "rat", Level = 2, Hp = 24, Atk = 6, Def = 1, Exp = 11, GoldMin = 1, GoldMax = 4, Aggressive = true, AggroRange = 3, MoveDur = 0.38, AtkDelay = 1.3, Drops = new[] { ("rat_tail", 0.5) } },
        new MonsterDef { Id = "squirrel", Name = "Striped Squirrel", Sprite = "squirrel", Level = 3, Hp = 32, Atk = 7, Def = 1, Exp = 14, GoldMin = 2, GoldMax = 5, MoveDur = 0.34, AtkDelay = 1.2, Drops = new[] { ("acorn", 0.55), ("spirit_tea", 0.05) } },
        new MonsterDef { Id = "paddy_snake", Name = "Paddy Snake", Sprite = "snake", Level = 5, Hp = 48, Atk = 11, Def = 3, Exp = 22, GoldMin = 3, GoldMax = 8, Aggressive = true, AggroRange = 4, MoveDur = 0.4, AtkDelay = 1.3, Drops = new[] { ("snake_skin", 0.45), ("copper_ring", 0.03), ("herbal_tonic", 0.06) } },
        new MonsterDef { Id = "fox", Name = "Red Fox", Sprite = "fox", Level = 6, Hp = 75, Atk = 15, Def = 4, Exp = 40, GoldMin = 5, GoldMax = 12, Aggressive = true, AggroRange = 3, MoveDur = 0.3, AtkDelay = 1.2, Drops = new[] { ("fox_pelt", 0.45), ("herbal_tonic", 0.1) } },
        new MonsterDef { Id = "wolf", Name = "Grey Wolf", Sprite = "wolf", Level = 8, Hp = 115, Atk = 20, Def = 6, Exp = 60, GoldMin = 8, GoldMax = 16, Aggressive = true, AggroRange = 5, MoveDur = 0.3, AtkDelay = 1.3, Drops = new[] { ("wolf_fang", 0.45), ("wolf_ring", 0.02), ("spirit_tea", 0.08) } },
        new MonsterDef { Id = "boar", Name = "Wild Boar", Sprite = "boar", Level = 10, Hp = 180, Atk = 25, Def = 10, Exp = 85, GoldMin = 10, GoldMax = 22, MoveDur = 0.36, AtkDelay = 1.5, Drops = new[] { ("boar_tusk", 0.5), ("ginseng_tonic", 0.06), ("leather_armor", 0.02) } },
        new MonsterDef { Id = "cave_bat", Name = "Cave Bat", Sprite = "bat", Level = 13, Hp = 150, Atk = 28, Def = 8, Exp = 110, GoldMin = 12, GoldMax = 25, Aggressive = true, AggroRange = 6, MoveDur = 0.26, AtkDelay = 1.1, Drops = new[] { ("bat_wing", 0.45), ("mountain_tea", 0.08) } },
        new MonsterDef { Id = "dokkaebi", Name = "Dokkaebi", Sprite = "dokkaebi", Level = 15, Hp = 260, Atk = 34, Def = 14, Exp = 170, GoldMin = 20, GoldMax = 45, Aggressive = true, AggroRange = 5, MoveDur = 0.34, AtkDelay = 1.4, Drops = new[] { ("dokkaebi_horn", 0.4), ("ginseng_tonic", 0.1), ("iron_helm", 0.02) } },
        new MonsterDef { Id = "stone_spirit", Name = "Stone Spirit", Sprite = "golem", Level = 17, Hp = 420, Atk = 38, Def = 24, Exp = 240, GoldMin = 25, GoldMax = 50, Aggressive = true, AggroRange = 3, MoveDur = 0.55, AtkDelay = 2.0, Drops = new[] { ("spirit_stone", 0.5), ("spirit_amulet", 0.02) } },
        new MonsterDef { Id = "mountain_lord", Name = "Horang, Lord of the Mountain", Sprite = "tiger", Level = 20, Hp = 2400, Atk = 46, Def = 22, Exp = 2500, GoldMin = 400, GoldMax = 600, Aggressive = true, AggroRange = 6, Boss = true, Respawn = 120, MoveDur = 0.32, AtkDelay = 1.6, Drops = new[] { ("tiger_whisker", 1.0), ("tiger_fang_blade", 0.35), ("tiger_amulet", 0.35) } },
    }.ToDictionary(m => m.Id);

    public static readonly Dictionary<string, string[]> Shops = new()
    {
        ["smith"] = new[] { "wooden_sword", "iron_sword", "dagger", "oak_staff", "pilgrim_staff", "steel_glaive", "viper_fang", "spirit_staff", "lotus_staff",
            "straw_hat", "leather_cap", "iron_helm", "leather_armor", "silk_robe", "lamellar", "shadow_garb", "wooden_shield", "iron_shield",
            "copper_ring", "jade_ring", "bead_necklace", "spirit_amulet" },
        ["herbs"] = new[] { "herbal_tonic", "ginseng_tonic", "spirit_tea", "mountain_tea", "return_scroll" },
        ["hermit"] = new[] { "herbal_tonic", "ginseng_tonic", "spirit_tea", "mountain_tea", "return_scroll" },
    };

    // services: rest | shop:<id> | storage | revive | job:<class>
    public static readonly Dictionary<string, NpcDef> Npcs = new[]
    {
        new NpcDef { Id = "innkeeper", Name = "Innkeeper Ok-sun", Map = "town", X = 8, Y = 7, Look = new("#f2c9a0", "#2e2018", "bun", "npc_inn"), Services = new[] { "rest" },
            Greet = "Welcome to the Swallow Inn! A warm room costs ten gold (free for youngsters under level 5). You always wake up here." },
        new NpcDef { Id = "smith", Name = "Smith Dol-soe", Map = "town", X = 34, Y = 7, Look = new("#d9a070", "#1a1a1a", "topknot", "leather_armor", "iron_sword"), Services = new[] { "shop:smith" },
            Greet = "Steel, leather and a bit of jewelry. Everything you need to stay alive past the paddies." },
        new NpcDef { Id = "priest", Name = "Shrine Keeper Hwa", Map = "town", X = 23, Y = 6, Look = new("#f2d0b0", "#e8e8e8", "bun", "npc_priest"), Services = new[] { "revive" },
            Greet = "The wind remembers everyone who falls. Walk onto the golden circle as a ghost and you will return to life." },
        new NpcDef { Id = "elder", Name = "Elder Baek", Map = "town", X = 24, Y = 11, Look = new("#e8c098", "#d8d8d8", "topknot", "npc_elder", null, "npc_gat"),
            Greet = "Ah, a new face in Pungseong. The fields beyond the south gate are restless these days. Help us, and grow strong." },
        new NpcDef { Id = "guard", Name = "Gate Guard Jang", Map = "town", X = 17, Y = 27, Look = new("#d9a070", "#1a1a1a", "topknot", "lamellar", "steel_glaive", "iron_helm"),
            Greet = "South through this gate are the paddies. Past them, east, the Whispering Pinewood. North of the pines lies the Hollow. Mind your level." },
        new NpcDef { Id = "herbalist", Name = "Herbalist Yeon", Map = "town", X = 6, Y = 22, Look = new("#f2c9a0", "#3a2418", "long", "npc_herb"), Services = new[] { "shop:herbs" },
            Greet = "Tonics for the body, tea for the spirit. And a scroll that carries you home if things go badly." },
        new NpcDef { Id = "keeper", Name = "Storekeeper Gu", Map = "town", X = 34, Y = 22, Look = new("#e0b088", "#2a2a2a", "topknot", "npc_keeper"), Services = new[] { "storage" },
            Greet = "Your things are safe with me. No fee, no questions." },
        new NpcDef { Id = "m_warrior", Name = "Master Gang", Map = "town", X = 11, Y = 22, Look = new("#d9a070", "#1a1a1a", "topknot", "npc_red", "steel_glaive"), Services = new[] { "job:warrior" },
            Greet = "A Warrior stands where the blows land, so the others can live. Strength and Constitution, nothing else matters." },
        new NpcDef { Id = "m_rogue", Name = "Master So-ri", Map = "town", X = 13, Y = 22, Look = new("#f0c8a0", "#2a1a2a", "long", "shadow_garb", "dagger"), Services = new[] { "job:rogue" },
            Greet = "Rogues finish fights before they start. Strike from behind, poison the wound, vanish. Dexterity is everything." },
        new NpcDef { Id = "m_mystic", Name = "Master Un", Map = "town", X = 15, Y = 22, Look = new("#f2d0b0", "#c8c8d8", "long", "silk_robe", "spirit_staff"), Services = new[] { "job:mystic" },
            Greet = "Fire, ice and thunder answer a trained mind. Stay back, keep your target in sight, and never let them reach you." },
        new NpcDef { Id = "m_monk", Name = "Master Do-hyang", Map = "town", X = 17, Y = 22, Look = new("#e8c098", "#e8c098", "bald", "npc_monk", "pilgrim_staff"), Services = new[] { "job:monk" },
            Greet = "A Monk mends what others break. Heal, bless, and when all is lost, bring the spirit home. Wisdom guides you." },
        new NpcDef { Id = "hermit", Name = "Hermit Seol", Map = "forest", X = 29, Y = 3, Look = new("#e8c098", "#f0f0f0", "long", "npc_hermit", "pilgrim_staff", "straw_hat"), Services = new[] { "shop:hermit", "revive" },
            Greet = "This old mountain shrine still answers. Ghosts may return to life on its circle. I also keep a few tonics for those heading into the Hollow." },
    }.ToDictionary(n => n.Id);

    public static readonly Dictionary<string, QuestDef> Quests = new[]
    {
        new QuestDef { Id = "q_rabbits", Giver = "elder", Level = 1, Title = "Rabbits in the Paddies", KillMonster = "rabbit", Count = 8,
            Text = "The rabbits are eating the young rice south of the gate. Thin them out, eight should do.", RewardExp = 60, RewardGold = 40, RewardItems = new[] { ("herbal_tonic", 3) } },
        new QuestDef { Id = "q_acorns", Giver = "herbalist", Level = 2, Title = "Acorns for Tea", BringItem = "acorn", Count = 5,
            Text = "Squirrels in the paddies hoard acorns. Bring me five and I will brew you something.", RewardExp = 90, RewardGold = 50, RewardItems = new[] { ("spirit_tea", 3) } },
        new QuestDef { Id = "q_snakes", Giver = "guard", Level = 4, Title = "Snakes on the Dikes", KillMonster = "paddy_snake", Count = 6,
            Text = "Farmers keep getting bitten on the east dikes. Kill six paddy snakes.", RewardExp = 200, RewardGold = 80, RewardItems = new[] { ("leather_cap", 1) } },
        new QuestDef { Id = "q_wolves", Giver = "elder", Level = 7, Title = "Wolves at the Forest Edge", KillMonster = "wolf", Count = 10,
            Text = "Wolf packs have been seen on the forest road. Ten fewer wolves and the traders will come again.", RewardExp = 700, RewardGold = 200, RewardItems = new[] { ("jade_ring", 1) } },
        new QuestDef { Id = "q_tusks", Giver = "smith", Level = 9, Title = "Tusks for the Forge", BringItem = "boar_tusk", Count = 6,
            Text = "Boar tusk makes the best hilts. Bring me six and I will fit you a helm.", RewardExp = 900, RewardGold = 250, RewardItems = new[] { ("iron_helm", 1) } },
        new QuestDef { Id = "q_lord", Giver = "priest", Level = 15, Title = "The Mountain Lord", KillMonster = "mountain_lord", Count = 1,
            Text = "Horang, the tiger spirit of the Hollow, has gone wild. Bring the mountain peace. Go well prepared.", RewardExp = 3000, RewardGold = 1000, RewardItems = new[] { ("spirit_amulet", 1) } },
    }.ToDictionary(q => q.Id);

    // Townsfolk who stand in for other players until networking arrives (Phase 2).
    public static readonly FolkDef[] Folk =
    {
        new() { Name = "Haneul", Cls = "warrior", Level = 14, Look = new("#e0b088", "#1a1a1a", "topknot", "lamellar", "steel_glaive", "iron_helm", "iron_shield") },
        new() { Name = "Dalbit", Cls = "monk", Level = 12, Look = new("#f2d0b0", "#e8c098", "bald", "npc_monk", "lotus_staff") },
        new() { Name = "Sorim", Cls = "mystic", Level = 11, Look = new("#f2c9a0", "#5a3a8a", "long", "silk_robe", "spirit_staff") },
        new() { Name = "Gaon", Cls = "rogue", Level = 9, Look = new("#d9a070", "#2a1a2a", "topknot", "shadow_garb", "viper_fang", "leather_cap") },
        new() { Name = "Narae", Cls = null, Level = 3, Look = new("#f2c9a0", "#8a4a2a", "bun", "hemp_robe", "wooden_sword", "straw_hat") },
    };

    public static readonly string[] FolkShouts =
    {
        "LFG pinewood, need a monk!", "Selling wolf fangs, 40g each, whisper me", "Anyone up for the Mountain Lord? Need 2 more",
        "Buying a jade ring!!", "The cave bats hit way harder than you think...", "who keeps stealing my boar spot >:(",
        "Monk LFG, lv 12, can heal and bless", "Tip: stand on the golden circle at the shrine if you die",
        "Is the dokkaebi drop rate bugged or am I just unlucky", "Free hugs at the well",
        "Warrior lv14 looking for a party to the Hollow", "Just hit level 10 after all night in the forest!",
    };
    public static readonly string[] FolkGreetings = { "hi!", "hello o/", "welcome to Pungseong!", "hey there", "yo", "good luck out there!" };
    public static readonly string[] FolkWhispers =
    {
        "Hi! The wolves come in packs, pull them one at a time.", "sorry, AFK making tea",
        "Need a party? Sadly parties come with the online update haha", "Buy return scrolls before the Hollow. Trust me.",
        "The hermit in the pinewood sells tonics, saves the walk back.",
    };

    public static readonly string[] HairStyles = { "topknot", "bun", "long", "bald" };
}
