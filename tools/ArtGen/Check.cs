using KotW.Shared;

namespace KotW.ArtGen;

// Cross-checks the content tables and maps before any art is drawn: every ID a table mentions
// must exist, every portal must land on open ground, and every NPC must be reachable.
public static class Check
{
    public static List<string> Run()
    {
        var bad = new List<string>();
        void Need(bool ok, string what) { if (!ok) bad.Add(what); }
        bool Item(string id) => Content.Items.ContainsKey(id);

        foreach (var c in Content.Classes.Values)
        {
            if (c.Weapon != "") Need(Item(c.Weapon), $"class {c.Id}: weapon {c.Weapon}");
            if (c.Base != null) Need(Content.Classes.ContainsKey(c.Base), $"class {c.Id}: base {c.Base}");
            if (c.Trial != null) Need(Content.Quests.ContainsKey(c.Trial), $"class {c.Id}: trial {c.Trial}");
        }
        foreach (var s in Content.Skills.Values) Need(Content.Classes.ContainsKey(s.Cls), $"skill {s.Id}: class {s.Cls}");
        foreach (var m in Content.Monsters.Values)
        {
            Need(Monsters.All.ContainsKey(m.Sprite), $"monster {m.Id}: sprite {m.Sprite}");
            if (m.Summon != null) Need(Content.Monsters.ContainsKey(m.Summon), $"monster {m.Id}: summon {m.Summon}");
            foreach (var (item, _) in m.Drops) Need(Item(item), $"monster {m.Id}: drop {item}");
        }
        foreach (var (shop, list) in Content.Shops) foreach (var id in list) Need(Item(id), $"shop {shop}: {id}");
        foreach (var n in Content.Npcs.Values)
        {
            foreach (var id in new[] { n.Look.Armor, n.Look.Weapon, n.Look.Helmet, n.Look.Shield }) if (id != null) Need(Item(id), $"npc {n.Id}: gear {id}");
            foreach (var svc in n.Services.Where(v => v.StartsWith("shop:"))) Need(Content.Shops.ContainsKey(svc[5..]), $"npc {n.Id}: {svc}");
            foreach (var svc in n.Services.Where(v => v.StartsWith("job:"))) Need(Content.Classes.ContainsKey(svc[4..]), $"npc {n.Id}: {svc}");
        }
        foreach (var f in Content.Folk) foreach (var id in new[] { f.Look.Armor, f.Look.Weapon, f.Look.Helmet, f.Look.Shield }) if (id != null) Need(Item(id), $"folk {f.Name}: gear {id}");
        foreach (var q in Content.Quests.Values)
        {
            Need(Content.Npcs.ContainsKey(q.Giver), $"quest {q.Id}: giver {q.Giver}");
            if (q.TurnIn != null) Need(Content.Npcs.ContainsKey(q.TurnIn), $"quest {q.Id}: turn-in {q.TurnIn}");
            if (q.KillMonster != null) Need(Content.Monsters.ContainsKey(q.KillMonster), $"quest {q.Id}: monster {q.KillMonster}");
            if (q.BringItem != null) Need(Item(q.BringItem), $"quest {q.Id}: item {q.BringItem}");
            if (q.Requires != null) Need(Content.Quests.ContainsKey(q.Requires), $"quest {q.Id}: requires {q.Requires}");
            foreach (var (item, _) in q.RewardItems) Need(Item(item), $"quest {q.Id}: reward {item}");
        }

        var maps = Maps.BuildAll();
        foreach (var it in Content.Items.Values.Where(i => i.Teleport))
            Need(maps.TryGetValue(it.Dest, out var dm) && dm.Spawn is { } sp && dm.Walkable(sp.X, sp.Y), $"item {it.Id}: destination {it.Dest}");
        foreach (var m in maps.Values)
        {
            foreach (var c in m.T) Need(Content.Tiles.ContainsKey(c), $"map {m.Id}: unknown tile '{c}'");
            var reach = Reachable(m);
            foreach (var pt in m.Portals.Values)
            {
                if (!maps.TryGetValue(pt.To, out var to)) { bad.Add($"map {m.Id}: portal to missing map {pt.To}"); continue; }
                Need(to.Walkable(pt.Tx, pt.Ty) && to.PortalAt(pt.Tx, pt.Ty) == null, $"map {m.Id}: portal ({pt.X},{pt.Y}) lands on {pt.To} ({pt.Tx},{pt.Ty}) '{to.Tile(pt.Tx, pt.Ty)}'");
                Need(reach.Contains((pt.X, pt.Y)), $"map {m.Id}: portal ({pt.X},{pt.Y}) cannot be reached");
            }
            foreach (var n in Content.Npcs.Values.Where(n => n.Map == m.Id))
                Need(new[] { (0, 1), (0, -1), (1, 0), (-1, 0) }.Any(d => reach.Contains((n.X + d.Item1, n.Y + d.Item2))), $"npc {n.Id}: nobody can walk up to ({n.X},{n.Y}) on {m.Id}");
            foreach (var s in m.Spawns)
            {
                Need(Content.Monsters.ContainsKey(s.Monster), $"map {m.Id}: spawn {s.Monster}");
                if (s.Fixed) Need(reach.Contains((s.X0, s.Y0)), $"map {m.Id}: {s.Monster} stands on unreachable ({s.X0},{s.Y0}) '{m.Tile(s.X0, s.Y0)}'");
                else
                {
                    int open = 0;
                    for (int y = s.Y0; y <= s.Y1; y++) for (int x = s.X0; x <= s.X1; x++) if (reach.Contains((x, y))) open++;
                    Need(open >= s.Count * 4, $"map {m.Id}: only {open} open tiles for {s.Count} {s.Monster}");
                }
            }
        }
        return bad;
    }

    // Every tile you can walk to from the map's first portal.
    private static HashSet<(int, int)> Reachable(GameMap m)
    {
        var seen = new HashSet<(int, int)>();
        var start = m.Portals.Values.First();
        var stack = new Stack<(int X, int Y)>();
        stack.Push((start.X, start.Y));
        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();
            if (!m.Walkable(x, y) || !seen.Add((x, y))) continue;
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }) stack.Push((x + dx, y + dy));
        }
        return seen;
    }
}
