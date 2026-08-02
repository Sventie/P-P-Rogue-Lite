namespace PPRogueLite;

using Godot;
using PPRogueLite.Cards;

/// <summary>
/// Echtzeit-Arena: löst den alten rundenbasierten Testkampf (Main.tscn) ab.
/// Spawnt fortlaufend Gegner, zeigt HP/Überlebenszeit, zeigt beim Level-up
/// kurz die neu gezogene Karte (Player.AbilityGained) und schickt bei
/// Niederlage zurück in den Hub.
/// </summary>
public partial class Arena : Node2D
{
    private const float SpawnMargin = 40f;
    private const double LevelUpDisplaySeconds = 2.2;

    private static readonly Color HpGoodColor = new(0.352941f, 0.478431f, 0.309804f);
    private static readonly Color HpBadColor = new(0.611765f, 0.231373f, 0.231373f);

    private PackedScene _enemyScene = null!;
    private PackedScene _cardViewScene = null!;
    private Player _player = null!;
    private Timer _spawnTimer = null!;
    private Label _hpLabel = null!;
    private ProgressBar _hpBar = null!;
    private Label _survivalLabel = null!;
    private Button _returnToHubButton = null!;
    private Control _levelUpLayer = null!;

    private double _survivalSeconds;
    private bool _gameOver;

    public override void _Ready()
    {
        _enemyScene = GD.Load<PackedScene>("res://scenes/EnemyGoblin.tscn");
        _cardViewScene = GD.Load<PackedScene>("res://scenes/CardView.tscn");

        _player = GetNode<Player>("Player");
        _player.AbilityGained += OnAbilityGained;

        _spawnTimer = GetNode<Timer>("EnemySpawnTimer");
        _spawnTimer.Timeout += SpawnEnemy;

        _hpLabel = GetNode<Label>("HUD/MarginContainer/VBoxContainer/HpLabel");
        _hpBar = GetNode<ProgressBar>("HUD/MarginContainer/VBoxContainer/HpBar");
        _survivalLabel = GetNode<Label>("HUD/MarginContainer/VBoxContainer/SurvivalLabel");
        _returnToHubButton = GetNode<Button>("HUD/MarginContainer/VBoxContainer/ReturnToHubButton");
        _returnToHubButton.Visible = false;
        _returnToHubButton.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/Hub.tscn");

        _levelUpLayer = GetNode<Control>("HUD/LevelUpLayer");
    }

    public override void _Process(double delta)
    {
        if (_gameOver)
        {
            return;
        }

        _survivalSeconds += delta;
        _survivalLabel.Text = $"Überlebt: {(int)_survivalSeconds}s";

        UpdateHpDisplay();

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

    private void SpawnEnemy()
    {
        if (_gameOver)
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

    private void OnAbilityGained(CardDefinition card)
    {
        var cardView = _cardViewScene.Instantiate<CardView>();
        _levelUpLayer.AddChild(cardView);
        cardView.Populate(card);

        var tween = CreateTween();
        tween.TweenInterval(LevelUpDisplaySeconds);
        tween.TweenCallback(Callable.From(cardView.QueueFree));
    }

    private void EndRun()
    {
        _gameOver = true;
        _spawnTimer.Stop();
        _player.SetDisabled(true);
        _returnToHubButton.Visible = true;
    }
}
