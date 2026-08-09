namespace PPRogueLite.Meta;

using System.Collections.Generic;
using System.Linq;
using PPRogueLite.Cards;
using PPRogueLite.Dungeon;

/// <summary>
/// Zustand eines laufenden Dungeons, prozessweit über Szenenwechsel hinweg
/// (Arena ↔ Lager, gleiches static-Muster wie PlayerCardCollection/
/// PlayerWallet - Godot behält beim Szenenwechsel keinen Node-State).
///
/// CurrentStage == 0 bedeutet "kein Dungeon aktiv" (nur zwischen Dungeons,
/// wenn die Taverne/Hub gezeigt wird). HasProgress unterscheidet innerhalb
/// eines laufenden Dungeons zwischen Stage 1 (frischer Charakter) und
/// Folge-Stages (gespeicherter Fortschritt wird von Player.cs übernommen).
///
/// Map/CurrentNode sind der verzweigte Dungeon-Pfad aus Issue #10: Start()
/// erzeugt den kompletten Knoten-Graph einmalig, CurrentNodeId zeigt auf
/// die Stage, die gerade läuft bzw. gerade abgeschlossen wurde;
/// SelectNextNode wird vom Lager (Camp.cs) aufgerufen, wenn der Spieler
/// seine nächste Route wählt.
/// </summary>
public static class DungeonRun
{
    public const int TotalStages = 10;

    public static DungeonMap? Map { get; private set; }

    public static int CurrentStage { get; private set; }

    public static int CurrentNodeId { get; private set; }

    public static bool HasProgress { get; private set; }

    public static int SavedHp { get; private set; }

    public static int SavedXp { get; private set; }

    public static int SavedLevel { get; private set; }

    public static List<CardDefinition> SavedDrawPile { get; private set; } = new();

    public static List<CardDefinition> SavedDiscardPile { get; private set; } = new();

    public static List<CardDefinition> SavedEquippedCards { get; private set; } = new();

    public static StageNode CurrentNode => Map!.GetNode(CurrentNodeId);

    public static void Start()
    {
        Map = DungeonMapGenerator.Generate(TotalStages);
        CurrentStage = 1;
        CurrentNodeId = Map.Nodes.First(node => node.Column == 1).Id;
        HasProgress = false;
    }

    /// <summary>Die vom aktuellen Knoten aus erreichbaren nächsten Routen (Issue #10).</summary>
    public static IReadOnlyList<StageNode> GetNextChoices() =>
        CurrentNode.NextNodeIds.Select(id => Map!.GetNode(id)).ToList();

    /// <summary>Vom Lager aufgerufen, wenn der Spieler seine nächste Route gewählt hat.</summary>
    public static void SelectNextNode(int nodeId)
    {
        CurrentNodeId = nodeId;
        CurrentStage++;
    }

    public static void SaveProgress(int hp, int xp, int level, List<CardDefinition> drawPile, List<CardDefinition> discardPile, List<CardDefinition> equippedCards)
    {
        SavedHp = hp;
        SavedXp = xp;
        SavedLevel = level;
        SavedDrawPile = drawPile;
        SavedDiscardPile = discardPile;
        SavedEquippedCards = equippedCards;
        HasProgress = true;
    }

    public static void End()
    {
        Map = null;
        CurrentStage = 0;
        CurrentNodeId = 0;
        HasProgress = false;
        SavedDrawPile = new List<CardDefinition>();
        SavedDiscardPile = new List<CardDefinition>();
        SavedEquippedCards = new List<CardDefinition>();
    }
}
