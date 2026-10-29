namespace KotW.Shared;

// Classes and their skills. Six base classes are chosen at level 5; each has an advanced
// class earned at level 50 that keeps the base skills and adds five more.
public static partial class Content
{
    public const int AdvanceLevel = 50;

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
        new ClassDef { Id = "hunter", Name = "Hunter", Role = "Ranged physical damage", HpLv = 9, MpLv = 5, Color = "#8ac860", Evade = 0.06, Reach = 5,
            Weapon = "hunting_bow", Recommend = "DEX and STR",
            Desc = "Fights from five tiles away with a bow: every basic attack is an arrow. Pins foes in place and rains arrows on packs." },
        new ClassDef { Id = "shaman", Name = "Shaman", Role = "Curses, life drain", HpLv = 8, MpLv = 9, Color = "#e0607a",
            Weapon = "paper_fan", Recommend = "INT and WIS",
            Desc = "Dances with the spirits of the dead. Curses foes so they take more harm, weakens their blows and drinks their life to heal." },

        new ClassDef { Id = "warlord", Base = "warrior", Trial = "t_warrior", Name = "Warlord", Role = "Front-line commander", HpLv = 18, MpLv = 4, Color = "#f0503a",
            Recommend = "STR and CON", Desc = "A general who turns the tide alone. Raises a war banner, cleaves whole ranks and cannot be broken." },
        new ClassDef { Id = "shadowblade", Base = "rogue", Trial = "t_rogue", Name = "Shadowblade", Role = "Assassin", HpLv = 12, MpLv = 5, Color = "#9a6ae0", Evade = 0.16,
            Recommend = "DEX and STR", Desc = "Marks a victim for death, dances through them with five cuts and finishes the wounded in one stroke." },
        new ClassDef { Id = "stormcaller", Base = "mystic", Trial = "t_mystic", Name = "Stormcaller", Role = "Elemental devastation", HpLv = 7, MpLv = 13, Color = "#6a90ff",
            Recommend = "INT and WIS", Desc = "Lightning leaps from foe to foe, frost bursts around them and fire falls from the sky." },
        new ClassDef { Id = "lotus_saint", Base = "monk", Trial = "t_monk", Name = "Lotus Saint", Role = "Holy guardian", HpLv = 12, MpLv = 10, Color = "#f8d878",
            Recommend = "WIS and CON", Desc = "Mercy made flesh. Blooms holy light that harms foes and heals the saint, and opens the gate of heaven itself." },
        new ClassDef { Id = "windstrider", Base = "hunter", Trial = "t_hunter", Name = "Windstrider", Role = "Master archer", HpLv = 11, MpLv = 6, Color = "#5ad8b0", Evade = 0.1, Reach = 6,
            Recommend = "DEX and STR", Desc = "Arrows that blow foes off their feet, storms of shafts and one shot that splits the sky. Basic attacks reach six tiles." },
        new ClassDef { Id = "grand_shaman", Base = "shaman", Trial = "t_shaman", Name = "Grand Shaman", Role = "Spirit sovereign", HpLv = 10, MpLv = 12, Color = "#ff5a8a",
            Recommend = "INT and WIS", Desc = "Leads the great gut, the ritual that binds the dead. Siphons life from many foes and opens the gate of the underworld." },
    }.ToDictionary(c => c.Id);

    // The class and its base, so advanced classes keep base skills and gear.
    public static string[] ClassLine(string? cls) =>
        cls == null ? Array.Empty<string>() : Classes[cls].Base is { } b ? new[] { cls, b } : new[] { cls };

    public static string BaseClass(string cls) => Classes[cls].Base ?? cls;

    public static ClassDef? AdvancedOf(string baseCls) => Classes.Values.FirstOrDefault(c => c.Base == baseCls);

    public static readonly Dictionary<string, SkillDef> Skills = new[]
    {
        // ---- Warrior / Warlord ----
        new SkillDef { Id = "heavy_strike", Cls = "warrior", Level = 5, Name = "Heavy Strike", Glyph = "擊", Color = "#d0583e", Mp = 6, Cd = 3, Type = SkillType.Melee, Mult = 2.1, Desc = "A crushing blow on the foe in front of you." },
        new SkillDef { Id = "taunt", Cls = "warrior", Level = 7, Name = "Taunt", Glyph = "挑", Color = "#c08040", Mp = 8, Cd = 12, Type = SkillType.Self, Effect = "taunt", Radius = 5, Dur = 8, Desc = "Pull every foe within 5 tiles onto you and take 35% less damage for 8s." },
        new SkillDef { Id = "war_cry", Cls = "warrior", Level = 10, Name = "War Cry", Glyph = "吼", Color = "#e07030", Mp = 12, Cd = 30, Type = SkillType.Self, Effect = "warcry", Dur = 20, Desc = "+30% attack for 20s." },
        new SkillDef { Id = "whirlwind", Cls = "warrior", Level = 14, Name = "Whirlwind", Glyph = "旋", Color = "#f0a040", Mp = 15, Cd = 6, Type = SkillType.Around, Mult = 1.5, Desc = "Strike every foe around you." },
        new SkillDef { Id = "shield_bash", Cls = "warrior", Level = 20, Name = "Shield Bash", Glyph = "盾", Color = "#a8b0c0", Mp = 14, Cd = 9, Type = SkillType.Melee, Mult = 1.4, Effect = "stun", Dur = 2, Knock = 1, Desc = "Slam the foe back a tile and stun it for 2s." },
        new SkillDef { Id = "iron_skin", Cls = "warrior", Level = 27, Name = "Iron Skin", Glyph = "鐵", Color = "#8a96a8", Mp = 20, Cd = 40, Type = SkillType.Self, Effect = "ironskin", Dur = 15, Desc = "+60% defense for 15s." },
        new SkillDef { Id = "earthsplitter", Cls = "warrior", Level = 35, Name = "Earthsplitter", Glyph = "裂", Color = "#c08a50", Mp = 26, Cd = 8, Type = SkillType.Line, Phys = true, Range = 4, Power = 2.0, Fx = "beam", Desc = "Split the ground ahead: hits every foe in a line 4 tiles long." },
        new SkillDef { Id = "charge", Cls = "warrior", Level = 44, Name = "Charge", Glyph = "突", Color = "#e8603a", Mp = 24, Cd = 10, Type = SkillType.Dash, Range = 5, Mult = 1.8, Effect = "stun", Dur = 1, Desc = "Rush to a foe up to 5 tiles away, strike it and stun it for 1s." },
        new SkillDef { Id = "war_banner", Cls = "warlord", Level = 50, Name = "War Banner", Glyph = "旗", Color = "#d83a2a", Mp = 40, Cd = 45, Type = SkillType.Self, Effect = "banner", Dur = 25, Desc = "Plant the banner: +20% attack, +20% defense and steady healing for 25s." },
        new SkillDef { Id = "cleave_ranks", Cls = "warlord", Level = 60, Name = "Cleave the Ranks", Glyph = "斬", Color = "#f07848", Mp = 38, Cd = 7, Type = SkillType.Around, Radius = 2, Mult = 2.4, Desc = "A sweeping cut that hits every foe within 2 tiles." },
        new SkillDef { Id = "unbreakable", Cls = "warlord", Level = 70, Name = "Unbreakable", Glyph = "堅", Color = "#c8d0e0", Mp = 50, Cd = 60, Type = SkillType.Self, Effect = "barrier", Power = 0.4, Dur = 20, Desc = "A barrier that soaks up damage equal to 40% of your health, for up to 20s." },
        new SkillDef { Id = "execution", Cls = "warlord", Level = 80, Name = "Execution", Glyph = "誅", Color = "#a82020", Mp = 45, Cd = 12, Type = SkillType.Melee, Mult = 3.0, Execute = 0.3, Desc = "A brutal blow. Deals double damage to a foe below 30% health." },
        new SkillDef { Id = "generals_wrath", Cls = "warlord", Level = 90, Name = "General's Wrath", Glyph = "霸", Color = "#ff6030", Mp = 70, Cd = 20, Type = SkillType.Around, Radius = 3, Mult = 3.2, Effect = "stun", Dur = 2, Fx = "nova", Desc = "The ground shakes: hits every foe within 3 tiles and stuns them for 2s." },

        // ---- Rogue / Shadowblade ----
        new SkillDef { Id = "backstab", Cls = "rogue", Level = 5, Name = "Backstab", Glyph = "刺", Color = "#9a72d8", Mp = 5, Cd = 4, Type = SkillType.Melee, Mult = 1.6, BehindMult = 2.8, Desc = "Stab the foe in reach. Much stronger from behind or against an unaware foe." },
        new SkillDef { Id = "poison_blade", Cls = "rogue", Level = 7, Name = "Poison Blade", Glyph = "毒", Color = "#60c050", Mp = 8, Cd = 8, Type = SkillType.Melee, Mult = 1.0, Effect = "poison", Dur = 6, Desc = "Strike and poison the foe for 6s." },
        new SkillDef { Id = "stealth", Cls = "rogue", Level = 10, Name = "Shadow Veil", Glyph = "隱", Color = "#6a5a90", Mp = 10, Cd = 20, Type = SkillType.Self, Effect = "stealth", Dur = 6, Desc = "Vanish for 6s. Foes lose track of you and your next strike always crits." },
        new SkillDef { Id = "flurry", Cls = "rogue", Level = 14, Name = "Flurry", Glyph = "亂", Color = "#c070e0", Mp = 14, Cd = 7, Type = SkillType.Melee, Hits = 3, Mult = 0.9, Desc = "Three quick strikes on the foe in reach." },
        new SkillDef { Id = "smoke_bomb", Cls = "rogue", Level = 20, Name = "Smoke Bomb", Glyph = "煙", Color = "#8a8a9a", Mp = 16, Cd = 18, Type = SkillType.Around, Radius = 2, Mult = 0.6, Effect = "weak", Dur = 8, Fx = "smoke", Desc = "Choking smoke: foes within 2 tiles deal 30% less damage for 8s." },
        new SkillDef { Id = "shadowstep", Cls = "rogue", Level = 27, Name = "Shadowstep", Glyph = "影", Color = "#5a4a80", Mp = 18, Cd = 10, Type = SkillType.Dash, Range = 6, Mult = 1.4, BehindMult = 2.6, Desc = "Step through the shadows behind a foe up to 6 tiles away and stab it in the back." },
        new SkillDef { Id = "focus", Cls = "rogue", Level = 35, Name = "Killer's Focus", Glyph = "眼", Color = "#e0c050", Mp = 20, Cd = 35, Type = SkillType.Self, Effect = "focus", Dur = 15, Desc = "+25% critical chance for 15s." },
        new SkillDef { Id = "fan_of_knives", Cls = "rogue", Level = 44, Name = "Fan of Knives", Glyph = "扇", Color = "#a0e070", Mp = 26, Cd = 9, Type = SkillType.Around, Radius = 2, Mult = 1.6, Effect = "poison", Dur = 6, Desc = "Knives in every direction: hits and poisons every foe within 2 tiles." },
        new SkillDef { Id = "death_mark", Cls = "shadowblade", Level = 50, Name = "Death Mark", Glyph = "標", Color = "#b02050", Mp = 30, Cd = 14, Type = SkillType.Target, Phys = true, Range = 5, Power = 1.2, Effect = "curse", Dur = 10, Fx = "spirit", Desc = "Throw a marked blade: the foe takes 25% more damage from everything for 10s." },
        new SkillDef { Id = "blade_dance", Cls = "shadowblade", Level = 60, Name = "Blade Dance", Glyph = "舞", Color = "#d080f0", Mp = 34, Cd = 9, Type = SkillType.Melee, Hits = 5, Mult = 0.95, Desc = "Five whirling cuts on the foe in reach." },
        new SkillDef { Id = "haste", Cls = "shadowblade", Level = 70, Name = "Quicksilver", Glyph = "疾", Color = "#80e0f0", Mp = 30, Cd = 40, Type = SkillType.Self, Effect = "haste", Dur = 15, Desc = "Move a third faster and attack 30% faster for 15s." },
        new SkillDef { Id = "assassinate", Cls = "shadowblade", Level = 80, Name = "Assassinate", Glyph = "殺", Color = "#601830", Mp = 45, Cd = 14, Type = SkillType.Melee, Mult = 2.2, BehindMult = 4, Execute = 0.35, Desc = "Huge damage from behind; double again on a foe below 35% health." },
        new SkillDef { Id = "thousand_cuts", Cls = "shadowblade", Level = 90, Name = "Thousand Cuts", Glyph = "千", Color = "#e050c0", Mp = 60, Cd = 18, Type = SkillType.Around, Radius = 2, Hits = 3, Mult = 1.3, Desc = "Three blinding passes that cut every foe within 2 tiles." },

        // ---- Mystic / Stormcaller ----
        new SkillDef { Id = "fire_bolt", Cls = "mystic", Level = 5, Name = "Fire Bolt", Glyph = "火", Color = "#f06030", Mp = 6, Cd = 1.2, Type = SkillType.Target, Range = 7, Power = 1.7, Fx = "fire", Desc = "Hurl fire at your target." },
        new SkillDef { Id = "frost_shard", Cls = "mystic", Level = 7, Name = "Frost Shard", Glyph = "氷", Color = "#70c8f0", Mp = 9, Cd = 3, Type = SkillType.Target, Range = 7, Power = 1.3, Effect = "slow", Dur = 4, Fx = "ice", Desc = "Ice that damages and slows the target for 4s." },
        new SkillDef { Id = "paralyze", Cls = "mystic", Level = 10, Name = "Paralyze", Glyph = "麻", Color = "#e8d040", Mp = 12, Cd = 15, Type = SkillType.Target, Range = 6, Power = 0.4, Effect = "stun", Dur = 3, Fx = "paralyze", Desc = "Freeze the target in place for 3s (1s on bosses)." },
        new SkillDef { Id = "lightning", Cls = "mystic", Level = 14, Name = "Thunder Call", Glyph = "雷", Color = "#b0a0ff", Mp = 20, Cd = 6, Type = SkillType.Target, Range = 7, Power = 1.5, Radius = 2, Fx = "lightning", Desc = "Lightning strikes the target and every foe within 2 tiles of it." },
        new SkillDef { Id = "flame_wave", Cls = "mystic", Level = 20, Name = "Flame Wave", Glyph = "炎", Color = "#f08030", Mp = 22, Cd = 5, Type = SkillType.Line, Range = 5, Power = 1.6, Effect = "burn", Dur = 5, Fx = "beam", Desc = "A wall of fire rolls 5 tiles ahead, burning every foe it touches for 5s." },
        new SkillDef { Id = "mana_shield", Cls = "mystic", Level = 27, Name = "Mana Shield", Glyph = "護", Color = "#6080e0", Mp = 25, Cd = 40, Type = SkillType.Self, Effect = "manashield", Dur = 20, Desc = "For 20s, half the damage you take drains your mana instead of your health." },
        new SkillDef { Id = "blizzard", Cls = "mystic", Level = 35, Name = "Blizzard", Glyph = "雪", Color = "#c0e8ff", Mp = 32, Cd = 8, Type = SkillType.Target, Range = 7, Radius = 2, Power = 1.5, Effect = "slow", Dur = 5, Fx = "ice", Desc = "Freezing wind around the target: damages and slows every foe within 2 tiles." },
        new SkillDef { Id = "meteor", Cls = "mystic", Level = 44, Name = "Meteor", Glyph = "隕", Color = "#ff5020", Mp = 40, Cd = 12, Type = SkillType.Target, Range = 7, Radius = 2, Power = 2.4, Effect = "burn", Dur = 5, Fx = "fire", Desc = "A falling star: huge damage within 2 tiles of the target, and they burn." },
        new SkillDef { Id = "chain_lightning", Cls = "stormcaller", Level = 50, Name = "Chain Lightning", Glyph = "鎖", Color = "#a0b0ff", Mp = 36, Cd = 5, Type = SkillType.Target, Range = 7, Chain = 4, Power = 1.8, Fx = "lightning", Desc = "Lightning that leaps from the target to four more foes nearby." },
        new SkillDef { Id = "frost_nova", Cls = "stormcaller", Level = 60, Name = "Frost Nova", Glyph = "凍", Color = "#a0e0ff", Mp = 40, Cd = 12, Type = SkillType.Around, Radius = 2, Power = 1.8, Effect = "root", Dur = 4, Fx = "nova", Desc = "Ice bursts out around you: damages foes within 2 tiles and freezes their feet for 4s." },
        new SkillDef { Id = "spell_surge", Cls = "stormcaller", Level = 70, Name = "Spell Surge", Glyph = "湧", Color = "#c080ff", Mp = 30, Cd = 45, Type = SkillType.Self, Effect = "surge", Dur = 15, Desc = "+40% magic power for 15s." },
        new SkillDef { Id = "inferno", Cls = "stormcaller", Level = 80, Name = "Inferno", Glyph = "焰", Color = "#ff4010", Mp = 60, Cd = 14, Type = SkillType.Target, Range = 7, Radius = 3, Power = 3.0, Effect = "burn", Dur = 6, Fx = "fire", Desc = "A sea of fire within 3 tiles of the target." },
        new SkillDef { Id = "nine_heavens", Cls = "stormcaller", Level = 90, Name = "Wrath of the Nine Heavens", Glyph = "天", Color = "#ffffff", Mp = 90, Cd = 25, Type = SkillType.Target, Range = 8, Radius = 4, Power = 3.6, Effect = "stun", Dur = 2, Fx = "lightning", Desc = "Heaven's storm falls within 4 tiles of the target and stuns everything it touches." },

        // ---- Monk / Lotus Saint ----
        new SkillDef { Id = "smite", Cls = "monk", Level = 5, Name = "Smite", Glyph = "光", Color = "#f0d060", Mp = 5, Cd = 1.5, Type = SkillType.Target, Range = 6, Power = 1.3, Fx = "holy", Desc = "A beam of holy light on your target." },
        new SkillDef { Id = "heal", Cls = "monk", Level = 5, Name = "Heal", Glyph = "治", Color = "#70e090", Mp = 8, Cd = 2, Type = SkillType.Self, Effect = "heal", Desc = "Restore your health. Scales with Wisdom." },
        new SkillDef { Id = "blessing", Cls = "monk", Level = 8, Name = "Blessing", Glyph = "福", Color = "#f0a0c0", Mp = 12, Cd = 30, Type = SkillType.Self, Effect = "blessing", Dur = 25, Desc = "+30% defense, +10% attack and faster regeneration for 25s." },
        new SkillDef { Id = "resurrect", Cls = "monk", Level = 12, Name = "Resurrect", Glyph = "魂", Color = "#e0e0ff", Mp = 0, Cd = 60, Type = SkillType.Ghost, Desc = "While a ghost, call your spirit back into your body. (With other players this will revive them.)" },
        new SkillDef { Id = "spring_of_life", Cls = "monk", Level = 20, Name = "Spring of Life", Glyph = "泉", Color = "#60d0a0", Mp = 18, Cd = 25, Type = SkillType.Self, Effect = "regen", Dur = 15, Desc = "Healing wells up: restore 5% of your health every 2s for 15s." },
        new SkillDef { Id = "holy_barrier", Cls = "monk", Level = 27, Name = "Holy Barrier", Glyph = "障", Color = "#f8f0b0", Mp = 24, Cd = 35, Type = SkillType.Self, Effect = "barrier", Power = 0.3, Dur = 20, Desc = "A shell of light that soaks up damage equal to 30% of your health." },
        new SkillDef { Id = "judgment", Cls = "monk", Level = 35, Name = "Judgment", Glyph = "審", Color = "#ffe080", Mp = 28, Cd = 6, Type = SkillType.Target, Range = 6, Radius = 1, Power = 2.2, Fx = "holy", Desc = "Heaven's verdict on the target and every foe beside it." },
        new SkillDef { Id = "spirit_palm", Cls = "monk", Level = 44, Name = "Spirit Palm", Glyph = "掌", Color = "#f0c060", Mp = 22, Cd = 8, Type = SkillType.Melee, Mult = 1.8, Knock = 3, Effect = "stun", Dur = 1, Desc = "An open-palm strike that throws the foe back 3 tiles and stuns it." },
        new SkillDef { Id = "lotus_bloom", Cls = "lotus_saint", Level = 50, Name = "Lotus Bloom", Glyph = "蓮", Color = "#f8a0c8", Mp = 40, Cd = 8, Type = SkillType.Around, Radius = 3, Power = 2.0, Drain = 0.3, Fx = "nova", Desc = "A lotus of light opens: holy damage within 3 tiles, and you heal for 30% of it." },
        new SkillDef { Id = "sanctuary", Cls = "lotus_saint", Level = 60, Name = "Sanctuary", Glyph = "聖", Color = "#fff4c0", Mp = 45, Cd = 50, Type = SkillType.Self, Effect = "sanctuary", Dur = 10, Desc = "Take half damage from everything for 10s." },
        new SkillDef { Id = "radiant_smite", Cls = "lotus_saint", Level = 70, Name = "Radiant Smite", Glyph = "輝", Color = "#ffe060", Mp = 38, Cd = 5, Type = SkillType.Target, Range = 7, Chain = 3, Power = 2.4, Fx = "holy", Desc = "Holy light on the target and three more foes nearby." },
        new SkillDef { Id = "avatar_of_mercy", Cls = "lotus_saint", Level = 80, Name = "Avatar of Mercy", Glyph = "慈", Color = "#f0e0ff", Mp = 60, Cd = 60, Type = SkillType.Self, Effect = "avatar", Dur = 25, Desc = "+30% magic power, +30% defense and steady healing for 25s." },
        new SkillDef { Id = "heavens_gate", Cls = "lotus_saint", Level = 90, Name = "Heaven's Gate", Glyph = "門", Color = "#ffffff", Mp = 85, Cd = 22, Type = SkillType.Target, Range = 7, Radius = 4, Power = 3.4, Effect = "stun", Dur = 1.5, Fx = "holy", Desc = "The gate of heaven opens over the target: holy fire within 4 tiles." },

        // ---- Hunter / Windstrider ----
        new SkillDef { Id = "aimed_shot", Cls = "hunter", Level = 5, Name = "Aimed Shot", Glyph = "射", Color = "#a0c860", Mp = 6, Cd = 2.5, Type = SkillType.Target, Phys = true, Range = 7, Power = 1.8, Fx = "arrow", Desc = "A careful shot at a target up to 7 tiles away." },
        new SkillDef { Id = "pinning_shot", Cls = "hunter", Level = 7, Name = "Pinning Shot", Glyph = "釘", Color = "#c0a070", Mp = 9, Cd = 10, Type = SkillType.Target, Phys = true, Range = 7, Power = 1.0, Effect = "root", Dur = 4, Fx = "arrow", Desc = "Pin the target's foot to the ground: it cannot move for 4s." },
        new SkillDef { Id = "hawk_eye", Cls = "hunter", Level = 10, Name = "Hawk Eye", Glyph = "鷹", Color = "#e0c060", Mp = 12, Cd = 30, Type = SkillType.Self, Effect = "focus", Dur = 15, Desc = "+25% critical chance for 15s." },
        new SkillDef { Id = "multishot", Cls = "hunter", Level = 14, Name = "Multishot", Glyph = "多", Color = "#90e070", Mp = 15, Cd = 5, Type = SkillType.Target, Phys = true, Range = 6, Chain = 2, Power = 1.2, Fx = "arrow", Desc = "Three arrows at once: the target and two more foes nearby." },
        new SkillDef { Id = "piercing_arrow", Cls = "hunter", Level = 20, Name = "Piercing Arrow", Glyph = "貫", Color = "#d0e0a0", Mp = 20, Cd = 6, Type = SkillType.Line, Phys = true, Range = 7, Power = 1.7, Fx = "beam", Desc = "An arrow that passes through every foe in a line 7 tiles long." },
        new SkillDef { Id = "fleet_foot", Cls = "hunter", Level = 27, Name = "Fleet Foot", Glyph = "走", Color = "#80d0e0", Mp = 18, Cd = 35, Type = SkillType.Self, Effect = "haste", Dur = 15, Desc = "Move a third faster and shoot 30% faster for 15s." },
        new SkillDef { Id = "venom_arrow", Cls = "hunter", Level = 35, Name = "Venom Arrow", Glyph = "蛇", Color = "#60c050", Mp = 22, Cd = 7, Type = SkillType.Target, Phys = true, Range = 7, Power = 1.4, Effect = "poison", Dur = 8, Fx = "arrow", Desc = "A poisoned arrowhead: hits and poisons the target for 8s." },
        new SkillDef { Id = "rain_of_arrows", Cls = "hunter", Level = 44, Name = "Rain of Arrows", Glyph = "雨", Color = "#b0d080", Mp = 32, Cd = 10, Type = SkillType.Target, Phys = true, Range = 7, Radius = 2, Power = 1.7, Fx = "arrow", Desc = "Arrows fall on every foe within 2 tiles of the target." },
        new SkillDef { Id = "gale_shot", Cls = "windstrider", Level = 50, Name = "Gale Shot", Glyph = "颶", Color = "#90f0d0", Mp = 30, Cd = 7, Type = SkillType.Target, Phys = true, Range = 8, Power = 2.4, Knock = 3, Fx = "arrow", Desc = "An arrow wrapped in wind that hurls the target 3 tiles back." },
        new SkillDef { Id = "arrow_storm", Cls = "windstrider", Level = 60, Name = "Storm of Arrows", Glyph = "嵐", Color = "#70e0b0", Mp = 45, Cd = 11, Type = SkillType.Target, Phys = true, Range = 8, Radius = 3, Power = 2.1, Effect = "slow", Dur = 4, Fx = "arrow", Desc = "A storm of shafts within 3 tiles of the target. Survivors are slowed." },
        new SkillDef { Id = "wind_walk", Cls = "windstrider", Level = 70, Name = "Wind Walk", Glyph = "風", Color = "#c0fff0", Mp = 30, Cd = 30, Type = SkillType.Self, Effect = "windwalk", Dur = 8, Desc = "Become the wind: vanish and move faster for 8s. Your next shot always crits." },
        new SkillDef { Id = "heartseeker", Cls = "windstrider", Level = 80, Name = "Heartseeker", Glyph = "心", Color = "#ff6070", Mp = 45, Cd = 12, Type = SkillType.Target, Phys = true, Range = 9, Power = 3.2, Execute = 0.3, Fx = "arrow", Desc = "An arrow for the heart. Double damage on a foe below 30% health." },
        new SkillDef { Id = "sky_splitter", Cls = "windstrider", Level = 90, Name = "Sky-Splitting Arrow", Glyph = "破", Color = "#ffffff", Mp = 75, Cd = 18, Type = SkillType.Line, Phys = true, Range = 10, Power = 4.0, Fx = "beam", Desc = "One arrow that tears the sky: hits every foe in a line 10 tiles long." },

        // ---- Shaman / Grand Shaman ----
        new SkillDef { Id = "spirit_lash", Cls = "shaman", Level = 5, Name = "Spirit Lash", Glyph = "靈", Color = "#c060e0", Mp = 6, Cd = 1.5, Type = SkillType.Target, Range = 6, Power = 1.5, Fx = "spirit", Desc = "A restless spirit lashes your target." },
        new SkillDef { Id = "hex", Cls = "shaman", Level = 7, Name = "Hex", Glyph = "呪", Color = "#8a3aa0", Mp = 9, Cd = 10, Type = SkillType.Target, Range = 6, Power = 0.4, Effect = "curse", Dur = 10, Fx = "spirit", Desc = "Curse the target: it takes 25% more damage from everything for 10s." },
        new SkillDef { Id = "ancestral_ward", Cls = "shaman", Level = 10, Name = "Ancestral Ward", Glyph = "祖", Color = "#d0a060", Mp = 14, Cd = 35, Type = SkillType.Self, Effect = "ironskin", Dur = 15, Desc = "Your ancestors stand guard: +60% defense for 15s." },
        new SkillDef { Id = "soul_drain", Cls = "shaman", Level = 14, Name = "Soul Drain", Glyph = "吸", Color = "#e04060", Mp = 14, Cd = 4, Type = SkillType.Target, Range = 6, Power = 1.4, Drain = 0.5, Fx = "drain", Desc = "Tear out part of the target's soul and heal for half the damage dealt." },
        new SkillDef { Id = "wither", Cls = "shaman", Level = 20, Name = "Wither", Glyph = "枯", Color = "#a08040", Mp = 18, Cd = 9, Type = SkillType.Target, Range = 6, Power = 0.8, Effect = "weak", Dur = 10, Fx = "spirit", Desc = "Rot the target's strength: it deals 30% less damage for 10s." },
        new SkillDef { Id = "spirit_dance", Cls = "shaman", Level = 27, Name = "Spirit Dance", Glyph = "踊", Color = "#f080a0", Mp = 24, Cd = 8, Type = SkillType.Around, Radius = 2, Power = 1.5, Effect = "slow", Dur = 4, Fx = "nova", Desc = "Dance with the dead: spirits swirl out 2 tiles, harming and slowing every foe." },
        new SkillDef { Id = "ghost_chains", Cls = "shaman", Level = 35, Name = "Ghost Chains", Glyph = "縛", Color = "#9090c0", Mp = 26, Cd = 12, Type = SkillType.Target, Range = 6, Radius = 1, Power = 1.3, Effect = "root", Dur = 5, Fx = "spirit", Desc = "Spectral chains bind the target and the foes beside it in place for 5s." },
        new SkillDef { Id = "blood_rite", Cls = "shaman", Level = 44, Name = "Blood Rite", Glyph = "血", Color = "#c02040", Mp = 25, Cd = 40, Type = SkillType.Self, Effect = "surge", Dur = 15, Desc = "Offer blood to the spirits: +40% magic power for 15s." },
        new SkillDef { Id = "spirit_storm", Cls = "grand_shaman", Level = 50, Name = "Spirit Storm", Glyph = "魄", Color = "#d070ff", Mp = 38, Cd = 7, Type = SkillType.Target, Range = 7, Radius = 2, Power = 2.2, Effect = "curse", Dur = 8, Fx = "spirit", Desc = "A storm of ghosts within 2 tiles of the target. Everything it touches is cursed." },
        new SkillDef { Id = "life_siphon", Cls = "grand_shaman", Level = 60, Name = "Life Siphon", Glyph = "命", Color = "#ff4070", Mp = 40, Cd = 8, Type = SkillType.Target, Range = 7, Chain = 3, Power = 1.9, Drain = 0.4, Fx = "drain", Desc = "Drink the life of the target and three foes nearby, healing for 40% of it." },
        new SkillDef { Id = "gut_of_protection", Cls = "grand_shaman", Level = 70, Name = "Gut of Protection", Glyph = "巫", Color = "#ffd080", Mp = 45, Cd = 50, Type = SkillType.Self, Effect = "sanctuary", Dur = 10, Desc = "The great ritual: the spirits take half of every blow for 10s." },
        new SkillDef { Id = "call_of_the_dead", Cls = "grand_shaman", Level = 80, Name = "Call of the Dead", Glyph = "冥", Color = "#7040a0", Mp = 60, Cd = 14, Type = SkillType.Target, Range = 7, Radius = 3, Power = 2.8, Effect = "weak", Dur = 8, Fx = "spirit", Desc = "The dead rise around the target: heavy damage within 3 tiles, and their blows weaken." },
        new SkillDef { Id = "underworld_gate", Cls = "grand_shaman", Level = 90, Name = "Underworld Gate", Glyph = "獄", Color = "#ff2060", Mp = 85, Cd = 22, Type = SkillType.Around, Radius = 4, Power = 3.2, Drain = 0.2, Effect = "stun", Dur = 2, Fx = "nova", Desc = "Open the gate of the dead beneath you: every foe within 4 tiles is dragged down and stunned, and you drink their life." },
    }.ToDictionary(s => s.Id);
}
