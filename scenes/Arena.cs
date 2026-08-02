namespace PPRogueLite;

using System;
using System.Collections.Generic;
using Godot;
using PPRogueLite.Cards;
using PPRogueLite.Character;

/// <summary>
/// Echtzeit-Arena: löst den alten rundenbasierten Testkampf (Main.tscn) ab.
/// Spawnt fortlaufend Gegner, zeigt HP/XP/Überlebenszeit/Fähigkeiten-Leiste
/// mit Cooldown-Balken. Beim Level-up pausiert die Runde (Spieler/Gegner/
/// Spawner deaktiviert) und zeigt die neu gezogene Karte zusammen mit einer
/// Deck-/Ablage-Übersicht und dem Charakterbogen, bis der Spieler per
/// "Weiter"-Button bestätigt. Schickt bei Niederlage zurück in den Hub.
/// </summary>
public partial class Arena : Node2D
{
    private const float SpawnMargin = 40f;

    private static readonly Color HpGoodColor = new(0.352941f, 0.478431f, 0.309804f);
    private static readonly Color HpBadColor = new(0.611765f, 0.231373f, 0.231373f);
    private static readonly Color CooldownFillColor = new(0.690196f, 0.552941f, 0.239216f);

    private static readonly (Ability Ability, string Label)[] StatOrder =
    {
        (Ability.Strength, "Stärke"),
        (Ability.Dexterity, "Geschick"),
        (Ability.Constitution, "Konstit."),
        (Ability.Intelligence, "Intell."),
        (Ability.Wisdom, "Weisheit"),
        (Ability.Charisma, "Charisma"),
    };

    private PackedScene _enemyScene = null!;
    private PackedScene _cardViewScene = null!;
    private Player _player = null!;
    private Timer _spawnTimer = null!;
    private Label _hpLabel = null!;
    private ProgressBar _hpBar = null!;
    private Label _xpLabel = null!;
    private ProgressBar _xpBar = null!;
    private Label _survivalLabel = null!;
    private Button _returnToHubButton = null!;
    private Control _levelUpLayer = null!;
    private HBoxContainer _abilityBar = null!;

    private readonly Dictionary<string, ProgressBar> _cooldownBars = new();
    private readonly Dictionary<string, Label> _abilityNameLabels = new();

    private double _survivalSeconds;
    private bool _gameOver;
    private bool _paused;

    public override void _Ready()
    {
        _enemyScene = GD.Load<PackedScene>("res://scenes/EnemyGoblin.tscn");
        _cardViewScene = GD.Load<PackedScene>("res://scenes/CardView.tscn");

        _player = GetNode<Player>("Player");
        _player.AbilityGained += OnAbilityGained;
        _player.NewAbilityTypeUnlocked += AddAbilityBadge;

        _spawnTimer = GetNode<Timer>("EnemySpawnTimer");
        _spawnTimer.Timeout += SpawnEnemy;

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
        _survivalLabel = GetNode<Label>("HUD/MarginContainer/VBoxContainer/SurvivalLabel");
        _returnToHubButton = GetNode<Button>("HUD/MarginContainer/VBoxContainer/ReturnToHubButton");
        _returnToHubButton.Visible = false;
        _returnToHubButton.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/Hub.tscn");

        _levelUpLayer = GetNode<Control>("HUD/LevelUpLayer");

        // Faehigkeiten, die der Player schon vor diesem _Ready() ausgeruestet hat
        // (z. B. das garantierte Start-Hieb), muessen nachtraeglich angezeigt
        // werden - das Event allein wuerde sie verpassen (Player._Ready laeuft
        // als Kind vor Arena._Ready).
        foreach (var card in _player.EquippedAbilityTypes)
        {
            AddAbilityBadge(card);
        }
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
            EndRun();
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

        var enemy = _enemyScene.Instantiate<EnemyGoblin>();
        AddChild(enemy);
        enemy.Position = RandomEdgePosition();
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

    private void AddAbilityBadge(CardDefinition card)
    {
        var panel = new PanelContainer { ThemeTypeVariation = "CardPanel" };

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
        UpdateAbilityLabel(card);
    }

    private void UpdateAbilityLabel(CardDefinition card)
    {
        if (!_abilityNameLabels.TryGetValue(card.Id, out var label))
        {
            return;
        }

        int count = _player.CountEquipped(card.Id);
        label.Text = count > 1 ? $"{card.DisplayName}  ×{count}" : card.DisplayName;
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

        var deckColumn = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        deckColumn.AddThemeConstantOverride("separation", 12);
        deckColumn.AddChild(BuildPileOverview("Nachziehstapel", _player.CountInDrawPile));
        deckColumn.AddChild(BuildPileOverview("Bereits gezogen", _player.CountEquipped));

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
            if (node is EnemyGoblin enemy)
            {
                enemy.SetDisabled(paused);
            }
        }

        if (paused)
        {
            _spawnTimer.Stop();
        }
        else
        {
            _spawnTimer.Start();
        }
    }

    private void EndRun()
    {
        _gameOver = true;
        _spawnTimer.Stop();
        _player.SetDisabled(true);
        _returnToHubButton.Visible = true;
    }
}
