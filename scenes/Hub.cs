namespace PPRogueLite;

using Godot;

/// <summary>
/// Menü-Hub zwischen den Dungeon-Runs. "Dungeon betreten" startet den
/// Testkampf (scenes/Main.tscn). Die übrigen Buttons sind reine UI-Struktur
/// ohne Funktion - siehe CLAUDE.md für die geplante Funktionalität.
/// </summary>
public partial class Hub : Control
{
    public override void _Ready()
    {
        var enterDungeonButton = GetNode<Button>(
            "MarginContainer/VBoxContainer/MenuCenter/MenuPanel/MenuVBox/EnterDungeonButton");
        enterDungeonButton.Pressed += OnEnterDungeonPressed;
    }

    private void OnEnterDungeonPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/Main.tscn");
    }
}
