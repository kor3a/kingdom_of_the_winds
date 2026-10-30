namespace KotW.Shared;

// Combat math, progression and the save record. No engine code, so the
// .NET server can run exactly the same rules later.

public sealed class ItemStack
{
    public string Id { get; set; } = "";
    public int Qty { get; set; } = 1;
    public ItemStack() { }
    public ItemStack(string id, int qty = 1) { Id = id; Qty = qty; }
    public ItemStack Clone() => new(Id, Qty);
}

public sealed class QuestProgress
{
    public string State { get; set; } = "active"; // active | done
    public int Count { get; set; }
}

public sealed class StatBlock
{
    public int Str { get; set; } = 6;
    public int Dex { get; set; } = 5;
    public int Int { get; set; } = 4;
    public int Wis { get; set; } = 4;
    public int Con { get; set; } = 6;
    public int this[Stat s]
    {
        get => s switch { Stat.Str => Str, Stat.Dex => Dex, Stat.Int => Int, Stat.Wis => Wis, _ => Con };
        set { switch (s) { case Stat.Str: Str = value; break; case Stat.Dex: Dex = value; break; case Stat.Int: Int = value; break; case Stat.Wis: Wis = value; break; default: Con = value; break; } }
    }
}

// Everything that gets written to disk (and later to PostgreSQL).
public sealed class SaveData
{
    public int Version { get; set; } = 1;
    public string Name { get; set; } = "";
    public string Skin { get; set; } = "#f2c9a0";
    public string Hair { get; set; } = "#1a1a1a";
    public string Style { get; set; } = "topknot";
    public string? Cls { get; set; }
    public int Level { get; set; } = 1;
    public int Exp { get; set; }
    public StatBlock Stats { get; set; } = new();
    public int Unspent { get; set; }
    public double Hp { get; set; }
    public double Mp { get; set; }
    public int Gold { get; set; } = 30;
    public ItemStack?[] Inv { get; set; } = new ItemStack?[Rules.BagSize];
    public Dictionary<string, string?> Equip { get; set; } = new();
    public ItemStack?[] Storage { get; set; } = new ItemStack?[Rules.StorageSize];
    public Dictionary<string, QuestProgress> Quests { get; set; } = new();
    public string?[] Bar { get; set; } = new string?[Rules.SkillSlots]; // skill on each hotbar key
    public string MapId { get; set; } = "town";
    public int X { get; set; } = Rules.TownSpawnX;
    public int Y { get; set; } = Rules.TownSpawnY;
    public Dir Facing { get; set; } = Dir.Down;
    public bool Ghost { get; set; }
}

public sealed class Derived
{
    public StatBlock Stats = new();
    public int MaxHp, MaxMp, Atk, Matk, Def;
    public double Crit, Evade, AtkDelay, MoveMul = 1;
}

public static class Rules
{
    public const int MaxLevel = 100;
    public const int SkillSlots = 8;
    public const int BagSize = 24;
    public const int StorageSize = 24;
    public const int PointsPerLevel = 3;
    public const int MaxName = 12;
    public const int TownSpawnX = 6, TownSpawnY = 8;
    public static readonly Stat[] StatKeys = { Stat.Str, Stat.Dex, Stat.Int, Stat.Wis, Stat.Con };

    public static int ExpToNext(int level) => (int)Math.Floor(15 * Math.Pow(level, 1.9) + 10);

    public static Derived Derive(Player p)
    {
        int atkG = 0, matkG = 0, defG = 0, hpG = 0, mpG = 0;
        double critG = 0;
        var s = new StatBlock();
        foreach (var k in StatKeys) s[k] = p.Stats[k];
        foreach (var slot in Content.EquipSlots)
        {
            if (!p.Equip.TryGetValue(slot, out var id) || id == null) continue;
            var it = Content.Items[id];
            atkG += it.Atk; matkG += it.Matk; defG += it.Def; hpG += it.Hp; mpG += it.Mp; critG += it.Crit;
            foreach (var k in StatKeys) s[k] += it.Bonus(k);
        }
        var cls = Content.Classes[p.Cls ?? "none"];
        int lv = p.Level - 1;
        double atk = 2 + s.Str * 1.3 + s.Dex * 0.3 + atkG;
        double def = s.Con * 0.5 + defG;
        double matk = 2 + s.Int * 1.6 + s.Wis * 0.8 + matkG;
        bool Has(string b) => p.Buffs.ContainsKey(b);
        if (Has("warcry")) atk *= 1.3;
        if (Has("blessing")) { atk *= 1.1; def = def * 1.3 + 3; }
        if (Has("banner")) { atk *= 1.2; def *= 1.2; }
        if (Has("ironskin") || Has("fortress")) def *= 1.6;
        if (Has("berserk")) { atk *= 1.4; def *= 0.8; }
        if (Has("surge")) matk *= 1.4;
        if (Has("avatar")) { matk *= 1.3; def *= 1.3; }
        bool haste = Has("haste") || Has("berserk");
        return new Derived
        {
            Stats = s,
            MaxHp = (int)Math.Round(30 + s.Con * 6.0 + lv * cls.HpLv + hpG),
            MaxMp = (int)Math.Round(10 + s.Wis * 4.0 + s.Int * 2 + lv * cls.MpLv + mpG),
            Atk = (int)Math.Round(atk),
            Matk = (int)Math.Round(matk),
            Def = (int)Math.Round(def),
            Crit = Math.Min(Has("focus") ? 0.75 : 0.5, 0.03 + s.Dex * 0.006 + critG + (Has("focus") ? 0.25 : 0)),
            Evade = Math.Min(0.4, 0.02 + s.Dex * 0.004 + cls.Evade),
            AtkDelay = Math.Max(0.35, 0.75 - s.Dex * 0.006) * (haste ? 0.7 : 1),
            MoveMul = haste ? 0.75 : 1,
        };
    }

    public readonly record struct Hit(bool Miss, bool Crit, int Dmg);

    public static Hit RollMelee(Derived pd, MonsterDef mon, int playerLevel, double mult = 1, bool forceCrit = false)
    {
        double miss = Math.Clamp(0.05 + (mon.Level - playerLevel) * 0.03, 0.02, 0.4);
        if (!forceCrit && Rnd.Next() < miss) return new Hit(true, false, 0);
        bool crit = forceCrit || Rnd.Next() < pd.Crit;
        double dmg = pd.Atk * mult * (0.85 + Rnd.Next() * 0.3) - mon.Def * 0.6;
        if (crit) dmg *= 1.75;
        return new Hit(false, crit, Math.Max(1, (int)Math.Round(dmg)));
    }

    // Spells never miss but crit half as often.
    public static Hit RollSpell(Derived pd, MonsterDef mon, double power, bool forceCrit = false)
    {
        bool crit = forceCrit || Rnd.Next() < pd.Crit * 0.5;
        double dmg = pd.Matk * power * (0.9 + Rnd.Next() * 0.2) - mon.Def * 0.3;
        if (crit) dmg *= 1.6;
        return new Hit(false, crit, Math.Max(1, (int)Math.Round(dmg)));
    }

    public static Hit RollMonsterHit(MonsterDef mon, Derived pd, double mult, bool guarded, bool unavoidable)
    {
        if (!unavoidable && Rnd.Next() < pd.Evade) return new Hit(true, false, 0);
        double dmg = mon.Atk * mult * (0.85 + Rnd.Next() * 0.3) - pd.Def * 0.6;
        if (guarded) dmg *= 0.65;
        return new Hit(false, false, Math.Max(1, (int)Math.Round(dmg)));
    }

    public static int HealAmount(Derived pd) => (int)Math.Round(15 + pd.Matk * 1.3 + pd.Stats.Wis * 1.5);

    // Fighting far below your level pays less; slightly above pays a bit more.
    public static double ExpMultiplier(int playerLevel, int monLevel)
    {
        int gap = playerLevel - monLevel;
        if (gap <= 0) return Math.Min(1.3, 1 - gap * 0.05);
        if (gap <= 3) return 1;
        return Math.Max(0.1, 1 - (gap - 3) * 0.2);
    }

    public static int SellPrice(ItemDef it) => it.Sell ?? (int)Math.Floor(it.Price * 0.35);

    public static string? EquipProblem(Player p, ItemDef it)
    {
        if (it.Level > 0 && p.Level < it.Level) return $"You need to be level {it.Level} to use {it.Name}.";
        if (it.Classes != null && !Content.ClassLine(p.Cls).Any(it.Classes.Contains))
            return $"Only a {string.Join(" or ", it.Classes.Select(c => Content.Classes[c].Name))} can use {it.Name}.";
        return null;
    }

    // Character names keep letters, digits, spaces, '-' and '_'.
    public static string CleanName(string raw)
    {
        var name = new string(raw.Where(ch => char.IsLetterOrDigit(ch) || ch is ' ' or '-' or '_').ToArray()).Trim();
        return (name.Length > MaxName ? name[..MaxName] : name).TrimEnd();
    }

    public static SaveData NewCharacter(string name, string skin, string hair, string style)
    {
        var d = new SaveData { Name = name, Skin = skin, Hair = hair, Style = style };
        d.Inv[0] = new ItemStack("herbal_tonic", 5);
        d.Inv[1] = new ItemStack("spirit_tea", 2);
        d.Inv[2] = new ItemStack("straw_hat");
        foreach (var slot in Content.EquipSlots) d.Equip[slot] = null;
        d.Equip["weapon"] = "wooden_sword";
        d.Equip["armor"] = "hemp_robe";
        d.Hp = d.Mp = 99999; // clamped to the real maximum on load
        return d;
    }
}
