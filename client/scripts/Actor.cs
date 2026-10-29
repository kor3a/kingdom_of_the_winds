using Godot;
using KotW.Shared;

namespace KotW;

// One on-screen character or monster. People are a stack of paper-doll layers
// (skin, base clothes, armour, hair, helmet, shield, weapon); the draw order
// changes with facing so a weapon held on the far side goes behind the body.
public partial class Actor : Node2D
{
    private const int Cols = 6;
    private Sprite2D _skin = null!, _body = null!, _armor = null!, _hair = null!, _helmet = null!, _shield = null!, _weapon = null!, _ghost = null!;
    private Sprite2D? _mon;
    private Look? _look;
    private Dir? _orderFor;

    public static Actor Person()
    {
        var a = new Actor();
        Sprite2D Layer() { var s = new Sprite2D { Hframes = Cols, Vframes = 4, Centered = false, Offset = new Vector2(-16, -46) }; a.AddChild(s); return s; }
        a._shield = Layer(); a._weapon = Layer(); a._skin = Layer(); a._body = Layer(); a._armor = Layer();
        a._hair = Layer(); a._helmet = Layer(); a._ghost = Layer();
        a._body.Texture = Assets.Person("body");
        a._skin.Texture = Assets.Person("skin");
        a._ghost.Texture = Assets.Person("ghost");
        return a;
    }

    public static Actor ForMonster(MonsterDef d)
    {
        var a = new Actor();
        var tex = Assets.Monster(d.Sprite);
        int w = tex.GetWidth() / 3, h = tex.GetHeight();
        a._mon = new Sprite2D { Texture = tex, Hframes = 3, Centered = false, Offset = new Vector2(-w / 2f, -h + 2) };
        a.AddChild(a._mon);
        return a;
    }

    public void SetLook(Look look)
    {
        if (look == _look) return;
        _look = look;
        _skin.Material = Assets.Tint(look.Skin);
        _hair.Texture = Assets.Person($"hair_{look.Style}");
        _hair.Material = Assets.Tint(look.Hair);
        _armor.Texture = Assets.Gear(look.Armor);
        _helmet.Texture = Assets.Gear(look.Helmet);
        _weapon.Texture = Assets.Gear(look.Weapon);
        _shield.Texture = Assets.Gear(look.Shield);
    }

    private void Order(Dir d)
    {
        if (_orderFor == d) return;
        _orderFor = d;
        Sprite2D[] order = d switch
        {
            Dir.Up => new[] { _weapon, _shield, _skin, _body, _armor, _hair, _helmet },
            Dir.Down => new[] { _skin, _body, _armor, _hair, _helmet, _shield, _weapon },
            _ => new[] { _shield, _skin, _body, _armor, _hair, _helmet, _weapon },
        };
        for (int i = 0; i < order.Length; i++) MoveChild(order[i], i);
    }

    // col: 0 idle, 1-3 walk, 4-5 attack
    public void ShowPerson(Dir dir, int col, bool ghost, float alpha)
    {
        Order(dir);
        int frame = (int)dir * Cols + col;
        foreach (var s in new[] { _skin, _body, _armor, _hair, _helmet, _shield, _weapon })
        {
            s.Visible = !ghost && s.Texture != null;
            s.Frame = frame;
        }
        _ghost.Visible = ghost;
        _ghost.Frame = (int)dir * Cols + col % 4;
        Modulate = new Color(1, 1, 1, alpha);
    }

    public void ShowMonster(Dir hface, int col, bool flash, float bob)
    {
        if (_mon == null) return;
        _mon.Frame = col;
        _mon.FlipH = hface == Dir.Left;
        _mon.Position = new Vector2(0, bob);
        _mon.Modulate = flash ? new Color(4, 4, 4) : Colors.White;
    }
}
