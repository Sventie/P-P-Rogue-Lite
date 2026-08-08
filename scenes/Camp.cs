namespace PPRogueLite;

using Godot;
using PPRogueLite.Meta;

/// <summary>
/// Lager: Zwischenstopp zwischen zwei Stages eines laufenden Dungeons -
/// nicht zu verwechseln mit der Taverne/Hub, die nur zwischen zwei
/// Dungeons erreichbar ist. Bewusst kein "Karten managen" hier (siehe
/// Issue #3: Deck-Bearbeitung ist nur zwischen Dungeons erlaubt).
///
/// "Nächste Stage betreten" wechselt zurück in die Arena - der laufende
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
        int finishedStage = DungeonRun.CurrentStage - 1;
        var subtitleLabel = GetNode<Label>("MarginContainer/VBoxContainer/SubtitleLabel");
        subtitleLabel.Text = $"Stage {finishedStage} von {DungeonRun.TotalStages} abgeschlossen";

        var goldLabel = GetNode<Label>("MarginContainer/VBoxContainer/GoldLabel");
        goldLabel.Text = $"Gold: {PlayerWallet.Gold}";

        var nextStageButton = GetNode<Button>(
            "MarginContainer/VBoxContainer/MenuCenter/MenuPanel/MenuVBox/NextStageButton");
        nextStageButton.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/Arena.tscn");

        var endDungeonButton = GetNode<Button>(
            "MarginContainer/VBoxContainer/MenuCenter/MenuPanel/MenuVBox/EndDungeonButton");
        endDungeonButton.Pressed += OnEndDungeonPressed;
    }

    private void OnEndDungeonPressed()
    {
        DungeonRun.End();
        GetTree().ChangeSceneToFile("res://scenes/Hub.tscn");
    }
}
