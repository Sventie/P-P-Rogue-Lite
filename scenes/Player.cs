namespace PPRogueLite;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PPRogueLite.Cards;
using PPRogueLite.Character;
using PPRogueLite.Meta;

/// <summary>
/// Spielerfigur der Echtzeit-Arena: WASD-Bewegung, dazu ein eigenes
/// CharacterLoadout (Issue #26 - Ausrüst-/Auslöse-Logik für aktive
/// Fähigkeiten, geteilt mit Companion). Player bleibt Eigentümer des EINEN
/// laufweiten Decks und des Level-ups (geteilte XP-Leiste für die ganze
/// Gruppe) - beim Level-up wird eine Karte aus dem Deck gezogen und dann,
/// nach Auswahl von Karte UND Ziel-Charakter (siehe Arena), auf dessen
/// Loadout ausgerüstet. Alle Treffer/Fehlschläge werden weiterhin per
/// verstecktem W20-Wurf entschieden, nur ohne Popup - sichtbar wird nur das
/// Ergebnis (Flugtext).
/// </summary>
public partial class Player : Node2D, IPartyMember
{
    private const float Speed = 220f;
    private const float MeleeRange = 90f; // nur für den Reichweiten-Ring in _Draw, siehe CharacterLoadout für die eigentliche Angriffslogik
    private const float Radius = 16f;
    private const int XpPerLevel = 2; // Test-Balance-Wert fuer schnelleres Testen (1 XP pro Kill)
    private const int LevelUpCardChoices = 2; // Issue #15: so viele Karten werden pro Level-up zur Auswahl gezogen

    private static readonly Color BodyColor = new(0.690196f, 0.552941f, 0.239216f);
    private static readonly Color RangeColor = new(0.85098f, 0.698039f, 0.361961f, 0.3f);

    /// <summary>Feuert bei einem Level-up, wenn nur eine Karte gezogen wurde (kein echter Choice) - der Aufrufer (Arena) zeigt die Karte und lässt den Spieler einen Ziel-Charakter wählen, dann direkt targetLoadout.EquipCard aufrufen.</summary>
    public event Action<CardDefinition>? AbilityGained;

    /// <summary>Feuert bei einem Level-up, wenn mehrere Karten zur Auswahl gezogen wurden (Issue #15) - der Aufrufer muss ResolveCardChoice mit Wahl UND Ziel-Charakter aufrufen.</summary>
    public event Action<IReadOnlyList<CardDefinition>>? CardChoiceOffered;

    /// <summary>Node-Referenz auf den aktuell laufenden Player (Issue #5) - Enemy nutzt sie, um erledigte Gegner immer dem gemeinsamen XP-Pool des Hauptcharakters gutzuschreiben. CharacterLoadout nutzt sie, um bei Wiederkehr/Erinnerung auf das eine geteilte Deck zuzugreifen, unabhängig davon, welcher Charakter die Karte ausgerüstet hat.</summary>
    public static Player? Instance { get; private set; }

    public PlayerCharacter Character { get; private set; } = null!;

    public CharacterLoadout Loadout { get; private set; } = null!;

    /// <summary>Das eine laufweite Deck der ganzen Gruppe (Issue #26: es gibt nur ein Deck/ein Level-up, egal wie viele Charaktere ausgerüstet werden).</summary>
    public Deck RunDeck => _runDeck;

    public int EffectiveArmorClass => Character.ArmorClass + Loadout.ArmorClassBonus;

    public bool IsDefeated => Character.IsDefeated;

    public int Xp => _xp;

    public int XpToNextLevel => XpPerLevel;

    private Deck _runDeck = null!;

    private bool _disabled;
    private float _slowTimer;
    private float _slowMultiplier = 1f;
    private float _hasteTimer;
    private float _hasteMultiplier = 1f;
    private int _xp;
    private int _level = 1;

    public override void _Ready()
    {
        Instance = this;
        AddToGroup("party");

        // Player spielt aktuell fest den Krieger (siehe CharacterClassCatalog,
        // Issue #13) - echte Auswahl zwischen den Archetypen folgt erst mit
        // einer echten Charakterauswahl für den Leader, der Katalog ist
        // bisher nur Datengrundlage.
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
        Loadout = new CharacterLoadout(this, this, Character.Stats);

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
                Loadout.EquipCard(card);
            }
        }
        else
        {
            _runDeck = new Deck(PlayerCardCollection.DeckCards);
            var startingCard = CardCatalog.AllCardTypes().First(card => card.Id == characterClass.StartingCardId);
            Loadout.EquipCard(startingCard);
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
        DungeonRun.SaveProgress(
            Character.Hp,
            _xp,
            _level,
            new List<CardDefinition>(_runDeck.DrawPile),
            new List<CardDefinition>(_runDeck.DiscardPile),
            Loadout.AllEquippedCards.ToList());
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

    public void Heal(int amount) => Character.Heal(amount);

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
    /// Zieht LevelUpCardChoices Karten aus dem EINEN geteilten Deck der
    /// Gruppe (Issue #15/#26). Bei einer echten Auswahl (mehr als eine
    /// Karte gezogen) entscheidet der Spieler über CardChoiceOffered/
    /// ResolveCardChoice; wird nur eine Karte gezogen (Nachziehstapel fast
    /// leer), gibt es keine Kartenwahl, aber weiterhin die Zielcharakter-
    /// Wahl (Arena zeigt beides zusammen). Wird der Nachziehstapel dabei
    /// leer gezogen (kein Deck-Exhaustion-Handling, siehe Issue #20), bleibt
    /// der Level-up ohne neue Fähigkeit.
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
            AbilityGained?.Invoke(drawn[0]);
            return;
        }

        CardChoiceOffered?.Invoke(drawn);
    }

    /// <summary>
    /// Schließt eine per CardChoiceOffered angebotene Level-up-Auswahl ab:
    /// die gewählte Karte wird auf targetLoadout ausgerüstet (Issue #26 -
    /// beliebiger Charakter der Gruppe, von Arena nach der Kartenwahl per
    /// Zielcharakter-Auswahl bestimmt), die übrigen wandern dauerhaft auf
    /// die (geteilte) Ablage.
    /// </summary>
    public void ResolveCardChoice(CardDefinition chosen, IReadOnlyList<CardDefinition> candidates, CharacterLoadout targetLoadout)
    {
        foreach (var card in candidates)
        {
            if (card == chosen)
            {
                targetLoadout.EquipCard(card);
            }
            else
            {
                _runDeck.Discard(card);
            }
        }
    }

    public void TakeDamage(int amount)
    {
        Character.TakeDamage(amount);
    }

    public void SetDisabled(bool disabled)
    {
        _disabled = disabled;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
