namespace PPRogueLite;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PPRogueLite.Cards;
using PPRogueLite.Character;
using PPRogueLite.Combat;

/// <summary>
/// Ausrüst-/Auslöse-/Modifikator-Logik für aktive Fähigkeiten (Issue #26):
/// ursprünglich exklusiv in Player.cs, jetzt als eigenständige Komponente
/// extrahiert, damit Karten nicht mehr fest dem Hauptcharakter gehören,
/// sondern beim Level-up einem beliebigen Gruppenmitglied (Leader oder
/// Companion) zugewiesen werden können - Player UND Companion besitzen je
/// eine eigene CharacterLoadout-Instanz mit vollem Mechanik-Umfang
/// (Vorteil/Krit, gekoppelte Modifikatoren, Statuseffekte).
///
/// Kennt selbst KEIN Deck und KEIN Level-up - das bleibt zentral bei Player
/// (ein gemeinsames Deck/Level-up für die ganze Gruppe, siehe Player.cs).
/// Für die beiden einzigen Karten, die den geteilten Ablagestapel anfassen
/// (Wiederkehr/Erinnerung), greift Loadout über Player.Instance auf das
/// gemeinsame Deck zu - unabhängig davon, welcher Charakter die Karte
/// ausgerüstet hat.
/// </summary>
public sealed class CharacterLoadout
{
    private const float MeleeRange = 90f;
    private const float RangedAttackRange = 300f; // Issue #13: Pfeilschuss/Arkaner Blitz
    private const float ParadeBonus = 4f;
    private const float ParadeDuration = 2f;

    // Modifikatorkarten-Werte (Issue #12/#14/#23/#24), Test-Balance-Werte:
    private const float HasteDuration = 2f; // Adrenalin
    private const float HasteMultiplier = 1.3f;
    private const float ExplosiveHeilungRadius = 140f;
    private const int ExplosiveHeilungDamage = 6;
    private const int GiftklingeChancePercent = 50;
    private const int PoisonDamagePerTick = 2;
    private const float PoisonDuration = 4f;
    private const int BrandChancePercent = 50;
    private const int BurnDamagePerStackPerTick = 2;
    private const float BurnDuration = 4f;
    private const int BurnMaxStacks = 3;
    private const int BlutungChancePercent = 50;
    private const float BleedPercentPerTick = 0.05f;
    private const float BleedDuration = 4f;

    private static readonly Color MissColor = new(0.662745f, 0.603922f, 0.470588f);
    private static readonly Color HitColor = new(0.352941f, 0.478431f, 0.309804f);
    private static readonly Color CritColor = new(0.85098f, 0.698039f, 0.361961f);
    private static readonly Color ArrowColor = new(0.55f, 0.4f, 0.25f);
    private static readonly Color ArcaneColor = new(0.4f, 0.3f, 0.75f);

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

        public bool Enabled = true;
    }

    /// <summary>Feuert, wenn eine ausgerüstete Erinnerung-Karte auslöst (Issue #22) - der Aufrufer muss den gewählten Ablage-Kandidaten aus dem geteilten Deck (Player.Instance.RunDeck) entfernen und hier erneut EquipCard aufrufen. Feuert nicht bei leerer Ablage.</summary>
    public event Action<IReadOnlyList<CardDefinition>>? DiscardChoiceOffered;

    private readonly Node2D _owner;
    private readonly IPartyMember _partyMember;
    private readonly AbilityScores _stats;
    private readonly PackedScene _floatingTextScene;

    private readonly List<ActiveAbility> _abilities = new();
    private readonly List<CardDefinition> _modifiers = new();
    private readonly Dictionary<string, List<string>> _modifiersByTargetCardId = new();

    private bool _advantageReady;
    private float _paradeTimer;
    private int _bonusAttackRoll;

    public CharacterLoadout(Node2D owner, IPartyMember partyMember, AbilityScores stats)
    {
        _owner = owner;
        _partyMember = partyMember;
        _stats = stats;
        _floatingTextScene = GD.Load<PackedScene>("res://scenes/FloatingText.tscn");
    }

    /// <summary>Temporärer RK-Bonus durch Parade - fließt in EffectiveArmorClass des Besitzers ein (siehe Player/Companion).</summary>
    public int ArmorClassBonus => _paradeTimer > 0f ? (int)ParadeBonus : 0;

    /// <summary>Aktuell aktive Fähigkeiten, ein Eintrag pro verschiedenem Kartentyp - für die Fähigkeiten-Leiste.</summary>
    public IEnumerable<CardDefinition> EquippedAbilityTypes => _abilities
        .Select(ability => ability.Card)
        .GroupBy(card => card.Id)
        .Select(group => group.First());

    /// <summary>Alle ausgerüsteten Karten (Aktion + Modifikator) - für Persistenz über Stage-Wechsel (Player.SaveProgress/Arena.SaveCompanionProgress).</summary>
    public IReadOnlyList<CardDefinition> AllEquippedCards =>
        _abilities.Select(ability => ability.Card).Concat(_modifiers).ToList();

    /// <summary>Tickt alle Cooldowns/Timer - vom Besitzer (Player/Companion) einmal pro Frame in _Process aufgerufen.</summary>
    public void Update(float delta)
    {
        // Über eine Kopie iterieren: TriggerAbility kann einen Gegner
        // besiegen -> GrantXp -> LevelUp -> EquipCard fügt der Liste ein
        // neues Element hinzu, was sonst eine InvalidOperationException
        // auslöst, wenn das mitten in dieser Schleife passiert.
        foreach (var ability in _abilities.ToList())
        {
            if (!ability.Enabled)
            {
                continue;
            }

            ability.Timer -= delta;
            if (ability.Timer <= 0f)
            {
                ability.Timer = ability.Cooldown;
                TriggerAbility(ability);
            }
        }

        if (_paradeTimer > 0f)
        {
            _paradeTimer -= delta;
        }
    }

    /// <summary>
    /// Aktiviert/deaktiviert alle Instanzen einer Fähigkeit (Klick auf das
    /// Badge in der Fähigkeiten-Leiste, siehe Arena.BuildAbilityBadge). Der
    /// Cooldown pausiert währenddessen, statt weiterzulaufen.
    /// </summary>
    public void ToggleAbility(string cardId)
    {
        bool newState = !IsAbilityEnabled(cardId);
        foreach (var ability in _abilities.Where(ability => ability.Card.Id == cardId))
        {
            ability.Enabled = newState;
        }
    }

    public bool IsAbilityEnabled(string cardId) =>
        _abilities.FirstOrDefault(ability => ability.Card.Id == cardId)?.Enabled ?? true;

    private void TriggerAbility(ActiveAbility ability)
    {
        switch (ability.Card.Id)
        {
            case "hieb":
            {
                int modifier = _stats.Modifier(Ability.Strength);
                var target = ResolveAttack(modifier + _bonusAttackRoll, damageDiceCount: 1, damageDie: 8, damageBonus: modifier, range: MeleeRange);

                if (target is not null)
                {
                    foreach (var modifierId in ModifiersFor("hieb"))
                    {
                        ApplyCoupledEffect(modifierId, target);
                    }
                }

                break;
            }

            case "wuchtschlag":
            {
                int fullModifier = _stats.Modifier(Ability.Strength);
                var target = ResolveAttack(fullModifier - 2 + _bonusAttackRoll, damageDiceCount: 2, damageDie: 8, damageBonus: fullModifier, range: MeleeRange);

                if (target is not null)
                {
                    foreach (var modifierId in ModifiersFor("wuchtschlag"))
                    {
                        ApplyCoupledEffect(modifierId, target);
                    }
                }

                break;
            }

            case "pfeilschuss":
            {
                int modifier = _stats.Modifier(Ability.Dexterity);
                var target = ResolveAttack(modifier + _bonusAttackRoll, damageDiceCount: 1, damageDie: 6, damageBonus: modifier, range: RangedAttackRange);

                if (target is not null)
                {
                    SpawnProjectile(target.Position, ArrowColor);

                    foreach (var modifierId in ModifiersFor("pfeilschuss"))
                    {
                        ApplyCoupledEffect(modifierId, target);
                    }
                }

                break;
            }

            case "arkaner_blitz":
            {
                int modifier = _stats.Modifier(Ability.Intelligence);
                var target = ResolveAttack(modifier + _bonusAttackRoll, damageDiceCount: 1, damageDie: 6, damageBonus: modifier, range: RangedAttackRange);

                if (target is not null)
                {
                    SpawnProjectile(target.Position, ArcaneColor);

                    foreach (var modifierId in ModifiersFor("arkaner_blitz"))
                    {
                        ApplyCoupledEffect(modifierId, target);
                    }
                }

                break;
            }

            case "parade":
                _paradeTimer = ParadeDuration;
                SpawnFloatingText(_owner.Position, "Deckung!", MissColor);
                break;

            case "finte":
                _advantageReady = true;
                SpawnFloatingText(_owner.Position, "Vorteil!", CritColor);
                break;

            case "atemholen":
            {
                int heal = Dice.Roll(10) + 1;
                _partyMember.Heal(heal);
                SpawnFloatingText(_owner.Position, $"+{heal}", HitColor);

                foreach (var modifierId in ModifiersFor("atemholen"))
                {
                    ApplyCoupledEffect(modifierId);
                }

                break;
            }

            case "wiederkehr":
                if (Player.Instance is not null && Player.Instance.RunDeck.DiscardPile.Count > 0)
                {
                    Player.Instance.RunDeck.ReshuffleDiscardIntoDrawPile();
                    SpawnFloatingText(_owner.Position, "Wiederkehr!", CritColor);
                }

                break;

            case "erinnerung":
                if (Player.Instance is not null && Player.Instance.RunDeck.DiscardPile.Count > 0)
                {
                    DiscardChoiceOffered?.Invoke(new List<CardDefinition>(Player.Instance.RunDeck.DiscardPile));
                }

                break;
        }
    }

    /// <summary>
    /// Wirkung eines an eine Aktionskarte gekoppelten Modifikators (Issue
    /// #12), ausgelöst wenn die Zielkarte auslöst. target ist nur bei
    /// Effekten gesetzt, die ein konkretes Trefferziel brauchen (z. B.
    /// Giftklinge, Issue #14) - bei reinen Selbst-/AOE-Effekten (Explosive
    /// Heilung) bleibt es null.
    /// </summary>
    private void ApplyCoupledEffect(string modifierId, Enemy? target = null)
    {
        switch (modifierId)
        {
            case "explosive_heilung":
                DealAoeDamage(ExplosiveHeilungRadius, ExplosiveHeilungDamage);
                break;

            case "giftklinge":
                if (target is not null && Dice.Roll(100) <= GiftklingeChancePercent)
                {
                    target.ApplyPoison(PoisonDamagePerTick, PoisonDuration);
                }

                break;

            case "brand":
                if (target is not null && Dice.Roll(100) <= BrandChancePercent)
                {
                    target.ApplyBurn(BurnDamagePerStackPerTick, BurnDuration, BurnMaxStacks);
                }

                break;

            case "blutung":
                if (target is not null && Dice.Roll(100) <= BlutungChancePercent)
                {
                    target.ApplyBleed(BleedPercentPerTick, BleedDuration);
                }

                break;
        }
    }

    private void DealAoeDamage(float radius, int damage)
    {
        foreach (Node node in _owner.GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not Enemy enemy)
            {
                continue;
            }

            if (_owner.Position.DistanceTo(enemy.Position) > radius)
            {
                continue;
            }

            enemy.TakeDamage(damage);
            SpawnFloatingText(enemy.Position, damage.ToString(), CritColor);
        }
    }

    /// <summary>Löst einen Angriff auf den nächsten Gegner innerhalb von range auf. Gibt das getroffene Ziel zurück (null bei Fehlschlag/keinem Ziel) - Aufrufer nutzen das z. B. für gekoppelte Modifikatoren, die ein konkretes Trefferziel brauchen.</summary>
    private Enemy? ResolveAttack(int attackModifier, int damageDiceCount, int damageDie, int damageBonus, float range)
    {
        var target = FindNearestEnemyInRange(range);
        if (target is null)
        {
            return null;
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
            return null;
        }

        int damage = damageBonus;
        int rolls = critical ? damageDiceCount * 2 : damageDiceCount;
        for (int i = 0; i < rolls; i++)
        {
            damage += Dice.Roll(damageDie);
        }

        target.TakeDamage(damage);
        SpawnFloatingText(target.Position, damage.ToString(), critical ? CritColor : HitColor);

        if (critical)
        {
            OnCriticalHit();
        }

        return target;
    }

    /// <summary>Rein kosmetisches Projektil für Fernkampfkarten (Pfeilschuss/Arkaner Blitz): Treffer/Schaden sind zu diesem Zeitpunkt schon über ResolveAttack aufgelöst - das Projektil liefert nur die visuelle Rückmeldung.</summary>
    private void SpawnProjectile(Vector2 targetPosition, Color color)
    {
        var projectile = new Projectile
        {
            Direction = (targetPosition - _owner.Position).Normalized(),
            Cosmetic = true,
            Color = color,
        };
        _owner.GetParent().AddChild(projectile);
        projectile.GlobalPosition = _owner.GlobalPosition;
    }

    /// <summary>Reaktiver Modifikator-Hook (Issue #12): wird bei jedem kritischen Treffer aufgerufen.</summary>
    private void OnCriticalHit()
    {
        if (HasModifier("adrenalin"))
        {
            _partyMember.ApplyHaste(HasteDuration, HasteMultiplier);
            SpawnFloatingText(_owner.Position, "Adrenalin!", CritColor);
        }
    }

    private Enemy? FindNearestEnemyInRange(float range)
    {
        Enemy? nearest = null;
        float nearestDistance = range;

        foreach (Node node in _owner.GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not Enemy enemy)
            {
                continue;
            }

            float distance = _owner.Position.DistanceTo(enemy.Position);
            if (distance <= nearestDistance)
            {
                nearest = enemy;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    /// <summary>Verdrahtet eine ausgewählte Karte je nach CardKind (Issue #12) entweder als ActiveAbility oder als passiven Modifikator. Wird von Arena aufgerufen, nachdem der Spieler beim Level-up sowohl die Karte als auch den Ziel-Charakter gewählt hat (Issue #26).</summary>
    public void EquipCard(CardDefinition card)
    {
        if (card.Kind == CardKind.Modifier)
        {
            EquipModifier(card);
        }
        else
        {
            EquipAbility(card);
        }
    }

    private void EquipAbility(CardDefinition card)
    {
        _abilities.Add(new ActiveAbility(card, CooldownFor(card.Id)));
    }

    /// <summary>Registriert eine Modifikatorkarte einmalig (kein Cooldown-Slot, siehe Kartenplanung/Issue #12 in CLAUDE.md). Erscheint bewusst NICHT in der Fähigkeiten-Leiste.</summary>
    private void EquipModifier(CardDefinition card)
    {
        _modifiers.Add(card);

        if (card.CoupledCardId is not null)
        {
            if (!_modifiersByTargetCardId.TryGetValue(card.CoupledCardId, out var attached))
            {
                attached = new List<string>();
                _modifiersByTargetCardId[card.CoupledCardId] = attached;
            }

            attached.Add(card.Id);
        }

        ApplyModifierEffect(card);
    }

    /// <summary>Einmalige Wirkung beim Ausrüsten einer Modifikatorkarte - für globale passive Boni (Issue #12).</summary>
    private void ApplyModifierEffect(CardDefinition card)
    {
        switch (card.Id)
        {
            case "kampfrausch":
                _bonusAttackRoll += 1;
                break;
        }
    }

    private bool HasModifier(string modifierId) => _modifiers.Any(m => m.Id == modifierId);

    private IReadOnlyList<string> ModifiersFor(string cardId) =>
        _modifiersByTargetCardId.TryGetValue(cardId, out var attached) ? attached : Array.Empty<string>();

    /// <summary>Wie oft eine Karte dieses Typs bei diesem Charakter bereits ausgerüstet wurde (Aktion oder Modifikator).</summary>
    public int CountEquipped(string cardId) =>
        _abilities.Count(ability => ability.Card.Id == cardId) + _modifiers.Count(m => m.Id == cardId);

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
        "wiederkehr" => 20.0f,
        "erinnerung" => 15.0f,
        "pfeilschuss" => 1.0f,
        "arkaner_blitz" => 1.2f,
        _ => 2.0f,
    };

    private void SpawnFloatingText(Vector2 worldPosition, string text, Color color)
    {
        var floatingText = _floatingTextScene.Instantiate<FloatingText>();
        _owner.GetParent().AddChild(floatingText);
        floatingText.GlobalPosition = worldPosition;
        floatingText.ShowText(text, color);
    }
}
