namespace PPRogueLite;

using System;
using System.Collections.Generic;
using Godot;
using PPRogueLite.Cards;
using PPRogueLite.Character;
using PPRogueLite.Combat;

/// <summary>
/// Verkabelt die reine Kampf-/Karten-Logik mit der UI. Entspricht dem Ablauf
/// aus dice-and-cards-prototype.html, nur in C#/Godot statt Vanilla-JS.
/// </summary>
public partial class Main : Control
{
    private const int HandSize = 5;

    private PlayerCharacter _player = null!;
    private Enemy _enemy = null!;
    private Deck _deck = null!;
    private CombatEngine _engine = null!;
    private List<CardDefinition> _hand = new();
    private bool _turnLocked;
    private bool _gameOver;

    private Label _nameLabel = null!;
    private Label _classLabel = null!;
    private GridContainer _statGrid = null!;
    private Label _acLabel = null!;
    private Label _hpLabel = null!;
    private ProgressBar _playerHpBar = null!;
    private Label _enemyNameLabel = null!;
    private Label _enemyHpLabel = null!;
    private ProgressBar _enemyHpBar = null!;
    private Label _rollReadoutLabel = null!;
    private RichTextLabel _logLabel = null!;
    private Label _handLabel = null!;
    private HBoxContainer _handContainer = null!;
    private Button _restartButton = null!;

    private static readonly Color HpGoodColor = new(0.352941f, 0.478431f, 0.309804f);
    private static readonly Color HpBadColor = new(0.611765f, 0.231373f, 0.231373f);

    private static readonly (Ability Ability, string Label)[] StatOrder =
    {
        (Ability.Strength, "Stärke"),
        (Ability.Dexterity, "Geschick"),
        (Ability.Constitution, "Konstit."),
        (Ability.Intelligence, "Intell."),
        (Ability.Wisdom, "Weisheit"),
        (Ability.Charisma, "Charisma"),
    };

    public override void _Ready()
    {
        _nameLabel = GetNode<Label>("MarginContainer/VBoxContainer/TopRow/SheetPanel/SheetVBox/NameLabel");
        _classLabel = GetNode<Label>("MarginContainer/VBoxContainer/TopRow/SheetPanel/SheetVBox/ClassLabel");
        _statGrid = GetNode<GridContainer>("MarginContainer/VBoxContainer/TopRow/SheetPanel/SheetVBox/StatGrid");
        _acLabel = GetNode<Label>("MarginContainer/VBoxContainer/TopRow/SheetPanel/SheetVBox/AcLabel");
        _hpLabel = GetNode<Label>("MarginContainer/VBoxContainer/TopRow/SheetPanel/SheetVBox/HpLabel");
        _playerHpBar = GetNode<ProgressBar>("MarginContainer/VBoxContainer/TopRow/SheetPanel/SheetVBox/PlayerHpBar");

        _enemyNameLabel = GetNode<Label>("MarginContainer/VBoxContainer/TopRow/RoomPanel/RoomVBox/EnemyNameLabel");
        _enemyHpLabel = GetNode<Label>("MarginContainer/VBoxContainer/TopRow/RoomPanel/RoomVBox/EnemyHpLabel");
        _enemyHpBar = GetNode<ProgressBar>("MarginContainer/VBoxContainer/TopRow/RoomPanel/RoomVBox/EnemyHpBar");
        _rollReadoutLabel = GetNode<Label>("MarginContainer/VBoxContainer/TopRow/RoomPanel/RoomVBox/RollReadoutLabel");

        _logLabel = GetNode<RichTextLabel>("MarginContainer/VBoxContainer/TopRow/RoomPanel/RoomVBox/LogLabel");
        _handLabel = GetNode<Label>("MarginContainer/VBoxContainer/HandLabel");
        _handContainer = GetNode<HBoxContainer>("MarginContainer/VBoxContainer/HandContainer");
        _restartButton = GetNode<Button>("MarginContainer/VBoxContainer/RestartButton");

        _restartButton.Pressed += StartNewCombat;

        StartNewCombat();
    }

    private void StartNewCombat()
    {
        _player = new PlayerCharacter
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

        _enemy = new Enemy
        {
            Name = "Höhlengoblin",
            MaxHp = 12,
            Hp = 12,
            ArmorClass = 13,
            AttackBonus = 4,
            DamageDie = 6,
            DamageBonus = 2,
        };

        _engine = new CombatEngine(_player, _enemy, AppendLog, text => _rollReadoutLabel.Text = text);
        _deck = new Deck(CardCatalog.BuildWarriorStartingDeck());
        _hand = new List<CardDefinition>();
        _turnLocked = false;
        _gameOver = false;

        _restartButton.Visible = false;
        _logLabel.Clear();
        _rollReadoutLabel.Text = "Wähle eine Karte, um den Kampf zu beginnen.";

        _nameLabel.Text = _player.Name;
        _classLabel.Text = _player.ClassName;
        _enemyNameLabel.Text = _enemy.Name;

        RenderStats();
        RenderVitals();
        DrawHand();
        AppendLog("Ein Goblin springt aus dem Schatten hervor!", LogTag.System);
    }

    private void DrawHand()
    {
        _hand = _deck.DrawHand(HandSize, AppendLog);
        RenderHand();
        RenderCounts();
    }

    private void PlayCard(int index)
    {
        if (_turnLocked || _gameOver)
        {
            return;
        }

        var card = _hand[index];
        _turnLocked = true;

        AppendLog($"— Du spielst [b]{card.DisplayName}[/b] —", LogTag.System);
        card.Play(_engine);
        RenderVitals();

        _deck.ResolveHand(_hand, index);
        _hand.Clear();
        RenderHand();
        RenderCounts();

        if (_enemy.IsDefeated)
        {
            DelayedCall(0.5, () => EndCombat(true));
            return;
        }

        DelayedCall(0.8, EnemyTurn);
    }

    private void EnemyTurn()
    {
        _engine.EnemyAttack();
        RenderVitals();

        if (_player.IsDefeated)
        {
            DelayedCall(0.4, () => EndCombat(false));
            return;
        }

        _turnLocked = false;
        DelayedCall(0.5, DrawHand);
    }

    private void EndCombat(bool won)
    {
        _gameOver = true;
        RenderHand();
        _restartButton.Visible = true;

        string message = won ? "Der Goblin fällt. Der Weg ist frei." : "Deine Kräfte verlassen dich...";
        _rollReadoutLabel.Text = message;
        AppendLog(message, won ? LogTag.Good : LogTag.Bad);
    }

    private void DelayedCall(double seconds, Action action)
    {
        var timer = GetTree().CreateTimer(seconds);
        timer.Timeout += action;
    }

    private void RenderStats()
    {
        foreach (Node child in _statGrid.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var (ability, label) in StatOrder)
        {
            int score = _player.Stats.Score(ability);
            int modifier = _player.Stats.Modifier(ability);
            var statLabel = new Label
            {
                Text = $"{label}\n{score} ({CombatEngine.FormatMod(modifier)})",
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            _statGrid.AddChild(statLabel);
        }
    }

    private void RenderVitals()
    {
        _acLabel.Text = $"Rüstungsklasse: {_player.ArmorClass}";
        _hpLabel.Text = $"Trefferpunkte: {_player.Hp} / {_player.MaxHp}";
        _playerHpBar.MaxValue = _player.MaxHp;
        _playerHpBar.Value = _player.Hp;
        SetHpBarColor(_playerHpBar, _player.Hp <= _player.MaxHp * 0.35);

        _enemyHpLabel.Text = $"Trefferpunkte: {_enemy.Hp} / {_enemy.MaxHp}";
        _enemyHpBar.MaxValue = _enemy.MaxHp;
        _enemyHpBar.Value = _enemy.Hp;
        SetHpBarColor(_enemyHpBar, _enemy.Hp <= _enemy.MaxHp * 0.35);
    }

    private static void SetHpBarColor(ProgressBar bar, bool low)
    {
        var fill = new StyleBoxFlat
        {
            BgColor = low ? HpBadColor : HpGoodColor,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomRight = 2,
            CornerRadiusBottomLeft = 2,
        };
        bar.AddThemeStyleboxOverride("fill", fill);
    }

    private void RenderCounts()
    {
        _handLabel.Text = $"HAND ({_hand.Count}) — Nachziehstapel: {_deck.DrawPile.Count} · Ablage: {_deck.DiscardPile.Count}";
    }

    private void RenderHand()
    {
        foreach (Node child in _handContainer.GetChildren())
        {
            child.QueueFree();
        }

        for (int i = 0; i < _hand.Count; i++)
        {
            var card = _hand[i];
            int capturedIndex = i;

            var button = new Button
            {
                Text = card.DisplayName,
                TooltipText = $"{card.CardType}\n{card.Description}\n{card.RequirementText}",
                Disabled = _turnLocked || _gameOver,
                CustomMinimumSize = new Vector2(150, 70),
                ThemeTypeVariation = "CardButton",
            };
            button.Pressed += () => PlayCard(capturedIndex);
            _handContainer.AddChild(button);
        }
    }

    private void AppendLog(string message, LogTag tag)
    {
        string bbcode = tag switch
        {
            LogTag.Good => $"[color=#3d5a34]{message}[/color]",
            LogTag.Bad => $"[color=#9c3b3b]{message}[/color]",
            _ => $"[color=#5c5240][i]{message}[/i][/color]",
        };
        _logLabel.AppendText(bbcode + "\n");
    }
}
