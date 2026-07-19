namespace PPRogueLite;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using PPRogueLite.Cards;
using PPRogueLite.Character;
using PPRogueLite.Combat;

/// <summary>
/// Verkabelt die reine Kampf-/Karten-Logik mit der UI. Entspricht dem Ablauf
/// aus dice-and-cards-prototype.html, nur in C#/Godot statt Vanilla-JS.
///
/// Jede Log-Zeile aus dem CombatEngine landet zunächst in einer Warteschlange
/// und wird nacheinander groß im PopupPanel gezeigt (Würfe zusätzlich mit
/// Erfolg/Misserfolg-Stempel); der Name der laufenden Aktion (Karte oder
/// Gegner) steht währenddessen fest als Titel über dem Popup. Jede Zeile
/// wird erst per "Weiter"-Button bestätigt, bevor sie dauerhaft ins Log
/// wandert. Werte (HP-Balken etc.) werden erst gerendert, nachdem die
/// zugehörigen Popups durchgelaufen sind - so wirkt sich eine Aktion nicht
/// "sofort" sichtbar aus.
/// </summary>
public partial class Main : Control
{
    private const int HandSize = 5;

    private readonly record struct PendingLogEntry(string Message, LogTag Tag, RollStamp? Stamp);

    private static readonly Color HpGoodColor = new(0.352941f, 0.478431f, 0.309804f);
    private static readonly Color HpBadColor = new(0.611765f, 0.231373f, 0.231373f);

    private readonly Queue<PendingLogEntry> _pendingLog = new();

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
    private Button _returnToHubButton = null!;
    private PanelContainer _popupPanel = null!;
    private Label _popupActionLabel = null!;
    private RichTextLabel _popupMessageLabel = null!;
    private Label _popupStampLabel = null!;
    private Button _popupContinueButton = null!;
    private PackedScene _cardViewScene = null!;

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
        _returnToHubButton = GetNode<Button>("MarginContainer/VBoxContainer/ReturnToHubButton");

        _popupPanel = GetNode<PanelContainer>("PopupLayer/PopupPanel");
        _popupActionLabel = GetNode<Label>("PopupLayer/PopupPanel/PopupVBox/PopupActionLabel");
        _popupMessageLabel = GetNode<RichTextLabel>("PopupLayer/PopupPanel/PopupVBox/PopupMessageLabel");
        _popupStampLabel = GetNode<Label>("PopupLayer/PopupPanel/PopupVBox/PopupStampLabel");
        _popupContinueButton = GetNode<Button>("PopupLayer/PopupPanel/PopupVBox/PopupContinueButton");
        _cardViewScene = GD.Load<PackedScene>("res://scenes/CardView.tscn");

        _returnToHubButton.Pressed += ReturnToHub;

        StartNewCombat();
    }

    private async void StartNewCombat()
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

        _engine = new CombatEngine(_player, _enemy, EnqueueLog);
        _deck = new Deck(CardCatalog.BuildWarriorStartingDeck());
        _hand = new List<CardDefinition>();
        _pendingLog.Clear();
        _turnLocked = false;
        _gameOver = false;

        _popupPanel.Visible = false;
        _returnToHubButton.Visible = false;
        _logLabel.Clear();
        _rollReadoutLabel.Text = "Wähle eine Karte, um den Kampf zu beginnen.";

        _nameLabel.Text = _player.Name;
        _classLabel.Text = _player.ClassName;
        _enemyNameLabel.Text = _enemy.Name;

        RenderStats();
        RenderVitals();
        RenderHand();

        _popupActionLabel.Text = _enemy.Name;
        EnqueueLog("Ein Goblin springt aus dem Schatten hervor!", LogTag.System, null);
        await DrawHandAsync();
    }

    private async Task DrawHandAsync()
    {
        _hand = _deck.DrawHand(HandSize, (message, tag) => EnqueueLog(message, tag, null));
        await RevealPendingLogAsync();
        RenderHand();
        RenderCounts();
    }

    private async void PlayCard(int index)
    {
        if (_turnLocked || _gameOver)
        {
            return;
        }

        var card = _hand[index];
        _turnLocked = true;
        RenderHand();

        _popupActionLabel.Text = card.DisplayName;
        card.Play(_engine);

        _deck.ResolveHand(_hand, index);
        _hand.Clear();

        await RevealPendingLogAsync();
        RenderVitals();
        RenderCounts();
        RenderHand();

        if (_enemy.IsDefeated)
        {
            await Delay(0.4);
            EndCombat(true);
            return;
        }

        await Delay(0.6);
        await EnemyTurnAsync();
    }

    private async Task EnemyTurnAsync()
    {
        _popupActionLabel.Text = _enemy.Name;
        _engine.EnemyAttack();
        await RevealPendingLogAsync();
        RenderVitals();

        if (_player.IsDefeated)
        {
            await Delay(0.4);
            EndCombat(false);
            return;
        }

        _turnLocked = false;
        await Delay(0.4);
        await DrawHandAsync();
    }

    private void EndCombat(bool won)
    {
        _gameOver = true;
        RenderHand();
        _returnToHubButton.Visible = true;

        string message = won ? "Der Goblin fällt. Der Weg ist frei." : "Deine Kräfte verlassen dich...";
        _rollReadoutLabel.Text = message;
        _popupActionLabel.Text = won ? "Sieg" : "Niederlage";
        EnqueueLog(message, won ? LogTag.Good : LogTag.Bad, null);
        _ = RevealPendingLogAsync();
    }

    private void ReturnToHub()
    {
        GetTree().ChangeSceneToFile("res://scenes/Hub.tscn");
    }

    private void EnqueueLog(string message, LogTag tag, RollStamp? stamp)
    {
        _pendingLog.Enqueue(new PendingLogEntry(message, tag, stamp));
    }

    private async Task RevealPendingLogAsync()
    {
        if (_pendingLog.Count == 0)
        {
            return;
        }

        _popupPanel.Visible = true;

        while (_pendingLog.Count > 0)
        {
            var entry = _pendingLog.Dequeue();
            ApplyPopupContent(entry);
            await WaitForContinueAsync();
            CommitLogEntry(entry);
        }

        _popupPanel.Visible = false;
    }

    private void ApplyPopupContent(PendingLogEntry entry)
    {
        _popupMessageLabel.Text = $"[center]{FormatLogBbcode(entry.Message, entry.Tag)}[/center]";

        if (entry.Stamp is { } stamp)
        {
            _popupStampLabel.Visible = true;
            _popupStampLabel.Text = stamp.Success ? stamp.SuccessText : stamp.FailureText;
            _popupStampLabel.AddThemeColorOverride("font_color", stamp.Success ? HpGoodColor : HpBadColor);
        }
        else
        {
            _popupStampLabel.Visible = false;
        }
    }

    private async Task WaitForContinueAsync()
    {
        await ToSignal(_popupContinueButton, Button.SignalName.Pressed);
    }

    private void CommitLogEntry(PendingLogEntry entry)
    {
        _logLabel.AppendText(FormatLogBbcode(entry.Message, entry.Tag) + "\n");
    }

    private static string FormatLogBbcode(string message, LogTag tag) => tag switch
    {
        LogTag.Good => $"[color=#3d5a34]{message}[/color]",
        LogTag.Bad => $"[color=#9c3b3b]{message}[/color]",
        _ => $"[color=#5c5240][i]{message}[/i][/color]",
    };

    private async Task Delay(double seconds)
    {
        await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
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

            var cardView = _cardViewScene.Instantiate<CardView>();
            cardView.Disabled = _turnLocked || _gameOver;
            cardView.Clicked += () => PlayCard(capturedIndex);
            _handContainer.AddChild(cardView);
            cardView.Populate(card);
        }
    }
}
