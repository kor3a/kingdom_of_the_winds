namespace KotW.Shared;

// The game simulation. It owns every rule: movement, combat, loot, quests.
// The client only feeds it inputs and calls the action methods (each one maps
// onto a future network message), then reads state and drains events.
public sealed class World
{
    private const double PlayerStep = 0.19, GhostStep = 0.12;
    private static readonly Dictionary<string, string> BuffLabels = new()
    {
        ["guard"] = "Taunt guard", ["warcry"] = "War Cry", ["stealth"] = "Shadow Veil", ["blessing"] = "Blessing",
    };

    public readonly Dictionary<string, GameMap> Maps;
    public readonly Player P;
    public GameMap Map = null!;
    public List<Monster> Monsters = new();
    public List<Npc> Npcs = new();
    public List<Folk> FolkList = new();
    public string? Target;
    public double Time;
    public Derived Pd = null!;

    private List<GameEvent> _events = new();
    private readonly List<(double At, Action Fn)> _timers = new();
    private readonly List<(double At, SpawnRule Spawn)> _respawns = new();
    private int _nextId = 1;
    private double _regenT, _folkShoutT = 6;
    private int[] _seen = Array.Empty<int>();
    private sbyte[] _first = Array.Empty<sbyte>();
    private int _stamp;

    public World(SaveData save)
    {
        Maps = KotW.Shared.Maps.BuildAll();
        P = Player.FromSave(save);
        EnterMap(P.MapId, P.X, P.Y, P.Facing, true);
        P.Hp = Math.Min(P.Hp, Pd.MaxHp);
        P.Mp = Math.Min(P.Mp, Pd.MaxMp);
    }

    // ---- events ----------------------------------------------------------------
    private void Emit(GameEvent e) => _events.Add(e);
    public void Msg(string text, string channel = "system", string from = "") => Emit(new GameEvent { Type = EvType.Msg, Text = text, Channel = channel, From = from });
    private void Sfx(string name) => Emit(new GameEvent { Type = EvType.Sfx, Kind = name });
    private void Fx(string kind, int x, int y, Action<GameEvent>? more = null)
    {
        var e = new GameEvent { Type = EvType.Fx, Kind = kind, X = x, Y = y };
        more?.Invoke(e);
        Emit(e);
    }
    private void Number(int x, int y, string text, string kind) => Emit(new GameEvent { Type = EvType.Dmg, X = x, Y = y, Text = text, Kind = kind });
    public List<GameEvent> Drain() { var e = _events; _events = new(); return e; }
    private void Later(double delay, Action fn) => _timers.Add((Time + delay, fn));

    public void Refresh()
    {
        Pd = Rules.Derive(P);
        P.Hp = Math.Min(P.Hp, Pd.MaxHp);
        P.Mp = Math.Min(P.Mp, Pd.MaxMp);
    }

    // ---- maps ------------------------------------------------------------------
    public void EnterMap(string mapId, int x, int y, Dir? facing, bool initial = false)
    {
        Map = Maps[mapId];
        P.MapId = mapId;
        P.Place(x, y);
        if (facing.HasValue) P.Facing = facing.Value;
        Monsters = new();
        _respawns.Clear();
        Target = null;
        Npcs = Content.Npcs.Values.Where(n => n.Map == mapId).Select(n =>
        {
            var npc = new Npc { Def = n, Id = n.Id, Name = n.Name, Facing = n.Facing };
            npc.Place(n.X, n.Y);
            return npc;
        }).ToList();
        FolkList = new();
        foreach (var s in Map.Spawns) for (int i = 0; i < s.Count; i++) SpawnMonster(s);
        if (Map.FolkArea != null) foreach (var f in Content.Folk) SpawnFolk(f);
        Refresh();
        Emit(new GameEvent { Type = EvType.Map, Id = mapId, Initial = initial });
        if (!initial) Sfx("portal");
    }

    private (int X, int Y)? FindFreeTile(int x0, int y0, int x1, int y1, int minPlayerDist = 5)
    {
        for (int i = 0; i < 80; i++)
        {
            int x = Rnd.Int(x0, x1), y = Rnd.Int(y0, y1);
            if (!IsFree(x, y, true) || Map.Tile(x, y) == 'S') continue;
            if (P.Manhattan(x, y) < minPlayerDist) continue;
            if (Map.Portals.Values.Any(pt => Math.Abs(pt.X - x) + Math.Abs(pt.Y - y) < 7)) continue;
            return (x, y);
        }
        return null;
    }

    private Monster? SpawnMonster(SpawnRule s, (int X, int Y)? at = null, bool noRespawn = false, string? defId = null)
    {
        var d = Content.Monsters[defId ?? s.Monster];
        var pos = at ?? (s.Fixed ? (s.X0, s.Y0) : FindFreeTile(s.X0, s.Y0, s.X1, s.Y1));
        if (pos == null || !IsFree(pos.Value.X, pos.Value.Y, true)) return null;
        var m = new Monster
        {
            Id = $"m{_nextId++}", Name = d.Name, D = d, Spawn = s, Hp = d.Hp, MaxHp = d.Hp, StepDur = d.MoveDur,
            ThinkT = Rnd.Range(0.5, 3), HFace = Rnd.Next() < 0.5 ? Dir.Left : Dir.Right,
            HomeX = pos.Value.X, HomeY = pos.Value.Y, NoRespawn = noRespawn,
        };
        m.Place(pos.Value.X, pos.Value.Y);
        Monsters.Add(m);
        return m;
    }

    private void SpawnFolk(FolkDef f)
    {
        var a = Map.FolkArea!;
        var pos = FindFreeTile(a.X0, a.Y0, a.X1, a.Y1, 2);
        if (pos == null) return;
        var e = new Folk { Def = f, Id = $"folk:{f.Name}", Name = f.Name, ThinkT = Rnd.Range(1, 4), Facing = Rnd.Pick(DirX.All), StepDur = 0.24 };
        e.Place(pos.Value.X, pos.Value.Y);
        FolkList.Add(e);
    }

    // ---- occupancy ---------------------------------------------------------------
    public Entity? EntityAt(int x, int y)
    {
        foreach (var m in Monsters) if (m.X == x && m.Y == y) return m;
        foreach (var n in Npcs) if (n.X == x && n.Y == y) return n;
        foreach (var f in FolkList) if (f.X == x && f.Y == y) return f;
        return null;
    }

    // forMonster: monsters also avoid exits, shrine circles and the living player.
    public bool IsFree(int x, int y, bool forMonster)
    {
        if (!Map.Walkable(x, y)) return false;
        if (forMonster)
        {
            if (Map.PortalAt(x, y) != null || Map.Tile(x, y) == 'S') return false;
            if (!P.Ghost && P.X == x && P.Y == y) return false;
        }
        return EntityAt(x, y) == null;
    }

    public Entity? EntityById(string id)
    {
        if (id == "player") return P;
        return (Entity?)Monsters.Find(m => m.Id == id) ?? (Entity?)Npcs.Find(n => n.Id == id) ?? FolkList.Find(f => f.Id == id);
    }

    public Monster? GetTarget()
    {
        if (Target == null) return null;
        var t = Monsters.Find(m => m.Id == Target);
        if (t == null) Target = null;
        return t;
    }

    // ---- main tick ---------------------------------------------------------------
    public void Update(double dt, TickInput input)
    {
        Time += dt;
        for (int i = _timers.Count - 1; i >= 0; i--)
        {
            if (Time < _timers[i].At) continue;
            var fn = _timers[i].Fn;
            _timers.RemoveAt(i);
            fn();
        }
        P.AtkCd = Math.Max(0, P.AtkCd - dt);
        P.PotCd = Math.Max(0, P.PotCd - dt);
        P.AttackAnim = Math.Max(0, P.AttackAnim - dt);
        P.HurtFlash = Math.Max(0, P.HurtFlash - dt);
        foreach (var k in P.Cds.Keys.ToList()) { P.Cds[k] -= dt; if (P.Cds[k] <= 0) P.Cds.Remove(k); }
        UpdateBuffs(dt);
        UpdatePlayer(dt, input);
        UpdateRegen(dt);
        foreach (var m in Monsters.ToList()) if (m.Hp > 0) UpdateMonster(m, dt);
        UpdateFolk(dt);
        for (int i = _respawns.Count - 1; i >= 0; i--)
        {
            var (at, spawn) = _respawns[i];
            if (Time < at) continue;
            if (SpawnMonster(spawn) != null) _respawns.RemoveAt(i); else _respawns[i] = (Time + 3, spawn);
        }
        Refresh();
    }

    private void UpdateBuffs(double dt)
    {
        foreach (var k in P.Buffs.Keys.ToList())
        {
            P.Buffs[k] -= dt;
            if (P.Buffs[k] <= 0) { P.Buffs.Remove(k); Msg($"{BuffLabels[k]} wore off.", "info"); }
        }
    }

    private void UpdateRegen(double dt)
    {
        _regenT += dt;
        if (_regenT < 2) return;
        _regenT -= 2;
        if (P.Ghost) return;
        bool calm = Time - P.LastCombat > 6;
        double bless = P.Buffs.ContainsKey("blessing") ? 0.03 : 0;
        P.Hp = Math.Min(Pd.MaxHp, P.Hp + Math.Max(1, Math.Round(Pd.MaxHp * ((calm ? 0.04 : 0.01) + bless) + Pd.Stats.Con * 0.1)));
        P.Mp = Math.Min(Pd.MaxMp, P.Mp + Math.Max(1, Math.Round(Pd.MaxMp * ((calm ? 0.05 : 0.02) + bless))));
    }

    private void UpdatePlayer(double dt, TickInput input)
    {
        if (P.MoveT < 1)
        {
            P.MoveT = Math.Min(1, P.MoveT + dt / P.StepDur);
            if (P.MoveT >= 1 && Arrive()) return;
        }
        if (P.MoveT >= 1 && input.Dir.HasValue)
        {
            var dir = input.Dir.Value;
            var (dx, dy) = dir.Vec();
            P.Facing = dir;
            int nx = P.X + dx, ny = P.Y + dy;
            var ent = EntityAt(nx, ny);
            bool canPass = Map.Walkable(nx, ny) && (ent == null || (P.Ghost && ent is Monster));
            if (canPass) P.StartStep(nx, ny, P.Ghost ? GhostStep : PlayerStep);
            else if (ent is Monster m && !P.Ghost) BasicAttack(m); // bump to attack
            else if (ent != null && input.Fresh) Interact(ent);
        }
        if (input.Attack) AttackFront(input.AttackFresh);
    }

    // Runs when the player finishes a step. Returns true if the map changed.
    private bool Arrive()
    {
        var pt = Map.PortalAt(P.X, P.Y);
        if (pt != null) { EnterMap(pt.To, pt.Tx, pt.Ty, pt.Facing); return true; }
        if (P.Ghost && Map.Tile(P.X, P.Y) == 'S')
        {
            Revive(0.6);
            Msg("The shrine's light pulls your spirit back into your body.");
        }
        return false;
    }

    public void Interact(Entity ent)
    {
        if (ent is Npc n) TalkTo(n.Id);
        else if (ent is Folk f)
            Msg($"{f.Name} · Lv {f.Def.Level} {Content.Classes[f.Def.Cls ?? "none"].Name}", "info");
    }

    // ---- player combat -------------------------------------------------------------
    private void AttackFront(bool fresh)
    {
        if (P.Ghost || P.AtkCd > 0 || P.MoveT < 1) return;
        var t = GetTarget();
        if (t != null && t.Manhattan(P) == 1) { BasicAttack(t); return; }
        var (dx, dy) = P.Facing.Vec();
        var ent = EntityAt(P.X + dx, P.Y + dy);
        if (ent is Monster m) { BasicAttack(m); return; }
        if (ent != null) { if (fresh) Interact(ent); return; }
        P.AtkCd = Pd.AtkDelay;
        P.AttackAnim = 0.22;
        Fx("slash", P.X + dx, P.Y + dy, e => e.Dir = P.Facing);
        Sfx("swing");
    }

    private void BasicAttack(Monster m)
    {
        if (P.AtkCd > 0) return;
        P.AtkCd = Pd.AtkDelay;
        P.AttackAnim = 0.22;
        P.Facing = DirX.Of(m.X - P.X, m.Y - P.Y);
        Target ??= m.Id;
        var r = Rules.RollMelee(Pd, m.D, P.Level, 1, BreakStealth());
        Fx("slash", m.X, m.Y, e => e.Dir = P.Facing);
        ApplyHit(m, r);
    }

    private void ApplyHit(Monster m, Rules.Hit r, string kind = "hit", string sfx = "hit")
    {
        if (r.Miss)
        {
            Number(m.X, m.Y, "miss", "miss");
            Sfx("swing");
            Aggro(m);
            return;
        }
        HurtMonster(m, r.Dmg, r.Crit, kind, sfx);
    }

    private void HurtMonster(Monster m, int dmg, bool crit, string kind = "hit", string sfx = "hit", bool noAggro = false)
    {
        if (m.Hp <= 0) return;
        m.Hp -= dmg;
        m.Flash = 0.12;
        m.LastHit = Time;
        P.LastCombat = Time;
        Number(m.X, m.Y, dmg.ToString(), crit ? "crit" : kind);
        Sfx(crit ? "crit" : sfx);
        if (m.Hp <= 0) { KillMonster(m); return; }
        if (!noAggro) Aggro(m);
    }

    private void Aggro(Monster m)
    {
        if (!P.Ghost && !P.Buffs.ContainsKey("stealth")) m.State = "chase";
    }

    private bool BreakStealth() => P.Buffs.Remove("stealth");

    private void KillMonster(Monster m)
    {
        var d = m.D;
        Monsters.Remove(m);
        if (Target == m.Id) Target = null;
        Fx("death", m.X, m.Y, e => e.Big = d.Boss);
        Sfx(d.Boss ? "bossdie" : "kill");
        int exp = Math.Max(1, (int)Math.Round(d.Exp * Rules.ExpMultiplier(P.Level, d.Level)));
        int gold = Rnd.Int(d.GoldMin, d.GoldMax);
        P.Gold += gold;
        Msg($"{d.Name} defeated. +{exp} EXP, +{gold} gold", "loot");
        foreach (var (item, chance) in d.Drops)
        {
            if (Rnd.Next() >= chance) continue;
            if (AddItem(item)) Msg($"You found {Content.Items[item].Name}.", "loot");
            else Msg($"Your bag is full, so {Content.Items[item].Name} was left behind.", "warn");
        }
        foreach (var (qid, qs) in P.Quests)
        {
            var q = Content.Quests[qid];
            if (qs.State != "active" || q.KillMonster != d.Id || qs.Count >= q.Count) continue;
            qs.Count++;
            if (qs.Count >= q.Count) Msg($"\"{q.Title}\" complete! Return to {Content.Npcs[q.Giver].Name}.", "quest");
            else Msg($"{q.Title}: {qs.Count}/{q.Count}", "quest");
        }
        if (d.Boss) Msg($"{P.Name} has brought peace to the Hollow by defeating {d.Name}!", "shout", "Herald");
        GainExp(exp);
        if (!m.NoRespawn) _respawns.Add((Time + (d.Respawn > 0 ? d.Respawn : Rnd.Int(10, 18)), m.Spawn));
    }

    public void GainExp(int n)
    {
        if (P.Level >= Rules.MaxLevel) return;
        P.Exp += n;
        bool leveled = false;
        while (P.Level < Rules.MaxLevel && P.Exp >= Rules.ExpToNext(P.Level))
        {
            P.Exp -= Rules.ExpToNext(P.Level);
            P.Level++;
            P.Unspent += Rules.PointsPerLevel;
            leveled = true;
            Msg($"Level up! You are now level {P.Level}. You have {P.Unspent} stat points to spend (C).", "level");
            foreach (var s in Content.Skills.Values)
                if (s.Cls == P.Cls && s.Level == P.Level) Msg($"You learned {s.Name}! It is on your hotbar.", "level");
            if (P.Level == 5 && P.Cls == null)
                Msg("You can now choose a path! Visit the job masters in front of the hall south of the plaza.", "quest");
        }
        if (P.Level >= Rules.MaxLevel) P.Exp = 0;
        if (!leveled) return;
        Refresh();
        P.Hp = Pd.MaxHp;
        P.Mp = Pd.MaxMp;
        Fx("levelup", P.X, P.Y);
        Sfx("levelup");
    }

    // ---- monster AI ----------------------------------------------------------------
    private void UpdateMonster(Monster m, double dt)
    {
        var d = m.D;
        var st = m.Status;
        m.Flash = Math.Max(0, m.Flash - dt);
        m.AttackAnim = Math.Max(0, m.AttackAnim - dt);
        m.AtkCd -= dt;
        foreach (var s in st.Values) s.T -= dt;
        if (st.TryGetValue("poison", out var poison))
        {
            poison.Tick -= dt;
            if (poison.Tick <= 0)
            {
                poison.Tick += 1;
                HurtMonster(m, poison.Dmg, false, "poison", "poison", true);
                if (m.Hp <= 0) return;
            }
        }
        foreach (var k in st.Keys.ToList()) if (st[k].T <= 0) st.Remove(k);
        if (m.MoveT < 1) m.MoveT = Math.Min(1, m.MoveT + dt / m.StepDur);
        if (st.ContainsKey("stun")) return;
        if (m.RoarT > 0)
        {
            m.RoarT -= dt;
            if (m.RoarT <= 0) BossRoar(m);
            return;
        }
        if (m.MoveT < 1) return;

        bool visible = !P.Ghost && !P.Buffs.ContainsKey("stealth");
        int dist = m.Manhattan(P);
        if (m.State == "idle")
        {
            if (d.Aggressive && visible && dist <= d.AggroRange) m.State = "chase";
            else
            {
                if (Time - m.LastHit > 5 && m.Hp < m.MaxHp) m.Hp = Math.Min(m.MaxHp, m.Hp + m.MaxHp * 0.1 * dt);
                m.ThinkT -= dt;
                if (m.ThinkT <= 0)
                {
                    m.ThinkT = Rnd.Range(1.2, 3.5);
                    Dir? dir = m.Manhattan(m.HomeX, m.HomeY) > 5 ? GreedyDir(m, m.HomeX, m.HomeY) : Rnd.Pick(DirX.All);
                    if (dir.HasValue && Rnd.Next() < 0.75) StepMonster(m, dir.Value, d.MoveDur * 1.7);
                }
                return;
            }
        }
        if (!visible || dist > 14 || m.Manhattan(m.HomeX, m.HomeY) > 18)
        {
            m.State = "idle";
            m.ThinkT = 0.5;
            return;
        }
        if (d.Boss && BossSpecials(m, dist, dt)) return;
        if (dist == 1)
        {
            m.Facing = DirX.Of(P.X - m.X, P.Y - m.Y);
            if (m.Facing.Horizontal()) m.HFace = m.Facing;
            if (m.AtkCd <= 0)
            {
                m.AtkCd = d.AtkDelay;
                m.AttackAnim = 0.25;
                MonsterHit(m, 1);
            }
            return;
        }
        var step = PathDir(m, P.X, P.Y) ?? GreedyDir(m, P.X, P.Y);
        if (step.HasValue) StepMonster(m, step.Value, d.MoveDur * (st.ContainsKey("slow") ? 1.8 : 1));
    }

    private bool BossSpecials(Monster m, int dist, double dt)
    {
        if (!m.Summoned && m.Hp < m.MaxHp / 2)
        {
            m.Summoned = true;
            Msg($"{m.D.Name} roars, and the bats of the Hollow answer!", "boss");
            int n = 0;
            for (int dy = -2; dy <= 2 && n < 2; dy++)
                for (int dx = -2; dx <= 2 && n < 2; dx++)
                {
                    var bat = SpawnMonster(m.Spawn, (m.X + dx, m.Y + dy), true, "cave_bat");
                    if (bat != null) { bat.State = "chase"; n++; }
                }
        }
        m.SpecialT -= dt;
        if (m.SpecialT <= 0 && dist <= 4)
        {
            m.SpecialT = 9;
            m.RoarT = 1.2;
            Fx("roarwarn", m.X, m.Y, e => { e.R = 2; e.Dur = 1.2; });
            Sfx("growl");
            return true;
        }
        return false;
    }

    private void BossRoar(Monster m)
    {
        Fx("roar", m.X, m.Y, e => e.R = 2);
        Sfx("roar");
        if (!P.Ghost && m.Chebyshev(P) <= 2) MonsterHit(m, 1.6, true);
    }

    private void MonsterHit(Monster m, double mult, bool unavoidable = false)
    {
        if (P.Ghost) return;
        var r = Rules.RollMonsterHit(m.D, Pd, mult, P.Buffs.ContainsKey("guard"), unavoidable);
        if (r.Miss) { Number(P.X, P.Y, "dodge", "miss"); return; }
        HurtPlayer(r.Dmg);
    }

    public void HurtPlayer(int dmg)
    {
        P.Hp -= dmg;
        P.LastCombat = Time;
        P.HurtFlash = 0.15;
        Number(P.X, P.Y, dmg.ToString(), "hurt");
        Sfx("hurt");
        if (P.Hp <= 0) Die();
    }

    private void Die()
    {
        P.Hp = 0;
        P.Ghost = true;
        P.GhostAt = Time;
        P.Buffs.Clear();
        int loss = (int)Math.Floor(Rules.ExpToNext(P.Level) * 0.05);
        P.Exp = Math.Max(0, P.Exp - loss);
        Target = null;
        foreach (var m in Monsters) m.State = "idle";
        Fx("death", P.X, P.Y);
        Sfx("die");
        bool canRes = LearnedSkills().Contains("resurrect");
        Msg($"You have fallen and lost {loss} EXP. As a ghost, walk to a shrine's golden circle to return to life{(canRes ? ", or pray with Resurrect" : "")}.", "warn");
    }

    private void Revive(double frac)
    {
        P.Ghost = false;
        Refresh();
        P.Hp = Math.Max(1, Math.Round(Pd.MaxHp * frac));
        P.Mp = Math.Max(P.Mp, Math.Round(Pd.MaxMp * frac));
        Fx("revive", P.X, P.Y);
        Sfx("revive");
    }

    private bool StepMonster(Monster m, Dir dir, double dur)
    {
        var (dx, dy) = dir.Vec();
        m.Facing = dir;
        if (dx != 0) m.HFace = dir;
        int nx = m.X + dx, ny = m.Y + dy;
        if (!IsFree(nx, ny, true)) return false;
        m.StartStep(nx, ny, dur);
        return true;
    }

    private Dir? GreedyDir(Entity m, int gx, int gy)
    {
        int dx = gx - m.X, dy = gy - m.Y;
        var h = dx > 0 ? Dir.Right : Dir.Left;
        var v = dy > 0 ? Dir.Down : Dir.Up;
        var opts = new List<Dir>();
        if (Math.Abs(dx) >= Math.Abs(dy)) { if (dx != 0) opts.Add(h); if (dy != 0) opts.Add(v); }
        else { if (dy != 0) opts.Add(v); if (dx != 0) opts.Add(h); }
        foreach (var dir in opts)
        {
            var (ox, oy) = dir.Vec();
            if (IsFree(m.X + ox, m.Y + oy, true)) return dir;
        }
        return null;
    }

    // Breadth-first search toward the goal tile; returns the first step's direction.
    private Dir? PathDir(Entity m, int gx, int gy, int maxNodes = 600)
    {
        int W = Map.W, N = W * Map.H;
        if (_seen.Length != N) { _seen = new int[N]; _first = new sbyte[N]; _stamp = 0; }
        int stamp = ++_stamp;
        int start = m.Y * W + m.X, target = gy * W + gx;
        _seen[start] = stamp;
        var q = new List<int> { start };
        for (int head = 0; head < q.Count && head < maxNodes; head++)
        {
            int cur = q[head], cx = cur % W, cy = cur / W;
            for (int i = 0; i < 4; i++)
            {
                var (dx, dy) = DirX.All[i].Vec();
                int nx = cx + dx, ny = cy + dy;
                if (!Map.InBounds(nx, ny)) continue;
                int ni = ny * W + nx;
                if (_seen[ni] == stamp) continue;
                sbyte firstDir = cur == start ? (sbyte)i : _first[cur];
                if (ni == target) return DirX.All[firstDir];
                _seen[ni] = stamp;
                if (!IsFree(nx, ny, true)) continue;
                _first[ni] = firstDir;
                q.Add(ni);
            }
        }
        return null;
    }

    // ---- townsfolk -------------------------------------------------------------------
    private void UpdateFolk(double dt)
    {
        if (FolkList.Count == 0) return;
        var a = Map.FolkArea!;
        foreach (var f in FolkList)
        {
            if (f.MoveT < 1) { f.MoveT = Math.Min(1, f.MoveT + dt / f.StepDur); continue; }
            f.ThinkT -= dt;
            if (f.ThinkT > 0) continue;
            f.ThinkT = Rnd.Range(1.5, 6);
            var dir = Rnd.Pick(DirX.All);
            var (dx, dy) = dir.Vec();
            int nx = f.X + dx, ny = f.Y + dy;
            f.Facing = dir;
            if (nx < a.X0 || nx > a.X1 || ny < a.Y0 || ny > a.Y1 || !IsFree(nx, ny, true)) continue;
            f.StartStep(nx, ny, 0.24);
        }
        _folkShoutT -= dt;
        if (_folkShoutT > 0) return;
        _folkShoutT = Rnd.Range(18, 40);
        var who = Rnd.Pick(FolkList);
        var line = Rnd.Pick(Content.FolkShouts);
        Msg(line, "shout", who.Name);
        Emit(new GameEvent { Type = EvType.Bubble, Id = who.Id, Text = line });
    }

    // ---- skills ------------------------------------------------------------------------
    public List<string> LearnedSkills() => P.Cls == null ? new() :
        Content.Skills.Values.Where(s => s.Cls == P.Cls && s.Level <= P.Level).OrderBy(s => s.Level).Select(s => s.Id).ToList();

    // Slots 0-5 are class skills, 6 and 7 drink the best healing and mana items.
    public string?[] Hotbar()
    {
        var sk = LearnedSkills();
        var slots = new string?[8];
        for (int i = 0; i < 6; i++) slots[i] = i < sk.Count ? sk[i] : null;
        slots[6] = "potion:hp";
        slots[7] = "potion:mp";
        return slots;
    }

    public void UseHotbar(int i)
    {
        var s = Hotbar()[i];
        if (s == null) return;
        if (s.StartsWith("potion:")) QuickPotion(s[7..]);
        else CastSkill(s);
    }

    private Monster? MeleeTarget()
    {
        var t = GetTarget();
        if (t != null && t.Manhattan(P) == 1) return t;
        var (dx, dy) = P.Facing.Vec();
        if (EntityAt(P.X + dx, P.Y + dy) is Monster m) return m;
        return Monsters.Find(o => o.Manhattan(P) == 1);
    }

    private Monster? SpellTarget(int range)
    {
        var t = GetTarget();
        if (t == null || t.Manhattan(P) > range)
            t = Monsters.Where(o => o.Manhattan(P) <= range).OrderBy(o => o.Manhattan(P)).FirstOrDefault();
        if (t != null) Target = t.Id;
        return t;
    }

    private bool IsBehind(Monster m)
    {
        var (fx, fy) = m.Facing.Vec();
        return m.State != "chase" || (P.X == m.X - fx && P.Y == m.Y - fy);
    }

    public void CastSkill(string id)
    {
        var s = Content.Skills[id];
        if (!LearnedSkills().Contains(id)) return;
        if (s.Type == SkillType.Ghost)
        {
            if (!P.Ghost) { Msg($"{s.Name} can only be used while you are a ghost.", "warn"); return; }
            if (P.Cds.ContainsKey(id)) { Msg($"{s.Name} is not ready yet.", "warn"); return; }
            if (Time - P.GhostAt < 3) { Msg("Your spirit is still gathering itself...", "warn"); return; }
            P.Cds[id] = s.Cd;
            Revive(0.4);
            Msg("You pray, and your spirit returns to your body.");
            return;
        }
        if (P.Ghost) { Msg("Ghosts cannot use skills.", "warn"); return; }
        if (P.Cds.ContainsKey(id)) return;
        if (P.Mp < s.Mp) { Msg("Not enough mana.", "warn"); return; }
        var pd = Pd;

        switch (s.Type)
        {
            case SkillType.Melee:
            {
                var m = MeleeTarget();
                if (m == null) { Msg("No enemy within reach.", "warn"); return; }
                P.Facing = DirX.Of(m.X - P.X, m.Y - P.Y);
                P.AttackAnim = 0.22;
                Target = m.Id;
                bool forceCrit = BreakStealth();
                double mult = s.BehindMult > 0 && IsBehind(m) ? s.BehindMult : s.Mult;
                for (int i = 0; i < s.Hits; i++)
                {
                    int hit = i;
                    Later(i * 0.12, () =>
                    {
                        if (m.Hp <= 0) return;
                        Fx("slash", m.X, m.Y, e => { e.Dir = P.Facing; e.Color = s.Color; });
                        ApplyHit(m, Rules.RollMelee(pd, m.D, P.Level, mult, forceCrit && hit == 0));
                    });
                }
                if (s.Effect == "poison")
                    Later(0.05, () => { if (m.Hp > 0) m.Status["poison"] = new StatusEffect { T = s.Dur, Dmg = Math.Max(2, (int)Math.Round(pd.Atk * 0.35)) }; });
                break;
            }
            case SkillType.Around:
            {
                var ms = Monsters.Where(o => o.Chebyshev(P) == 1).ToList();
                if (ms.Count == 0) { Msg("No enemies around you.", "warn"); return; }
                P.AttackAnim = 0.3;
                bool forceCrit = BreakStealth();
                Fx("whirl", P.X, P.Y);
                foreach (var m in ms) ApplyHit(m, Rules.RollMelee(pd, m.D, P.Level, s.Mult, forceCrit));
                break;
            }
            case SkillType.Target:
            {
                var m = SpellTarget(s.Range);
                if (m == null) { Msg("No target in range. Press Tab to pick one.", "warn"); return; }
                P.Facing = DirX.Of(m.X - P.X, m.Y - P.Y);
                P.AttackAnim = 0.3;
                bool forceCrit = BreakStealth();
                var victims = s.Radius > 0 ? Monsters.Where(o => o.Chebyshev(m) <= s.Radius).ToList() : new List<Monster> { m };
                Fx(s.Fx, m.X, m.Y, e => { e.FromX = P.X; e.FromY = P.Y; e.R = s.Radius; });
                Sfx(s.Fx);
                Later(s.Fx is "fire" or "ice" ? 0.18 : 0.05, () =>
                {
                    foreach (var v in victims)
                    {
                        if (v.Hp <= 0) continue;
                        HurtMonster(v, Rules.RollSpell(pd, v.D, s.Power, forceCrit).Dmg, false, "spell");
                        if (v.Hp <= 0) continue;
                        if (s.Effect == "slow") v.Status["slow"] = new StatusEffect { T = s.Dur };
                        if (s.Effect == "stun") v.Status["stun"] = new StatusEffect { T = v.D.Boss ? 1 : s.Dur };
                    }
                });
                break;
            }
            case SkillType.Self:
                SelfSkill(s);
                break;
        }
        P.Mp -= s.Mp;
        P.Cds[id] = s.Cd;
        Refresh();
    }

    private void SelfSkill(SkillDef s)
    {
        switch (s.Effect)
        {
            case "heal":
            {
                int amt = (int)Math.Min(Pd.MaxHp - P.Hp, Rules.HealAmount(Pd));
                P.Hp += amt;
                Number(P.X, P.Y, $"+{amt}", "heal");
                Fx("heal", P.X, P.Y);
                Sfx("heal");
                break;
            }
            case "taunt":
            {
                int n = 0;
                foreach (var m in Monsters) if (m.Manhattan(P) <= s.Radius) { m.State = "chase"; n++; }
                P.Buffs["guard"] = s.Dur;
                Fx("buff", P.X, P.Y, e => e.Color = s.Color);
                Sfx("growl");
                Msg($"You taunt {n} {(n == 1 ? "foe" : "foes")} and raise your guard.", "info");
                break;
            }
            case "stealth":
                P.Buffs["stealth"] = s.Dur;
                foreach (var m in Monsters) if (m.State == "chase") { m.State = "idle"; m.ThinkT = 1; }
                Fx("smoke", P.X, P.Y);
                Sfx("stealth");
                break;
            default:
                P.Buffs[s.Effect] = s.Dur;
                Fx("buff", P.X, P.Y, e => e.Color = s.Color);
                Sfx("buff");
                break;
        }
    }

    // ---- targeting ---------------------------------------------------------------------
    public void CycleTarget()
    {
        var list = Monsters.Where(m => m.Manhattan(P) <= 9).OrderBy(m => m.Manhattan(P)).ToList();
        if (list.Count == 0) { Target = null; return; }
        int i = list.FindIndex(m => m.Id == Target);
        Target = list[(i + 1) % list.Count].Id;
    }
    public void SelectTarget(string id) => Target = id;
    public void ClearTarget() => Target = null;

    // ---- items ---------------------------------------------------------------------------
    public bool AddItem(string id, int qty = 1)
    {
        var it = Content.Items[id];
        if (it.Stack)
        {
            var s = Array.Find(P.Inv, x => x != null && x.Id == id);
            if (s != null) { s.Qty += qty; return true; }
            int free = Array.IndexOf(P.Inv, null);
            if (free < 0) return false;
            P.Inv[free] = new ItemStack(id, qty);
            return true;
        }
        for (int k = 0; k < qty; k++)
        {
            int free = Array.IndexOf(P.Inv, null);
            if (free < 0) return false;
            P.Inv[free] = new ItemStack(id);
        }
        return true;
    }

    public int CountItem(string id) => P.Inv.Where(s => s != null && s.Id == id).Sum(s => s!.Qty);

    private void RemoveItem(string id, int qty)
    {
        for (int i = 0; i < P.Inv.Length && qty > 0; i++)
        {
            var s = P.Inv[i];
            if (s == null || s.Id != id) continue;
            int take = Math.Min(qty, s.Qty);
            s.Qty -= take;
            qty -= take;
            if (s.Qty <= 0) P.Inv[i] = null;
        }
    }

    public void UseItem(int i)
    {
        var s = P.Inv[i];
        if (s == null) return;
        var it = Content.Items[s.Id];
        if (it.IsGear) { Equip(i); return; }
        if (it.Type != ItemType.Use) { Msg($"{it.Name} is a trade good. Merchants will buy it.", "info"); return; }
        if (P.Ghost) { Msg("Ghosts cannot use items.", "warn"); return; }
        if (it.Level > 0 && P.Level < it.Level) { Msg($"You need level {it.Level} for {it.Name}.", "warn"); return; }
        if (it.Heal > 0 || it.Mana > 0)
        {
            if (P.PotCd > 0) return;
            if (it.Heal > 0 && P.Hp >= Pd.MaxHp) { Msg("Your health is already full.", "info"); return; }
            if (it.Mana > 0 && P.Mp >= Pd.MaxMp) { Msg("Your mana is already full.", "info"); return; }
            if (it.Heal > 0)
            {
                int amt = (int)Math.Min(Pd.MaxHp - P.Hp, it.Heal);
                P.Hp += amt;
                Number(P.X, P.Y, $"+{amt}", "heal");
            }
            if (it.Mana > 0)
            {
                int amt = (int)Math.Min(Pd.MaxMp - P.Mp, it.Mana);
                P.Mp += amt;
                Number(P.X, P.Y, $"+{amt}", "mana");
            }
            P.PotCd = 1;
            Sfx("drink");
        }
        Consume(i);
        if (it.Teleport)
        {
            Fx("portal", P.X, P.Y);
            EnterMap("town", Rules.TownSpawnX, Rules.TownSpawnY, Dir.Down);
            Msg("The scroll's wind carries you home.", "info");
        }
    }

    private void Consume(int i)
    {
        var s = P.Inv[i]!;
        if (--s.Qty <= 0) P.Inv[i] = null;
    }

    public string? BestPotion(string which)
    {
        string? best = null;
        int bestVal = 0;
        foreach (var s in P.Inv)
        {
            if (s == null) continue;
            var it = Content.Items[s.Id];
            int v = which == "hp" ? it.Heal : it.Mana;
            if (v > bestVal && (it.Level == 0 || P.Level >= it.Level)) { best = s.Id; bestVal = v; }
        }
        return best;
    }

    private void QuickPotion(string which)
    {
        var best = BestPotion(which);
        if (best == null) { Msg(which == "hp" ? "You have no healing tonics." : "You have no tea for your mana.", "warn"); return; }
        UseItem(Array.FindIndex(P.Inv, s => s != null && s.Id == best));
    }

    public void Equip(int i)
    {
        var s = P.Inv[i]!;
        var it = Content.Items[s.Id];
        var problem = Rules.EquipProblem(P, it);
        if (problem != null) { Msg(problem, "warn"); return; }
        string slot = it.Type.ToString().ToLowerInvariant();
        if (it.Type == ItemType.Ring) slot = P.Equip["ring1"] == null ? "ring1" : P.Equip["ring2"] == null ? "ring2" : "ring1";
        var old = P.Equip[slot];
        P.Equip[slot] = s.Id;
        P.Inv[i] = old != null ? new ItemStack(old) : null;
        Sfx("equip");
        Refresh();
    }

    public void Unequip(string slot)
    {
        var id = P.Equip[slot];
        if (id == null) return;
        int i = Array.IndexOf(P.Inv, null);
        if (i < 0) { Msg("Your bag is full.", "warn"); return; }
        P.Inv[i] = new ItemStack(id);
        P.Equip[slot] = null;
        Sfx("equip");
        Refresh();
    }

    public void DropItem(int i)
    {
        var s = P.Inv[i];
        if (s == null) return;
        P.Inv[i] = null;
        Msg($"You threw away {Content.Items[s.Id].Name}{(s.Qty > 1 ? $" x{s.Qty}" : "")}.", "info");
    }

    // ---- shops, storage, inn -------------------------------------------------------------
    public void Buy(string shopId, string itemId, int qty = 1)
    {
        if (!Content.Shops.TryGetValue(shopId, out var list) || !list.Contains(itemId)) return;
        var it = Content.Items[itemId];
        int cost = it.Price * qty;
        if (P.Gold < cost) { Msg("You cannot afford that.", "warn"); return; }
        if (!AddItem(itemId, qty)) { Msg("Your bag is full.", "warn"); return; }
        P.Gold -= cost;
        Sfx("coin");
        Msg($"Bought {it.Name}{(qty > 1 ? $" x{qty}" : "")} for {cost} gold.", "info");
    }

    public void Sell(int i, bool all)
    {
        var s = P.Inv[i];
        if (s == null) return;
        var it = Content.Items[s.Id];
        int qty = all ? s.Qty : 1;
        int gold = Rules.SellPrice(it) * qty;
        s.Qty -= qty;
        if (s.Qty <= 0) P.Inv[i] = null;
        P.Gold += gold;
        Sfx("coin");
        Msg($"Sold {it.Name}{(qty > 1 ? $" x{qty}" : "")} for {gold} gold.", "info");
    }

    public void Deposit(int i) => MoveStack(P.Inv, i, P.Storage, "Your storage is full.");
    public void Withdraw(int j) => MoveStack(P.Storage, j, P.Inv, "Your bag is full.");

    private void MoveStack(ItemStack?[] from, int i, ItemStack?[] to, string fullMsg)
    {
        var s = from[i];
        if (s == null) return;
        var stack = Content.Items[s.Id].Stack ? Array.Find(to, x => x != null && x.Id == s.Id) : null;
        if (stack != null) stack.Qty += s.Qty;
        else
        {
            int j = Array.IndexOf(to, null);
            if (j < 0) { Msg(fullMsg, "warn"); return; }
            to[j] = s.Clone();
        }
        from[i] = null;
        Sfx("equip");
    }

    public int RestCost => P.Level < 5 ? 0 : 10;

    public void Rest()
    {
        if (P.Ghost) { Msg("The innkeeper cannot see you. Find the shrine first.", "warn"); return; }
        if (P.Gold < RestCost) { Msg("You cannot afford a room.", "warn"); return; }
        P.Gold -= RestCost;
        P.Hp = Pd.MaxHp;
        P.Mp = Pd.MaxMp;
        Fx("heal", P.X, P.Y);
        Sfx("revive");
        Msg(RestCost > 0 ? $"You rest for {RestCost} gold and wake fully restored." : "You rest and wake fully restored.", "info");
    }

    // ---- progression ---------------------------------------------------------------------
    public void ChooseClass(string cls)
    {
        if (P.Cls != null) { Msg($"You already walk the path of the {Content.Classes[P.Cls].Name}.", "warn"); return; }
        if (P.Level < 5) { Msg("Come back when you reach level 5.", "warn"); return; }
        P.Cls = cls;
        var c = Content.Classes[cls];
        AddItem(c.Weapon);
        Refresh();
        P.Hp = Pd.MaxHp;
        P.Mp = Pd.MaxMp;
        Fx("levelup", P.X, P.Y);
        Sfx("quest");
        Msg($"You are now a {c.Name}! Your master gives you the {Content.Items[c.Weapon].Name}.", "level");
        foreach (var id in LearnedSkills()) Msg($"You learned {Content.Skills[id].Name}. Press its number key to use it.", "level");
    }

    public void AllocStat(Stat k)
    {
        if (P.Unspent <= 0) return;
        P.Stats[k]++;
        P.Unspent--;
        Refresh();
        Sfx("ui");
    }

    public string QuestState(string qid)
    {
        var q = Content.Quests[qid];
        if (P.Quests.TryGetValue(qid, out var qs))
        {
            if (qs.State == "done") return "done";
            return QuestReady(qid) ? "ready" : "active";
        }
        return P.Level >= q.Level ? "available" : "locked";
    }

    private bool QuestReady(string qid)
    {
        var q = Content.Quests[qid];
        return q.KillMonster != null ? P.Quests[qid].Count >= q.Count : CountItem(q.BringItem!) >= q.Count;
    }

    public string QuestProgress(string qid)
    {
        var q = Content.Quests[qid];
        int count = P.Quests.TryGetValue(qid, out var qs) ? qs.Count : 0;
        return q.KillMonster != null
            ? $"{Content.Monsters[q.KillMonster].Name}: {Math.Min(count, q.Count)}/{q.Count}"
            : $"{Content.Items[q.BringItem!].Name}: {Math.Min(CountItem(q.BringItem!), q.Count)}/{q.Count}";
    }

    public void AcceptQuest(string qid)
    {
        if (QuestState(qid) != "available") return;
        P.Quests[qid] = new QuestProgress();
        Sfx("ui");
        Msg($"New quest: {Content.Quests[qid].Title}. ({QuestProgress(qid)})", "quest");
    }

    public void CompleteQuest(string qid)
    {
        if (QuestState(qid) != "ready") return;
        var q = Content.Quests[qid];
        if (q.BringItem != null) RemoveItem(q.BringItem, q.Count);
        P.Quests[qid].State = "done";
        P.Gold += q.RewardGold;
        Msg($"Quest complete: {q.Title}! +{q.RewardExp} EXP, +{q.RewardGold} gold", "quest");
        foreach (var (id, n) in q.RewardItems)
        {
            if (AddItem(id, n)) Msg($"Received {Content.Items[id].Name}{(n > 1 ? $" x{n}" : "")}.", "loot");
            else Msg($"Your bag is full, so {Content.Items[id].Name} was lost.", "warn");
        }
        Sfx("quest");
        GainExp(q.RewardExp);
    }

    // '!' when an NPC has a quest to offer, '?' when one is ready to turn in.
    public char? QuestMark(string npcId)
    {
        char? mark = null;
        foreach (var q in Content.Quests.Values)
        {
            if (q.Giver != npcId) continue;
            var st = QuestState(q.Id);
            if (st == "ready") return '?';
            if (st == "available") mark = '!';
        }
        return mark;
    }

    public void TalkTo(string npcId)
    {
        var n = Npcs.Find(x => x.Id == npcId);
        if (n == null) return;
        if (n.Manhattan(P) > 3) { Msg($"{n.Name} is too far away to hear you.", "warn"); return; }
        if (n.Manhattan(P) > 0) n.Facing = DirX.Of(P.X - n.X, P.Y - n.Y);
        if (P.Ghost && n.Def.Services.Contains("revive"))
        {
            Revive(0.6);
            Msg($"{n.Name} chants softly, and you return to life.");
        }
        Emit(new GameEvent { Type = EvType.Dialog, Id = npcId });
    }

    // ---- chat ------------------------------------------------------------------------------
    public void Say(string raw)
    {
        var text = raw.Trim();
        if (text.Length > 120) text = text[..120];
        if (text.Length == 0) return;
        if (!text.StartsWith('/'))
        {
            Msg(text, "local", P.Name);
            Emit(new GameEvent { Type = EvType.Bubble, Id = "player", Text = text });
            var lower = text.ToLowerInvariant();
            if (FolkList.Count > 0 && (lower.StartsWith("hi") || lower.Contains("hello") || lower.StartsWith("hey") || lower.Contains("안녕")))
            {
                var f = Rnd.Pick(FolkList);
                Later(Rnd.Range(1, 2.5), () =>
                {
                    var line = Rnd.Pick(Content.FolkGreetings);
                    Msg(line, "local", f.Name);
                    Emit(new GameEvent { Type = EvType.Bubble, Id = f.Id, Text = line });
                });
            }
            return;
        }
        var parts = text[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts.Length > 0 ? parts[0].ToLowerInvariant() : "";
        var rest = parts.Skip(1).ToArray();
        switch (cmd)
        {
            case "s": case "shout":
                if (rest.Length == 0) return;
                Msg(string.Join(' ', rest), "shout", P.Name);
                Emit(new GameEvent { Type = EvType.Bubble, Id = "player", Text = string.Join(' ', rest) });
                break;
            case "w": case "whisper":
            {
                if (rest.Length < 2) { Msg("Usage: /w name message", "info"); return; }
                var f = Content.Folk.FirstOrDefault(x => x.Name.Equals(rest[0], StringComparison.OrdinalIgnoreCase));
                if (f == null) { Msg($"{rest[0]} is not online.", "info"); return; }
                Msg($"To {f.Name}: {string.Join(' ', rest.Skip(1))}", "whisper");
                Later(Rnd.Range(1.5, 4), () => Msg(Rnd.Pick(Content.FolkWhispers), "whisper", f.Name));
                break;
            }
            case "p": case "party":
                Msg("You are not in a party. Parties arrive with the online update.", "info");
                break;
            case "who":
                Msg($"Online: {string.Join(", ", new[] { P.Name }.Concat(Content.Folk.Select(f => f.Name)))}", "info");
                break;
            default:
                Msg("Commands: /s message (shout), /w name message (whisper), /p (party), /who", "info");
                break;
        }
    }
}
