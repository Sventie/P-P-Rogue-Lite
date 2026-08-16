namespace PPRogueLite;

using System.Collections.Generic;
using System.Linq;
using Godot;
using PPRogueLite.Cards;
using PPRogueLite.Character;
using PPRogueLite.Meta;

/// <summary>
/// Gruppenmitglied neben dem Hauptcharakter (Issue #5): eigene HP/RK aus
/// seiner CharacterClassDefinition, folgt dem Leader über einen festen
/// Formations-Versatz (kein Pathfinding, wie überall sonst im Projekt) und
/// hat wie Player ein eigenes CharacterLoadout (Issue #26) - Karten werden
/// beim Level-up gezielt einem Charakter der Gruppe zugewiesen, nicht mehr
/// pauschal dem Hauptcharakter. Startet mit seiner StartingCardId-Karte
/// (Hieb/Pfeilschuss/Arkaner Blitz je nach Klasse) ausgerüstet, kann danach
/// über zugewiesene Level-up-Karten genauso wie Player wachsen - voller
/// Mechanik-Umfang (Vorteil/Krit, gekoppelte Modifikatoren, Statuseffekte).
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
    private const float FollowSpeed = 260f; // etwas schneller als Player.Speed, damit die Formation nicht dauerhaft hinterherhinkt
    private const float HpBarWidth = 26f;
    private const float HpBarHeight = 4f;

    private static readonly Color MissColor = new(0.662745f, 0.603922f, 0.470588f);
    private static readonly Color HpBarBackground = new(0f, 0f, 0f, 0.5f);
    private static readonly Color HpBarFill = new(0.352941f, 0.478431f, 0.309804f);

    public OwnedCharacter Owned { get; private set; } = null!;

    public Player Leader { get; private set; } = null!;

    public CharacterLoadout Loadout { get; private set; } = null!;

    public Vector2 FormationOffset { get; set; }

    public int CurrentHp => _hp;

    public int EffectiveArmorClass => _armorClass + Loadout.ArmorClassBonus;

    public bool IsDefeated => _hp <= 0;

    private int _hp;
    private int _maxHp;
    private int _armorClass;
    private float _slowTimer;
    private float _slowMultiplier = 1f;
    private float _hasteTimer;
    private float _hasteMultiplier = 1f;
    private bool _disabled;
    private PackedScene _floatingTextScene = null!;

    public override void _Ready()
    {
        AddToGroup("party");
        _floatingTextScene = GD.Load<PackedScene>("res://scenes/FloatingText.tscn");
    }

    /// <summary>Setzt Stats/Zugehörigkeit/Loadout - erst nach AddChild() aufrufen (gleiche Node-Lifecycle-Regel wie Enemy.Initialize). savedEquippedCards ist nur bei einer Folge-Stage desselben Dungeons gesetzt (Issue #26); ist es null, wird stattdessen die Startkarte der Klasse ausgerüstet (gleiches Sicherheitsnetz-Prinzip wie bei Player).</summary>
    public void Initialize(OwnedCharacter owned, Player leader, Vector2 formationOffset, int? savedHp, IReadOnlyList<CardDefinition>? savedEquippedCards)
    {
        Owned = owned;
        Leader = leader;
        FormationOffset = formationOffset;

        var definition = owned.ClassDefinition;
        var stats = definition.BuildStats();
        _maxHp = definition.MaxHp;
        _hp = savedHp ?? _maxHp;
        _armorClass = definition.BaseArmorClass;

        Loadout = new CharacterLoadout(this, this, stats);

        if (savedEquippedCards is not null)
        {
            foreach (var card in savedEquippedCards)
            {
                Loadout.EquipCard(card);
            }
        }
        else
        {
            var startingCard = CardCatalog.AllCardTypes().First(card => card.Id == definition.StartingCardId);
            Loadout.EquipCard(startingCard);
        }

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
        Loadout.Update(dt);

        if (_slowTimer > 0f)
        {
            _slowTimer -= dt;
            if (_slowTimer <= 0f)
            {
                _slowMultiplier = 1f;
            }
        }

        if (_hasteTimer > 0f)
        {
            _hasteTimer -= dt;
            if (_hasteTimer <= 0f)
            {
                _hasteMultiplier = 1f;
            }
        }
    }

    private void FollowLeader(float delta)
    {
        var targetPosition = Leader.Position + FormationOffset;
        Position = Position.MoveToward(targetPosition, FollowSpeed * _slowMultiplier * _hasteMultiplier * delta);
    }

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

    public void Heal(int amount)
    {
        _hp = Mathf.Min(_maxHp, _hp + amount);
        QueueRedraw();
    }

    public void ApplySlow(float duration, float multiplier)
    {
        _slowTimer = duration;
        _slowMultiplier = multiplier;
    }

    public void ApplyHaste(float duration, float multiplier)
    {
        _hasteTimer = duration;
        _hasteMultiplier = multiplier;
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
