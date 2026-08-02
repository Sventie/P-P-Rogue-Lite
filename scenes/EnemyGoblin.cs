namespace PPRogueLite;

using Godot;
using PPRogueLite.Combat;
using CharacterEnemy = PPRogueLite.Character.Enemy;

/// <summary>
/// Gegner der Echtzeit-Arena: läuft direkt auf die Spielerposition zu und
/// greift bei Kontakt automatisch an (verstecktem W20-Wurf, siehe Player).
/// Kein Godot-Physik-Kollisionssystem - Distanzprüfung per Hand, um
/// Kollisions-Layer/Masken-Konfiguration (ungetestet) zu vermeiden.
/// </summary>
public partial class EnemyGoblin : Node2D
{
    private const float Speed = 80f;
    private const float Radius = 14f;
    private const float ContactRange = 26f;
    private const float ContactCooldown = 1.0f;
    private const float HpBarWidth = 30f;
    private const float HpBarHeight = 4f;
    private const float HpBarOffsetY = -24f;

    private static readonly Color BodyColor = new(0.611765f, 0.231373f, 0.231373f);
    private static readonly Color MissColor = new(0.662745f, 0.603922f, 0.470588f);
    private static readonly Color HpBarBackground = new(0f, 0f, 0f, 0.5f);
    private static readonly Color HpBarFill = new(0.352941f, 0.478431f, 0.309804f);

    public CharacterEnemy Stats { get; private set; } = null!;

    private Player? _player;
    private float _contactTimer;
    private bool _disabled;
    private PackedScene _floatingTextScene = null!;

    public override void _Ready()
    {
        AddToGroup("enemies");
        _floatingTextScene = GD.Load<PackedScene>("res://scenes/FloatingText.tscn");

        Stats = new CharacterEnemy
        {
            Name = "Höhlengoblin",
            MaxHp = 12,
            Hp = 12,
            // Test-Balance-Wert: bewusst niedriger als die kanonische RK 13
            // aus dem Prototyp/Deck-Screen, damit sich XP/Level-ups in der
            // Arena beim Testen schneller sammeln lassen.
            ArmorClass = 8,
            AttackBonus = 4,
            DamageDie = 6,
            DamageBonus = 2,
        };

        var players = GetTree().GetNodesInGroup("player");
        _player = players.Count > 0 ? players[0] as Player : null;

        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, Radius, BodyColor);

        var topLeft = new Vector2(-HpBarWidth / 2f, HpBarOffsetY);
        DrawRect(new Rect2(topLeft, new Vector2(HpBarWidth, HpBarHeight)), HpBarBackground);

        float ratio = Stats.MaxHp > 0 ? Mathf.Clamp((float)Stats.Hp / Stats.MaxHp, 0f, 1f) : 0f;
        DrawRect(new Rect2(topLeft, new Vector2(HpBarWidth * ratio, HpBarHeight)), HpBarFill);
    }

    public override void _Process(double delta)
    {
        if (_disabled || _player is null || Stats.IsDefeated)
        {
            return;
        }

        Vector2 toPlayer = _player.Position - Position;
        float distance = toPlayer.Length();

        if (distance > ContactRange)
        {
            Position += toPlayer.Normalized() * Speed * (float)delta;
            return;
        }

        _contactTimer -= (float)delta;
        if (_contactTimer <= 0f)
        {
            _contactTimer = ContactCooldown;
            AttackPlayer();
        }
    }

    private void AttackPlayer()
    {
        if (_player is null)
        {
            return;
        }

        int roll = Dice.Roll(20);
        int total = roll + Stats.AttackBonus;
        bool hit = total >= _player.EffectiveArmorClass;

        if (!hit)
        {
            SpawnFloatingText(MissColor, "Verfehlt");
            return;
        }

        int damage = Dice.Roll(Stats.DamageDie) + Stats.DamageBonus;
        _player.TakeDamage(damage);
        SpawnFloatingText(BodyColor, damage.ToString());
    }

    public void TakeDamage(int amount)
    {
        Stats.TakeDamage(amount);
        QueueRedraw();

        if (!Stats.IsDefeated)
        {
            return;
        }

        _player?.GrantXp(1);
        QueueFree();
    }

    public void SetDisabled(bool disabled)
    {
        _disabled = disabled;
    }

    private void SpawnFloatingText(Color color, string text)
    {
        var floatingText = _floatingTextScene.Instantiate<FloatingText>();
        GetParent()?.AddChild(floatingText);
        floatingText.GlobalPosition = GlobalPosition;
        floatingText.ShowText(text, color);
    }
}
