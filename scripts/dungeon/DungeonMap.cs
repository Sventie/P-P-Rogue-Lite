namespace PPRogueLite.Dungeon;

using System.Collections.Generic;
using System.Linq;

/// <summary>Der komplette generierte Knoten-Graph eines Dungeons (Issue #10).</summary>
public sealed class DungeonMap
{
    public List<StageNode> Nodes { get; } = new();

    public StageNode GetNode(int id) => Nodes.First(node => node.Id == id);
}
