namespace PPRogueLite;

using System.Collections.Generic;
using Godot;
using PPRogueLite.Cards;

/// <summary>
/// Echtzeit-Arena: löst den alten rundenbasierten Testkampf (Main.tscn) ab.
/// Spawnt fortlaufend Gegner, zeigt HP/Überlebenszeit/Fähigkeiten-Leiste mit
/// Cooldown-Balken. Beim Level-up pausiert die Runde (Spieler/Gegner/Spawner
/// deaktiviert) und zeigt die neu gezogene Karte, bis der Spieler per
/// "Weiter"-Button bestätigt. Schickt bei Niederlage zurück in den Hub.
/// </summary>
public partial class Arena : Node2D
{
    private const float SpawnMargin = 40f;

    private static readonly Color HpGoodColor = new(0.352941f, 0.478431f, 0.309804f);
    private static readonly Color HpBadColor = new(0.611765f, 0.231373f, 0.231373f);
    private static readonly Color CooldownFillColor = new(0.690196f, 0.552941f, 0.239216f);

    private PackedScene _enemyScene = null!;
    private PackedScene _cardViewScene = null!;
    private Player _player = null!;
    private Timer _spawnTimer = null!;
    private Label _hpLabel = null!;
    private ProgressBar _hpBar = null!;
    private Label _survivalLabel = null!;
    private Button _returnToHubButton = null!;
    private Control _levelUpLayer = null!;
    private HBoxContainer _abilityBar = null!;

    private readonly Dictionary<string, ProgressBar> _cooldownBars = new();

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

        var label = new Label
        {
            Text = card.DisplayName,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

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
    }

    private async void OnAbilityGained(CardDefinition card)
    {
        SetWorldPaused(true);

        var wrapper = new VBoxContainer();
        wrapper.AddThemeConstantOverride("separation", 12);

        var cardView = _cardViewScene.Instantiate<CardView>();

        var continueButton = new Button
        {
            Text = "Weiter",
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        };

        wrapper.AddChild(cardView);
        wrapper.AddChild(continueButton);
        _levelUpLayer.AddChild(wrapper);
        cardView.Populate(card);

        await ToSignal(continueButton, Button.SignalName.Pressed);

        wrapper.QueueFree();
        SetWorldPaused(false);
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
