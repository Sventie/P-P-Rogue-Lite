namespace PPRogueLite;

using Godot;
using PPRogueLite.Meta;

/// <summary>
/// Menü-Hub zwischen den Dungeon-Runs (die "Taverne", nur zwischen zwei
/// Dungeons erreichbar - Zwischenstopps innerhalb eines laufenden Dungeons
/// laufen über das Lager, siehe Camp.cs). "Dungeon betreten" startet einen
/// neuen Dungeon (DungeonRun.Start()) und wechselt in die Echtzeit-Arena
/// (scenes/Arena.tscn), "Karten managen" öffnet den Deck-Screen, "Gruppe
/// managen" öffnet die Gruppenzusammenstellung (Issue #5, PartyScreen.tscn),
/// "Shop" öffnet den Shop (Issue #4, ersetzt den bisherigen "Mit Loot
/// entkommen"-Platzhalter).
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

        var managePartyButton = GetNode<Button>(
            "MarginContainer/VBoxContainer/MenuCenter/MenuPanel/MenuVBox/ManagePartyButton");
        managePartyButton.Pressed += OnManagePartyPressed;

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

    private void OnManagePartyPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/PartyScreen.tscn");
    }

    private void OnOpenShopPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/Shop.tscn");
    }
}
