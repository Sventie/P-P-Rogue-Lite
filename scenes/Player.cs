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
    private const float RangedAttackRange = 300f; // Issue #13: Pfeilschuss/Arkaner Blitz
    private const float Radius = 16f;
    private const int XpPerLevel = 2; // Test-Balance-Wert fuer schnelleres Testen (1 XP pro Kill)
    private const int LevelUpCardChoices = 2; // Issue #15: so viele Karten werden pro Level-up zur Auswahl gezogen
    private const float ParadeBonus = 4f;
    private const float ParadeDuration = 2f;

    // Modifikatorkarten-Startset (Issue #12), Test-Balance-Werte:
    private const float HasteDuration = 2f; // Adrenalin
    private const float HasteMultiplier = 1.3f;
    private const float ExplosiveHeilungRadius = 140f; // Explosive Heilung
    private const int ExplosiveHeilungDamage = 6;
    private const int GiftklingeChancePercent = 50; // Giftklinge (Issue #14: erste Nutzung des Gift-Status-Effekts)
    private const int PoisonDamagePerTick = 2;
    private const float PoisonDuration = 4f; // 4 Ticks bei Enemy.PoisonTickInterval = 1s
    private const int BrandChancePercent = 50; // Brand (Issue #23: zweite Nutzung, stackende Intensität)
    private const int BurnDamagePerStackPerTick = 2;
    private const float BurnDuration = 4f;
    private const int BurnMaxStacks = 3;
    private const int BlutungChancePercent = 50; // Blutung (Issue #24: dritte Nutzung, skaliert mit maximaler HP)
    private const float BleedPercentPerTick = 0.05f; // 5% der maximalen HP pro Tick
    private const float BleedDuration = 4f;

    private static readonly Color BodyColor = new(0.690196f, 0.552941f, 0.239216f);
    private static readonly Color MissColor = new(0.662745f, 0.603922f, 0.470588f);
    private static readonly Color HitColor = new(0.352941f, 0.478431f, 0.309804f);
    private static readonly Color CritColor = new(0.85098f, 0.698039f, 0.361961f);
    private static readonly Color RangeColor = new(0.85098f, 0.698039f, 0.361961f, 0.3f);
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

    /// <summary>Feuert bei jeder gezogenen Karte (auch Duplikaten) - für die Level-up-Anzeige. Feuert NICHT, wenn stattdessen CardChoiceOffered feuert (siehe LevelUp).</summary>
    public event Action<CardDefinition>? AbilityGained;

    /// <summary>Feuert nur, wenn der gezogene Kartentyp noch nicht aktiv war - für die Fähigkeiten-Leiste.</summary>
    public event Action<CardDefinition>? NewAbilityTypeUnlocked;

    /// <summary>Feuert bei einem Level-up, wenn mehrere Karten zur Auswahl gezogen wurden (Issue #15) - der Aufrufer muss ResolveCardChoice mit der Wahl aufrufen.</summary>
    public event Action<IReadOnlyList<CardDefinition>>? CardChoiceOffered;

    /// <summary>Feuert, wenn die Erinnerung-Fähigkeit auslöst (Issue #22) - der Aufrufer muss ResolveDiscardChoice mit der Wahl aufrufen. Feuert nicht bei leerer Ablage.</summary>
    public event Action<IReadOnlyList<CardDefinition>>? DiscardChoiceOffered;

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
    private readonly List<CardDefinition> _modifiers = new();
    private readonly Dictionary<string, List<string>> _modifiersByTargetCardId = new();
    private Deck _runDeck = null!;
    private PackedScene _floatingTextScene = null!;

    private bool _disabled;
    private bool _advantageReady;
    private float _paradeTimer;
    private float _slowTimer;
    private float _slowMultiplier = 1f;
    private float _hasteTimer;
    private float _hasteMultiplier = 1f;
    private int _bonusAttackRoll;
    private int _xp;
    private int _level = 1;

    public override void _Ready()
    {
        AddToGroup("player");
        _floatingTextScene = GD.Load<PackedScene>("res://scenes/FloatingText.tscn");

        // Player spielt aktuell fest den Krieger (siehe CharacterClassCatalog,
        // Issue #13) - echte Auswahl zwischen den Archetypen folgt erst mit
        // der Gruppe (#5/#26), der Katalog ist bisher nur Datengrundlage.
        var characterClass = CharacterClassCatalog.Krieger;
        Character = new PlayerCharacter
        {
            Name = characterClass.Name,
            ClassName = $"{characterClass.ClassName} — Stufe 1",
            Stats = characterClass.BuildStats(),
            MaxHp = characterClass.MaxHp,
            Hp = characterClass.MaxHp,
            BaseArmorClass = characterClass.BaseArmorClass,
        };

        if (DungeonRun.HasProgress)
        {
            // Fortsetzung eines laufenden Dungeons (Stage 2+): gespeicherten
            // Fortschritt aus der vorherigen Stage übernehmen statt frisch
            // zu starten (siehe DungeonRun/SaveProgress).
            Character.Hp = DungeonRun.SavedHp;
            _xp = DungeonRun.SavedXp;
            _level = DungeonRun.SavedLevel;
            _runDeck = new Deck(DungeonRun.SavedDrawPile, shuffleOnCreate: false, discardedCards: DungeonRun.SavedDiscardPile);

            foreach (var card in DungeonRun.SavedEquippedCards)
            {
                EquipCard(card, announce: false);
            }
        }
        else
        {
            _runDeck = new Deck(PlayerCardCollection.DeckCards);
            var startingCard = CardCatalog.AllCardTypes().First(card => card.Id == characterClass.StartingCardId);
            EquipAbility(startingCard, announce: false);
        }

        QueueRedraw();
    }

    /// <summary>
    /// Schreibt den aktuellen Fortschritt in DungeonRun, damit die nächste
    /// Stage desselben Dungeons (neue Arena-Szene, neue Player-Instanz)
    /// daran anknüpfen kann. Wird von Arena.CompleteStage aufgerufen, bevor
    /// in Lager/Hub gewechselt wird.
    /// </summary>
    public void SaveProgress()
    {
        var equippedCards = _abilities.Select(ability => ability.Card).Concat(_modifiers).ToList();
        DungeonRun.SaveProgress(
            Character.Hp,
            _xp,
            _level,
            new List<CardDefinition>(_runDeck.DrawPile),
            new List<CardDefinition>(_runDeck.DiscardPile),
            equippedCards);
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

    /// <summary>Verlangsamt die Bewegungsgeschwindigkeit für duration Sekunden (z. B. Kobold-Schamane-Treffer, Issue #9).</summary>
    public void ApplySlow(float duration, float multiplier)
    {
        _slowTimer = duration;
        _slowMultiplier = multiplier;
    }

    /// <summary>Beschleunigt die Bewegungsgeschwindigkeit für duration Sekunden (z. B. Adrenalin nach einem Krit, Issue #12).</summary>
    public void ApplyHaste(float duration, float multiplier)
    {
        _hasteTimer = duration;
        _hasteMultiplier = multiplier;
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
            Position += direction.Normalized() * Speed * _slowMultiplier * _hasteMultiplier * delta;
        }

        var viewportSize = GetViewport().GetVisibleRect().Size;
        Position = new Vector2(
            Mathf.Clamp(Position.X, Radius, viewportSize.X - Radius),
            Mathf.Clamp(Position.Y, Radius, viewportSize.Y - Radius));
    }

    private void UpdateAbilities(float delta)
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
    }

    /// <summary>
    /// Aktiviert/deaktiviert alle Instanzen einer Fähigkeit (Klick auf das
    /// Badge in der Fähigkeiten-Leiste, siehe Arena.AddAbilityBadge). Der
    /// Cooldown pausiert währenddessen, statt weiterzulaufen. Gedacht, um
    /// einzelne Effekte isoliert zu testen oder alle Angriffe auszuschalten,
    /// um kontrolliert Schaden zu nehmen - bewusst keine reine Testfunktion,
    /// soll später auch Spielern helfen, Builds auszuprobieren.
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
                int modifier = Character.Stats.Modifier(Ability.Strength);
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
                int fullModifier = Character.Stats.Modifier(Ability.Strength);
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
                int modifier = Character.Stats.Modifier(Ability.Dexterity);
                var target = ResolveAttack(modifier + _bonusAttackRoll, damageDiceCount: 1, damageDie: 6, damageBonus: modifier, range: RangedAttackRange);

                if (target is not null)
                {
                    SpawnPlayerProjectile(target.Position, ArrowColor);

                    foreach (var modifierId in ModifiersFor("pfeilschuss"))
                    {
                        ApplyCoupledEffect(modifierId, target);
                    }
                }

                break;
            }

            case "arkaner_blitz":
            {
                int modifier = Character.Stats.Modifier(Ability.Intelligence);
                var target = ResolveAttack(modifier + _bonusAttackRoll, damageDiceCount: 1, damageDie: 6, damageBonus: modifier, range: RangedAttackRange);

                if (target is not null)
                {
                    SpawnPlayerProjectile(target.Position, ArcaneColor);

                    foreach (var modifierId in ModifiersFor("arkaner_blitz"))
                    {
                        ApplyCoupledEffect(modifierId, target);
                    }
                }

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

                foreach (var modifierId in ModifiersFor("atemholen"))
                {
                    ApplyCoupledEffect(modifierId);
                }

                break;
            }

            case "wiederkehr":
                if (_runDeck.DiscardPile.Count > 0)
                {
                    _runDeck.ReshuffleDiscardIntoDrawPile();
                    SpawnFloatingText(Position, "Wiederkehr!", CritColor);
                }

                break;

            case "erinnerung":
                if (_runDeck.DiscardPile.Count > 0)
                {
                    DiscardChoiceOffered?.Invoke(new List<CardDefinition>(_runDeck.DiscardPile));
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
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not Enemy enemy)
            {
                continue;
            }

            if (Position.DistanceTo(enemy.Position) > radius)
            {
                continue;
            }

            enemy.TakeDamage(damage);
            SpawnFloatingText(enemy.Position, damage.ToString(), CritColor);
        }
    }

    /// <summary>Löst einen Angriff auf den nächsten Gegner innerhalb von range auf (Nahkampf: MeleeRange, Fernkampf: RangedAttackRange, Issue #13). Gibt das getroffene Ziel zurück (null bei Fehlschlag/keinem Ziel) - Aufrufer nutzen das z. B. für gekoppelte Modifikatoren, die ein konkretes Trefferziel brauchen (Issue #14).</summary>
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

    /// <summary>
    /// Rein kosmetisches Projektil für Fernkampfkarten (Pfeilschuss/Arkaner
    /// Blitz, Issue #13): Treffer/Schaden sind zu diesem Zeitpunkt schon
    /// über ResolveAttack aufgelöst (gleicher verdeckter W20 wie Nahkampf,
    /// nur mit RangedAttackRange statt MeleeRange) - das Projektil liefert
    /// nur die visuelle Rückmeldung, dass der Angriff aus der Distanz kam.
    /// Wiederverwendet Projectile.cs (sonst für gegnerische Fernangriffe),
    /// über Projectile.Cosmetic von der echten Trefferauflösung ausgenommen.
    /// </summary>
    private void SpawnPlayerProjectile(Vector2 targetPosition, Color color)
    {
        var projectile = new Projectile
        {
            Direction = (targetPosition - Position).Normalized(),
            Cosmetic = true,
            Color = color,
        };
        GetParent().AddChild(projectile);
        projectile.GlobalPosition = GlobalPosition;
    }

    /// <summary>Reaktiver Modifikator-Hook (Issue #12): wird bei jedem kritischen Treffer aufgerufen.</summary>
    private void OnCriticalHit()
    {
        if (HasModifier("adrenalin"))
        {
            ApplyHaste(HasteDuration, HasteMultiplier);
            SpawnFloatingText(Position, "Adrenalin!", CritColor);
        }
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

    /// <summary>Verdrahtet eine gezogene Karte je nach CardKind (Issue #12) entweder als ActiveAbility oder als passiven Modifikator.</summary>
    private void EquipCard(CardDefinition card, bool announce)
    {
        if (card.Kind == CardKind.Modifier)
        {
            EquipModifier(card, announce);
        }
        else
        {
            EquipAbility(card, announce);
        }
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

    /// <summary>
    /// Registriert eine Modifikatorkarte einmalig (kein Cooldown-Slot, siehe
    /// Kartenplanung/Issue #12 in CLAUDE.md). Erscheint bewusst NICHT in der
    /// Fähigkeiten-Leiste - dafür feuert nur AbilityGained (Level-up-Anzeige),
    /// nicht NewAbilityTypeUnlocked (Ability-Bar-Badge).
    /// </summary>
    private void EquipModifier(CardDefinition card, bool announce)
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

        if (announce)
        {
            AbilityGained?.Invoke(card);
        }
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

    /// <summary>Wie oft eine Karte dieses Typs bereits gezogen und ausgerüstet wurde (Aktion oder Modifikator).</summary>
    public int CountEquipped(string cardId) =>
        _abilities.Count(ability => ability.Card.Id == cardId) + _modifiers.Count(m => m.Id == cardId);

    /// <summary>Wie viele Karten dieses Typs noch im laufeigenen Nachziehstapel liegen.</summary>
    public int CountInDrawPile(string cardId) => _runDeck.DrawPile.Count(card => card.Id == cardId);

    /// <summary>Wie viele Karten dieses Typs auf der Ablage liegen (Issue #15: nicht gewählte Level-up-Karten).</summary>
    public int CountInDiscardPile(string cardId) => _runDeck.DiscardPile.Count(card => card.Id == cardId);

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
        "wiederkehr" => 20.0f, // Issue #21: seltener, build-prägender Effekt statt Basis-Tool
        "erinnerung" => 15.0f, // Issue #22
        "pfeilschuss" => 1.0f, // Issue #13: gleicher Rhythmus wie Hieb
        "arkaner_blitz" => 1.2f,
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

    /// <summary>
    /// Zieht LevelUpCardChoices Karten (Issue #15). Bei einer echten Auswahl
    /// (mehr als eine Karte gezogen) entscheidet der Spieler über
    /// CardChoiceOffered/ResolveCardChoice; wird nur eine Karte gezogen
    /// (Nachziehstapel fast leer), gibt es keine echte Wahl - dann wie
    /// bisher automatisch ausrüsten. Wird der Nachziehstapel dabei leer
    /// gezogen (kein Deck-Exhaustion-Handling, siehe Issue #20), bleibt der
    /// Level-up ohne neue Fähigkeit.
    /// </summary>
    private void LevelUp()
    {
        _level++;

        var drawn = _runDeck.DrawHand(LevelUpCardChoices, allowReshuffleFromDiscard: false);
        if (drawn.Count == 0)
        {
            return;
        }

        if (drawn.Count == 1)
        {
            EquipCard(drawn[0], announce: true);
            return;
        }

        CardChoiceOffered?.Invoke(drawn);
    }

    /// <summary>
    /// Schließt eine per CardChoiceOffered angebotene Level-up-Auswahl ab:
    /// die gewählte Karte wird ausgerüstet, die übrigen wandern dauerhaft
    /// auf die Ablage (Issue #15) - bis eine künftige Karte den Stapel
    /// gezielt zurückmischt (siehe Issue-Backlog), bleiben sie für den Rest
    /// des Runs unerreichbar.
    /// </summary>
    public void ResolveCardChoice(CardDefinition chosen, IReadOnlyList<CardDefinition> candidates)
    {
        foreach (var card in candidates)
        {
            if (card == chosen)
            {
                EquipCard(card, announce: false);
            }
            else
            {
                _runDeck.Discard(card);
            }
        }
    }

    /// <summary>Schließt eine per DiscardChoiceOffered angebotene Auswahl ab (Issue #22): die gewählte Karte wird aus der Ablage entfernt und direkt ausgerüstet. Über die Id statt einer konkreten Instanz aufgelöst (gleiches Muster wie DeckScreen.MoveOneCard), da Card-Instanzen desselben Typs austauschbar sind.</summary>
    public void ResolveDiscardChoice(string chosenCardId)
    {
        var card = _runDeck.TakeFromDiscard(chosenCardId);
        if (card is not null)
        {
            EquipCard(card, announce: false);
        }
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
