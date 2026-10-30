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
            Desc = "Stands at the front so the party can fight. Roars challenges, stands like a mountain and falls on foes like a meteor." },
        new ClassDef { Id = "rogue", Name = "Rogue", Role = "Burst melee, evasion", HpLv = 10, MpLv = 4, Color = "#b08ae8", Evade = 0.12,
            Weapon = "dagger", Recommend = "DEX and STR",
            Desc = "Strikes from the shadows. Poisons, vanishes and reappears behind its prey. Dodges more often." },
        new ClassDef { Id = "mystic", Name = "Mystic", Role = "Ranged magic, area damage", HpLv = 6, MpLv = 10, Color = "#5fa8f0",
            Weapon = "oak_staff", Recommend = "INT and WIS",
            Desc = "Commands foxfire, frost and thunder from range, breathes flame like a dragon and calls down meteors." },
        new ClassDef { Id = "monk", Name = "Monk", Role = "Healer, support", HpLv = 9, MpLv = 8, Color = "#f0c860",
            Weapon = "pilgrim_staff", Recommend = "WIS and CON",
            Desc = "Heals, blesses and sears foes with pillars of light. Can call their own spirit back from death." },
        new ClassDef { Id = "hunter", Name = "Hunter", Role = "Ranged physical damage", HpLv = 9, MpLv = 5, Color = "#8ac860", Evade = 0.06, Reach = 5,
            Weapon = "hunting_bow", Recommend = "DEX and STR",
            Desc = "Fights from five tiles away with a bow: every basic attack is an arrow. Snares foes in place and rains arrows on packs." },
        new ClassDef { Id = "shaman", Name = "Shaman", Role = "Curses, life drain", HpLv = 8, MpLv = 9, Color = "#e0607a",
            Weapon = "paper_fan", Recommend = "INT and WIS",
            Desc = "Dances with the spirits of the dead. Hexes foes so they take more harm, chains them with grave iron and drinks their life." },

        new ClassDef { Id = "warlord", Base = "warrior", Trial = "t_warrior", Name = "Warlord", Role = "Front-line commander", HpLv = 18, MpLv = 4, Color = "#f0503a",
            Recommend = "STR and CON", Desc = "A general who turns the tide alone. Raises a war banner, cleaves whole ranks and drags the battlefield to their feet." },
        new ClassDef { Id = "shadowblade", Base = "rogue", Trial = "t_rogue", Name = "Shadowblade", Role = "Assassin", HpLv = 12, MpLv = 5, Color = "#9a6ae0", Evade = 0.16,
            Recommend = "DEX and STR", Desc = "Marks a victim for death, dances through them in a storm of petals and blinks behind the wounded to finish them." },
        new ClassDef { Id = "stormcaller", Base = "mystic", Trial = "t_mystic", Name = "Stormcaller", Role = "Elemental devastation", HpLv = 7, MpLv = 13, Color = "#6a90ff",
            Recommend = "INT and WIS", Desc = "Lightning leaps from foe to foe, the air freezes solid and a firebird falls from the sky." },
        new ClassDef { Id = "lotus_saint", Base = "monk", Trial = "t_monk", Name = "Lotus Saint", Role = "Holy guardian", HpLv = 12, MpLv = 10, Color = "#f8d878",
            Recommend = "WIS and CON", Desc = "Mercy made flesh. Blooms holy light that harms foes and heals the saint, and opens the gate of heaven itself." },
        new ClassDef { Id = "windstrider", Base = "hunter", Trial = "t_hunter", Name = "Windstrider", Role = "Master archer", HpLv = 11, MpLv = 6, Color = "#5ad8b0", Evade = 0.1, Reach = 6,
            Recommend = "DEX and STR", Desc = "Typhoon arrows that blow foes off their feet, storms of shafts and one shot that splits the sky. Basic attacks reach six tiles." },
        new ClassDef { Id = "grand_shaman", Base = "shaman", Trial = "t_shaman", Name = "Grand Shaman", Role = "Spirit sovereign", HpLv = 10, MpLv = 12, Color = "#ff5a8a",
            Recommend = "INT and WIS", Desc = "Leads the great gut, the ritual that binds the dead. Marches a parade of ghosts through foes and opens the gate of the underworld." },
    }.ToDictionary(c => c.Id);

    // The class and its base, so advanced classes keep base skills and gear.
    public static string[] ClassLine(string? cls) =>
        cls == null ? Array.Empty<string>() : Classes[cls].Base is { } b ? new[] { cls, b } : new[] { cls };

    public static string BaseClass(string cls) => Classes[cls].Base ?? cls;

    public static ClassDef? AdvancedOf(string baseCls) => Classes.Values.FirstOrDefault(c => c.Base == baseCls);

    // Each skill's picture is drawn by tools/ArtGen/SkillIcons.cs, keyed by its Id.
    public static readonly Dictionary<string, SkillDef> Skills = new[]
    {
        // ---- Warrior / Warlord ----
        new SkillDef { Id = "tiger_fang", Cls = "warrior", Level = 5, Name = "Tiger Fang Strike", Color = "#d0583e", Mp = 6, Cd = 3, Type = SkillType.Melee, Mult = 2.2, Desc = "A pouncing two-handed blow that bites like a tiger's jaw." },
        new SkillDef { Id = "lions_challenge", Cls = "warrior", Level = 7, Name = "Lion's Challenge", Color = "#c08040", Mp = 8, Cd = 12, Type = SkillType.Self, Effect = "taunt", Radius = 5, Dur = 8, Desc = "Roar a challenge: every foe within 5 tiles turns on you, and you take 35% less damage for 8s." },
        new SkillDef { Id = "bloodlust", Cls = "warrior", Level = 10, Name = "Bloodlust", Color = "#e03828", Mp = 12, Cd = 35, Type = SkillType.Self, Effect = "berserk", Dur = 15, Desc = "Let the red haze take you: +40% attack and faster swings and footsteps for 15s, but 20% less defense." },
        new SkillDef { Id = "cyclone_blade", Cls = "warrior", Level = 14, Name = "Cyclone Blade", Color = "#f0a040", Mp = 15, Cd = 6, Type = SkillType.Around, Hits = 2, Mult = 1.0, Desc = "Spin twice in a storm of steel, cutting every foe around you on each turn." },
        new SkillDef { Id = "thunderclap_bash", Cls = "warrior", Level = 20, Name = "Thunderclap Bash", Color = "#a8b8d8", Mp = 14, Cd = 9, Type = SkillType.Melee, Mult = 1.5, Effect = "stun", Dur = 2, Knock = 2, Desc = "Slam your shield into a foe like a thunderclap: it flies back 2 tiles, stunned for 2s." },
        new SkillDef { Id = "iron_mountain", Cls = "warrior", Level = 27, Name = "Iron Mountain", Color = "#8a96a8", Mp = 20, Cd = 40, Type = SkillType.Self, Effect = "fortress", Dur = 15, Desc = "Stand like a mountain: +60% defense for 15s, and every foe that strikes you takes 30% of the blow back." },
        new SkillDef { Id = "earthshatter", Cls = "warrior", Level = 35, Name = "Earthshatter", Color = "#c08a50", Mp = 26, Cd = 8, Type = SkillType.Line, Phys = true, Range = 5, Power = 2.2, Effect = "stun", Dur = 1, Fx = "beam", Desc = "Drive your blade into the ground: a fissure rips 5 tiles ahead and every foe in it is stunned for 1s." },
        new SkillDef { Id = "meteor_leap", Cls = "warrior", Level = 44, Name = "Meteor Leap", Color = "#e8603a", Mp = 26, Cd = 10, Type = SkillType.Dash, Range = 6, Radius = 1, Mult = 1.8, Effect = "stun", Dur = 1, Desc = "Leap up to 6 tiles and crash down on a foe, striking it and everything beside it, all stunned for 1s." },
        new SkillDef { Id = "hundred_battle_banner", Cls = "warlord", Level = 50, Name = "Hundred-Battle Banner", Color = "#d83a2a", Mp = 40, Cd = 45, Type = SkillType.Self, Effect = "banner", Dur = 25, Desc = "Plant the banner of a hundred victories: +20% attack, +20% defense and steady healing for 25s." },
        new SkillDef { Id = "crescent_cleave", Cls = "warlord", Level = 60, Name = "Crescent Moon Cleave", Color = "#f07848", Mp = 38, Cd = 7, Type = SkillType.Around, Radius = 2, Mult = 2.4, Knock = 1, Desc = "A crescent of steel sweeps every foe within 2 tiles and throws them back." },
        new SkillDef { Id = "undying_will", Cls = "warlord", Level = 70, Name = "Undying Will", Color = "#ff8040", Mp = 50, Cd = 60, Type = SkillType.Self, Effect = "barrier", Power = 0.4, Dur = 20, Desc = "Will alone holds you up: a barrier soaks up damage equal to 40% of your health for 20s." },
        new SkillDef { Id = "headsmans_verdict", Cls = "warlord", Level = 80, Name = "Headsman's Verdict", Color = "#a82020", Mp = 45, Cd = 12, Type = SkillType.Melee, Mult = 3.0, Execute = 0.3, Desc = "One merciless blow. Deals double damage to a foe below 30% health." },
        new SkillDef { Id = "tyrants_domain", Cls = "warlord", Level = 90, Name = "Tyrant's Domain", Color = "#ff6030", Mp = 70, Cd = 20, Type = SkillType.Around, Radius = 4, Mult = 3.0, Pull = 4, Effect = "stun", Dur = 2, Fx = "nova", Desc = "The battlefield bows: every foe within 4 tiles is struck, dragged to your feet and stunned for 2s." },

        // ---- Rogue / Shadowblade ----
        new SkillDef { Id = "shadow_fang", Cls = "rogue", Level = 5, Name = "Shadow Fang", Color = "#9a72d8", Mp = 5, Cd = 4, Type = SkillType.Melee, Mult = 1.6, BehindMult = 2.8, Desc = "Bite with a blade from the dark. Much stronger from behind or against an unaware foe." },
        new SkillDef { Id = "vipers_kiss", Cls = "rogue", Level = 7, Name = "Viper's Kiss", Color = "#60c050", Mp = 8, Cd = 8, Type = SkillType.Melee, Mult = 1.0, Effect = "poison", Dur = 8, Desc = "A venom-slick cut: the foe is poisoned for 8s." },
        new SkillDef { Id = "vanishing_veil", Cls = "rogue", Level = 10, Name = "Vanishing Veil", Color = "#6a5a90", Mp = 10, Cd = 20, Type = SkillType.Self, Effect = "stealth", Dur = 6, Desc = "Melt into the shadows for 6s. Foes lose track of you and your next strike always crits." },
        new SkillDef { Id = "crimson_flurry", Cls = "rogue", Level = 14, Name = "Crimson Flurry", Color = "#e04870", Mp = 14, Cd = 7, Type = SkillType.Melee, Hits = 4, Mult = 0.75, Desc = "Four blurring cuts on the foe in reach." },
        new SkillDef { Id = "blackpowder_bomb", Cls = "rogue", Level = 20, Name = "Blackpowder Bomb", Color = "#8a8a9a", Mp = 16, Cd = 18, Type = SkillType.Around, Radius = 2, Mult = 0.8, Effect = "weak", Dur = 8, Fx = "smoke", Desc = "A burst of black smoke: hurts foes within 2 tiles, and they deal 30% less damage for 8s." },
        new SkillDef { Id = "phantom_step", Cls = "rogue", Level = 27, Name = "Phantom Step", Color = "#7a5ab0", Mp = 18, Cd = 10, Type = SkillType.Dash, Range = 6, Mult = 1.4, BehindMult = 2.6, Desc = "Vanish and reappear behind a foe up to 6 tiles away, blade already in its back." },
        new SkillDef { Id = "predators_eye", Cls = "rogue", Level = 35, Name = "Predator's Eye", Color = "#e0c050", Mp = 20, Cd = 35, Type = SkillType.Self, Effect = "focus", Dur = 15, Desc = "See every weak spot: +25% critical chance for 15s." },
        new SkillDef { Id = "blade_tempest", Cls = "rogue", Level = 44, Name = "Blade Tempest", Color = "#a0e070", Mp = 26, Cd = 9, Type = SkillType.Around, Radius = 2, Mult = 1.6, Effect = "poison", Dur = 6, Desc = "A ring of poisoned knives bursts outward: hits and poisons every foe within 2 tiles." },
        new SkillDef { Id = "mark_of_death", Cls = "shadowblade", Level = 50, Name = "Mark of Death", Color = "#b02050", Mp = 30, Cd = 14, Type = SkillType.Target, Phys = true, Range = 5, Power = 1.2, Effect = "curse", Dur = 10, Fx = "spirit", Desc = "Throw a marked blade: the foe takes 25% more damage from everything for 10s." },
        new SkillDef { Id = "hundred_petal_dance", Cls = "shadowblade", Level = 60, Name = "Hundred-Petal Dance", Color = "#e070c0", Mp = 34, Cd = 9, Type = SkillType.Melee, Hits = 6, Mult = 0.85, Desc = "Six whirling cuts that scatter like falling petals." },
        new SkillDef { Id = "quicksilver", Cls = "shadowblade", Level = 70, Name = "Quicksilver", Color = "#80e0f0", Mp = 30, Cd = 40, Type = SkillType.Self, Effect = "haste", Dur = 15, Desc = "Time seems to slow for everyone but you: move a third faster and attack 30% faster for 15s." },
        new SkillDef { Id = "final_whisper", Cls = "shadowblade", Level = 80, Name = "Final Whisper", Color = "#601830", Mp = 45, Cd = 14, Type = SkillType.Dash, Range = 5, Mult = 2.2, BehindMult = 4, Execute = 0.35, Desc = "Blink behind a foe up to 5 tiles away and end it: huge damage from behind, doubled again below 35% health." },
        new SkillDef { Id = "eclipse_of_blades", Cls = "shadowblade", Level = 90, Name = "Eclipse of a Thousand Cuts", Color = "#c040e0", Mp = 60, Cd = 18, Type = SkillType.Around, Radius = 3, Hits = 4, Mult = 1.0, Desc = "The light goes out. Four blinding passes cut every foe within 3 tiles." },

        // ---- Mystic / Stormcaller ----
        new SkillDef { Id = "foxfire", Cls = "mystic", Level = 5, Name = "Foxfire", Color = "#f06030", Mp = 6, Cd = 1.2, Type = SkillType.Target, Range = 7, Power = 1.7, Fx = "fire", Desc = "Hurl a ball of dancing foxfire at your target." },
        new SkillDef { Id = "frost_lance", Cls = "mystic", Level = 7, Name = "Frost Lance", Color = "#70c8f0", Mp = 9, Cd = 3, Type = SkillType.Target, Range = 7, Power = 1.3, Effect = "slow", Dur = 4, Fx = "ice", Desc = "A spear of ice that pierces and slows the target for 4s." },
        new SkillDef { Id = "thunder_seal", Cls = "mystic", Level = 10, Name = "Thunder Seal", Color = "#e8d040", Mp = 12, Cd = 15, Type = SkillType.Target, Range = 6, Power = 0.4, Effect = "stun", Dur = 3, Fx = "paralyze", Desc = "A ring of crackling runes locks the target in place for 3s (1s on bosses)." },
        new SkillDef { Id = "thunderstrike", Cls = "mystic", Level = 14, Name = "Thunderstrike", Color = "#b0a0ff", Mp = 20, Cd = 6, Type = SkillType.Target, Range = 7, Power = 1.5, Radius = 2, Fx = "lightning", Desc = "Call a bolt from the sky onto the target and every foe within 2 tiles of it." },
        new SkillDef { Id = "dragons_breath", Cls = "mystic", Level = 20, Name = "Dragon's Breath", Color = "#f08030", Mp = 22, Cd = 5, Type = SkillType.Line, Range = 5, Power = 1.6, Effect = "burn", Dur = 5, Fx = "beam", Desc = "Breathe a torrent of flame 5 tiles ahead, burning every foe it touches for 5s." },
        new SkillDef { Id = "arcane_aegis", Cls = "mystic", Level = 27, Name = "Arcane Aegis", Color = "#6080e0", Mp = 25, Cd = 40, Type = SkillType.Self, Effect = "manashield", Dur = 20, Desc = "A lattice of pure mana: for 20s, half the damage you take drains your mana instead of your health." },
        new SkillDef { Id = "howling_blizzard", Cls = "mystic", Level = 35, Name = "Howling Blizzard", Color = "#c0e8ff", Mp = 32, Cd = 8, Type = SkillType.Target, Range = 7, Radius = 2, Power = 1.5, Effect = "slow", Dur = 5, Fx = "ice", Desc = "A screaming white storm around the target: damages and slows every foe within 2 tiles." },
        new SkillDef { Id = "meteor_strike", Cls = "mystic", Level = 44, Name = "Meteor Strike", Color = "#ff5020", Mp = 40, Cd = 12, Type = SkillType.Target, Range = 7, Radius = 2, Power = 2.4, Effect = "burn", Dur = 5, Fx = "fire", Desc = "Pull a burning star out of the sky: huge damage within 2 tiles of the target, and they burn." },
        new SkillDef { Id = "chain_lightning", Cls = "stormcaller", Level = 50, Name = "Chain Lightning", Color = "#a0b0ff", Mp = 36, Cd = 5, Type = SkillType.Target, Range = 7, Chain = 4, Power = 1.8, Fx = "lightning", Desc = "Lightning that leaps from the target to four more foes nearby." },
        new SkillDef { Id = "absolute_zero", Cls = "stormcaller", Level = 60, Name = "Absolute Zero", Color = "#a0e0ff", Mp = 40, Cd = 12, Type = SkillType.Around, Radius = 2, Power = 1.8, Effect = "root", Dur = 4, Fx = "nova", Desc = "The air itself freezes: ice bursts out 2 tiles, damaging foes and locking their feet for 4s." },
        new SkillDef { Id = "overcharge", Cls = "stormcaller", Level = 70, Name = "Overcharge", Color = "#c080ff", Mp = 30, Cd = 45, Type = SkillType.Self, Effect = "surge", Dur = 15, Desc = "Pour raw storm into your spells: +40% magic power for 15s." },
        new SkillDef { Id = "phoenix_inferno", Cls = "stormcaller", Level = 80, Name = "Phoenix Inferno", Color = "#ff4010", Mp = 60, Cd = 14, Type = SkillType.Target, Range = 7, Radius = 3, Power = 3.0, Effect = "burn", Dur = 6, Fx = "fire", Desc = "A firebird dives onto the target and bursts: a sea of flame within 3 tiles." },
        new SkillDef { Id = "nine_heavens", Cls = "stormcaller", Level = 90, Name = "Wrath of the Nine Heavens", Color = "#ffffff", Mp = 90, Cd = 25, Type = SkillType.Target, Range = 8, Radius = 4, Hits = 3, Power = 1.4, Effect = "stun", Dur = 2, Fx = "lightning", Desc = "Three of heaven's bolts fall within 4 tiles of the target, one after another, stunning everything they touch." },

        // ---- Monk / Lotus Saint ----
        new SkillDef { Id = "sunbeam", Cls = "monk", Level = 5, Name = "Sunbeam", Color = "#f0d060", Mp = 5, Cd = 1.5, Type = SkillType.Target, Range = 6, Power = 1.3, Fx = "holy", Desc = "A shaft of sunlight sears your target." },
        new SkillDef { Id = "mending_light", Cls = "monk", Level = 5, Name = "Mending Light", Color = "#70e090", Mp = 8, Cd = 2, Type = SkillType.Self, Effect = "heal", Desc = "Gentle light closes your wounds. Scales with Wisdom." },
        new SkillDef { Id = "golden_blessing", Cls = "monk", Level = 8, Name = "Golden Blessing", Color = "#f0a0c0", Mp = 12, Cd = 30, Type = SkillType.Self, Effect = "blessing", Dur = 25, Desc = "A halo settles on you: +30% defense, +10% attack and faster regeneration for 25s." },
        new SkillDef { Id = "return_of_the_soul", Cls = "monk", Level = 12, Name = "Return of the Soul", Color = "#e0e0ff", Mp = 0, Cd = 60, Type = SkillType.Ghost, Desc = "While a ghost, call your spirit back into your body. (With other players this will revive them.)" },
        new SkillDef { Id = "spring_of_life", Cls = "monk", Level = 20, Name = "Spring of Life", Color = "#60d0a0", Mp = 18, Cd = 25, Type = SkillType.Self, Effect = "regen", Dur = 15, Desc = "Healing wells up: restore 5% of your health every 2s for 15s." },
        new SkillDef { Id = "golden_bell", Cls = "monk", Level = 27, Name = "Golden Bell", Color = "#f8d070", Mp = 24, Cd = 35, Type = SkillType.Self, Effect = "barrier", Power = 0.3, Dur = 20, Desc = "A great bell of light drops over you, soaking up damage equal to 30% of your health." },
        new SkillDef { Id = "pillar_of_light", Cls = "monk", Level = 35, Name = "Pillar of Light", Color = "#ffe080", Mp = 28, Cd = 6, Type = SkillType.Target, Range = 6, Radius = 1, Power = 2.2, Fx = "holy", Desc = "A column of heaven's light crashes down on the target and every foe beside it." },
        new SkillDef { Id = "thunder_palm", Cls = "monk", Level = 44, Name = "Thunder Palm", Color = "#f0c060", Mp = 22, Cd = 8, Type = SkillType.Melee, Mult = 1.8, Knock = 3, Effect = "stun", Dur = 1, Desc = "An open palm with a storm behind it: throws the foe back 3 tiles and stuns it." },
        new SkillDef { Id = "thousand_petal_lotus", Cls = "lotus_saint", Level = 50, Name = "Thousand-Petal Lotus", Color = "#f8a0c8", Mp = 40, Cd = 8, Type = SkillType.Around, Radius = 3, Power = 2.0, Drain = 0.3, Fx = "nova", Desc = "A lotus of light opens beneath you: holy damage within 3 tiles, and you heal for 30% of it." },
        new SkillDef { Id = "sanctuary", Cls = "lotus_saint", Level = 60, Name = "Sanctuary", Color = "#fff4c0", Mp = 45, Cd = 50, Type = SkillType.Self, Effect = "sanctuary", Dur = 10, Desc = "Holy ground rises around you: take half damage from everything for 10s." },
        new SkillDef { Id = "prism_of_mercy", Cls = "lotus_saint", Level = 70, Name = "Prism of Mercy", Color = "#ffe060", Mp = 38, Cd = 5, Type = SkillType.Target, Range = 7, Chain = 3, Power = 2.4, Fx = "holy", Desc = "Light splits through a prism onto the target and three more foes nearby." },
        new SkillDef { Id = "avatar_of_mercy", Cls = "lotus_saint", Level = 80, Name = "Avatar of Mercy", Color = "#f0e0ff", Mp = 60, Cd = 60, Type = SkillType.Self, Effect = "avatar", Dur = 25, Desc = "Become a vessel of mercy: +30% magic power, +30% defense and steady healing for 25s." },
        new SkillDef { Id = "heavens_gate", Cls = "lotus_saint", Level = 90, Name = "Heaven's Gate", Color = "#ffffff", Mp = 85, Cd = 22, Type = SkillType.Target, Range = 7, Radius = 4, Power = 3.4, Effect = "stun", Dur = 1.5, Fx = "holy", Desc = "The gate of heaven swings open over the target: holy fire within 4 tiles stuns all it touches." },

        // ---- Hunter / Windstrider ----
        new SkillDef { Id = "eagle_shot", Cls = "hunter", Level = 5, Name = "Eagle Shot", Color = "#a0c860", Mp = 6, Cd = 2.5, Type = SkillType.Target, Phys = true, Range = 7, Power = 1.8, Fx = "arrow", Desc = "A patient, perfect shot at a target up to 7 tiles away." },
        new SkillDef { Id = "snare_shot", Cls = "hunter", Level = 7, Name = "Snare Shot", Color = "#c0a070", Mp = 9, Cd = 10, Type = SkillType.Target, Phys = true, Range = 7, Power = 1.0, Effect = "root", Dur = 4, Fx = "arrow", Desc = "Nail the target's foot to the ground: it cannot move for 4s." },
        new SkillDef { Id = "hawk_eye", Cls = "hunter", Level = 10, Name = "Hawk Eye", Color = "#e0c060", Mp = 12, Cd = 30, Type = SkillType.Self, Effect = "focus", Dur = 15, Desc = "See with the eyes of a hawk: +25% critical chance for 15s." },
        new SkillDef { Id = "split_shot", Cls = "hunter", Level = 14, Name = "Split Shot", Color = "#90e070", Mp = 15, Cd = 5, Type = SkillType.Target, Phys = true, Range = 6, Chain = 2, Power = 1.2, Fx = "arrow", Desc = "One arrow splits into three in flight: the target and two more foes nearby." },
        new SkillDef { Id = "bone_piercer", Cls = "hunter", Level = 20, Name = "Bone Piercer", Color = "#d0e0a0", Mp = 20, Cd = 6, Type = SkillType.Line, Phys = true, Range = 7, Power = 1.7, Fx = "beam", Desc = "A heavy arrow that punches through every foe in a line 7 tiles long." },
        new SkillDef { Id = "fleet_foot", Cls = "hunter", Level = 27, Name = "Fleet Foot", Color = "#80d0e0", Mp = 18, Cd = 35, Type = SkillType.Self, Effect = "haste", Dur = 15, Desc = "Run with the deer: move a third faster and shoot 30% faster for 15s." },
        new SkillDef { Id = "serpent_arrow", Cls = "hunter", Level = 35, Name = "Serpent Arrow", Color = "#60c050", Mp = 22, Cd = 7, Type = SkillType.Target, Phys = true, Range = 7, Radius = 1, Power = 1.4, Effect = "poison", Dur = 8, Fx = "arrow", Desc = "An arrowhead dipped in viper venom bursts on impact: poisons the target and every foe beside it for 8s." },
        new SkillDef { Id = "arrow_rain", Cls = "hunter", Level = 44, Name = "Arrow Rain", Color = "#b0d080", Mp = 32, Cd = 10, Type = SkillType.Target, Phys = true, Range = 7, Radius = 2, Power = 1.7, Fx = "arrow", Desc = "Loose a volley high into the sky: arrows fall on every foe within 2 tiles of the target." },
        new SkillDef { Id = "typhoon_arrow", Cls = "windstrider", Level = 50, Name = "Typhoon Arrow", Color = "#90f0d0", Mp = 30, Cd = 7, Type = SkillType.Target, Phys = true, Range = 8, Power = 2.4, Knock = 3, Fx = "arrow", Desc = "An arrow wrapped in a howling gale that hurls the target 3 tiles back." },
        new SkillDef { Id = "arrow_storm", Cls = "windstrider", Level = 60, Name = "Storm of Arrows", Color = "#70e0b0", Mp = 45, Cd = 11, Type = SkillType.Target, Phys = true, Range = 8, Radius = 3, Power = 2.1, Effect = "slow", Dur = 4, Fx = "arrow", Desc = "A whirlwind of shafts within 3 tiles of the target. Survivors are slowed." },
        new SkillDef { Id = "ride_the_wind", Cls = "windstrider", Level = 70, Name = "Ride the Wind", Color = "#c0fff0", Mp = 30, Cd = 30, Type = SkillType.Self, Effect = "windwalk", Dur = 8, Desc = "Become the wind: vanish and move faster for 8s. Your next shot always crits." },
        new SkillDef { Id = "heartseeker", Cls = "windstrider", Level = 80, Name = "Heartseeker", Color = "#ff6070", Mp = 45, Cd = 12, Type = SkillType.Target, Phys = true, Range = 9, Power = 3.2, Execute = 0.3, Fx = "arrow", Desc = "An arrow that hunts the heart. Double damage on a foe below 30% health." },
        new SkillDef { Id = "sky_splitter", Cls = "windstrider", Level = 90, Name = "Sky-Splitting Arrow", Color = "#ffffff", Mp = 75, Cd = 18, Type = SkillType.Line, Phys = true, Range = 10, Power = 4.0, Fx = "beam", Desc = "One arrow that tears the sky in two: hits every foe in a line 10 tiles long." },

        // ---- Shaman / Grand Shaman ----
        new SkillDef { Id = "wailing_spirit", Cls = "shaman", Level = 5, Name = "Wailing Spirit", Color = "#c060e0", Mp = 6, Cd = 1.5, Type = SkillType.Target, Range = 6, Power = 1.5, Fx = "spirit", Desc = "Loose a shrieking ghost at your target." },
        new SkillDef { Id = "crows_hex", Cls = "shaman", Level = 7, Name = "Crow's Hex", Color = "#8a3aa0", Mp = 9, Cd = 10, Type = SkillType.Target, Range = 6, Power = 0.4, Effect = "curse", Dur = 10, Fx = "spirit", Desc = "The crow marks its prey: the target takes 25% more damage from everything for 10s." },
        new SkillDef { Id = "ancestor_totem", Cls = "shaman", Level = 10, Name = "Ancestor Totem", Color = "#d0a060", Mp = 14, Cd = 35, Type = SkillType.Self, Effect = "ironskin", Dur = 15, Desc = "The village guardians stand watch over you: +60% defense for 15s." },
        new SkillDef { Id = "soul_drain", Cls = "shaman", Level = 14, Name = "Soul Drain", Color = "#e04060", Mp = 14, Cd = 4, Type = SkillType.Target, Range = 6, Power = 1.4, Drain = 0.5, Fx = "drain", Desc = "Tear out a piece of the target's soul and heal for half the damage dealt." },
        new SkillDef { Id = "withering_curse", Cls = "shaman", Level = 20, Name = "Withering Curse", Color = "#a08040", Mp = 18, Cd = 9, Type = SkillType.Target, Range = 6, Power = 0.8, Effect = "weak", Dur = 10, Fx = "spirit", Desc = "Rot the target from within: it deals 30% less damage for 10s." },
        new SkillDef { Id = "dance_of_the_dead", Cls = "shaman", Level = 27, Name = "Dance of the Dead", Color = "#f080a0", Mp = 24, Cd = 8, Type = SkillType.Around, Radius = 2, Power = 1.5, Effect = "slow", Dur = 4, Fx = "nova", Desc = "Ring the spirit bells and dance: ghosts swirl out 2 tiles, harming and slowing every foe." },
        new SkillDef { Id = "grave_chains", Cls = "shaman", Level = 35, Name = "Grave Chains", Color = "#9090c0", Mp = 26, Cd = 12, Type = SkillType.Target, Range = 6, Radius = 1, Power = 1.3, Pull = 2, Effect = "root", Dur = 5, Fx = "spirit", Desc = "Chains from the grave drag the target and the foes beside it 2 tiles toward you and bind them for 5s." },
        new SkillDef { Id = "blood_pact", Cls = "shaman", Level = 44, Name = "Blood Pact", Color = "#c02040", Mp = 25, Cd = 40, Type = SkillType.Self, Effect = "surge", Dur = 15, Desc = "Seal a pact in your own blood: +40% magic power for 15s." },
        new SkillDef { Id = "night_parade", Cls = "grand_shaman", Level = 50, Name = "Night Parade", Color = "#d070ff", Mp = 38, Cd = 7, Type = SkillType.Target, Range = 7, Radius = 2, Power = 2.2, Effect = "curse", Dur = 8, Fx = "spirit", Desc = "A procession of a hundred ghosts marches through the target: everything within 2 tiles is hurt and cursed." },
        new SkillDef { Id = "crimson_harvest", Cls = "grand_shaman", Level = 60, Name = "Crimson Harvest", Color = "#ff4070", Mp = 40, Cd = 8, Type = SkillType.Target, Range = 7, Chain = 3, Power = 1.9, Drain = 0.4, Fx = "drain", Desc = "Reap the life of the target and three foes nearby, healing for 40% of it." },
        new SkillDef { Id = "great_gut", Cls = "grand_shaman", Level = 70, Name = "The Great Gut", Color = "#ffd080", Mp = 45, Cd = 50, Type = SkillType.Self, Effect = "sanctuary", Dur = 10, Desc = "Beat the ritual drum: the spirits take half of every blow for 10s." },
        new SkillDef { Id = "call_of_the_grave", Cls = "grand_shaman", Level = 80, Name = "Call of the Grave", Color = "#7040a0", Mp = 60, Cd = 14, Type = SkillType.Target, Range = 7, Radius = 3, Power = 2.8, Effect = "weak", Dur = 8, Fx = "spirit", Desc = "Bony hands claw up from the earth around the target: heavy damage within 3 tiles, and their blows weaken." },
        new SkillDef { Id = "underworld_gate", Cls = "grand_shaman", Level = 90, Name = "Underworld Gate", Color = "#ff2060", Mp = 85, Cd = 22, Type = SkillType.Around, Radius = 4, Power = 3.2, Drain = 0.2, Pull = 4, Effect = "stun", Dur = 2, Fx = "nova", Desc = "Open the gate of the dead beneath you: every foe within 4 tiles is dragged in and stunned, and you drink their life." },
    }.ToDictionary(s => s.Id);
}
