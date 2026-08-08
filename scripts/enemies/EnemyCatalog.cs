namespace PPRogueLite.Enemies;

using System.Collections.Generic;
using System.Linq;

public static class EnemyCatalog
{
    public static IReadOnlyList<EnemyDefinition> AllTypes { get; } = new List<EnemyDefinition>
    {
        new GoblinDefinition(),
        new TrollDefinition(),
        new ArcherDefinition(),
        new ShamanDefinition(),
        new RogueDefinition(),
    };

    /// <summary>Gegnertypen, die ab der angegebenen Dungeon-Stage im Spawn-Pool verfügbar sind (Issue #9: gestaffelte Einführung).</summary>
    public static IReadOnlyList<EnemyDefinition> AvailableForStage(int stage) =>
        AllTypes.Where(type => type.MinStage <= stage).ToList();
}
