namespace KotW.Shared;

// Every monster, zone by zone. The first eleven are hand-tuned; the rest take their numbers
// from Mob(), which scales health, attack, defence and experience with level so a fair fight
// at any level takes about the same number of blows.
public static partial class Content
{
    private static MonsterDef Mob(string id, string name, string sprite, int level, double hp = 1, double atk = 1, double def = 1,
        int aggro = 0, double move = 0.34, double delay = 1.4, bool boss = false, string? summon = null, params (string, double)[] drops)
    {
        double bossMul = boss ? 1 : 0;
        return new MonsterDef
        {
            Id = id, Name = name, Sprite = sprite, Level = level,
            Hp = (int)Math.Round((40 + 18 * level) * hp * (boss ? 11 : 1)),
            Atk = (int)Math.Round((6 + 1.9 * level) * atk * (boss ? 1.25 : 1)),
            Def = (int)Math.Round((1 + 0.75 * level) * def * (boss ? 1.2 : 1)),
            Exp = (int)Math.Round(Rules.ExpToNext(level) / 14.0 * hp * (1 + bossMul * 14)),
            GoldMin = (int)Math.Round(level * 1.5 * (1 + bossMul * 9)), GoldMax = (int)Math.Round(level * 3 * (1 + bossMul * 9)),
            Aggressive = aggro > 0, AggroRange = aggro, MoveDur = move, AtkDelay = delay,
            Boss = boss, Respawn = boss ? 150 : 0, Summon = summon, Drops = drops,
        };
    }

    public static readonly Dictionary<string, MonsterDef> Monsters = new[]
    {
        // Gate Paddies, Whispering Pinewood, the Hollow (1-20)
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
        new MonsterDef { Id = "mountain_lord", Name = "Horang, Lord of the Mountain", Sprite = "tiger", Level = 20, Hp = 2400, Atk = 46, Def = 22, Exp = 2500, GoldMin = 400, GoldMax = 600, Aggressive = true, AggroRange = 6, Boss = true, Respawn = 120, MoveDur = 0.32, AtkDelay = 1.6, Summon = "cave_bat",
            Drops = new[] { ("tiger_whisker", 1.0), ("pearl_shard", 1.0), ("tiger_fang_blade", 0.35), ("tiger_amulet", 0.35) } },

        // Weeping Reed Marsh (18-28)
        Mob("marsh_toad", "Bog Toad", "toad", 19, hp: 1.1, move: 0.4, drops: new[] { ("toad_skin", 0.5), ("ginseng_tonic", 0.08) }),
        Mob("reed_viper", "Reed Viper", "reed_viper", 21, atk: 1.1, aggro: 4, move: 0.36, delay: 1.2, drops: new[] { ("snake_skin", 0.6), ("antler_tonic", 0.05), ("silver_ring", 0.01) }),
        Mob("marsh_bandit", "Causeway Bandit", "bandit", 23, aggro: 5, move: 0.32, drops: new[] { ("bandit_token", 0.5), ("antler_tonic", 0.06), ("stalker_garb", 0.01) }),
        Mob("marsh_wisp", "Dokkaebi Fire", "wisp", 26, hp: 0.8, atk: 1.15, def: 0.6, aggro: 5, move: 0.3, delay: 1.2, drops: new[] { ("wisp_ember", 0.5), ("pine_tea", 0.06) }),
        Mob("bandit_chief", "Ok-cheol the Causeway King", "bandit_chief", 28, hp: 0.55, aggro: 6, move: 0.32, boss: true, summon: "marsh_bandit",
            drops: new[] { ("bandit_token", 1.0), ("bronze_hwando", 0.15), ("plains_recurve", 0.15), ("five_color_fan", 0.15), ("silver_ring", 0.3) }),

        // Saltwind Coast (28-36)
        Mob("shore_crab", "Shore Crab", "crab", 29, hp: 1.1, def: 1.5, move: 0.42, delay: 1.5, drops: new[] { ("crab_claw", 0.55), ("antler_tonic", 0.06) }),
        Mob("sea_snake", "Banded Sea Snake", "sea_snake", 31, atk: 1.1, aggro: 4, move: 0.34, delay: 1.2, drops: new[] { ("sea_scale", 0.5), ("pine_tea", 0.06) }),
        Mob("pirate", "Red-Sail Pirate", "pirate", 33, aggro: 5, move: 0.32, drops: new[] { ("pirate_coin", 0.5), ("antler_tonic", 0.08), ("saltwind_leathers", 0.01), ("pearl_ring", 0.01) }),
        Mob("drowned_sailor", "Drowned Sailor", "drowned", 35, hp: 1.1, aggro: 5, move: 0.4, delay: 1.5, drops: new[] { ("drowned_charm", 0.5), ("pine_tea", 0.08), ("tide_robe", 0.01) }),

        // Drowned Shrine of the Dragon King (36-45)
        Mob("coral_golem", "Coral Golem", "coral_golem", 38, hp: 1.5, atk: 1.1, def: 1.6, aggro: 3, move: 0.55, delay: 2.0, drops: new[] { ("coral_shard", 0.55), ("coral_ring", 0.02) }),
        Mob("abyss_eel", "Abyss Eel", "abyss_eel", 39, atk: 1.15, aggro: 5, move: 0.28, delay: 1.1, drops: new[] { ("eel_skin", 0.5), ("antler_tonic", 0.08) }),
        Mob("water_ghost", "Mul-gwisin", "water_ghost", 41, hp: 0.9, atk: 1.2, def: 0.7, aggro: 6, move: 0.3, delay: 1.2, drops: new[] { ("ghost_veil", 0.5), ("pine_tea", 0.08) }),
        Mob("dragon_guard", "Dragon King's Guard", "dragon_guard", 43, hp: 1.3, def: 1.3, aggro: 5, move: 0.34, drops: new[] { ("coral_shard", 0.4), ("steel_lamellar", 0.01), ("imugi_scale", 0.004) }),
        Mob("imugi", "Imugi, the Serpent Who Was Never a Dragon", "imugi", 45, aggro: 7, move: 0.3, delay: 1.5, boss: true, summon: "abyss_eel",
            drops: new[] { ("pearl_shard", 1.0), ("eel_skin", 1.0), ("imugi_fang", 0.3), ("imugi_scale", 0.35), ("dragonscale_mail", 0.1) }),

        // Cinder Highlands (44-54)
        Mob("ash_boar", "Ashen Boar", "ash_boar", 46, hp: 1.3, def: 1.2, move: 0.36, delay: 1.5, drops: new[] { ("ash_tusk", 0.5), ("wild_ginseng", 0.04) }),
        Mob("fire_dokkaebi", "Fire Dokkaebi", "fire_dokkaebi", 48, aggro: 5, move: 0.32, drops: new[] { ("fire_horn", 0.45), ("lotus_dew", 0.05) }),
        Mob("ember_bat", "Ember Bat", "ember_bat", 50, hp: 0.8, atk: 1.15, def: 0.7, aggro: 6, move: 0.26, delay: 1.1, drops: new[] { ("ember_wing", 0.5), ("lotus_dew", 0.05) }),
        Mob("lava_golem", "Lava Golem", "lava_golem", 52, hp: 1.6, atk: 1.1, def: 1.6, aggro: 3, move: 0.55, delay: 2.0, drops: new[] { ("magma_core", 0.5), ("ember_ring", 0.015) }),
        Mob("cultist", "Black Cloud Cultist", "cultist", 53, aggro: 6, move: 0.32, drops: new[] { ("cult_sigil", 0.5), ("wild_ginseng", 0.06), ("ember_robe", 0.01) }),
        Mob("bulgasari", "Bulgasari the Iron-Eater", "bulgasari", 55, hp: 1.1, def: 1.4, aggro: 6, move: 0.4, delay: 1.7, boss: true, summon: "ash_boar",
            drops: new[] { ("pearl_shard", 1.0), ("iron_heart", 0.4), ("thunder_halberd", 0.15), ("turtle_shield", 0.15) }),

        // Frostfang Pass (55-65)
        Mob("snow_wolf", "Snow Wolf", "snow_wolf", 57, aggro: 6, move: 0.28, delay: 1.2, drops: new[] { ("snow_pelt", 0.5), ("wild_ginseng", 0.06) }),
        Mob("frost_bat", "Frost Bat", "frost_bat", 59, hp: 0.8, atk: 1.15, def: 0.7, aggro: 6, move: 0.26, delay: 1.1, drops: new[] { ("frost_wing", 0.5), ("lotus_dew", 0.06) }),
        Mob("ice_dokkaebi", "Ice Dokkaebi", "ice_dokkaebi", 61, aggro: 5, move: 0.33, drops: new[] { ("dokkaebi_horn", 0.5), ("frost_ring", 0.012) }),
        Mob("ice_golem", "Ice Golem", "ice_golem", 63, hp: 1.6, atk: 1.1, def: 1.6, aggro: 3, move: 0.55, delay: 2.0, drops: new[] { ("ice_core", 0.5), ("snowfox_coat", 0.01) }),
        Mob("cult_adept", "Black Cloud Adept", "cult_adept", 64, atk: 1.15, aggro: 6, move: 0.32, drops: new[] { ("cult_sigil", 0.6), ("wild_ginseng", 0.08), ("frostweave_robe", 0.01) }),
        Mob("white_tiger", "Baekho, the White Tiger of the West", "white_tiger", 66, aggro: 7, move: 0.3, delay: 1.5, boss: true, summon: "snow_wolf",
            drops: new[] { ("pearl_shard", 1.0), ("white_fang_necklace", 0.4), ("snowfox_cap", 0.2), ("flame_plate", 0.1) }),

        // Moonlit Bamboo Grove (65-75)
        Mob("moon_rabbit", "Moon Rabbit", "moon_rabbit", 67, hp: 0.9, move: 0.36, drops: new[] { ("moon_fur", 0.55), ("heavenly_peach", 0.03) }),
        Mob("grove_toad", "Bamboo Toad", "grove_toad", 68, hp: 1.2, move: 0.4, drops: new[] { ("toad_skin", 0.7), ("lotus_dew", 0.06) }),
        Mob("moon_fox", "Moonlight Fox", "moon_fox", 69, atk: 1.1, aggro: 5, move: 0.28, delay: 1.2, drops: new[] { ("fox_hair", 0.5), ("moonstone_ring", 0.012) }),
        Mob("egg_ghost", "Dalgyal Gwisin", "egg_ghost", 71, hp: 0.9, atk: 1.2, def: 0.8, aggro: 6, move: 0.32, drops: new[] { ("eggshell", 0.5), ("starlight_nectar", 0.03) }),
        Mob("fox_maiden", "Fox Maiden", "fox_maiden", 73, atk: 1.1, aggro: 6, move: 0.3, drops: new[] { ("fox_hair", 0.6), ("heavenly_peach", 0.05), ("night_hood", 0.01) }),
        Mob("gumiho", "The Gumiho of Nine Tails", "gumiho", 76, aggro: 7, move: 0.28, delay: 1.4, boss: true, summon: "moon_fox",
            drops: new[] { ("pearl_shard", 1.0), ("fox_bead", 0.4), ("gumiho_tail_fan", 0.3), ("moonstone_ring", 0.3) }),

        // Road of the Yellow Springs (75-85)
        Mob("plague_rat", "Grave Rat", "plague_rat", 77, hp: 0.9, aggro: 4, move: 0.32, delay: 1.2, drops: new[] { ("bone_dust", 0.5), ("heavenly_peach", 0.04) }),
        Mob("grave_wisp", "Soul Lantern Wisp", "grave_wisp", 79, hp: 0.8, atk: 1.2, def: 0.6, aggro: 6, move: 0.3, delay: 1.2, drops: new[] { ("lantern_shard", 0.5), ("starlight_nectar", 0.05) }),
        Mob("ghost_soldier", "Ghost Soldier", "ghost_soldier", 81, hp: 1.2, def: 1.3, aggro: 5, move: 0.34, drops: new[] { ("bone_dust", 0.5), ("celestial_helm", 0.005), ("moonshadow_garb", 0.008) }),
        Mob("reaper", "Jeoseung Saja", "reaper", 83, atk: 1.2, aggro: 6, move: 0.3, drops: new[] { ("reaper_cord", 0.5), ("heavenly_peach", 0.06), ("lantern_robe", 0.01) }),
        Mob("lantern_judge", "The Lantern Judge", "lantern_judge", 86, aggro: 7, move: 0.34, delay: 1.5, boss: true, summon: "ghost_soldier",
            drops: new[] { ("pearl_shard", 1.0), ("lantern_of_souls", 0.4), ("judge_ring", 0.3), ("celestial_armor", 0.1) }),

        // Ruins of the Sky Palace (85-95)
        Mob("thunder_crane", "Thunder Crane", "thunder_crane", 87, hp: 0.9, atk: 1.15, aggro: 6, move: 0.28, delay: 1.2, drops: new[] { ("crane_feather", 0.5), ("starlight_nectar", 0.06) }),
        Mob("heavenly_guard", "Bewitched Heavenly Guard", "heavenly_guard", 89, hp: 1.2, def: 1.3, aggro: 5, move: 0.32, drops: new[] { ("guard_insignia", 0.5), ("heavenly_peach", 0.06), ("sky_shield", 0.01) }),
        Mob("haetae", "Stone Haetae", "haetae", 91, hp: 1.4, def: 1.5, aggro: 4, move: 0.4, delay: 1.7, drops: new[] { ("sky_stone", 0.5), ("four_winds_ring", 0.004) }),
        Mob("sky_sentinel", "Sky Sentinel", "sky_sentinel", 93, hp: 1.6, atk: 1.1, def: 1.6, aggro: 3, move: 0.55, delay: 2.0, drops: new[] { ("sky_stone", 0.6), ("heavenly_hat", 0.01) }),
        Mob("thunder_roc", "Thunder Roc of the Ninth Heaven", "thunder_roc", 96, aggro: 8, move: 0.26, delay: 1.3, boss: true, summon: "thunder_crane",
            drops: new[] { ("pearl_shard", 1.0), ("roc_feather", 0.4), ("sky_splitter_bow", 0.12), ("nine_heavens_staff", 0.12) }),

        // Eye of the Winds (95-100)
        Mob("storm_wisp", "Storm Wisp", "storm_wisp", 96, hp: 0.9, atk: 1.2, def: 0.7, aggro: 6, move: 0.26, delay: 1.1, drops: new[] { ("storm_essence", 0.5), ("starlight_nectar", 0.06) }),
        Mob("storm_wraith", "Storm Wraith", "storm_wraith", 98, hp: 1.2, atk: 1.15, aggro: 6, move: 0.3, drops: new[] { ("storm_essence", 0.6), ("heavenly_peach", 0.08), ("four_winds_armor", 0.006), ("windrunner_raiment", 0.006), ("heaven_vestments", 0.006) }),
        Mob("heuk_un", "Heuk-un, the Black Cloud", "heuk_un", 100, hp: 1.4, atk: 1.1, aggro: 9, move: 0.3, delay: 1.4, boss: true, summon: "storm_wraith",
            drops: new[] { ("pearl_shard", 1.0), ("storm_eye_glaive", 0.35), ("four_winds_ring", 0.3), ("underworld_fan", 0.12), ("first_wind_sword", 0.12) }),
    }.ToDictionary(m => m.Id);
}
