namespace PPRogueLite;

using Godot;

/// <summary>
/// Menü-Hub zwischen den Dungeon-Runs. "Dungeon betreten" startet die
/// Echtzeit-Arena (scenes/Arena.tscn), "Karten managen" öffnet den
/// Deck-Screen. Die übrigen Buttons sind reine UI-Struktur ohne Funktion -
/// siehe CLAUDE.md für die geplante Funktionalität.
/// </summary>
public partial class Hub : Control
{
    public override void _Ready()
    {
        var enterDungeonButton = GetNode<Button>(
            "MarginContainer/VBoxContainer/MenuCenter/MenuPanel/MenuVBox/EnterDungeonButton");
        enterDungeonButton.Pressed += OnEnterDungeonPressed;

        var manageCardsButton = GetNode<Button>(
            "MarginContainer/VBoxContainer/MenuCenter/MenuPanel/MenuVBox/ManageCardsButton");
        manageCardsButton.Pressed += OnManageCardsPressed;
    }

    private void OnEnterDungeonPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/Arena.tscn");
    }

    private void OnManageCardsPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/DeckScreen.tscn");
    }
}
