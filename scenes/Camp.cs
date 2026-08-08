namespace PPRogueLite;

using Godot;
using PPRogueLite.Meta;

/// <summary>
/// Lager: Zwischenstopp zwischen zwei Stages eines laufenden Dungeons -
/// nicht zu verwechseln mit der Taverne/Hub, die nur zwischen zwei
/// Dungeons erreichbar ist. Bewusst kein "Karten managen" hier (siehe
/// Issue #3: Deck-Bearbeitung ist nur zwischen Dungeons erlaubt).
///
/// Zeigt die vom aktuellen Pfad-Knoten aus erreichbaren nächsten Routen
/// (DungeonRun.GetNextChoices(), Issue #10) als anklickbare Buttons mit
/// ihren Modifikatoren (Wellenanzahl, Gold-Multiplikator) - meist 1-2
/// Optionen. Auswahl ruft DungeonRun.SelectNextNode(id) auf (erhöht dabei
/// erst die Stage-Nummer) und wechselt in die Arena; der laufende
/// Charakter-Fortschritt (DungeonRun.SavedHp/-Xp/-Level/-Deck) wurde beim
/// Abschluss der vorherigen Stage bereits gesichert (Player.SaveProgress)
/// und wird dort automatisch übernommen. "Dungeon beenden" beendet den
/// Dungeon vorzeitig; bereits verdientes Gold bleibt erhalten, da es
/// schon nach jeder Stage direkt in PlayerWallet gutgeschrieben wird.
/// </summary>
public partial class Camp : Control
{
    public override void _Ready()
    {
        var subtitleLabel = GetNode<Label>("MarginContainer/VBoxContainer/SubtitleLabel");
        subtitleLabel.Text = $"Stage {DungeonRun.CurrentStage} von {DungeonRun.TotalStages} abgeschlossen";

        var goldLabel = GetNode<Label>("MarginContainer/VBoxContainer/GoldLabel");
        goldLabel.Text = $"Gold: {PlayerWallet.Gold}";

        var choiceContainer = GetNode<HBoxContainer>(
            "MarginContainer/VBoxContainer/MenuCenter/MenuPanel/MenuVBox/ChoiceContainer");

        int routeIndex = 0;
        foreach (var choice in DungeonRun.GetNextChoices())
        {
            char routeLetter = (char)('A' + routeIndex);
            routeIndex++;

            var button = new Button
            {
                CustomMinimumSize = new Vector2(200, 56),
                Text = $"Route {routeLetter}\n{choice.WaveCount} Wellen · Gold ×{choice.GoldMultiplier:0.0}",
            };
            button.Pressed += () => OnChoiceSelected(choice.Id);
            choiceContainer.AddChild(button);
        }

        var endDungeonButton = GetNode<Button>(
            "MarginContainer/VBoxContainer/MenuCenter/MenuPanel/MenuVBox/EndDungeonButton");
        endDungeonButton.Pressed += OnEndDungeonPressed;
    }

    private void OnChoiceSelected(int nodeId)
    {
        DungeonRun.SelectNextNode(nodeId);
        GetTree().ChangeSceneToFile("res://scenes/Arena.tscn");
    }

    private void OnEndDungeonPressed()
    {
        DungeonRun.End();
        GetTree().ChangeSceneToFile("res://scenes/Hub.tscn");
    }
}
