namespace PPRogueLite;

using Godot;
using PPRogueLite.Combat;
using PPRogueLite.Enemies;
using CharacterEnemy = PPRogueLite.Character.Enemy;

/// <summary>
/// Gemeinsamer Gegner-Node der Echtzeit-Arena (Issue #9, löst das bisher
/// goblin-spezifische EnemyGoblin.cs ab): Bewegung/Angriff werden nicht
/// pro Gegnertyp dupliziert, sondern über eine EnemyDefinition
/// (PPRogueLite.Enemies, kein Godot-Bezug) datengetrieben verzweigt -
/// analog zu Player.TriggerAbility, das über CardDefinition.Id switcht.
///
/// Arena.SpawnEnemy() instanziert diese Szene, ruft AddChild() und danach
/// Initialize(definition) auf (Godot-Lifecycle-Regel: _Ready() feuert
/// erst NACH AddChild(), Initialize() setzt die eigentlichen Werte also
/// erst danach - siehe CLAUDE.md "Node-Lifecycle").
///
/// Kein Godot-Physik-Kollisionssystem - Distanzprüfung per Hand, um
/// Kollisions-Layer/Masken-Konfiguration (ungetestet) zu vermeiden.
/// </summary>
public partial class Enemy : Node2D
{
    private const float HpBarWidth = 30f;
    private const float HpBarHeight = 4f;
    private const float RangedTolerance = 40f;
    private const float RetreatDuration = 1.0f;
    private const float SlowDuration = 2f;
    private const float SlowMultiplier = 0.5f;

    private static readonly Color MissColor = new(0.662745f, 0.603922f, 0.470588f);
    private static readonly Color SlowColor = new(0.45f, 0.25f, 0.55f);
    private static readonly Color HpBarBackground = new(0f, 0f, 0f, 0.5f);
    private static readonly Color HpBarFill = new(0.352941f, 0.478431f, 0.309804f);

    public CharacterEnemy Stats { get; private set; } = null!;

    private EnemyDefinition _definition = null!;
    private Player? _player;
    private float _attackTimer;
    private float _retreatTimer;
    private bool _disabled;
    private PackedScene _floatingTextScene = null!;

    public override void _Ready()
    {
        AddToGroup("enemies");
        _floatingTextScene = GD.Load<PackedScene>("res://scenes/FloatingText.tscn");

        var players = GetTree().GetNodesInGroup("player");
        _player = players.Count > 0 ? players[0] as Player : null;
    }

    /// <summary>Setzt Stats/Verhalten des Gegners - erst nach AddChild() aufrufen (siehe Klassenkommentar).</summary>
    public void Initialize(EnemyDefinition definition)
    {
        _definition = definition;
        Stats = new CharacterEnemy
        {
            Name = definition.DisplayName,
            MaxHp = definition.MaxHp,
            Hp = definition.MaxHp,
            ArmorClass = definition.ArmorClass,
            AttackBonus = definition.AttackBonus,
            DamageDie = definition.DamageDie,
            DamageBonus = definition.DamageBonus,
        };
        QueueRedraw();
    }

    public override void _Draw()
    {
        float radius = _definition.Radius;
        DrawCircle(Vector2.Zero, radius, ColorFor(_definition.Id));

        float hpBarOffsetY = -(radius + 10f);
        var topLeft = new Vector2(-HpBarWidth / 2f, hpBarOffsetY);
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

        float dt = (float)delta;
        switch (_definition.Movement)
        {
            case EnemyMovement.Melee:
                UpdateMelee(dt);
                break;
            case EnemyMovement.Ranged:
                UpdateRanged(dt);
                break;
            case EnemyMovement.HitAndRun:
                UpdateHitAndRun(dt);
                break;
        }
    }

    private void UpdateMelee(float delta)
    {
        Vector2 toPlayer = _player!.Position - Position;
        float distance = toPlayer.Length();

        if (distance > _definition.EngagementRange)
        {
            Position += toPlayer.Normalized() * _definition.MoveSpeed * delta;
            return;
        }

        _attackTimer -= delta;
        if (_attackTimer <= 0f)
        {
            _attackTimer = _definition.AttackCooldown;
            AttackPlayer();
        }
    }

    /// <summary>Hält EngagementRange als Wunschabstand (nähert sich/weicht zurück), feuert Projektile sobald in Reichweite.</summary>
    private void UpdateRanged(float delta)
    {
        Vector2 toPlayer = _player!.Position - Position;
        float distance = toPlayer.Length();

        if (distance > _definition.EngagementRange + RangedTolerance)
        {
            Position += toPlayer.Normalized() * _definition.MoveSpeed * delta;
        }
        else if (distance < _definition.EngagementRange - RangedTolerance)
        {
            Position -= toPlayer.Normalized() * _definition.MoveSpeed * delta;
        }

        _attackTimer -= delta;
        if (_attackTimer <= 0f && distance <= _definition.EngagementRange + RangedTolerance)
        {
            _attackTimer = _definition.AttackCooldown;
            FireProjectile(toPlayer.Normalized());
        }
    }

    /// <summary>Nähert sich, greift bei Kontakt an, zieht sich danach für RetreatDuration zurück statt stehen zu bleiben.</summary>
    private void UpdateHitAndRun(float delta)
    {
        Vector2 toPlayer = _player!.Position - Position;

        if (_retreatTimer > 0f)
        {
            _retreatTimer -= delta;
            Position -= toPlayer.Normalized() * _definition.MoveSpeed * delta;
            return;
        }

        float distance = toPlayer.Length();
        if (distance > _definition.EngagementRange)
        {
            Position += toPlayer.Normalized() * _definition.MoveSpeed * delta;
            return;
        }

        _attackTimer -= delta;
        if (_attackTimer <= 0f)
        {
            _attackTimer = _definition.AttackCooldown;
            AttackPlayer();
            _retreatTimer = RetreatDuration;
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
        SpawnFloatingText(ColorFor(_definition.Id), damage.ToString());
        ApplyOnHitEffect();
    }

    private void FireProjectile(Vector2 direction)
    {
        var projectile = new Projectile
        {
            Direction = direction,
            AttackBonus = Stats.AttackBonus,
            DamageDie = Stats.DamageDie,
            DamageBonus = Stats.DamageBonus,
            OnHitEffect = _definition.OnHitEffect,
            Color = ColorFor(_definition.Id),
        };
        GetParent()?.AddChild(projectile);
        projectile.GlobalPosition = GlobalPosition;
    }

    private void ApplyOnHitEffect()
    {
        if (_definition.OnHitEffect == EnemyOnHitEffect.Slow)
        {
            _player?.ApplySlow(SlowDuration, SlowMultiplier);
            SpawnFloatingText(SlowColor, "Verlangsamt!");
        }
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

    private static Color ColorFor(string id) => id switch
    {
        "goblin" => new Color(0.611765f, 0.231373f, 0.231373f),
        "troll" => new Color(0.4f, 0.35f, 0.25f),
        "archer" => new Color(0.3f, 0.5f, 0.3f),
        "schamane" => new Color(0.45f, 0.25f, 0.55f),
        "schurke" => new Color(0.25f, 0.25f, 0.4f),
        _ => new Color(0.5f, 0.5f, 0.5f),
    };
}
