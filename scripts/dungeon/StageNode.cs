namespace PPRogueLite.Dungeon;

using System.Collections.Generic;

/// <summary>
/// Ein Knoten im verzweigten Dungeon-Pfad (Issue #10): entspricht einer
/// Stage an einer bestimmten Spalte des Pfads. WaveCount/GoldMultiplier
/// sind die "Route hat unterschiedliche Modifikatoren/Belohnungen" aus dem
/// Issue - bewusst auf bereits existierende Werte beschränkt (Wellenanzahl,
/// Gold), da Gegner-Vielfalt (#9) und Kartenshop (#4) noch nicht existieren.
/// </summary>
public sealed class StageNode
{
    public int Id { get; init; }

    public int Column { get; init; }

    public int WaveCount { get; init; }

    public double GoldMultiplier { get; init; }

    /// <summary>Ids der Knoten in der nächsten Spalte, die von hier aus erreichbar sind.</summary>
    public List<int> NextNodeIds { get; } = new();
}
