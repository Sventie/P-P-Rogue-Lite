namespace PPRogueLite;

using Godot;
using PPRogueLite.Meta;

/// <summary>
/// Menü-Hub zwischen den Dungeon-Runs (die "Taverne", nur zwischen zwei
/// Dungeons erreichbar - Zwischenstopps innerhalb eines laufenden Dungeons
/// laufen über das Lager, siehe Camp.cs). "Dungeon betreten" startet einen
/// neuen Dungeon (DungeonRun.Start()) und wechselt in die Echtzeit-Arena
/// (scenes/Arena.tscn), "Karten managen" öffnet den Deck-Screen, "Shop"
/// öffnet den Shop (Issue #4, ersetzt den bisherigen "Mit Loot
/// entkommen"-Platzhalter). Der übrige Button ist reine UI-Struktur ohne
/// Funktion - siehe CLAUDE.md für die geplante Funktionalität.
/// </summary>
public partial class Hub : Control
{
    public override void _Ready()
    {
        var goldLabel = GetNode<Label>("MarginContainer/VBoxContainer/GoldLabel");
        goldLabel.Text = $"Gold: {PlayerWallet.Gold}";

        var enterDungeonButton = GetNode<Button>(
            "MarginContainer/VBoxContainer/MenuCenter/MenuPanel/MenuVBox/EnterDungeonButton");
        enterDungeonButton.Pressed += OnEnterDungeonPressed;

        var manageCardsButton = GetNode<Button>(
            "MarginContainer/VBoxContainer/MenuCenter/MenuPanel/MenuVBox/ManageCardsButton");
        manageCardsButton.Pressed += OnManageCardsPressed;

        var openShopButton = GetNode<Button>(
            "MarginContainer/VBoxContainer/MenuCenter/MenuPanel/MenuVBox/OpenShopButton");
        openShopButton.Pressed += OnOpenShopPressed;
    }

    private void OnEnterDungeonPressed()
    {
        DungeonRun.Start();
        GetTree().ChangeSceneToFile("res://scenes/Arena.tscn");
    }

    private void OnManageCardsPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/DeckScreen.tscn");
    }

    private void OnOpenShopPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/Shop.tscn");
    }
}
