namespace PPRogueLite;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using PPRogueLite.Cards;
using PPRogueLite.Character;
using PPRogueLite.Enemies;
using PPRogueLite.Meta;

/// <summary>
/// Echtzeit-Arena: löst den alten rundenbasierten Testkampf (Main.tscn) ab.
/// Eine Stage besteht aus mehreren Wellen (Welle N spawnt N Gegner, Anzahl
/// der Wellen kommt vom gewählten Pfad-Knoten - siehe DungeonRun/Issue
/// #10); die nächste Welle startet erst, wenn die aktuelle vollständig
/// besiegt ist (siehe CheckWaveCleared). HP/XP/Stage/Welle/Überlebenszeit/
/// Fähigkeiten-Leiste mit Cooldown-Balken werden im HUD angezeigt. Beim
/// Level-up pausiert die Runde (Spieler/Gegner/Wellenwechsel deaktiviert)
/// und zeigt die neu gezogene Karte zusammen mit einer Deck-/Ablage-
/// Übersicht und dem Charakterbogen, bis der Spieler per "Weiter"-Button
/// bestätigt.
///
/// Eine Stage ist eine von mehreren Stationen eines Dungeons (siehe
/// DungeonRun): nach Sieg in einer Nicht-Schluss-Stage geht's ins Lager
/// (Camp.tscn) für die Routenwahl der nächsten Stage, nach der letzten
/// Stage oder bei Niederlage zurück in die Taverne (Hub.tscn).
/// </summary>
public partial class Arena : Node2D
{
    private const float SpawnMargin = 40f;
    private const int BaseGoldReward = 25; // Test-Balance-Wert, wird mit DungeonRun.CurrentNode.GoldMultiplier skaliert

    // NUR ZUM TESTEN: überschreibt die tatsächlich gespielte Wellenanzahl
    // jeder Stage, unabhängig vom Pfad-Knoten - Lager/Routenwahl zeigen
    // weiterhin die echte Wellenanzahl des Knotens an, nur die Arena
    // spielt kürzer. Auf 0 setzen (oder die Zeile in _Ready löschen), um
    // wieder DungeonRun.CurrentNode.WaveCount zu verwenden.
    private const int TestWaveCountOverride = 2;

    private static readonly Color HpGoodColor = new(0.352941f, 0.478431f, 0.309804f);
    private static readonly Color HpBadColor = new(0.611765f, 0.231373f, 0.231373f);
    private static readonly Color CooldownFillColor = new(0.690196f, 0.552941f, 0.239216f);
    private static readonly Color DisabledAbilityModulate = new(1f, 1f, 1f, 0.5f);

    private static readonly (Ability Ability, string Label)[] StatOrder =
    {
        (Ability.Strength, "Stärke"),
        (Ability.Dexterity, "Geschick"),
        (Ability.Constitution, "Konstit."),
        (Ability.Intelligence, "Intell."),
        (Ability.Wisdom, "Weisheit"),
        (Ability.Charisma, "Charisma"),
    };

    private static readonly Vector2[] CompanionFormationOffsets =
    {
        new(-50, -40), new(50, -40), new(0, 60),
    };

    private PackedScene _enemyScene = null!;
    private PackedScene _cardViewScene = null!;
    private PackedScene _companionScene = null!;
    private Player _player = null!;
    private readonly List<Companion> _companions = new();
    private Timer _waveTransitionTimer = null!;
    private Label _hpLabel = null!;
    private ProgressBar _hpBar = null!;
    private Label _xpLabel = null!;
    private ProgressBar _xpBar = null!;
    private Label _stageLabel = null!;
    private Label _waveLabel = null!;
    private Label _survivalLabel = null!;
    private Label _outcomeLabel = null!;
    private Button _continueButton = null!;
    private Control _levelUpLayer = null!;
    private HBoxContainer _abilityBar = null!;

    private readonly Dictionary<string, ProgressBar> _cooldownBars = new();
    private readonly Dictionary<string, Label> _abilityNameLabels = new();
    private readonly Dictionary<string, Control> _abilityPanels = new();

    private double _survivalSeconds;
    private int _totalWaves = 10;
    private int _currentWave;
    private bool _isBossStage;
    private bool _waveTransitionPending;
    private bool _gameOver;
    private bool _paused;
    private string _pendingTargetScene = "res://scenes/Hub.tscn";

    public override void _Ready()
    {
        // Ermöglicht auch einen direkten Testlauf der Arena-Szene in Godot
        // (z. B. F6), ohne vorher über den Hub "Dungeon betreten" geklickt
        // zu haben - ohne diese Absicherung wäre CurrentStage sonst 0 und
        // Stage-/Lager-Anzeige würden falsche Werte zeigen.
        if (DungeonRun.CurrentStage == 0)
        {
            DungeonRun.Start();
        }

        // Wellenanzahl kommt vom gewählten Pfad-Knoten (Issue #10) - Start-
        // Stage hat immer die Standard-Wellenanzahl ("ohne Modifikatoren"),
        // Zwischen-Stages variieren je nach gewählter Route. Die Boss-Stage
        // (letzte Stage, Issue #11) hat AUSSCHLIESSLICH den Boss als
        // Encounter - keine normalen Wellen davor, siehe BeginWave/SpawnBoss.
        _isBossStage = DungeonRun.CurrentStage == DungeonRun.TotalStages;
        _totalWaves = _isBossStage
            ? 1
            : (TestWaveCountOverride > 0 ? TestWaveCountOverride : DungeonRun.CurrentNode.WaveCount);

        _enemyScene = GD.Load<PackedScene>("res://scenes/Enemy.tscn");
        _cardViewScene = GD.Load<PackedScene>("res://scenes/CardView.tscn");
        _companionScene = GD.Load<PackedScene>("res://scenes/Companion.tscn");

        _player = GetNode<Player>("Player");
        _player.AbilityGained += OnAbilityGained;
        _player.NewAbilityTypeUnlocked += AddAbilityBadge;
        _player.CardChoiceOffered += OnCardChoiceOffered;
        _player.DiscardChoiceOffered += OnDiscardChoiceOffered;

        SpawnCompanions();

        _waveTransitionTimer = GetNode<Timer>("WaveTransitionTimer");
        _waveTransitionTimer.Timeout += OnWaveTransitionTimeout;

        _abilityBar = GetNode<HBoxContainer>("HUD/MarginContainer/VBoxContainer/AbilityBar");
        _hpLabel = GetNode<Label>("HUD/MarginContainer/VBoxContainer/HpLabel");
        _hpBar = GetNode<ProgressBar>("HUD/MarginContainer/VBoxContainer/HpBar");
        _xpLabel = GetNode<Label>("HUD/MarginContainer/VBoxContainer/XpLabel");
        _xpBar = GetNode<ProgressBar>("HUD/MarginContainer/VBoxContainer/XpBar");
        _xpBar.AddThemeStyleboxOverride("fill", new StyleBoxFlat
        {
            BgColor = CooldownFillColor,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomRight = 2,
            CornerRadiusBottomLeft = 2,
        });
        _stageLabel = GetNode<Label>("HUD/MarginContainer/VBoxContainer/StageLabel");
        _stageLabel.Text = $"Stage: {DungeonRun.CurrentStage} / {DungeonRun.TotalStages}";
        _waveLabel = GetNode<Label>("HUD/MarginContainer/VBoxContainer/WaveLabel");
        _survivalLabel = GetNode<Label>("HUD/MarginContainer/VBoxContainer/SurvivalLabel");
        _outcomeLabel = GetNode<Label>("HUD/MarginContainer/VBoxContainer/OutcomeLabel");
        _continueButton = GetNode<Button>("HUD/MarginContainer/VBoxContainer/ContinueButton");
        _continueButton.Visible = false;
        _continueButton.Pressed += () => GetTree().ChangeSceneToFile(_pendingTargetScene);

        _levelUpLayer = GetNode<Control>("HUD/LevelUpLayer");

        // Faehigkeiten, die der Player schon vor diesem _Ready() ausgeruestet hat
        // (z. B. das garantierte Start-Hieb), muessen nachtraeglich angezeigt
        // werden - das Event allein wuerde sie verpassen (Player._Ready laeuft
        // als Kind vor Arena._Ready).
        foreach (var card in _player.EquippedAbilityTypes)
        {
            AddAbilityBadge(card);
        }

        BeginWave(1);
    }

    public override void _Process(double delta)
    {
        if (_gameOver || _paused)
        {
            return;
        }

        _survivalSeconds += delta;
        _survivalLabel.Text = $"Überlebt: {(int)_survivalSeconds}s";

        UpdateHpDisplay();
        UpdateXpDisplay();
        UpdateCooldownBars();

        if (_player.Character.IsDefeated)
        {
            DungeonRun.End();
            FinishRun("Niederlage", "Zurück zur Taverne", "res://scenes/Hub.tscn");
            return;
        }

        CheckWaveCleared();
    }

    /// <summary>
    /// Spawnt einen Companion-Node je ausgewähltem, noch lebendem Gefährten
    /// (Issue #5, PlayerCharacterCollection.SelectedCompanions) auf einem
    /// festen Formations-Platz um den Leader. Bei einer Folge-Stage
    /// desselben Dungeons wird die gespeicherte HP übernommen
    /// (DungeonRun.SavedCompanionHp), sonst startet der Companion mit
    /// voller HP.
    /// </summary>
    private void SpawnCompanions()
    {
        var selected = PlayerCharacterCollection.SelectedCompanions.Where(character => character.IsAlive).ToList();

        for (int i = 0; i < selected.Count; i++)
        {
            var companion = _companionScene.Instantiate<Companion>();
            AddChild(companion);

            var offset = CompanionFormationOffsets[i % CompanionFormationOffsets.Length];
            companion.Position = _player.Position + offset;

            int? savedHp = DungeonRun.HasProgress && DungeonRun.SavedCompanionHp.TryGetValue(selected[i].Id, out int hp)
                ? hp
                : null;
            companion.Initialize(selected[i], _player, offset, savedHp);

            _companions.Add(companion);
        }
    }

    private void UpdateHpDisplay()
    {
        var character = _player.Character;
        _hpLabel.Text = $"HP: {character.Hp} / {character.MaxHp}";
        _hpBar.MaxValue = character.MaxHp;
        _hpBar.Value = character.Hp;

        bool low = character.Hp <= character.MaxHp * 0.35;
        var fill = new StyleBoxFlat
        {
            BgColor = low ? HpBadColor : HpGoodColor,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomRight = 2,
            CornerRadiusBottomLeft = 2,
        };
        _hpBar.AddThemeStyleboxOverride("fill", fill);
    }

    private void UpdateXpDisplay()
    {
        _xpLabel.Text = $"XP: {_player.Xp} / {_player.XpToNextLevel}";
        _xpBar.MaxValue = _player.XpToNextLevel;
        _xpBar.Value = _player.Xp;
    }

    private void UpdateCooldownBars()
    {
        foreach (var (cardId, bar) in _cooldownBars)
        {
            bar.Value = _player.GetCooldownProgress(cardId);
        }
    }

    private void SpawnEnemy()
    {
        if (_gameOver || _paused)
        {
            return;
        }

        // Gestaffelte Einführung neuer Gegnertypen (Issue #9): welche Typen
        // im Pool sind, hängt von der aktuellen Dungeon-Stage ab, nicht von
        // der Stage-internen Wellenanzahl.
        var pool = EnemyCatalog.AvailableForStage(DungeonRun.CurrentStage);
        var definition = pool[GD.RandRange(0, pool.Count - 1)];

        var enemy = _enemyScene.Instantiate<Enemy>();
        AddChild(enemy);
        enemy.Position = RandomEdgePosition();
        enemy.Initialize(definition);
    }

    /// <summary>Startet Welle N: spawnt N Gegner auf einmal (Welle 1 = 1, Welle 2 = 2, ...). Auf der Boss-Stage (_totalWaves == 1) wird stattdessen einmalig der Boss gespawnt.</summary>
    private void BeginWave(int waveNumber)
    {
        _currentWave = waveNumber;
        UpdateWaveDisplay();

        if (_isBossStage)
        {
            SpawnBoss();
            return;
        }

        for (int i = 0; i < waveNumber; i++)
        {
            SpawnEnemy();
        }
    }

    private void UpdateWaveDisplay()
    {
        _waveLabel.Text = _isBossStage ? "Boss-Kampf" : $"Welle: {_currentWave} / {_totalWaves}";
    }

    private void SpawnBoss()
    {
        var boss = _enemyScene.Instantiate<Enemy>();
        AddChild(boss);
        boss.Position = RandomEdgePosition();
        boss.Initialize(new OgerHaeuptlingDefinition());
    }

    /// <summary>Spawnt einen Gegner nahe einer Position mit leichtem Zufalls-Versatz - genutzt vom Oger-Häuptling, um Verstärkung zu rufen (Issue #11).</summary>
    public void SpawnEnemyNear(EnemyDefinition definition, Vector2 position, float scatterRadius)
    {
        var enemy = _enemyScene.Instantiate<Enemy>();
        AddChild(enemy);
        var offset = new Vector2((float)GD.RandRange(-scatterRadius, scatterRadius), (float)GD.RandRange(-scatterRadius, scatterRadius));
        enemy.Position = position + offset;
        enemy.Initialize(definition);
    }

    /// <summary>
    /// Erkennt per Poll (kein Event von Enemy), ob die aktuelle Welle
    /// besiegt ist - dann entweder Stage abschließen (letzte Welle) oder
    /// nach einer kurzen Pause (WaveTransitionTimer) die nächste Welle
    /// starten.
    /// </summary>
    private void CheckWaveCleared()
    {
        if (_waveTransitionPending || _currentWave == 0)
        {
            return;
        }

        if (GetTree().GetNodesInGroup("enemies").Count > 0)
        {
            return;
        }

        if (_currentWave >= _totalWaves)
        {
            CompleteStage();
            return;
        }

        _waveTransitionPending = true;
        _waveTransitionTimer.Start();
    }

    private void OnWaveTransitionTimeout()
    {
        _waveTransitionPending = false;
        BeginWave(_currentWave + 1);
    }

    /// <summary>
    /// Letzte Welle besiegt: Gold gutschreiben (Basisbetrag × Gold-
    /// Multiplikator des gewählten Pfad-Knotens, Issue #10), Fortschritt
    /// sichern (DungeonRun/Player.SaveProgress) und je nachdem, ob noch
    /// Stages übrig sind, entweder ins Lager (Routenwahl für die nächste
    /// Stage) oder zurück in die Taverne (Dungeon komplett abgeschlossen).
    /// Die Stage-Nummer selbst wird hier NICHT erhöht - das passiert erst,
    /// wenn der Spieler im Lager eine Route wählt (DungeonRun.SelectNextNode).
    /// </summary>
    private void CompleteStage()
    {
        int reward = (int)Math.Round(BaseGoldReward * DungeonRun.CurrentNode.GoldMultiplier);
        PlayerWallet.Gold += reward;
        _player.SaveProgress();
        SaveCompanionProgress();

        if (DungeonRun.CurrentStage < DungeonRun.TotalStages)
        {
            FinishRun(
                $"Stage {DungeonRun.CurrentStage} von {DungeonRun.TotalStages} abgeschlossen! +{reward} Gold",
                "Weiter zum Lager",
                "res://scenes/Camp.tscn");
        }
        else
        {
            DungeonRun.End();
            FinishRun($"Dungeon abgeschlossen! +{reward} Gold", "Zurück zur Taverne", "res://scenes/Hub.tscn");
        }
    }

    /// <summary>Schreibt die aktuelle HP aller noch lebenden Companions in DungeonRun, damit die nächste Stage desselben Dungeons daran anknüpfen kann (gleiches Prinzip wie Player.SaveProgress).</summary>
    private void SaveCompanionProgress()
    {
        var hp = new Dictionary<Guid, int>();
        foreach (var companion in _companions)
        {
            if (!IsInstanceValid(companion))
            {
                continue;
            }

            hp[companion.Owned.Id] = companion.CurrentHp;
        }

        DungeonRun.SaveCompanionHp(hp);
    }

    private Vector2 RandomEdgePosition()
    {
        var viewportSize = GetViewport().GetVisibleRect().Size;
        int side = GD.RandRange(0, 3);

        return side switch
        {
            0 => new Vector2((float)GD.RandRange(0.0, viewportSize.X), -SpawnMargin),
            1 => new Vector2((float)GD.RandRange(0.0, viewportSize.X), viewportSize.Y + SpawnMargin),
            2 => new Vector2(-SpawnMargin, (float)GD.RandRange(0.0, viewportSize.Y)),
            _ => new Vector2(viewportSize.X + SpawnMargin, (float)GD.RandRange(0.0, viewportSize.Y)),
        };
    }

    /// <summary>
    /// Fähigkeiten-Badge in der Fähigkeiten-Leiste - anklickbar, um genau
    /// diese Fähigkeit (alle Instanzen, siehe Player.ToggleAbility) ein-
    /// oder auszuschalten. Gedacht, damit einzelne Effekte isoliert
    /// getestet werden können oder alle Angriffe deaktiviert werden können,
    /// um kontrolliert Schaden zu nehmen - bewusst keine reine
    /// Testfunktion, siehe Player.ToggleAbility.
    /// </summary>
    private void AddAbilityBadge(CardDefinition card)
    {
        var panel = new PanelContainer
        {
            ThemeTypeVariation = "CardPanel",
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
        };
        panel.GuiInput += @event => OnAbilityBadgeInput(card, @event);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);

        var label = new Label { HorizontalAlignment = HorizontalAlignment.Center };

        var bar = new ProgressBar
        {
            CustomMinimumSize = new Vector2(90, 8),
            MaxValue = 1.0,
            Value = 0.0,
            ShowPercentage = false,
        };
        bar.AddThemeStyleboxOverride("fill", new StyleBoxFlat
        {
            BgColor = CooldownFillColor,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomRight = 2,
            CornerRadiusBottomLeft = 2,
        });

        vbox.AddChild(label);
        vbox.AddChild(bar);
        panel.AddChild(vbox);
        _abilityBar.AddChild(panel);

        _cooldownBars[card.Id] = bar;
        _abilityNameLabels[card.Id] = label;
        _abilityPanels[card.Id] = panel;
        UpdateAbilityLabel(card);
    }

    private void UpdateAbilityLabel(CardDefinition card)
    {
        if (!_abilityNameLabels.TryGetValue(card.Id, out var label))
        {
            return;
        }

        int count = _player.CountEquipped(card.Id);
        string baseText = count > 1 ? $"{card.DisplayName}  ×{count}" : card.DisplayName;
        bool enabled = _player.IsAbilityEnabled(card.Id);
        label.Text = enabled ? baseText : $"{baseText} (aus)";

        if (_abilityPanels.TryGetValue(card.Id, out var panel))
        {
            panel.Modulate = enabled ? Colors.White : DisabledAbilityModulate;
        }
    }

    private void OnAbilityBadgeInput(CardDefinition card, InputEvent @event)
    {
        if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false })
        {
            return;
        }

        _player.ToggleAbility(card.Id);
        UpdateAbilityLabel(card);
    }

    private async void OnAbilityGained(CardDefinition card)
    {
        SetWorldPaused(true);
        UpdateAbilityLabel(card);

        // HBoxContainer kennt kein "space-between" - volle Bildschirmbreite
        // wird ueber Fill/Expand-Spacer zwischen den drei fixen Spalten
        // erreicht (LevelUpLayer ist ein MarginContainer, spannt also die
        // volle Breite auf, statt wie vorher nur den Inhalt zu zentrieren).
        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand,
            SizeFlagsVertical = Control.SizeFlags.Fill | Control.SizeFlags.Expand,
        };
        row.AddThemeConstantOverride("separation", 24);

        var deckColumn = BuildDeckOverviewColumn();

        var cardColumn = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        cardColumn.AddThemeConstantOverride("separation", 12);

        var cardView = _cardViewScene.Instantiate<CardView>();
        var continueButton = new Button
        {
            Text = "Weiter",
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        };
        cardColumn.AddChild(cardView);
        cardColumn.AddChild(continueButton);

        var sheetColumn = BuildCharacterSheet();
        sheetColumn.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;

        row.AddChild(deckColumn);
        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand });
        row.AddChild(cardColumn);
        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand });
        row.AddChild(sheetColumn);

        _levelUpLayer.AddChild(row);
        cardView.Populate(card);

        await ToSignal(continueButton, Button.SignalName.Pressed);

        row.QueueFree();
        SetWorldPaused(false);
    }

    /// <summary>
    /// Level-up mit echter Kartenwahl (Issue #15): zeigt alle gezogenen
    /// Karten nebeneinander, ein Klick auf eine Karte (CardView.Clicked,
    /// bisher ungenutzt seit dem Pivot zu Echtzeit) entscheidet direkt -
    /// kein zusätzlicher "Weiter"-Button nötig. Die nicht gewählten Karten
    /// wandern über Player.ResolveCardChoice auf die Ablage.
    /// </summary>
    private async void OnCardChoiceOffered(IReadOnlyList<CardDefinition> candidates)
    {
        SetWorldPaused(true);

        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand,
            SizeFlagsVertical = Control.SizeFlags.Fill | Control.SizeFlags.Expand,
        };
        row.AddThemeConstantOverride("separation", 24);

        var deckColumn = BuildDeckOverviewColumn();

        var choiceColumn = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        choiceColumn.AddThemeConstantOverride("separation", 12);
        choiceColumn.AddChild(new Label
        {
            Text = "Wähle eine Karte",
            HorizontalAlignment = HorizontalAlignment.Center,
            ThemeTypeVariation = "ColumnHeaderLabel",
        });

        var cardsRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        cardsRow.AddThemeConstantOverride("separation", 16);
        choiceColumn.AddChild(cardsRow);

        var selection = new TaskCompletionSource<CardDefinition>();
        foreach (var candidate in candidates)
        {
            var cardView = _cardViewScene.Instantiate<CardView>();
            cardsRow.AddChild(cardView);
            cardView.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
            cardView.Populate(candidate);
            cardView.Clicked += () => selection.TrySetResult(candidate);
        }

        var sheetColumn = BuildCharacterSheet();
        sheetColumn.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;

        row.AddChild(deckColumn);
        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand });
        row.AddChild(choiceColumn);
        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand });
        row.AddChild(sheetColumn);

        _levelUpLayer.AddChild(row);

        var chosen = await selection.Task;
        _player.ResolveCardChoice(chosen, candidates);
        UpdateAbilityLabel(chosen);

        row.QueueFree();
        SetWorldPaused(false);
    }

    /// <summary>
    /// Auswahl aus der Ablage (Issue #22, "Erinnerung"): gleicher Aufbau
    /// wie OnCardChoiceOffered, aber die Kandidaten kommen aus der Ablage
    /// (gruppiert nach Kartentyp mit Stückzahl, gleiches Muster wie
    /// DeckScreen.RenderColumn) statt aus frisch gezogenen Karten. Die
    /// gewählte Karte wird über Player.ResolveDiscardChoice direkt
    /// ausgerüstet, alle anderen bleiben in der Ablage liegen.
    /// </summary>
    private async void OnDiscardChoiceOffered(IReadOnlyList<CardDefinition> discardPile)
    {
        SetWorldPaused(true);

        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand,
            SizeFlagsVertical = Control.SizeFlags.Fill | Control.SizeFlags.Expand,
        };
        row.AddThemeConstantOverride("separation", 24);

        var deckColumn = BuildDeckOverviewColumn();

        var choiceColumn = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        choiceColumn.AddThemeConstantOverride("separation", 12);
        choiceColumn.AddChild(new Label
        {
            Text = "Wähle eine Karte aus der Ablage",
            HorizontalAlignment = HorizontalAlignment.Center,
            ThemeTypeVariation = "ColumnHeaderLabel",
        });

        var cardsRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        cardsRow.AddThemeConstantOverride("separation", 16);
        choiceColumn.AddChild(cardsRow);

        var grouped = discardPile
            .GroupBy(card => card.Id)
            .Select(group => (Card: group.First(), Count: group.Count()))
            .OrderBy(entry => entry.Card.DisplayName);

        var selection = new TaskCompletionSource<CardDefinition>();
        foreach (var (card, count) in grouped)
        {
            var cardView = _cardViewScene.Instantiate<CardView>();
            cardsRow.AddChild(cardView);
            cardView.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
            cardView.Populate(card, count);
            cardView.Clicked += () => selection.TrySetResult(card);
        }

        var sheetColumn = BuildCharacterSheet();
        sheetColumn.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;

        row.AddChild(deckColumn);
        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand });
        row.AddChild(choiceColumn);
        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand });
        row.AddChild(sheetColumn);

        _levelUpLayer.AddChild(row);

        var chosen = await selection.Task;
        _player.ResolveDiscardChoice(chosen.Id);
        UpdateAbilityLabel(chosen);

        row.QueueFree();
        SetWorldPaused(false);
    }

    /// <summary>Linke Spalte des Level-up-Screens: Nachziehstapel/Ausgerüstet/Ablage-Übersicht, gemeinsam genutzt von OnAbilityGained und OnCardChoiceOffered.</summary>
    private VBoxContainer BuildDeckOverviewColumn()
    {
        var deckColumn = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        deckColumn.AddThemeConstantOverride("separation", 12);
        deckColumn.AddChild(BuildPileOverview("Nachziehstapel", _player.CountInDrawPile));
        deckColumn.AddChild(BuildPileOverview("Bereits gezogen", _player.CountEquipped));
        deckColumn.AddChild(BuildPileOverview("Ablage", _player.CountInDiscardPile));
        return deckColumn;
    }

    /// <summary>
    /// Übersicht über einen Kartenstapel: eine schrumpfende Stapel-Grafik
    /// (<see cref="DeckStackView"/>) plus alle bekannten Kartentypen mit
    /// Stückzahl, ausgegraut bei 0. Zeigt bewusst nur Zusammenfassungen pro
    /// Typ, nicht die tatsächliche (gemischte) Reihenfolge des Nachziehstapels.
    /// </summary>
    private static Control BuildPileOverview(string title, Func<string, int> countLookup)
    {
        var panel = new PanelContainer { ThemeTypeVariation = "CardPanel" };

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);

        var header = new Label { Text = title, ThemeTypeVariation = "CardTypeLabel" };
        vbox.AddChild(header);

        int total = 0;
        foreach (var card in CardCatalog.AllCardTypes())
        {
            total += countLookup(card.Id);
        }

        vbox.AddChild(new DeckStackView
        {
            Count = total,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        });

        foreach (var card in CardCatalog.AllCardTypes())
        {
            int count = countLookup(card.Id);
            var row = new Label { Text = $"{card.DisplayName} – {count}×" };
            if (count == 0)
            {
                row.Modulate = new Color(1f, 1f, 1f, 0.4f);
            }

            vbox.AddChild(row);
        }

        panel.AddChild(vbox);
        return panel;
    }

    private Control BuildCharacterSheet()
    {
        var panel = new PanelContainer
        {
            ThemeTypeVariation = "CardPanel",
            CustomMinimumSize = new Vector2(220, 0),
        };

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);

        var portrait = new PanelContainer
        {
            CustomMinimumSize = new Vector2(96, 96),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        };
        portrait.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0f, 0f, 0f, 0.08f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderColor = new Color(0f, 0f, 0f, 0.3f),
            CornerRadiusTopLeft = 48,
            CornerRadiusTopRight = 48,
            CornerRadiusBottomRight = 48,
            CornerRadiusBottomLeft = 48,
        });
        portrait.AddChild(new Label
        {
            Text = "Bild",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand,
            SizeFlagsVertical = Control.SizeFlags.Fill | Control.SizeFlags.Expand,
        });

        var character = _player.Character;
        vbox.AddChild(portrait);
        vbox.AddChild(new Label { Text = character.Name });
        vbox.AddChild(new Label { Text = character.ClassName });
        vbox.AddChild(BuildStatLine("Rüstungsklasse", character.BaseArmorClass, _player.EffectiveArmorClass));

        foreach (var (ability, label) in StatOrder)
        {
            int score = character.Stats.Score(ability);
            vbox.AddChild(BuildStatLine(label, score, score));
        }

        panel.AddChild(vbox);
        return panel;
    }

    /// <summary>
    /// Eine Statzeile im Format "Name: Effektivwert (Basiswert Delta)" - das
    /// Delta ist aktuell fast immer 0 (nur die Rüstungsklasse kann sich durch
    /// Parade kurzzeitig erhöhen), das Format ist aber bereits vorbereitet
    /// für künftige Boni durch Ereignisse/Ausrüstung (grün) bzw. Mali (rot).
    /// </summary>
    private static Label BuildStatLine(string label, int baseValue, int effectiveValue)
    {
        int delta = effectiveValue - baseValue;
        string deltaText = delta >= 0 ? $"+{delta}" : delta.ToString();

        var line = new Label { Text = $"{label}: {effectiveValue} ({baseValue} {deltaText})" };
        if (delta > 0)
        {
            line.AddThemeColorOverride("font_color", HpGoodColor);
        }
        else if (delta < 0)
        {
            line.AddThemeColorOverride("font_color", HpBadColor);
        }

        return line;
    }

    private void SetWorldPaused(bool paused)
    {
        _paused = paused;
        _player.SetDisabled(paused);

        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is Enemy enemy)
            {
                enemy.SetDisabled(paused);
            }
        }

        foreach (Node node in GetTree().GetNodesInGroup("projectiles"))
        {
            if (node is Projectile projectile)
            {
                projectile.SetDisabled(paused);
            }
        }

        foreach (var companion in _companions)
        {
            if (IsInstanceValid(companion))
            {
                companion.SetDisabled(paused);
            }
        }

        // Der Timer wird nur für die kurze Pause zwischen zwei Wellen
        // gebraucht - ihn bei jeder Level-up-Pause blind zu stoppen/starten
        // würde sonst faelschlich einen Wellenwechsel anstossen, obwohl die
        // aktuelle Welle noch gar nicht besiegt ist.
        if (!_waveTransitionPending)
        {
            return;
        }

        if (paused)
        {
            _waveTransitionTimer.Stop();
        }
        else
        {
            _waveTransitionTimer.Start();
        }
    }

    private void FinishRun(string outcomeText, string buttonText, string targetScene)
    {
        _gameOver = true;
        _waveTransitionTimer.Stop();
        _player.SetDisabled(true);
        _outcomeLabel.Text = outcomeText;
        _outcomeLabel.Visible = true;
        _continueButton.Text = buttonText;
        _continueButton.Visible = true;
        _pendingTargetScene = targetScene;
    }
}
