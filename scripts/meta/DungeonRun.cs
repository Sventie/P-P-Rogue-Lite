namespace PPRogueLite.Meta;

using System.Collections.Generic;
using PPRogueLite.Cards;

/// <summary>
/// Zustand eines laufenden Dungeons, prozessweit über Szenenwechsel hinweg
/// (Arena ↔ Lager, gleiches static-Muster wie PlayerCardCollection/
/// PlayerWallet - Godot behält beim Szenenwechsel keinen Node-State).
///
/// CurrentStage == 0 bedeutet "kein Dungeon aktiv" (nur zwischen Dungeons,
/// wenn die Taverne/Hub gezeigt wird). HasProgress unterscheidet innerhalb
/// eines laufenden Dungeons zwischen Stage 1 (frischer Charakter) und
/// Folge-Stages (gespeicherter Fortschritt wird von Player.cs übernommen).
/// </summary>
public static class DungeonRun
{
    public const int TotalStages = 5;

    public static int CurrentStage { get; private set; }

    public static bool HasProgress { get; private set; }

    public static int SavedHp { get; private set; }

    public static int SavedXp { get; private set; }

    public static int SavedLevel { get; private set; }

    public static List<CardDefinition> SavedDrawPile { get; private set; } = new();

    public static List<CardDefinition> SavedEquippedCards { get; private set; } = new();

    public static void Start()
    {
        CurrentStage = 1;
        HasProgress = false;
    }

    public static void AdvanceStage()
    {
        CurrentStage++;
    }

    public static void SaveProgress(int hp, int xp, int level, List<CardDefinition> drawPile, List<CardDefinition> equippedCards)
    {
        SavedHp = hp;
        SavedXp = xp;
        SavedLevel = level;
        SavedDrawPile = drawPile;
        SavedEquippedCards = equippedCards;
        HasProgress = true;
    }

    public static void End()
    {
        CurrentStage = 0;
        HasProgress = false;
        SavedDrawPile = new List<CardDefinition>();
        SavedEquippedCards = new List<CardDefinition>();
    }
}
