namespace PPRogueLite;

using Godot;
using PPRogueLite.Character;
using PPRogueLite.Combat;
using PPRogueLite.Meta;

/// <summary>
/// Gruppenmitglied neben dem Hauptcharakter (Issue #5): eigene HP/RK aus
/// seiner CharacterClassDefinition, folgt dem Leader über einen festen
/// Formations-Versatz (kein Pathfinding, wie überall sonst im Projekt) und
/// greift automatisch NUR mit seiner StartingCardId-Karte an (Hieb/
/// Pfeilschuss/Arkaner Blitz) - Karten/Modifikatoren/Level-up gelten bisher
/// ausschließlich für den Hauptcharakter (Player), bis die Kartenzuweisung
/// pro Charakter existiert (Issue #26). Bewusst vereinfachte Trefferauflösung
/// ohne Vorteil/Krit-Hooks, gleiches Prinzip wie ein gewöhnlicher Gegner.
///
/// Tötet ein Companion einen Gegner, landet die XP trotzdem im gemeinsamen
/// Pool des Hauptcharakters (Enemy.TakeDamage ruft dafür Player.Instance
/// direkt auf, unabhängig davon, wer den Kill gemacht hat - siehe Player.cs).
///
/// Stirbt dieser Companion (HP 0), ist das dauerhaft (Permadeath, Issue #7):
/// PlayerCharacterCollection.MarkDead entfernt ihn aus der Gruppenauswahl,
/// der Node wird aus der laufenden Arena entfernt, der Run läuft weiter -
/// im Gegensatz zum Leader, dessen Niederlage den Run sofort beendet.
/// </summary>
public partial class Companion : Node2D, IPartyMember
{
    private const float Radius = 13f;
    private const float MeleeRange = 90f;
    private const float RangedAttackRange = 300f;
    private const float FollowSpeed = 260f; // etwas schneller als Player.Speed, damit die Formation nicht dauerhaft hinterherhinkt
    private const float HpBarWidth = 26f;
    private const float HpBarHeight = 4f;

    private static readonly Color MissColor = new(0.662745f, 0.603922f, 0.470588f);
    private static readonly Color HitColor = new(0.352941f, 0.478431f, 0.309804f);
    private static readonly Color HpBarBackground = new(0f, 0f, 0f, 0.5f);
    private static readonly Color HpBarFill = new(0.352941f, 0.478431f, 0.309804f);

    public OwnedCharacter Owned { get; private set; } = null!;

    public Player Leader { get; private set; } = null!;

    public Vector2 FormationOffset { get; set; }

    public int CurrentHp => _hp;

    public int EffectiveArmorClass => _armorClass;

    public bool IsDefeated => _hp <= 0;

    private AbilityScores _stats = null!;
    private string _abilityCardId = null!;
    private int _hp;
    private int _maxHp;
    private int _armorClass;
    private float _cooldown;
    private float _timer;
    private float _slowTimer;
    private float _slowMultiplier = 1f;
    private bool _disabled;
    private PackedScene _floatingTextScene = null!;

    public override void _Ready()
    {
        AddToGroup("party");
        _floatingTextScene = GD.Load<PackedScene>("res://scenes/FloatingText.tscn");
    }

    /// <summary>Setzt Stats/Zugehörigkeit - erst nach AddChild() aufrufen (gleiche Node-Lifecycle-Regel wie Enemy.Initialize).</summary>
    public void Initialize(OwnedCharacter owned, Player leader, Vector2 formationOffset, int? savedHp)
    {
        Owned = owned;
        Leader = leader;
        FormationOffset = formationOffset;

        var definition = owned.ClassDefinition;
        _stats = definition.BuildStats();
        _maxHp = definition.MaxHp;
        _hp = savedHp ?? _maxHp;
        _armorClass = definition.BaseArmorClass;
        _abilityCardId = definition.StartingCardId;
        _cooldown = CooldownFor(_abilityCardId);
        _timer = _cooldown;
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, Radius, ColorFor(Owned.ClassDefinition.ClassName));

        float hpBarOffsetY = -(Radius + 8f);
        var topLeft = new Vector2(-HpBarWidth / 2f, hpBarOffsetY);
        DrawRect(new Rect2(topLeft, new Vector2(HpBarWidth, HpBarHeight)), HpBarBackground);

        float ratio = _maxHp > 0 ? Mathf.Clamp((float)_hp / _maxHp, 0f, 1f) : 0f;
        DrawRect(new Rect2(topLeft, new Vector2(HpBarWidth * ratio, HpBarHeight)), HpBarFill);
    }

    public override void _Process(double delta)
    {
        if (_disabled || IsDefeated)
        {
            return;
        }

        float dt = (float)delta;
        FollowLeader(dt);
        UpdateAbility(dt);

        if (_slowTimer > 0f)
        {
            _slowTimer -= dt;
            if (_slowTimer <= 0f)
            {
                _slowMultiplier = 1f;
            }
        }
    }

    private void FollowLeader(float delta)
    {
        var targetPosition = Leader.Position + FormationOffset;
        Position = Position.MoveToward(targetPosition, FollowSpeed * _slowMultiplier * delta);
    }

    private void UpdateAbility(float delta)
    {
        _timer -= delta;
        if (_timer > 0f)
        {
            return;
        }

        _timer = _cooldown;
        TriggerAbility();
    }

    private void TriggerAbility()
    {
        (Ability ability, int damageDiceCount, int damageDie, float range) = _abilityCardId switch
        {
            "pfeilschuss" => (Ability.Dexterity, 1, 6, RangedAttackRange),
            "arkaner_blitz" => (Ability.Intelligence, 1, 6, RangedAttackRange),
            _ => (Ability.Strength, 1, 8, MeleeRange), // "hieb" - Startkarte von Krieger/Tank
        };

        var target = FindNearestEnemyInRange(range);
        if (target is null)
        {
            return;
        }

        int modifier = _stats.Modifier(ability);
        int roll = Dice.Roll(20);
        bool hit = roll + modifier >= target.Stats.ArmorClass;

        if (!hit)
        {
            SpawnFloatingText(target.Position, "Verfehlt", MissColor);
            return;
        }

        int damage = modifier;
        for (int i = 0; i < damageDiceCount; i++)
        {
            damage += Dice.Roll(damageDie);
        }

        target.TakeDamage(damage);
        SpawnFloatingText(target.Position, damage.ToString(), HitColor);
    }

    private Enemy? FindNearestEnemyInRange(float range)
    {
        Enemy? nearest = null;
        float nearestDistance = range;

        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not Enemy enemy)
            {
                continue;
            }

            float distance = Position.DistanceTo(enemy.Position);
            if (distance <= nearestDistance)
            {
                nearest = enemy;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private static float CooldownFor(string cardId) => cardId switch
    {
        "hieb" => 1.0f,
        "pfeilschuss" => 1.0f,
        "arkaner_blitz" => 1.2f,
        _ => 2.0f,
    };

    private static Color ColorFor(string className) => className switch
    {
        "Bogenschütze" => new Color(0.3f, 0.5f, 0.3f),
        "Magier" => new Color(0.4f, 0.3f, 0.75f),
        "Tank" => new Color(0.5f, 0.5f, 0.55f),
        _ => new Color(0.6f, 0.6f, 0.6f),
    };

    public void TakeDamage(int amount)
    {
        _hp = Mathf.Max(0, _hp - amount);
        QueueRedraw();

        if (!IsDefeated)
        {
            return;
        }

        PlayerCharacterCollection.MarkDead(Owned);
        SpawnFloatingText(Position, "Gefallen!", MissColor);
        QueueFree();
    }

    public void ApplySlow(float duration, float multiplier)
    {
        _slowTimer = duration;
        _slowMultiplier = multiplier;
    }

    public void SetDisabled(bool disabled)
    {
        _disabled = disabled;
    }

    private void SpawnFloatingText(Vector2 worldPosition, string text, Color color)
    {
        var floatingText = _floatingTextScene.Instantiate<FloatingText>();
        GetParent().AddChild(floatingText);
        floatingText.GlobalPosition = worldPosition;
        floatingText.ShowText(text, color);
    }
}
