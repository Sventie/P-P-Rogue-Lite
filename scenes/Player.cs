namespace PPRogueLite;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PPRogueLite.Cards;
using PPRogueLite.Character;
using PPRogueLite.Combat;
using PPRogueLite.Meta;

/// <summary>
/// Spielerfigur der Echtzeit-Arena: WASD-Bewegung, dazu ein Satz aktiver
/// Fähigkeiten, die jede für sich automatisch auf Cooldown auslösen (keine
/// Klicks im Kampf mehr). Jede Fähigkeit basiert auf einer Karte aus dem
/// Deck - beim Level-up wird die oberste Karte des (laufweiten) Decks
/// gezogen und dauerhaft als neue Fähigkeit ausgerüstet. Alle Treffer/
/// Fehlschläge werden weiterhin per verstecktem W20-Wurf (siehe
/// PPRogueLite.Combat.Dice) entschieden, nur ohne Popup - sichtbar wird nur
/// das Ergebnis (Flugtext).
/// </summary>
public partial class Player : Node2D
{
    private const float Speed = 220f;
    private const float MeleeRange = 90f;
    private const float Radius = 16f;
    private const int XpPerLevel = 2; // Test-Balance-Wert fuer schnelleres Testen (1 XP pro Kill)
    private const float ParadeBonus = 4f;
    private const float ParadeDuration = 2f;

    private static readonly Color BodyColor = new(0.690196f, 0.552941f, 0.239216f);
    private static readonly Color MissColor = new(0.662745f, 0.603922f, 0.470588f);
    private static readonly Color HitColor = new(0.352941f, 0.478431f, 0.309804f);
    private static readonly Color CritColor = new(0.85098f, 0.698039f, 0.361961f);
    private static readonly Color RangeColor = new(0.85098f, 0.698039f, 0.361961f, 0.3f);

    private sealed class ActiveAbility
    {
        public ActiveAbility(CardDefinition card, float cooldown)
        {
            Card = card;
            Cooldown = cooldown;
            Timer = cooldown;
        }

        public CardDefinition Card { get; }

        public float Cooldown { get; }

        public float Timer;
    }

    /// <summary>Feuert bei jeder gezogenen Karte (auch Duplikaten) - für die Level-up-Anzeige.</summary>
    public event Action<CardDefinition>? AbilityGained;

    /// <summary>Feuert nur, wenn der gezogene Kartentyp noch nicht aktiv war - für die Fähigkeiten-Leiste.</summary>
    public event Action<CardDefinition>? NewAbilityTypeUnlocked;

    public PlayerCharacter Character { get; private set; } = null!;

    public int EffectiveArmorClass => Character.ArmorClass + (_paradeTimer > 0f ? (int)ParadeBonus : 0);

    /// <summary>Aktuell aktive Fähigkeiten, ein Eintrag pro verschiedenem Kartentyp.</summary>
    public IEnumerable<CardDefinition> EquippedAbilityTypes => _abilities
        .Select(ability => ability.Card)
        .GroupBy(card => card.Id)
        .Select(group => group.First());

    public int Xp => _xp;

    public int XpToNextLevel => XpPerLevel;

    private readonly List<ActiveAbility> _abilities = new();
    private Deck _runDeck = null!;
    private PackedScene _floatingTextScene = null!;

    private bool _disabled;
    private bool _advantageReady;
    private float _paradeTimer;
    private int _xp;
    private int _level = 1;

    public override void _Ready()
    {
        AddToGroup("player");
        _floatingTextScene = GD.Load<PackedScene>("res://scenes/FloatingText.tscn");

        Character = new PlayerCharacter
        {
            Name = "Rurik Steinfaust",
            ClassName = "Krieger — Stufe 1",
            Stats = new AbilityScores(new Dictionary<Ability, int>
            {
                [Ability.Strength] = 16,
                [Ability.Dexterity] = 12,
                [Ability.Constitution] = 14,
                [Ability.Intelligence] = 10,
                [Ability.Wisdom] = 10,
                [Ability.Charisma] = 8,
            }),
            MaxHp = 24,
            Hp = 24,
            BaseArmorClass = 15,
        };

        _runDeck = new Deck(PlayerCardCollection.DeckCards);
        EquipAbility(new HiebCard(), announce: false);

        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, Radius, BodyColor);
        DrawArc(Vector2.Zero, MeleeRange, 0f, Mathf.Tau, 64, RangeColor, 2f, true);
    }

    public override void _Process(double delta)
    {
        if (_disabled)
        {
            return;
        }

        float dt = (float)delta;
        HandleMovement(dt);
        UpdateAbilities(dt);

        if (_paradeTimer > 0f)
        {
            _paradeTimer -= dt;
        }
    }

    private void HandleMovement(float delta)
    {
        var direction = Vector2.Zero;
        if (Input.IsPhysicalKeyPressed(Key.W))
        {
            direction.Y -= 1;
        }

        if (Input.IsPhysicalKeyPressed(Key.S))
        {
            direction.Y += 1;
        }

        if (Input.IsPhysicalKeyPressed(Key.A))
        {
            direction.X -= 1;
        }

        if (Input.IsPhysicalKeyPressed(Key.D))
        {
            direction.X += 1;
        }

        if (direction != Vector2.Zero)
        {
            Position += direction.Normalized() * Speed * delta;
        }

        var viewportSize = GetViewport().GetVisibleRect().Size;
        Position = new Vector2(
            Mathf.Clamp(Position.X, Radius, viewportSize.X - Radius),
            Mathf.Clamp(Position.Y, Radius, viewportSize.Y - Radius));
    }

    private void UpdateAbilities(float delta)
    {
        foreach (var ability in _abilities)
        {
            ability.Timer -= delta;
            if (ability.Timer <= 0f)
            {
                ability.Timer = ability.Cooldown;
                TriggerAbility(ability);
            }
        }
    }

    private void TriggerAbility(ActiveAbility ability)
    {
        switch (ability.Card.Id)
        {
            case "hieb":
            {
                int modifier = Character.Stats.Modifier(Ability.Strength);
                ResolveMeleeAttack(modifier, damageDiceCount: 1, damageDie: 8, damageBonus: modifier);
                break;
            }

            case "wuchtschlag":
            {
                int fullModifier = Character.Stats.Modifier(Ability.Strength);
                ResolveMeleeAttack(fullModifier - 2, damageDiceCount: 2, damageDie: 8, damageBonus: fullModifier);
                break;
            }

            case "parade":
                _paradeTimer = ParadeDuration;
                SpawnFloatingText(Position, "Deckung!", MissColor);
                break;

            case "finte":
                _advantageReady = true;
                SpawnFloatingText(Position, "Vorteil!", CritColor);
                break;

            case "atemholen":
            {
                int heal = Dice.Roll(10) + 1;
                Character.Heal(heal);
                SpawnFloatingText(Position, $"+{heal}", HitColor);
                break;
            }
        }
    }

    private void ResolveMeleeAttack(int attackModifier, int damageDiceCount, int damageDie, int damageBonus)
    {
        var target = FindNearestEnemyInRange();
        if (target is null)
        {
            return;
        }

        int roll = Dice.Roll(20);
        if (_advantageReady)
        {
            roll = Mathf.Max(roll, Dice.Roll(20));
            _advantageReady = false;
        }

        bool critical = roll == 20;
        int total = roll + attackModifier;
        bool hit = critical || total >= target.Stats.ArmorClass;

        if (!hit)
        {
            SpawnFloatingText(target.Position, "Verfehlt", MissColor);
            return;
        }

        int damage = damageBonus;
        int rolls = critical ? damageDiceCount * 2 : damageDiceCount;
        for (int i = 0; i < rolls; i++)
        {
            damage += Dice.Roll(damageDie);
        }

        target.TakeDamage(damage);
        SpawnFloatingText(target.Position, damage.ToString(), critical ? CritColor : HitColor);
    }

    private EnemyGoblin? FindNearestEnemyInRange()
    {
        EnemyGoblin? nearest = null;
        float nearestDistance = MeleeRange;

        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not EnemyGoblin enemy)
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

    private void EquipAbility(CardDefinition card, bool announce)
    {
        bool isNewType = !_abilities.Any(ability => ability.Card.Id == card.Id);

        _abilities.Add(new ActiveAbility(card, CooldownFor(card.Id)));

        if (announce)
        {
            AbilityGained?.Invoke(card);
        }

        if (isNewType)
        {
            NewAbilityTypeUnlocked?.Invoke(card);
        }
    }

    /// <summary>Wie oft eine Karte dieses Typs bereits gezogen und ausgerüstet wurde.</summary>
    public int CountEquipped(string cardId) => _abilities.Count(ability => ability.Card.Id == cardId);

    /// <summary>Wie viele Karten dieses Typs noch im laufeigenen Nachziehstapel liegen.</summary>
    public int CountInDrawPile(string cardId) => _runDeck.DrawPile.Count(card => card.Id == cardId);

    /// <summary>
    /// Fortschritt (0..1) bis zum nächsten Auslösen der ersten aktiven
    /// Fähigkeit mit dieser Karten-Id - für die Cooldown-Anzeige in der
    /// Fähigkeiten-Leiste. Bei mehreren Instanzen desselben Kartentyps wird
    /// nur die erste (primäre) angezeigt.
    /// </summary>
    public float GetCooldownProgress(string cardId)
    {
        var ability = _abilities.FirstOrDefault(a => a.Card.Id == cardId);
        if (ability is null)
        {
            return 0f;
        }

        return Mathf.Clamp(1f - (ability.Timer / ability.Cooldown), 0f, 1f);
    }

    private static float CooldownFor(string cardId) => cardId switch
    {
        "hieb" => 1.0f,
        "wuchtschlag" => 1.8f,
        "parade" => 4.0f,
        "finte" => 5.0f,
        "atemholen" => 8.0f,
        _ => 2.0f,
    };

    public void GrantXp(int amount)
    {
        _xp += amount;
        while (_xp >= XpPerLevel)
        {
            _xp -= XpPerLevel;
            LevelUp();
        }
    }

    private void LevelUp()
    {
        _level++;

        var drawn = _runDeck.DrawHand(1);
        if (drawn.Count == 0)
        {
            return;
        }

        EquipAbility(drawn[0], announce: true);
    }

    private void SpawnFloatingText(Vector2 worldPosition, string text, Color color)
    {
        var floatingText = _floatingTextScene.Instantiate<FloatingText>();
        GetParent().AddChild(floatingText);
        floatingText.GlobalPosition = worldPosition;
        floatingText.ShowText(text, color);
    }

    public void TakeDamage(int amount)
    {
        Character.TakeDamage(amount);
    }

    public void SetDisabled(bool disabled)
    {
        _disabled = disabled;
    }
}
