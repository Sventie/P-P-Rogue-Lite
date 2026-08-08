namespace PPRogueLite.Dungeon;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Erzeugt den verzweigten Dungeon-Pfad aus Issue #10: Spalte 1 (Start) und
/// die letzte Spalte (künftiger Boss, #11) sind je ein einzelner Knoten
/// ohne Modifikatoren ("First stage is always without modifiers"), die
/// Spalten dazwischen haben 3-4 Knoten mit zufälligen Wellenanzahl-/
/// Gold-Presets. Jeder Knoten bekommt 1-2 Verbindungen nach rechts;
/// anschließend wird sichergestellt, dass jeder Knoten der nächsten Spalte
/// mindestens eine eingehende Verbindung hat - das garantiert per
/// Induktion Erreichbarkeit von Spalte 1 aus (Spalte 1 hat nur einen
/// Knoten, ist also trivial erreichbar; jede weitere Spalte erbt die
/// Erreichbarkeit über ihre garantierte eingehende Verbindung).
/// </summary>
public static class DungeonMapGenerator
{
    private const int MinBranchCount = 3;
    private const int MaxBranchCount = 4;
    private const int MinOutgoing = 1;
    private const int MaxOutgoing = 2;

    // Wellenanzahl/Gold-Multiplikator als gekoppeltes Risk/Reward-Preset:
    // mehr Wellen (härter) gibt mehr Gold. Bewusst auf bestehende Werte
    // beschränkt statt neue Gegner-/Kartensysteme vorwegzunehmen.
    private static readonly (int WaveCount, double GoldMultiplier)[] Presets =
    {
        (7, 1.0),
        (10, 1.3),
        (13, 1.6),
    };

    private static readonly (int WaveCount, double GoldMultiplier) NoModifiers = (10, 1.0);

    public static DungeonMap Generate(int totalStages)
    {
        var rng = new Random();
        var map = new DungeonMap();
        var columns = new List<List<StageNode>>();
        int nextId = 1;

        for (int column = 1; column <= totalStages; column++)
        {
            bool isEndpoint = column == 1 || column == totalStages;
            int count = isEndpoint ? 1 : rng.Next(MinBranchCount, MaxBranchCount + 1);

            var nodesInColumn = new List<StageNode>();
            for (int i = 0; i < count; i++)
            {
                var (waveCount, goldMultiplier) = isEndpoint ? NoModifiers : Presets[rng.Next(Presets.Length)];
                var node = new StageNode
                {
                    Id = nextId++,
                    Column = column,
                    WaveCount = waveCount,
                    GoldMultiplier = goldMultiplier,
                };
                nodesInColumn.Add(node);
                map.Nodes.Add(node);
            }

            columns.Add(nodesInColumn);
        }

        for (int column = 0; column < columns.Count - 1; column++)
        {
            var current = columns[column];
            var next = columns[column + 1];

            foreach (var node in current)
            {
                int outgoing = Math.Min(next.Count, rng.Next(MinOutgoing, MaxOutgoing + 1));
                var targets = next.OrderBy(_ => rng.Next()).Take(outgoing);
                foreach (var target in targets)
                {
                    node.NextNodeIds.Add(target.Id);
                }
            }

            foreach (var target in next)
            {
                bool hasIncoming = current.Any(node => node.NextNodeIds.Contains(target.Id));
                if (!hasIncoming)
                {
                    var source = current[rng.Next(current.Count)];
                    source.NextNodeIds.Add(target.Id);
                }
            }
        }

        return map;
    }
}
